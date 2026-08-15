namespace Licitaciones.Domain.Ofertas;

/// <summary>
/// Resultado de evaluar las ofertas de una licitación: cuál es la mejor, cuánto
/// se ahorra frente al presupuesto y cómo se clasifica ese ahorro.
/// </summary>
/// <param name="MejorOferta">Oferta ganadora, o <c>null</c> si no hay ofertas.</param>
/// <param name="PorcentajeAhorro">Ahorro porcentual sobre el presupuesto, o <c>null</c> si no hay ofertas.</param>
/// <param name="Clasificacion">Clasificación textual del ahorro.</param>
public sealed record EvaluacionOfertas(
    Oferta? MejorOferta,
    decimal? PorcentajeAhorro,
    ClasificacionAhorro Clasificacion)
{
    /// <summary>Evaluación de una licitación sin ofertas válidas.</summary>
    public static EvaluacionOfertas SinOfertas { get; } =
        new(null, null, ClasificacionAhorro.SinOfertasValidas);
}

/// <summary>
/// Determina la mejor oferta y su clasificación (enunciado §8.6).
/// </summary>
/// <remarks>
/// Es una función pura sobre datos ya cargados: no consulta la base de datos, de
/// modo que puede probarse por completo con pruebas unitarias.
/// </remarks>
public static class EvaluadorOfertas
{
    /// <summary>Umbral de ahorro a partir del cual la oferta se considera conveniente.</summary>
    public const decimal UmbralOfertaConveniente = 10m;

    /// <summary>
    /// Evalúa las ofertas de una licitación.
    /// </summary>
    /// <param name="ofertas">Ofertas válidas de la licitación.</param>
    /// <param name="presupuestoEstimadoCRC">Presupuesto contra el que se calcula el ahorro.</param>
    public static EvaluacionOfertas Evaluar(IEnumerable<Oferta> ofertas, decimal presupuestoEstimadoCRC)
    {
        ArgumentNullException.ThrowIfNull(ofertas);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(presupuestoEstimadoCRC);

        // Menor monto; en empate gana la registrada primero. El Id desempata el
        // caso extremo de dos ofertas con idéntica marca temporal, para que el
        // resultado sea siempre determinista.
        var mejor = ofertas
            .OrderBy(o => o.MontoOfertadoCRC)
            .ThenBy(o => o.FechaRegistro)
            .ThenBy(o => o.Id)
            .FirstOrDefault();

        if (mejor is null)
        {
            return EvaluacionOfertas.SinOfertas;
        }

        var ahorro = ((presupuestoEstimadoCRC - mejor.MontoOfertadoCRC) / presupuestoEstimadoCRC) * 100m;

        var clasificacion = ahorro switch
        {
            >= UmbralOfertaConveniente => ClasificacionAhorro.OfertaConveniente,
            > 0m => ClasificacionAhorro.OfertaAceptable,
            _ => ClasificacionAhorro.OfertaValidaSinAhorro,
        };

        return new EvaluacionOfertas(mejor, ahorro, clasificacion);
    }
}
