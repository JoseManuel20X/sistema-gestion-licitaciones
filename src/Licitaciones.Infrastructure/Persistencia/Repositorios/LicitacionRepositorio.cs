using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Common;
using Licitaciones.Domain.Common;
using Licitaciones.Domain.Licitaciones;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

internal sealed class LicitacionRepositorio : ILicitacionRepositorio
{
    private readonly LicitacionesDbContext _contexto;

    public LicitacionRepositorio(LicitacionesDbContext contexto) => _contexto = contexto;

    public async Task<Licitacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        await _contexto.Licitaciones
            .FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt == null, cancelacion);

    public async Task<bool> ExisteCodigoAsync(
        string codigoNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        await _contexto.Licitaciones
            .AnyAsync(
                l => l.CodigoNormalizado == codigoNormalizado
                     && l.DeletedAt == null
                     && (idExcluido == null || l.Id != idExcluido),
                cancelacion);

    public async Task<PaginaResultado<Licitacion>> ListarAsync(
        ParametrosConsulta consulta,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        var query = _contexto.Licitaciones.Where(l => l.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(consulta.Filtro))
        {
            var codigo = Normalizador.NormalizarCodigoLicitacion(consulta.Filtro);

            // El título se busca con ILIKE, la comparación sin distinción de
            // mayúsculas nativa de PostgreSQL: se traduce a SQL y evita traer la
            // tabla a memoria para compararla en el cliente.
            var patronTitulo = $"%{EscaparPatron(consulta.Filtro.Trim())}%";

            query = query.Where(l =>
                l.CodigoNormalizado.Contains(codigo) || EF.Functions.ILike(l.Titulo, patronTitulo));
        }

        query = (consulta.OrdenarPor?.ToLowerInvariant(), consulta.Descendente) switch
        {
            ("codigo", false) => query.OrderBy(l => l.CodigoNormalizado),
            ("codigo", true) => query.OrderByDescending(l => l.CodigoNormalizado),
            ("presupuesto", false) => query.OrderBy(l => l.PresupuestoEstimadoCRC),
            ("presupuesto", true) => query.OrderByDescending(l => l.PresupuestoEstimadoCRC),
            ("fechacierre", true) => query.OrderByDescending(l => l.FechaCierre),
            (_, true) => query.OrderByDescending(l => l.CreatedAt),
            ("fechacierre", false) => query.OrderBy(l => l.FechaCierre),
            _ => query.OrderBy(l => l.FechaCierre),
        };

        var total = await query.CountAsync(cancelacion);
        var elementos = await query
            .Skip(consulta.Omitir)
            .Take(consulta.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<Licitacion>(elementos, consulta.Pagina, consulta.TamanoPagina, total);
    }

    public async Task<decimal?> ObtenerMayorOfertaAsync(Guid licitacionId, CancellationToken cancelacion = default) =>
        await _contexto.Ofertas
            .Where(o => o.LicitacionId == licitacionId)
            .MaxAsync(o => (decimal?)o.MontoOfertadoCRC, cancelacion);

    public async Task<bool> TieneOfertasAsync(Guid licitacionId, CancellationToken cancelacion = default) =>
        await _contexto.Ofertas.AnyAsync(o => o.LicitacionId == licitacionId, cancelacion);

    public void Agregar(Licitacion licitacion) => _contexto.Licitaciones.Add(licitacion);

    public void Eliminar(Licitacion licitacion) => _contexto.Licitaciones.Remove(licitacion);

    /// <summary>
    /// Neutraliza los comodines de LIKE en el texto que escribió la persona
    /// usuaria, para que un «%» se busque como carácter literal y no coincida con
    /// toda la tabla.
    /// </summary>
    private static string EscaparPatron(string valor) =>
        valor.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("%", "\\%", StringComparison.Ordinal)
             .Replace("_", "\\_", StringComparison.Ordinal);
}
