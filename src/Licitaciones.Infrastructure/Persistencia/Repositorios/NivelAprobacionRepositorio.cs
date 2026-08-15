using Licitaciones.Application.Abstracciones;
using Licitaciones.Domain.Aprobaciones;
using Licitaciones.Domain.TiposCambio;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

internal sealed class NivelAprobacionRepositorio : INivelAprobacionRepositorio
{
    private readonly LicitacionesDbContext _contexto;

    public NivelAprobacionRepositorio(LicitacionesDbContext contexto) => _contexto = contexto;

    public async Task<NivelAprobacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        await _contexto.NivelesAprobacion.FirstOrDefaultAsync(n => n.Id == id, cancelacion);

    public async Task<IReadOnlyList<NivelAprobacion>> ListarTodosAsync(CancellationToken cancelacion = default) =>
        await _contexto.NivelesAprobacion
            .OrderBy(n => n.MontoMinimoCRC)
            .ToListAsync(cancelacion);

    public void Agregar(NivelAprobacion nivel) => _contexto.NivelesAprobacion.Add(nivel);

    public void Eliminar(NivelAprobacion nivel) => _contexto.NivelesAprobacion.Remove(nivel);
}

internal sealed class TipoCambioRepositorio : ITipoCambioRepositorio
{
    private readonly LicitacionesDbContext _contexto;

    public TipoCambioRepositorio(LicitacionesDbContext contexto) => _contexto = contexto;

    public async Task<TipoCambio?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        await _contexto.TiposCambio.FirstOrDefaultAsync(t => t.Id == id, cancelacion);

    public async Task<TipoCambio?> ObtenerActivoAsync(CancellationToken cancelacion = default) =>
        await _contexto.TiposCambio.FirstOrDefaultAsync(t => t.Activo, cancelacion);

    public async Task<IReadOnlyList<TipoCambio>> ListarTodosAsync(CancellationToken cancelacion = default) =>
        await _contexto.TiposCambio
            .OrderByDescending(t => t.FechaVigencia)
            .ToListAsync(cancelacion);

    public void Agregar(TipoCambio tipoCambio) => _contexto.TiposCambio.Add(tipoCambio);

    public void Eliminar(TipoCambio tipoCambio) => _contexto.TiposCambio.Remove(tipoCambio);
}
