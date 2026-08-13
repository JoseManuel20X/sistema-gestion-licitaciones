using Licitaciones.Domain.Common;

namespace Licitaciones.Domain.Licitaciones;

/// <summary>
/// Convocatoria para recibir ofertas económicas sobre un presupuesto estimado en colones.
/// </summary>
public sealed class Licitacion : EntidadAuditable
{
    // Constructor sin parámetros requerido por Entity Framework Core.
    private Licitacion()
    {
        Codigo = string.Empty;
        CodigoNormalizado = string.Empty;
        Titulo = string.Empty;
    }

    /// <summary>Código tal como lo escribió el usuario.</summary>
    public string Codigo { get; private set; }

    /// <summary>Código normalizado con índice único en PostgreSQL (enunciado §8.3).</summary>
    public string CodigoNormalizado { get; private set; }

    public string Titulo { get; private set; }

    /// <summary>Estado registrado. Puede diferir del efectivo si ya venció; ver <see cref="EstadoEfectivo"/>.</summary>
    public EstadoLicitacion Estado { get; private set; }

    /// <summary>Fecha y hora de cierre. Se almacena con desplazamiento y se compara en UTC.</summary>
    public DateTimeOffset FechaCierre { get; private set; }

    /// <summary>Presupuesto estimado en colones, con precisión <c>numeric(18,2)</c>.</summary>
    public decimal PresupuestoEstimadoCRC { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Token de concurrencia optimista respaldado por la columna <c>xmin</c> de PostgreSQL.</summary>
    public uint Version { get; private set; }

    public bool EstaEliminada => DeletedAt is not null;

    /// <summary>Crea una licitación en estado <see cref="EstadoLicitacion.Borrador"/>.</summary>
    public static Licitacion Crear(
        string codigo,
        string titulo,
        decimal presupuestoEstimadoCRC,
        DateTimeOffset fechaCierre,
        IReloj reloj)
    {
        var licitacion = new Licitacion();
        licitacion.AsignarCodigo(codigo);
        licitacion.AsignarTitulo(titulo);
        licitacion.AsignarPresupuesto(presupuestoEstimadoCRC, mayorOfertaRegistradaCRC: null);
        licitacion.FechaCierre = fechaCierre;
        licitacion.Estado = EstadoLicitacion.Borrador;
        licitacion.MarcarCreacion(reloj);
        return licitacion;
    }

    /// <summary>
    /// Actualiza los datos editables de la licitación.
    /// </summary>
    /// <param name="mayorOfertaRegistradaCRC">
    /// Monto de la mayor oferta ya registrada, o <c>null</c> si no hay ofertas. Se
    /// recibe como parámetro explícito en lugar de leerlo de una colección de
    /// navegación: así la regla no depende de que el ORM haya cargado los datos
    /// relacionados, cosa que quien llama puede olvidar sin obtener ningún aviso.
    /// </param>
    public void ActualizarDatos(
        string codigo,
        string titulo,
        decimal presupuestoEstimadoCRC,
        DateTimeOffset fechaCierre,
        decimal? mayorOfertaRegistradaCRC,
        IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        ExcepcionDominio.SiCumple(
            EstadoEfectivo(reloj) == EstadoLicitacion.Cerrada,
            CodigosError.LicitacionCerradaNoModificable,
            "Una licitación cerrada no puede modificarse.");

        AsignarCodigo(codigo);
        AsignarTitulo(titulo);
        AsignarPresupuesto(presupuestoEstimadoCRC, mayorOfertaRegistradaCRC);
        FechaCierre = fechaCierre;
        MarcarActualizacion(reloj);
    }

    /// <summary>
    /// Publica la licitación. Solo procede desde <see cref="EstadoLicitacion.Borrador"/>
    /// con datos completos, presupuesto válido y fecha de cierre futura.
    /// </summary>
    public void Publicar(IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        ExcepcionDominio.SiCumple(
            Estado != EstadoLicitacion.Borrador,
            CodigosError.TransicionEstadoInvalida,
            $"No se puede publicar una licitación en estado {Estado}.");

        ExcepcionDominio.SiCumple(
            string.IsNullOrWhiteSpace(Codigo) || string.IsNullOrWhiteSpace(Titulo),
            CodigosError.TituloLicitacionVacio,
            "La licitación debe tener código y título para publicarse.");

        ExcepcionDominio.SiCumple(
            PresupuestoEstimadoCRC <= 0m,
            CodigosError.PresupuestoNoPositivo,
            "El presupuesto debe ser mayor que cero para publicar.");

        ExcepcionDominio.SiCumple(
            FechaCierre <= reloj.AhoraUtc,
            CodigosError.FechaCierreNoFutura,
            "La fecha de cierre debe ser futura para publicar la licitación.");

        Estado = EstadoLicitacion.Publicada;
        MarcarActualizacion(reloj);
    }

    /// <summary>
    /// Cierra la licitación. Procede desde <see cref="EstadoLicitacion.Borrador"/>
    /// como cancelación documentada y desde <see cref="EstadoLicitacion.Publicada"/>
    /// por acción autorizada o por haberse alcanzado la fecha de cierre.
    /// </summary>
    public void Cerrar(IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        ExcepcionDominio.SiCumple(
            Estado == EstadoLicitacion.Cerrada,
            CodigosError.TransicionEstadoInvalida,
            "La licitación ya está cerrada.");

        Estado = EstadoLicitacion.Cerrada;
        MarcarActualizacion(reloj);
    }

    /// <summary>Indica si ya se alcanzó la fecha y hora de cierre.</summary>
    public bool EstaVencida(IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        return reloj.AhoraUtc >= FechaCierre;
    }

    /// <summary>
    /// Estado real considerando el vencimiento: una licitación publicada cuya fecha
    /// de cierre ya se alcanzó se considera cerrada aunque el campo persistido
    /// todavía indique <see cref="EstadoLicitacion.Publicada"/> (enunciado §8.1).
    /// </summary>
    public EstadoLicitacion EstadoEfectivo(IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        if (Estado == EstadoLicitacion.Publicada && EstaVencida(reloj))
        {
            return EstadoLicitacion.Cerrada;
        }

        return Estado;
    }

    /// <summary>Indica si la licitación admite registrar, editar o eliminar ofertas.</summary>
    public bool AceptaOfertas(IReloj reloj) => EstadoEfectivo(reloj) == EstadoLicitacion.Publicada;

    /// <summary>Verifica que la licitación admita cambios en sus ofertas y explica el motivo si no.</summary>
    /// <exception cref="ExcepcionDominio">Si no está publicada o si ya venció.</exception>
    public void GarantizarQueAceptaOfertas(IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        ExcepcionDominio.SiCumple(
            Estado != EstadoLicitacion.Publicada,
            CodigosError.OfertaLicitacionNoPublicada,
            $"Solo se admiten ofertas en licitaciones publicadas; esta está en estado {Estado}.");

        ExcepcionDominio.SiCumple(
            EstaVencida(reloj),
            CodigosError.OfertaLicitacionVencida,
            "La licitación alcanzó su fecha de cierre y ya no admite cambios en las ofertas.");
    }

    /// <summary>Aplica el borrado lógico de la licitación.</summary>
    public void Eliminar(IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        if (EstaEliminada)
        {
            return;
        }

        DeletedAt = reloj.AhoraUtc;
        MarcarActualizacion(reloj);
    }

    private void AsignarCodigo(string codigo)
    {
        ExcepcionDominio.SiCumple(
            string.IsNullOrWhiteSpace(codigo),
            CodigosError.CodigoLicitacionVacio,
            "El código de la licitación es obligatorio.");

        Codigo = codigo.Trim();
        CodigoNormalizado = Normalizador.NormalizarCodigoLicitacion(codigo);
    }

    private void AsignarTitulo(string titulo)
    {
        ExcepcionDominio.SiCumple(
            string.IsNullOrWhiteSpace(titulo),
            CodigosError.TituloLicitacionVacio,
            "El título de la licitación es obligatorio.");

        Titulo = Normalizador.LimpiarEspacios(titulo);
    }

    private void AsignarPresupuesto(decimal presupuesto, decimal? mayorOfertaRegistradaCRC)
    {
        ExcepcionDominio.SiCumple(
            presupuesto <= 0m,
            CodigosError.PresupuestoNoPositivo,
            "El presupuesto estimado debe ser mayor que cero.");

        ExcepcionDominio.SiCumple(
            mayorOfertaRegistradaCRC is { } mayor && presupuesto < mayor,
            CodigosError.PresupuestoMenorQueOferta,
            "El presupuesto no puede quedar por debajo de una oferta ya registrada.");

        PresupuestoEstimadoCRC = Dinero.Redondear(presupuesto);
    }
}
