using Licitaciones.Application.Aprobaciones;
using Licitaciones.Application.Common;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Licitaciones.Domain.Common;
using Licitaciones.UnitTests.Common;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Reglas que solo pueden comprobarse orquestando varias entidades: unicidad
/// entre registros, duplicidad de ofertas y resolución del aprobador.
/// </summary>
public sealed class CasosDeUsoTests
{
    private readonly AlmacenFalso _almacen = new();
    private readonly RelojFalso _reloj = RelojFalso.EnInstanteBase();

    private ProveedorServicio Proveedores => new(
        new ProveedorRepositorioFalso(_almacen),
        new UnidadDeTrabajoFalsa(_almacen),
        _reloj);

    private LicitacionServicio Licitaciones => new(
        new LicitacionRepositorioFalso(_almacen),
        new OfertaRepositorioFalso(_almacen),
        new NivelAprobacionRepositorioFalso(_almacen),
        new UnidadDeTrabajoFalsa(_almacen),
        _reloj);

    private OfertaServicio Ofertas => new(
        new OfertaRepositorioFalso(_almacen),
        new LicitacionRepositorioFalso(_almacen),
        new ProveedorRepositorioFalso(_almacen),
        new UnidadDeTrabajoFalsa(_almacen),
        _reloj);

    private NivelAprobacionServicio Niveles => new(
        new NivelAprobacionRepositorioFalso(_almacen),
        new UnidadDeTrabajoFalsa(_almacen),
        _reloj);

    // --- Unicidad de proveedor ---

