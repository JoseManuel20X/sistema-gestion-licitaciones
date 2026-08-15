using Licitaciones.Application;
using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Aprobaciones;
using Licitaciones.Application.Common;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Licitaciones.Domain.Common;
using Licitaciones.UnitTests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Comprueba que un fallo de la base de datos nunca escapa como excepción sin
/// tratar: los casos de uso lo traducen a un resultado con código y tipo.
/// </summary>
/// <remarks>
/// Estos fallos ocurren cuando dos peticiones simultáneas ganan la carrera contra
/// la comprobación previa. La restricción de PostgreSQL es la última defensa y su
/// error debe llegar al cliente como 409, no como 500.
/// </remarks>
public sealed class FallosDePersistenciaTests
{
    private readonly AlmacenFalso _almacen = new();
    private readonly RelojFalso _reloj = RelojFalso.EnInstanteBase();
    private readonly UnidadDeTrabajoFalsa _unidadDeTrabajo;

    public FallosDePersistenciaTests() => _unidadDeTrabajo = new UnidadDeTrabajoFalsa(_almacen);

    private ProveedorServicio Proveedores => new(
        new ProveedorRepositorioFalso(_almacen),
        _unidadDeTrabajo,
        _reloj);

    private LicitacionServicio Licitaciones => new(
        new LicitacionRepositorioFalso(_almacen),
        new OfertaRepositorioFalso(_almacen),
        new NivelAprobacionRepositorioFalso(_almacen),
        _unidadDeTrabajo,
        _reloj);

    private OfertaServicio Ofertas => new(
        new OfertaRepositorioFalso(_almacen),
        new LicitacionRepositorioFalso(_almacen),
        new ProveedorRepositorioFalso(_almacen),
        _unidadDeTrabajo,
        _reloj);

    private NivelAprobacionServicio Niveles => new(
        new NivelAprobacionRepositorioFalso(_almacen),
        _unidadDeTrabajo,
        _reloj);

    [Fact]
    public async Task CrearProveedor_ConIndiceUnicoViolado_DevuelveConflictoNoExcepcion()
    {
        _unidadDeTrabajo.FalloAlGuardar = new ExcepcionConflictoPersistencia(
            CodigosError.NombreProveedorDuplicado,
            "Ya existe un proveedor con ese nombre.");

        var resultado = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa Central"));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.NombreProveedorDuplicado, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Conflicto, resultado.Error.Tipo);
    }

    [Fact]
    public async Task ActualizarProveedor_ConEdicionSimultanea_DevuelveConflictoDeConcurrencia()
    {
        var creado = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa Central"));
        _unidadDeTrabajo.FalloAlGuardar = new ExcepcionConcurrencia();

        var resultado = await Proveedores.ActualizarAsync(
            creado.Valor!.Id,
            new ProveedorEntrada("Empresa Central Sur"));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.ConflictoConcurrencia, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Concurrencia, resultado.Error.Tipo);
    }

    [Fact]
    public async Task EliminarProveedor_ConEdicionSimultanea_DevuelveConflictoDeConcurrencia()
    {
        var creado = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa Central"));
        _unidadDeTrabajo.FalloAlGuardar = new ExcepcionConcurrencia();

        var resultado = await Proveedores.EliminarAsync(creado.Valor!.Id);

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.Concurrencia, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task CrearLicitacion_ConCodigoDuplicadoEnLaBase_DevuelveConflicto()
    {
        _unidadDeTrabajo.FalloAlGuardar = new ExcepcionConflictoPersistencia(
            CodigosError.CodigoLicitacionDuplicado,
            "Ya existe una licitación con ese código.");

        var resultado = await Licitaciones.CrearAsync(new LicitacionEntrada(
            "LIC-2026-001",
            "Compra de equipo",
            1_000_000m,
            _reloj.AhoraUtc.AddDays(30)));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.CodigoLicitacionDuplicado, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Conflicto, resultado.Error.Tipo);
    }

    [Fact]
    public async Task CambiarEstado_ConEdicionSimultanea_DevuelveConflictoDeConcurrencia()
    {
        var creada = await Licitaciones.CrearAsync(new LicitacionEntrada(
            "LIC-2026-001",
            "Compra de equipo",
            1_000_000m,
            _reloj.AhoraUtc.AddDays(30)));

        _unidadDeTrabajo.FalloAlGuardar = new ExcepcionConcurrencia();

        var resultado = await Licitaciones.CambiarEstadoAsync(
            creada.Valor!.Id,
            TransicionLicitacion.Publicar);

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.Concurrencia, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task RegistrarOferta_ConIndiceCompuestoViolado_DevuelveOfertaDuplicada()
    {
        var proveedor = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa Central"));
        var creada = await Licitaciones.CrearAsync(new LicitacionEntrada(
            "LIC-2026-001",
            "Compra de equipo",
            1_000_000m,
            _reloj.AhoraUtc.AddDays(30)));
        await Licitaciones.CambiarEstadoAsync(creada.Valor!.Id, TransicionLicitacion.Publicar);

        _unidadDeTrabajo.FalloAlGuardar = new ExcepcionConflictoPersistencia(
            CodigosError.OfertaDuplicada,
            "El proveedor ya registró una oferta en esta licitación.");

        var resultado = await Ofertas.RegistrarAsync(
            creada.Valor.Id,
            new OfertaEntrada(proveedor.Valor!.Id, 900_000m));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.OfertaDuplicada, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ActualizarNivel_ConEdicionSimultanea_DevuelveConflictoDeConcurrencia()
    {
        var creado = await Niveles.CrearAsync(
            new NivelAprobacionEntrada(0.01m, 999_999.99m, "Encargado de área"));

        _unidadDeTrabajo.FalloAlGuardar = new ExcepcionConcurrencia();

        var resultado = await Niveles.ActualizarAsync(
            creado.Valor!.Id,
            new NivelAprobacionEntrada(0.01m, 500_000m, "Encargado de área"));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.Concurrencia, resultado.Error!.Tipo);
    }

    [Fact]
    public void ContenedorDeDependencias_ResuelveTodosLosCasosDeUso()
    {
        var servicios = new ServiceCollection();

        // Se registran dobles de los puertos: la comprobación es que el
        // registro de la capa de aplicación declara sus dependencias completas.
        servicios.AddSingleton(_almacen);
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajoFalsa>();
        servicios.AddScoped<IProveedorRepositorio, ProveedorRepositorioFalso>();
        servicios.AddScoped<ILicitacionRepositorio, LicitacionRepositorioFalso>();
        servicios.AddScoped<IOfertaRepositorio, OfertaRepositorioFalso>();
        servicios.AddScoped<INivelAprobacionRepositorio, NivelAprobacionRepositorioFalso>();
        servicios.AddSingleton<IReloj>(_reloj);

        servicios.AgregarAplicacion();

        using var proveedor = servicios.BuildServiceProvider(validateScopes: true);
        using var ambito = proveedor.CreateScope();

        Assert.NotNull(ambito.ServiceProvider.GetRequiredService<ProveedorServicio>());
        Assert.NotNull(ambito.ServiceProvider.GetRequiredService<LicitacionServicio>());
        Assert.NotNull(ambito.ServiceProvider.GetRequiredService<OfertaServicio>());
        Assert.NotNull(ambito.ServiceProvider.GetRequiredService<NivelAprobacionServicio>());
    }
}
