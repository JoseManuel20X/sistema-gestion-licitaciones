using Licitaciones.Application.Common;
using Licitaciones.Domain.Aprobaciones;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;
using Licitaciones.Domain.TiposCambio;

namespace Licitaciones.Application.Abstracciones;

/// <summary>
/// Confirma los cambios pendientes como una sola transacción.
/// </summary>
/// <remarks>
/// Los repositorios solo registran intenciones; nada se escribe hasta que se
/// confirma aquí. Así una operación que toca varias entidades se guarda de
/// forma atómica (enunciado §11).
/// </remarks>
public interface IUnidadDeTrabajo
{
    /// <summary>Persiste los cambios pendientes.</summary>
    /// <exception cref="ExcepcionConcurrencia">Si otro proceso modificó un registro afectado.</exception>
    /// <exception cref="ExcepcionConflictoPersistencia">Si se infringe una restricción de integridad.</exception>
    Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default);

    /// <summary>Ejecuta una operación dentro de una transacción explícita.</summary>
    Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancelacion = default);
}

/// <summary>Acceso a proveedores.</summary>
public interface IProveedorRepositorio
{
    Task<Proveedor?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Indica si ya existe un proveedor vigente con el mismo nombre normalizado.</summary>
    /// <param name="idExcluido">Proveedor que debe ignorarse, útil al editar.</param>
    Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? idExcluido = null, CancellationToken cancelacion = default);

    Task<PaginaResultado<Proveedor>> ListarAsync(ParametrosConsulta consulta, CancellationToken cancelacion = default);

    /// <summary>Indica si el proveedor tiene al menos una oferta registrada.</summary>
    Task<bool> TieneOfertasAsync(Guid proveedorId, CancellationToken cancelacion = default);

    void Agregar(Proveedor proveedor);

    /// <summary>Elimina físicamente. Solo procede cuando no hay ofertas relacionadas.</summary>
    void Eliminar(Proveedor proveedor);
}

/// <summary>Acceso a licitaciones.</summary>
public interface ILicitacionRepositorio
{
    Task<Licitacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    Task<bool> ExisteCodigoAsync(string codigoNormalizado, Guid? idExcluido = null, CancellationToken cancelacion = default);

    Task<PaginaResultado<Licitacion>> ListarAsync(ParametrosConsulta consulta, CancellationToken cancelacion = default);

    /// <summary>Monto de la mayor oferta registrada, o <c>null</c> si no hay ofertas.</summary>
    Task<decimal?> ObtenerMayorOfertaAsync(Guid licitacionId, CancellationToken cancelacion = default);

    Task<bool> TieneOfertasAsync(Guid licitacionId, CancellationToken cancelacion = default);

    void Agregar(Licitacion licitacion);

    void Eliminar(Licitacion licitacion);
}

/// <summary>Acceso a ofertas.</summary>
public interface IOfertaRepositorio
{
    Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Indica si el proveedor ya ofertó en la licitación (enunciado §8.3).</summary>
    Task<bool> ExisteOfertaDelProveedorAsync(
        Guid licitacionId,
        Guid proveedorId,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default);

    Task<IReadOnlyList<Oferta>> ListarPorLicitacionAsync(Guid licitacionId, CancellationToken cancelacion = default);

    Task<PaginaResultado<Oferta>> ListarAsync(
        ParametrosConsulta consulta,
        Guid? licitacionId = null,
        Guid? proveedorId = null,
        CancellationToken cancelacion = default);

    void Agregar(Oferta oferta);

    void Eliminar(Oferta oferta);
}

/// <summary>Acceso a los niveles de aprobación.</summary>
public interface INivelAprobacionRepositorio
{
    Task<NivelAprobacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Devuelve todos los niveles ordenados por monto mínimo.</summary>
    Task<IReadOnlyList<NivelAprobacion>> ListarTodosAsync(CancellationToken cancelacion = default);

    void Agregar(NivelAprobacion nivel);

    void Eliminar(NivelAprobacion nivel);
}

/// <summary>Acceso a los tipos de cambio.</summary>
public interface ITipoCambioRepositorio
{
    Task<TipoCambio?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Devuelve el tipo de cambio activo, o <c>null</c> si no hay ninguno.</summary>
    Task<TipoCambio?> ObtenerActivoAsync(CancellationToken cancelacion = default);

    Task<IReadOnlyList<TipoCambio>> ListarTodosAsync(CancellationToken cancelacion = default);

    void Agregar(TipoCambio tipoCambio);

    void Eliminar(TipoCambio tipoCambio);
}
