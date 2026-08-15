using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Common;
using Licitaciones.Domain.Ofertas;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

internal sealed class OfertaRepositorio : IOfertaRepositorio
{
    private readonly LicitacionesDbContext _contexto;

    public OfertaRepositorio(LicitacionesDbContext contexto) => _contexto = contexto;

    public async Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        await _contexto.Ofertas
            .Include(o => o.Proveedor)
            .FirstOrDefaultAsync(o => o.Id == id, cancelacion);

    public async Task<bool> ExisteOfertaDelProveedorAsync(
        Guid licitacionId,
        Guid proveedorId,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        await _contexto.Ofertas
            .AnyAsync(
                o => o.LicitacionId == licitacionId
                     && o.ProveedorId == proveedorId
                     && (idExcluido == null || o.Id != idExcluido),
                cancelacion);

    public async Task<IReadOnlyList<Oferta>> ListarPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default) =>
        await _contexto.Ofertas
            .Include(o => o.Proveedor)
            .Where(o => o.LicitacionId == licitacionId)
            .OrderBy(o => o.MontoOfertadoCRC)
            .ThenBy(o => o.FechaRegistro)
            .ToListAsync(cancelacion);

    public async Task<PaginaResultado<Oferta>> ListarAsync(
        ParametrosConsulta consulta,
        Guid? licitacionId = null,
        Guid? proveedorId = null,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        var query = _contexto.Ofertas.Include(o => o.Proveedor).AsQueryable();

        if (licitacionId is { } licitacion)
        {
            query = query.Where(o => o.LicitacionId == licitacion);
        }

        if (proveedorId is { } proveedor)
        {
            query = query.Where(o => o.ProveedorId == proveedor);
        }

        query = (consulta.OrdenarPor?.ToLowerInvariant(), consulta.Descendente) switch
        {
            ("fecharegistro", false) => query.OrderBy(o => o.FechaRegistro),
            ("fecharegistro", true) => query.OrderByDescending(o => o.FechaRegistro),
            (_, true) => query.OrderByDescending(o => o.MontoOfertadoCRC),
            _ => query.OrderBy(o => o.MontoOfertadoCRC).ThenBy(o => o.FechaRegistro),
        };

        var total = await query.CountAsync(cancelacion);
        var elementos = await query
            .Skip(consulta.Omitir)
            .Take(consulta.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<Oferta>(elementos, consulta.Pagina, consulta.TamanoPagina, total);
    }

    public void Agregar(Oferta oferta) => _contexto.Ofertas.Add(oferta);

    public void Eliminar(Oferta oferta) => _contexto.Ofertas.Remove(oferta);
}
