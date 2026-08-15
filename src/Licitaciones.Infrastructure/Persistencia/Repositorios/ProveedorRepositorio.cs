using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Common;
using Licitaciones.Domain.Common;
using Licitaciones.Domain.Proveedores;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

internal sealed class ProveedorRepositorio : IProveedorRepositorio
{
    private readonly LicitacionesDbContext _contexto;

    public ProveedorRepositorio(LicitacionesDbContext contexto) => _contexto = contexto;

    public async Task<Proveedor?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        await _contexto.Proveedores
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancelacion);

    public async Task<bool> ExisteNombreAsync(
        string nombreNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        await _contexto.Proveedores
            .AnyAsync(
                p => p.NombreNormalizado == nombreNormalizado
                     && p.DeletedAt == null
                     && (idExcluido == null || p.Id != idExcluido),
                cancelacion);

    public async Task<PaginaResultado<Proveedor>> ListarAsync(
        ParametrosConsulta consulta,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        var query = _contexto.Proveedores.Where(p => p.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(consulta.Filtro))
        {
            // Se compara contra el nombre normalizado para que el filtro ignore
            // mayúsculas y espacios igual que la regla de unicidad.
            var filtro = Normalizador.NormalizarNombreProveedor(consulta.Filtro);
            query = query.Where(p => p.NombreNormalizado.Contains(filtro));
        }

        query = (consulta.OrdenarPor?.ToLowerInvariant(), consulta.Descendente) switch
        {
            ("createdat", false) => query.OrderBy(p => p.CreatedAt),
            ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
            (_, true) => query.OrderByDescending(p => p.NombreNormalizado),
            _ => query.OrderBy(p => p.NombreNormalizado),
        };

        var total = await query.CountAsync(cancelacion);
        var elementos = await query
            .Skip(consulta.Omitir)
            .Take(consulta.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<Proveedor>(elementos, consulta.Pagina, consulta.TamanoPagina, total);
    }

    public async Task<bool> TieneOfertasAsync(Guid proveedorId, CancellationToken cancelacion = default) =>
        await _contexto.Ofertas.AnyAsync(o => o.ProveedorId == proveedorId, cancelacion);

    public void Agregar(Proveedor proveedor) => _contexto.Proveedores.Add(proveedor);

    public void Eliminar(Proveedor proveedor) => _contexto.Proveedores.Remove(proveedor);
}