    [Theory]
    [InlineData("empresa central")]
    [InlineData("  EMPRESA   CENTRAL  ")]
    public async Task CrearProveedor_ConNombreEquivalenteAUnoExistente_DevuelveConflicto(string nombre)
    {
        await Proveedores.CrearAsync(new ProveedorEntrada("Empresa Central"));

        var resultado = await Proveedores.CrearAsync(new ProveedorEntrada(nombre));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.NombreProveedorDuplicado, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Conflicto, resultado.Error.Tipo);
    }

    [Fact]
    public async Task CrearProveedor_ConNombreDistinto_EsExitoso()
    {
        await Proveedores.CrearAsync(new ProveedorEntrada("Empresa Central"));

        var resultado = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa del Norte"));

        Assert.True(resultado.EsExitoso);
        Assert.Equal(2, _almacen.Proveedores.Count);
    }

    [Fact]
    public async Task CrearProveedor_ConCaracterProhibido_DevuelveErrorDeValidacion()
    {
        var resultado = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa @ Central"));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.Validacion, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task ActualizarProveedor_NoChocaConsigoMismo()
    {
        var creado = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa Central"));

        var resultado = await Proveedores.ActualizarAsync(
            creado.Valor!.Id,
            new ProveedorEntrada("EMPRESA CENTRAL"));

        Assert.True(resultado.EsExitoso);
    }

    [Fact]
    public async Task EliminarProveedor_ConOfertas_AplicaBorradoLogico()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));

        var resultado = await Proveedores.EliminarAsync(proveedorId);

        Assert.True(resultado.EsExitoso);
        // El registro sigue existiendo, marcado como eliminado.
        Assert.Single(_almacen.Proveedores);
        Assert.True(_almacen.Proveedores[0].EstaEliminado);
    }

    [Fact]
    public async Task EliminarProveedor_SinOfertas_LoBorraFisicamente()
    {
        var creado = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa Central"));

        var resultado = await Proveedores.EliminarAsync(creado.Valor!.Id);

        Assert.True(resultado.EsExitoso);
        Assert.Empty(_almacen.Proveedores);
    }

    // --- Unicidad de código de licitación ---

    [Theory]
    [InlineData("lic-2026-001")]
    [InlineData("  LIC-2026-001  ")]
    public async Task CrearLicitacion_ConCodigoEquivalente_DevuelveConflicto(string codigo)
    {
        await CrearLicitacionAsync("LIC-2026-001");

        var resultado = await Licitaciones.CrearAsync(new LicitacionEntrada(
            codigo,
            "Otra compra",
            500_000m,
            _reloj.AhoraUtc.AddDays(10)));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.CodigoLicitacionDuplicado, resultado.Error!.Codigo);
    }

    // --- Ofertas ---

    [Fact]
    public async Task RegistrarOferta_DuplicadaDelMismoProveedor_DevuelveConflicto()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));

        var resultado = await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 800_000m));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.OfertaDuplicada, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Conflicto, resultado.Error.Tipo);
        Assert.Single(_almacen.Ofertas);
    }

    [Fact]
    public async Task RegistrarOferta_DeOtroProveedorEnLaMismaLicitacion_EsExitoso()
    {
        var (licitacionId, primerProveedor) = await PrepararLicitacionPublicadaConProveedorAsync();
        var segundo = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa del Norte"));

        await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(primerProveedor, 900_000m));
        var resultado = await Ofertas.RegistrarAsync(
            licitacionId,
            new OfertaEntrada(segundo.Valor!.Id, 850_000m));

        Assert.True(resultado.EsExitoso);
        Assert.Equal(2, _almacen.Ofertas.Count);
    }

    [Fact]
    public async Task RegistrarOferta_SuperiorAlPresupuesto_DevuelveReglaDeNegocio()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();

        var resultado = await Ofertas.RegistrarAsync(
            licitacionId,
            new OfertaEntrada(proveedorId, 1_500_000m));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.OfertaSuperaPresupuesto, resultado.Error!.Codigo);
        Assert.Equal(TipoError.ReglaNegocio, resultado.Error.Tipo);
    }

    [Fact]
    public async Task RegistrarOferta_DespuesDelCierre_DevuelveReglaDeNegocio()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        _reloj.Avanzar(TimeSpan.FromDays(60));

        var resultado = await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.OfertaLicitacionVencida, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task RegistrarOferta_SobreProveedorInexistente_DevuelveNoEncontrado()
    {
        var licitacionId = await CrearLicitacionPublicadaAsync();

        var resultado = await Ofertas.RegistrarAsync(
            licitacionId,
            new OfertaEntrada(Guid.CreateVersion7(), 900_000m));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    // --- Mejor oferta y aprobador ---

    [Fact]
    public async Task ObtenerMejorOferta_DevuelveClasificacionYAprobadorDeLaTabla()
    {
        await SembrarNivelesAsync();
        var (licitacionId, primerProveedor) = await PrepararLicitacionPublicadaConProveedorAsync();
        var segundo = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa del Norte"));

        await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(primerProveedor, 950_000m));
        await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(segundo.Valor!.Id, 850_000m));

        var resultado = await Licitaciones.ObtenerMejorOfertaAsync(licitacionId);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(850_000m, resultado.Valor!.MontoMejorOfertaCRC);
        Assert.Equal("Oferta conveniente", resultado.Valor.Clasificacion);
        // 850 000 cae en el primer rango de la tabla de referencia.
        Assert.Equal("Encargado de área", resultado.Valor.Aprobador);
    }

    [Fact]
    public async Task ObtenerMejorOferta_SinOfertas_DevuelveSinOfertasValidasYSinAprobador()
    {
        await SembrarNivelesAsync();
        var licitacionId = await CrearLicitacionPublicadaAsync();

        var resultado = await Licitaciones.ObtenerMejorOfertaAsync(licitacionId);

        Assert.True(resultado.EsExitoso);
        Assert.Null(resultado.Valor!.MejorOfertaId);
        Assert.Equal("Sin ofertas válidas", resultado.Valor.Clasificacion);
        Assert.Null(resultado.Valor.Aprobador);
    }

    // --- Niveles de aprobación ---

    [Fact]
    public async Task CrearNivel_QueSeTraslapaConOtro_DevuelveConflicto()
    {
        await Niveles.CrearAsync(new NivelAprobacionEntrada(0.01m, 1_000_000m, "Encargado de área"));

        var resultado = await Niveles.CrearAsync(
            new NivelAprobacionEntrada(900_000m, 5_000_000m, "Gerencia"));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.RangoAprobacionTraslapado, resultado.Error!.Codigo);
        Assert.Single(_almacen.Niveles);
    }

    [Fact]
    public async Task CrearNivel_ConSegundoRangoAbierto_DevuelveConflicto()
    {
        await Niveles.CrearAsync(new NivelAprobacionEntrada(1_000_000m, null, "Gerencia"));

        var resultado = await Niveles.CrearAsync(
            new NivelAprobacionEntrada(10_000_000m, null, "Junta Directiva"));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.RangoAbiertoDuplicado, resultado.Error!.Codigo);
    }

    [Theory]
    [InlineData(500_000, "Encargado de área")]
    [InlineData(5_000_000, "Gerencia")]
    [InlineData(50_000_000, "Junta Directiva")]
    public async Task ResolverAprobador_UsaLaTablaYNoCondicionesFijas(decimal monto, string esperado)
    {
        await SembrarNivelesAsync();

        var resultado = await Niveles.ResolverAprobadorAsync(monto);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(esperado, resultado.Valor!.Aprobador);
    }

    [Fact]
    public async Task ResolverAprobador_ConMontoFueraDeTodoRango_DevuelveReglaDeNegocio()
    {
        await SembrarNivelesAsync();

        var resultado = await Niveles.ResolverAprobadorAsync(0.001m);

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.SinNivelAprobacionAplicable, resultado.Error!.Codigo);
    }

    // --- Utilidades ---

    private async Task SembrarNivelesAsync()
    {
        await Niveles.CrearAsync(new NivelAprobacionEntrada(0.01m, 999_999.99m, "Encargado de área"));
        await Niveles.CrearAsync(new NivelAprobacionEntrada(1_000_000m, 9_999_999.99m, "Gerencia"));
        await Niveles.CrearAsync(new NivelAprobacionEntrada(10_000_000m, null, "Junta Directiva"));
    }

    private async Task<Guid> CrearLicitacionAsync(string codigo)
    {
        var resultado = await Licitaciones.CrearAsync(new LicitacionEntrada(
            codigo,
            "Compra de equipo de cómputo",
            1_000_000m,
            _reloj.AhoraUtc.AddDays(30)));

        return resultado.Valor!.Id;
    }

    private async Task<Guid> CrearLicitacionPublicadaAsync()
    {
        var id = await CrearLicitacionAsync("LIC-2026-001");
        await Licitaciones.CambiarEstadoAsync(id, TransicionLicitacion.Publicar);
        return id;
    }

    private async Task<(Guid LicitacionId, Guid ProveedorId)> PrepararLicitacionPublicadaConProveedorAsync()
    {
        var proveedor = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa Central"));
        var licitacionId = await CrearLicitacionPublicadaAsync();

        return (licitacionId, proveedor.Valor!.Id);
    }
}
