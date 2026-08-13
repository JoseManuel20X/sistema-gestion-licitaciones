using Licitaciones.Domain.Licitaciones;

namespace Licitaciones.UnitTests.Common;

/// <summary>
/// Constructor de licitaciones para pruebas, con valores por defecto razonables.
/// </summary>
/// <remarks>
/// Evita repetir en cada prueba los datos que no son relevantes para lo que se
/// verifica, de modo que el escenario probado quede a la vista.
/// </remarks>
internal static class ConstructorLicitacion
{
    public const decimal PresupuestoPorDefecto = 1_000_000m;

    /// <summary>Crea una licitación en estado Borrador con cierre a 30 días.</summary>
    public static Licitacion EnBorrador(
        RelojFalso reloj,
        decimal presupuestoCRC = PresupuestoPorDefecto,
        string codigo = "LIC-2026-001",
        TimeSpan? plazo = null) =>
        Licitacion.Crear(
            codigo,
            "Compra de equipo de cómputo",
            presupuestoCRC,
            reloj.AhoraUtc.Add(plazo ?? TimeSpan.FromDays(30)),
            reloj);

    /// <summary>Crea una licitación ya publicada.</summary>
    public static Licitacion Publicada(
        RelojFalso reloj,
        decimal presupuestoCRC = PresupuestoPorDefecto,
        string codigo = "LIC-2026-001",
        TimeSpan? plazo = null)
    {
        var licitacion = EnBorrador(reloj, presupuestoCRC, codigo, plazo);
        licitacion.Publicar(reloj);
        return licitacion;
    }
}
