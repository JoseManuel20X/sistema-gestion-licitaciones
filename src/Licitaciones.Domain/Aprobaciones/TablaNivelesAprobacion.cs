using Licitaciones.Domain.Common;

namespace Licitaciones.Domain.Aprobaciones;

/// <summary>
/// Reglas que gobiernan el conjunto de niveles de aprobación tomado en bloque
/// (enunciado §8.7).
/// </summary>
public static class TablaNivelesAprobacion
{
    /// <summary>
    /// Resuelve el aprobador que corresponde a un monto consultando la tabla.
    /// </summary>
    /// <returns>El nivel aplicable, o <c>null</c> si el monto cae fuera de todos los rangos.</returns>
    public static NivelAprobacion? ResolverNivel(IEnumerable<NivelAprobacion> niveles, decimal montoCRC)
    {
        ArgumentNullException.ThrowIfNull(niveles);

        return niveles.FirstOrDefault(nivel => nivel.Contiene(montoCRC));
    }

    /// <summary>
    /// Verifica que los rangos no se traslapen y que exista a lo sumo un rango
    /// abierto.
    /// </summary>
    /// <param name="niveles">Conjunto completo de niveles tal como quedaría tras el cambio.</param>
    /// <exception cref="ExcepcionDominio">Si hay traslape o más de un rango abierto.</exception>
    public static void GarantizarConsistencia(IEnumerable<NivelAprobacion> niveles)
    {
        ArgumentNullException.ThrowIfNull(niveles);

        var ordenados = niveles.OrderBy(n => n.MontoMinimoCRC).ToList();

        ExcepcionDominio.SiCumple(
            ordenados.Count(n => n.EsRangoAbierto) > 1,
            CodigosError.RangoAbiertoDuplicado,
            "Solo puede existir un rango sin monto máximo.");

        for (var i = 0; i < ordenados.Count - 1; i++)
        {
            ExcepcionDominio.SiCumple(
                ordenados[i].SeTraslapaCon(ordenados[i + 1]),
                CodigosError.RangoAprobacionTraslapado,
                $"El rango que inicia en {ordenados[i].MontoMinimoCRC} se traslapa con el que inicia en {ordenados[i + 1].MontoMinimoCRC}.");
        }
    }
}
