using Licitaciones.Domain.Common;

namespace Licitaciones.UnitTests.Common;

/// <summary>
/// Reloj controlado por la prueba.
/// </summary>
/// <remarks>
/// Permite verificar el vencimiento de una licitación sin esperas reales: la
/// prueba fija el instante y lo adelanta cuando lo necesita. Sin esta
/// abstracción las pruebas de vencimiento serían lentas e intermitentes.
/// </remarks>
public sealed class RelojFalso : IReloj
{
    public RelojFalso(DateTimeOffset instanteInicial) => AhoraUtc = instanteInicial;

    /// <summary>Instante de referencia usado por defecto en las pruebas.</summary>
    public static DateTimeOffset InstanteBase { get; } =
        new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    public DateTimeOffset AhoraUtc { get; private set; }

    /// <summary>Crea un reloj situado en <see cref="InstanteBase"/>.</summary>
    public static RelojFalso EnInstanteBase() => new(InstanteBase);

    /// <summary>Adelanta el reloj.</summary>
    public void Avanzar(TimeSpan intervalo) => AhoraUtc = AhoraUtc.Add(intervalo);

    /// <summary>Sitúa el reloj en un instante concreto.</summary>
    public void Situar(DateTimeOffset instante) => AhoraUtc = instante;
}
