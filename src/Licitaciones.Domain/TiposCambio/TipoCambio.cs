using Licitaciones.Domain.Common;

namespace Licitaciones.Domain.TiposCambio;

/// <summary>
/// Tipo de cambio administrable de colones por dólar.
/// </summary>
/// <remarks>
/// El colón es la única moneda persistida; la vista en dólares es una
/// representación calculada que nunca altera los montos almacenados
/// (enunciado §8.8). El valor se administra localmente para que el sistema
/// funcione sin acceso a Internet.
/// </remarks>
public sealed class TipoCambio : EntidadAuditable
{
    // Constructor sin parámetros requerido por Entity Framework Core.
    private TipoCambio()
    {
    }

    /// <summary>Colones equivalentes a un dólar estadounidense.</summary>
    public decimal CRCporUSD { get; private set; }

    /// <summary>Fecha desde la que rige el tipo de cambio.</summary>
    public DateTimeOffset FechaVigencia { get; private set; }

    /// <summary>Indica si es el tipo de cambio usado en la operación ordinaria.</summary>
    public bool Activo { get; private set; }

    /// <summary>Crea un tipo de cambio.</summary>
    public static TipoCambio Crear(decimal crcPorUsd, DateTimeOffset fechaVigencia, bool activo, IReloj reloj)
    {
        var tipoCambio = new TipoCambio { FechaVigencia = fechaVigencia, Activo = activo };
        tipoCambio.AsignarValor(crcPorUsd);
        tipoCambio.MarcarCreacion(reloj);
        return tipoCambio;
    }

    /// <summary>Actualiza el valor y la fecha de vigencia.</summary>
    public void Actualizar(decimal crcPorUsd, DateTimeOffset fechaVigencia, IReloj reloj)
    {
        AsignarValor(crcPorUsd);
        FechaVigencia = fechaVigencia;
        MarcarActualizacion(reloj);
    }

    /// <summary>Marca este tipo de cambio como el activo.</summary>
    public void Activar(IReloj reloj)
    {
        Activo = true;
        MarcarActualizacion(reloj);
    }

    /// <summary>Deja de ser el tipo de cambio activo.</summary>
    public void Desactivar(IReloj reloj)
    {
        Activo = false;
        MarcarActualizacion(reloj);
    }

    /// <summary>Convierte un monto en colones a dólares usando este tipo de cambio.</summary>
    /// <remarks>El resultado se redondea a dos decimales solo para presentación.</remarks>
    public decimal ConvertirCrcAUsd(decimal montoCRC) =>
        Dinero.Redondear(montoCRC / CRCporUSD);

    private void AsignarValor(decimal crcPorUsd)
    {
        ExcepcionDominio.SiCumple(
            crcPorUsd <= 0m,
            CodigosError.TipoCambioNoPositivo,
            "El tipo de cambio debe ser mayor que cero.");

        CRCporUSD = Dinero.Redondear(crcPorUsd, 4);
    }
}
