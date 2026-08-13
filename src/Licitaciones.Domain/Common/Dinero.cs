namespace Licitaciones.Domain.Common;

/// <summary>
/// Política única de redondeo monetario del sistema.
/// </summary>
/// <remarks>
/// Todos los montos se guardan como <c>numeric(18,2)</c>; nunca se usa
/// <c>float</c> ni <c>double</c>, que no representan de forma exacta valores
/// decimales y acumulan error en sumas de dinero (enunciado §7).
/// Se redondea con <see cref="MidpointRounding.AwayFromZero"/>, el criterio
/// comercial habitual: 0,005 sube a 0,01. Centralizarlo en un solo lugar evita
/// que dos capas redondeen distinto y produzcan diferencias de un céntimo.
/// </remarks>
public static class Dinero
{
    /// <summary>Decimales con que se almacenan los montos.</summary>
    public const int Decimales = 2;

    /// <summary>Redondea un monto a la precisión monetaria del sistema.</summary>
    public static decimal Redondear(decimal monto) =>
        decimal.Round(monto, Decimales, MidpointRounding.AwayFromZero);

    /// <summary>Redondea un valor con precisión arbitraria, para tipos de cambio.</summary>
    public static decimal Redondear(decimal valor, int decimales) =>
        decimal.Round(valor, decimales, MidpointRounding.AwayFromZero);
}
