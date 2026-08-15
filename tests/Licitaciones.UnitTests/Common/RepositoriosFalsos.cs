using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Common;
using Licitaciones.Domain.Aprobaciones;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;

namespace Licitaciones.UnitTests.Common;

/// <summary>
/// Repositorios en memoria para probar los casos de uso.
/// </summary>
/// <remarks>
/// Verifican la orquestación de la capa de aplicación —comprobar duplicados,
/// resolver el aprobador, traducir errores— sin arrancar una base de datos. Las
/// restricciones que dependen del motor se prueban aparte contra PostgreSQL real
/// en <c>Licitaciones.IntegrationTests</c>.
/// </remarks>
internal sealed class AlmacenFalso
{
    public List<Proveedor> Proveedores { get; } = [];

    public List<Licitacion> Licitaciones { get; } = [];

    public List<Oferta> Ofertas { get; } = [];

    public List<NivelAprobacion> Niveles { get; } = [];

    /// <summary>Cantidad de confirmaciones solicitadas, para verificar que el caso de uso guarda.</summary>
    public int Confirmaciones { get; set; }
}

internal sealed class UnidadDeTrabajoFalsa : IUnidadDeTrabajo
{
    private readonly AlmacenFalso _almacen;

    public UnidadDeTrabajoFalsa(AlmacenFalso almacen) => _almacen = almacen;

    /// <summary>
    /// Excepción que se lanzará al confirmar, para simular que PostgreSQL rechazó
    /// la escritura por concurrencia o por una restricción de unicidad. Permite
    /// probar que el caso de uso traduce esos fallos a un resultado controlado.
    /// </summary>
    public Exception? FalloAlGuardar { get; set; }

    public Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default)
    {
        if (FalloAlGuardar is { } fallo)
        {
            throw fallo;
        }

        _almacen.Confirmaciones++;
        return Task.FromResult(1);
    }

    public Task<T> EnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default) => operacion(cancelacion);
}

internal sealed class ProveedorRepositorioFalso : IProveedorRepositorio
{
    private readonly AlmacenFalso _almacen;

    public ProveedorRepositorioFalso(AlmacenFalso almacen) => _almacen = almacen;

    public Task<Proveedor?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Proveedores.FirstOrDefault(p => p.Id == id && !p.EstaEliminado));

    public Task<bool> ExisteNombreAsync(
        string nombreNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Proveedores.Any(p =>
            p.NombreNormalizado == nombreNormalizado && !p.EstaEliminado && p.Id != idExcluido));

    public Task<PaginaResultado<Proveedor>> ListarAsync(
        ParametrosConsulta consulta,
        CancellationToken cancelacion = default)
    {
        var vigentes = _almacen.Proveedores.Where(p => !p.EstaEliminado).ToList();
        var pagina = vigentes.Skip(consulta.Omitir).Take(consulta.TamanoPagina).ToList();

        return Task.FromResult(
            new PaginaResultado<Proveedor>(pagina, consulta.Pagina, consulta.TamanoPagina, vigentes.Count));
    }

    public Task<bool> TieneOfertasAsync(Guid proveedorId, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Ofertas.Any(o => o.ProveedorId == proveedorId));

    public void Agregar(Proveedor proveedor) => _almacen.Proveedores.Add(proveedor);

    public void Eliminar(Proveedor proveedor) => _almacen.Proveedores.Remove(proveedor);
}

internal sealed class LicitacionRepositorioFalso : ILicitacionRepositorio
{
    private readonly AlmacenFalso _almacen;

    public LicitacionRepositorioFalso(AlmacenFalso almacen) => _almacen = almacen;

    public Task<Licitacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Licitaciones.FirstOrDefault(l => l.Id == id && !l.EstaEliminada));

    public Task<bool> ExisteCodigoAsync(
        string codigoNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Licitaciones.Any(l =>
            l.CodigoNormalizado == codigoNormalizado && !l.EstaEliminada && l.Id != idExcluido));

    public Task<PaginaResultado<Licitacion>> ListarAsync(
        ParametrosConsulta consulta,
        CancellationToken cancelacion = default)
    {
        var vigentes = _almacen.Licitaciones.Where(l => !l.EstaEliminada).ToList();
        var pagina = vigentes.Skip(consulta.Omitir).Take(consulta.TamanoPagina).ToList();

        return Task.FromResult(
            new PaginaResultado<Licitacion>(pagina, consulta.Pagina, consulta.TamanoPagina, vigentes.Count));
    }

    public Task<decimal?> ObtenerMayorOfertaAsync(Guid licitacionId, CancellationToken cancelacion = default)
    {
        var ofertas = _almacen.Ofertas.Where(o => o.LicitacionId == licitacionId).ToList();

        return Task.FromResult(ofertas.Count == 0 ? null : (decimal?)ofertas.Max(o => o.MontoOfertadoCRC));
    }

    public Task<bool> TieneOfertasAsync(Guid licitacionId, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Ofertas.Any(o => o.LicitacionId == licitacionId));

    public void Agregar(Licitacion licitacion) => _almacen.Licitaciones.Add(licitacion);

    public void Eliminar(Licitacion licitacion) => _almacen.Licitaciones.Remove(licitacion);
}

internal sealed class OfertaRepositorioFalso : IOfertaRepositorio
{
    private readonly AlmacenFalso _almacen;

    public OfertaRepositorioFalso(AlmacenFalso almacen) => _almacen = almacen;

    public Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Ofertas.FirstOrDefault(o => o.Id == id));

    public Task<bool> ExisteOfertaDelProveedorAsync(
        Guid licitacionId,
        Guid proveedorId,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Ofertas.Any(o =>
            o.LicitacionId == licitacionId && o.ProveedorId == proveedorId && o.Id != idExcluido));

    public Task<IReadOnlyList<Oferta>> ListarPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Oferta>>(
            [.. _almacen.Ofertas.Where(o => o.LicitacionId == licitacionId)]);

    public Task<PaginaResultado<Oferta>> ListarAsync(
        ParametrosConsulta consulta,
        Guid? licitacionId = null,
        Guid? proveedorId = null,
        CancellationToken cancelacion = default)
    {
        var filtradas = _almacen.Ofertas
            .Where(o => licitacionId is null || o.LicitacionId == licitacionId)
            .Where(o => proveedorId is null || o.ProveedorId == proveedorId)
            .ToList();

        var pagina = filtradas.Skip(consulta.Omitir).Take(consulta.TamanoPagina).ToList();

        return Task.FromResult(
            new PaginaResultado<Oferta>(pagina, consulta.Pagina, consulta.TamanoPagina, filtradas.Count));
    }

    public void Agregar(Oferta oferta) => _almacen.Ofertas.Add(oferta);

    public void Eliminar(Oferta oferta) => _almacen.Ofertas.Remove(oferta);
}

internal sealed class NivelAprobacionRepositorioFalso : INivelAprobacionRepositorio
{
    private readonly AlmacenFalso _almacen;

    public NivelAprobacionRepositorioFalso(AlmacenFalso almacen) => _almacen = almacen;

    public Task<NivelAprobacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_almacen.Niveles.FirstOrDefault(n => n.Id == id));

    public Task<IReadOnlyList<NivelAprobacion>> ListarTodosAsync(CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<NivelAprobacion>>([.. _almacen.Niveles.OrderBy(n => n.MontoMinimoCRC)]);

    public void Agregar(NivelAprobacion nivel) => _almacen.Niveles.Add(nivel);

    public void Eliminar(NivelAprobacion nivel) => _almacen.Niveles.Remove(nivel);
}
