using Licitaciones.Domain.Common;

namespace Licitaciones.Domain.Aprobaciones;

/// <summary>
/// Rango de montos en colones que determina quién debe aprobar una adjudicación.
/// </summary>
/// <remarks>
/// El aprobador se obtiene de esta tabla parametrizable y nunca de una cadena fija
/// de condiciones <c>if/else</c>: así se puede cambiar la política de aprobación
/// sin recompilar (enunciado §8.7).
/// </remarks>
public sealed class NivelAprobacion : EntidadAuditable
{
    // Constructor sin parámetros requerido por Entity Framework Core.
    private NivelAprobacion() => Aprobador = string.Empty;

    /// <summary>Monto mínimo del rango, inclusive.</summary>
    public decimal MontoMinimoCRC { get; private set; }

    /// <summary>Monto máximo del rango, inclusive. <c>null</c> indica un rango sin límite superior.</summary>
    public decimal? MontoMaximoCRC { get; private set; }

    /// <summary>Cargo o instancia responsable de aprobar montos dentro del rango.</summary>
    public string Aprobador { get; private set; }

    /// <summary>Indica si el rango no tiene límite superior.</summary>
    public bool EsRangoAbierto => MontoMaximoCRC is null;

    /// <summary>Crea un nivel de aprobación validando el rango y el aprobador.</summary>
    public static NivelAprobacion Crear(decimal montoMinimoCRC, decimal? montoMaximoCRC, string aprobador, IReloj reloj)
    {
        var nivel = new NivelAprobacion();
        nivel.AsignarDatos(montoMinimoCRC, montoMaximoCRC, aprobador);
        nivel.MarcarCreacion(reloj);
        return nivel;
    }

    /// <summary>Actualiza el rango y el aprobador.</summary>
    public void Actualizar(decimal montoMinimoCRC, decimal? montoMaximoCRC, string aprobador, IReloj reloj)
    {
        AsignarDatos(montoMinimoCRC, montoMaximoCRC, aprobador);
        MarcarActualizacion(reloj);
    }

    /// <summary>Indica si un monto cae dentro del rango, con ambos extremos inclusive.</summary>
    public bool Contiene(decimal montoCRC) =>
        montoCRC >= MontoMinimoCRC && (MontoMaximoCRC is null || montoCRC <= MontoMaximoCRC);

    /// <summary>Indica si este rango se traslapa con otro.</summary>
    public bool SeTraslapaCon(NivelAprobacion otro)
    {
        ArgumentNullException.ThrowIfNull(otro);

        var finPropio = MontoMaximoCRC ?? decimal.MaxValue;
        var finOtro = otro.MontoMaximoCRC ?? decimal.MaxValue;

        return MontoMinimoCRC <= finOtro && otro.MontoMinimoCRC <= finPropio;
    }

    private void AsignarDatos(decimal montoMinimoCRC, decimal? montoMaximoCRC, string aprobador)
    {
        ExcepcionDominio.SiCumple(
            montoMinimoCRC <= 0m,
            CodigosError.RangoAprobacionInvalido,
            "El monto mínimo del rango debe ser mayor que cero.");

        ExcepcionDominio.SiCumple(
            montoMaximoCRC is { } maximo && maximo <= montoMinimoCRC,
            CodigosError.RangoAprobacionInvalido,
            "El monto máximo debe ser mayor que el monto mínimo.");

        ExcepcionDominio.SiCumple(
            string.IsNullOrWhiteSpace(aprobador),
            CodigosError.AprobadorVacio,
            "El aprobador es obligatorio.");

        MontoMinimoCRC = Dinero.Redondear(montoMinimoCRC);
        MontoMaximoCRC = montoMaximoCRC is { } m ? Dinero.Redondear(m) : null;
        Aprobador = Normalizador.LimpiarEspacios(aprobador);
    }
}
