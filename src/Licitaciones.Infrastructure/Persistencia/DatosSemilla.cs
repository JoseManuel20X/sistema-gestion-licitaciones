using Licitaciones.Domain.Aprobaciones;
using Licitaciones.Domain.Common;
using Licitaciones.Domain.TiposCambio;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Datos iniciales exigidos por el enunciado §11: niveles de aprobación y un
/// tipo de cambio activo.
/// </summary>
/// <remarks>
/// Se siembra con código en lugar de <c>HasData</c> porque las entidades protegen
/// sus invariantes con constructores privados y fábricas: sembrar por código las
/// hace pasar por las mismas validaciones que cualquier alta, en vez de escribir
/// filas directamente. La operación es idempotente, así que puede ejecutarse en
/// cada arranque sin duplicar datos.
/// </remarks>
public static class DatosSemilla
{
    /// <summary>
    /// Tabla de aprobación de referencia del enunciado §8.7.
    /// </summary>
    public static IReadOnlyList<(decimal Minimo, decimal? Maximo, string Aprobador)> NivelesDeReferencia { get; } =
    [
        (0.01m, 999_999.99m, "Encargado de área"),
        (1_000_000m, 9_999_999.99m, "Gerencia"),
        (10_000_000m, null, "Junta Directiva"),
    ];

    /// <summary>
    /// Tipo de cambio inicial. Es un valor administrable: la persona usuaria lo
    /// actualiza desde la aplicación, que funciona sin acceso a Internet.
    /// </summary>
    public const decimal TipoCambioInicialCrcPorUsd = 520m;

    /// <summary>Inserta los datos iniciales que falten.</summary>
    public static async Task SembrarAsync(
        LicitacionesDbContext contexto,
        IReloj reloj,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(reloj);

        var huboCambios = false;

        if (!await contexto.NivelesAprobacion.AnyAsync(cancelacion))
        {
            foreach (var (minimo, maximo, aprobador) in NivelesDeReferencia)
            {
                contexto.NivelesAprobacion.Add(NivelAprobacion.Crear(minimo, maximo, aprobador, reloj));
            }

            huboCambios = true;
        }

        if (!await contexto.TiposCambio.AnyAsync(cancelacion))
        {
            contexto.TiposCambio.Add(
                TipoCambio.Crear(TipoCambioInicialCrcPorUsd, reloj.AhoraUtc, activo: true, reloj));

            huboCambios = true;
        }

        if (huboCambios)
        {
            await contexto.SaveChangesAsync(cancelacion);
        }
    }
}
