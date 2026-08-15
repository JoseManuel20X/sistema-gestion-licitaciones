using Licitaciones.Domain.Common;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Proveedores;

namespace Licitaciones.Domain.Ofertas;

/// <summary>
/// Propuesta económica de un proveedor para una licitación.
/// </summary>
/// <remarks>
/// A diferencia del resto de entidades no hereda de <see cref="EntidadAuditable"/>:
/// el enunciado §7 define <see cref="FechaRegistro"/> como su sello de creación, y
/// añadir además un <c>CreatedAt</c> duplicaría el mismo dato en dos columnas.
/// </remarks>
public sealed class Oferta
{
    // Constructor sin parámetros requerido por Entity Framework Core.
    private Oferta()
    {
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid LicitacionId { get; private set; }

    public Guid ProveedorId { get; private set; }

    /// <summary>Monto ofertado en colones, con precisión <c>numeric(18,2)</c>.</summary>
    public decimal MontoOfertadoCRC { get; private set; }

    /// <summary>Instante de registro en UTC. Define el desempate de la mejor oferta.</summary>
    public DateTimeOffset FechaRegistro { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Token de concurrencia optimista respaldado por la columna <c>xmin</c> de PostgreSQL.</summary>
    public uint Version { get; private set; }

    /// <summary>Licitación a la que pertenece la oferta.</summary>
    public Licitacion? Licitacion { get; private set; }

    /// <summary>Proveedor que presentó la oferta.</summary>
    public Proveedor? Proveedor { get; private set; }

    /// <summary>
    /// Registra una oferta validando el estado y el vencimiento de la licitación y
    /// el monto contra el presupuesto.
    /// </summary>
    /// <remarks>
    /// La unicidad de un proveedor por licitación no se comprueba aquí porque
    /// requiere consultar las ofertas existentes: se valida en la capa de
    /// aplicación y la respalda un índice único compuesto en PostgreSQL.
    /// </remarks>
    public static Oferta Registrar(Licitacion licitacion, Guid proveedorId, decimal montoOfertadoCRC, IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(licitacion);
        ArgumentNullException.ThrowIfNull(reloj);

        licitacion.GarantizarQueAceptaOfertas(reloj);

        var oferta = new Oferta
        {
            LicitacionId = licitacion.Id,
            ProveedorId = proveedorId,
            FechaRegistro = reloj.AhoraUtc,
        };

        oferta.AsignarMonto(montoOfertadoCRC, licitacion.PresupuestoEstimadoCRC);
        oferta.UpdatedAt = oferta.FechaRegistro;
        return oferta;
    }

    /// <summary>Modifica el monto ofertado mientras la licitación siga admitiendo cambios.</summary>
    public void CambiarMonto(decimal montoOfertadoCRC, Licitacion licitacion, IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(licitacion);
        ArgumentNullException.ThrowIfNull(reloj);

        licitacion.GarantizarQueAceptaOfertas(reloj);

        AsignarMonto(montoOfertadoCRC, licitacion.PresupuestoEstimadoCRC);
        UpdatedAt = reloj.AhoraUtc;
    }

    private void AsignarMonto(decimal monto, decimal presupuestoCRC)
    {
        ExcepcionDominio.SiCumple(
            monto <= 0m,
            CodigosError.MontoOfertaNoPositivo,
            "El monto ofertado debe ser mayor que cero.");

        var redondeado = Dinero.Redondear(monto);

        // Una oferta igual al presupuesto es válida; solo se rechaza la que lo supera.
        ExcepcionDominio.SiCumple(
            redondeado > presupuestoCRC,
            CodigosError.OfertaSuperaPresupuesto,
            "El monto ofertado no puede superar el presupuesto estimado de la licitación.");

        MontoOfertadoCRC = redondeado;
    }
}
