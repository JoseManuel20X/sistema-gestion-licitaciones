namespace Licitaciones.Domain.Common;

/// <summary>
/// Abstracción del reloj del sistema.
/// </summary>
/// <remarks>
/// El vencimiento de una licitación depende de la hora actual. Inyectar el reloj
/// permite que las pruebas fijen el instante y verifiquen el vencimiento de forma
/// determinista, sin esperas reales ni pruebas intermitentes (enunciado §8.2).
/// </remarks>
public interface IReloj
{
    /// <summary>Instante actual en UTC.</summary>
    DateTimeOffset AhoraUtc { get; }
}
