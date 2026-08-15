using Licitaciones.Application.Aprobaciones;
using Licitaciones.Application.Common;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Licitaciones.Domain.Common;
using Licitaciones.UnitTests.Common;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Consultas, listados paginados y operaciones de edición y borrado de los
/// casos de uso.
/// </summary>
public sealed class ConsultasYPaginacionTests
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

    // --- ParametrosConsulta ---

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void ParametrosConsulta_NormalizaLaPaginaAUnMinimoDeUno(int solicitada, int esperada)
    {
        var parametros = new ParametrosConsulta { Pagina = solicitada };

        Assert.Equal(esperada, parametros.Pagina);
    }

    [Theory]
    [InlineData(0, ParametrosConsulta.TamanoPaginaPorDefecto)]
    [InlineData(-1, ParametrosConsulta.TamanoPaginaPorDefecto)]
    [InlineData(500, ParametrosConsulta.TamanoPaginaMaximo)]
    [InlineData(25, 25)]
    public void ParametrosConsulta_AcotaElTamanoDePagina(int solicitado, int esperado)
    {
        var parametros = new ParametrosConsulta { TamanoPagina = solicitado };

        Assert.Equal(esperado, parametros.TamanoPagina);
    }

    [Fact]
    public void ParametrosConsulta_CalculaLosElementosQueDebeOmitir()
    {
        var parametros = new ParametrosConsulta { Pagina = 3, TamanoPagina = 20 };

        Assert.Equal(40, parametros.Omitir);
    }

    [Fact]
    public void PaginaResultado_CalculaTotalDePaginasYNavegacion()
    {
        var pagina = new PaginaResultado<int>([1, 2, 3], Pagina: 2, TamanoPagina: 3, TotalElementos: 7);

        Assert.Equal(3, pagina.TotalPaginas);
        Assert.True(pagina.TienePaginaAnterior);
        Assert.True(pagina.TienePaginaSiguiente);
    }

    [Fact]
    public void PaginaResultado_EnLaUltimaPaginaNoOfreceSiguiente()
    {
        var pagina = new PaginaResultado<int>([7], Pagina: 3, TamanoPagina: 3, TotalElementos: 7);

        Assert.False(pagina.TienePaginaSiguiente);
        Assert.True(pagina.TienePaginaAnterior);
    }

    [Fact]
    public void PaginaResultado_ProyectarConservaLosDatosDePaginacion()
    {
        var pagina = new PaginaResultado<int>([1, 2], Pagina: 1, TamanoPagina: 2, TotalElementos: 5);

        var proyectada = pagina.Proyectar(n => n.ToString(Normalizador.CulturaCostaRica));

        Assert.Equal(["1", "2"], proyectada.Elementos);
        Assert.Equal(5, proyectada.TotalElementos);
        Assert.Equal(3, proyectada.TotalPaginas);
    }

    // --- Consultas de proveedores ---

    [Fact]
    public async Task ObtenerProveedor_Existente_DevuelveElDto()
    {
        var creado = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa Central"));

        var resultado = await Proveedores.ObtenerAsync(creado.Valor!.Id);

        Assert.True(resultado.EsExitoso);
        Assert.Equal("Empresa Central", resultado.Valor!.Nombre);
    }

    [Fact]
    public async Task ObtenerProveedor_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Proveedores.ObtenerAsync(Guid.CreateVersion7());

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.ProveedorNoEncontrado, resultado.Error!.Codigo);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error.Tipo);
    }

    [Fact]
    public async Task ListarProveedores_DevuelveLaPaginaSolicitada()
    {
        for (var i = 1; i <= 5; i++)
        {
            await Proveedores.CrearAsync(new ProveedorEntrada($"Empresa {i}"));
        }

        var pagina = await Proveedores.ListarAsync(new ParametrosConsulta { Pagina = 2, TamanoPagina = 2 });

        Assert.Equal(2, pagina.Elementos.Count);
        Assert.Equal(5, pagina.TotalElementos);
        Assert.Equal(3, pagina.TotalPaginas);
    }

    [Fact]
    public async Task ListarProveedores_OmiteLosBorradosLogicamente()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));
        await Proveedores.EliminarAsync(proveedorId);

        var pagina = await Proveedores.ListarAsync(new ParametrosConsulta());

        Assert.Empty(pagina.Elementos);
    }

    [Fact]
    public async Task ActualizarProveedor_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Proveedores.ActualizarAsync(
            Guid.CreateVersion7(),
            new ProveedorEntrada("Empresa Central"));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task EliminarProveedor_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Proveedores.EliminarAsync(Guid.CreateVersion7());

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    // --- Consultas de licitaciones ---

    [Fact]
    public async Task ObtenerLicitacion_DevuelveEstadoRegistradoYEfectivo()
    {
        var id = await CrearLicitacionPublicadaAsync();

        var resultado = await Licitaciones.ObtenerAsync(id);

        Assert.True(resultado.EsExitoso);
        Assert.Equal("Publicada", resultado.Valor!.Estado);
        Assert.Equal("Publicada", resultado.Valor.EstadoEfectivo);
        Assert.True(resultado.Valor.AceptaOfertas);
    }

    [Fact]
    public async Task ObtenerLicitacion_VencidaDevuelveEstadoEfectivoCerrada()
    {
        var id = await CrearLicitacionPublicadaAsync();
        _reloj.Avanzar(TimeSpan.FromDays(60));

        var resultado = await Licitaciones.ObtenerAsync(id);

        Assert.Equal("Publicada", resultado.Valor!.Estado);
        Assert.Equal("Cerrada", resultado.Valor.EstadoEfectivo);
        Assert.True(resultado.Valor.Vencida);
        Assert.False(resultado.Valor.AceptaOfertas);
    }

    [Fact]
    public async Task ObtenerLicitacion_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Licitaciones.ObtenerAsync(Guid.CreateVersion7());

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.LicitacionNoEncontrada, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ListarLicitaciones_DevuelveTodasLasVigentes()
    {
        await CrearLicitacionAsync("LIC-001");
        await CrearLicitacionAsync("LIC-002");

        var pagina = await Licitaciones.ListarAsync(new ParametrosConsulta());

        Assert.Equal(2, pagina.TotalElementos);
    }

    [Fact]
    public async Task ActualizarLicitacion_ConDatosValidos_EsExitoso()
    {
        var id = await CrearLicitacionAsync("LIC-001");

        var resultado = await Licitaciones.ActualizarAsync(id, new LicitacionEntrada(
            "LIC-001",
            "Compra de mobiliario",
            750_000m,
            _reloj.AhoraUtc.AddDays(20)));

        Assert.True(resultado.EsExitoso);
        Assert.Equal("Compra de mobiliario", resultado.Valor!.Titulo);
        Assert.Equal(750_000m, resultado.Valor.PresupuestoEstimadoCRC);
    }

    [Fact]
    public async Task ActualizarLicitacion_BajandoElPresupuestoPorDebajoDeUnaOferta_EsRechazado()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));

        var resultado = await Licitaciones.ActualizarAsync(licitacionId, new LicitacionEntrada(
            "LIC-2026-001",
            "Compra de equipo de cómputo",
            500_000m,
            _reloj.AhoraUtc.AddDays(30)));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.PresupuestoMenorQueOferta, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ActualizarLicitacion_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Licitaciones.ActualizarAsync(Guid.CreateVersion7(), new LicitacionEntrada(
            "LIC-999",
            "Título",
            100_000m,
            _reloj.AhoraUtc.AddDays(5)));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task CambiarEstado_Cerrar_DejaLaLicitacionCerrada()
    {
        var id = await CrearLicitacionPublicadaAsync();

        var resultado = await Licitaciones.CambiarEstadoAsync(id, TransicionLicitacion.Cerrar);

        Assert.True(resultado.EsExitoso);
        Assert.Equal("Cerrada", resultado.Valor!.Estado);
    }

    [Fact]
    public async Task CambiarEstado_DeLicitacionInexistente_DevuelveNoEncontrado()
    {
        var resultado = await Licitaciones.CambiarEstadoAsync(
            Guid.CreateVersion7(),
            TransicionLicitacion.Publicar);

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task EliminarLicitacion_SinOfertas_LaBorraFisicamente()
    {
        var id = await CrearLicitacionAsync("LIC-001");

        var resultado = await Licitaciones.EliminarAsync(id);

        Assert.True(resultado.EsExitoso);
        Assert.Empty(_almacen.Licitaciones);
    }

    [Fact]
    public async Task EliminarLicitacion_ConOfertas_AplicaBorradoLogico()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));

        var resultado = await Licitaciones.EliminarAsync(licitacionId);

        Assert.True(resultado.EsExitoso);
        Assert.Single(_almacen.Licitaciones);
        Assert.True(_almacen.Licitaciones[0].EstaEliminada);
    }

    [Fact]
    public async Task EliminarLicitacion_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Licitaciones.EliminarAsync(Guid.CreateVersion7());

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    // --- Consultas y edición de ofertas ---

    [Fact]
    public async Task ObtenerOferta_Existente_DevuelveElDto()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        var registrada = await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));

        var resultado = await Ofertas.ObtenerAsync(registrada.Valor!.Id);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(900_000m, resultado.Valor!.MontoOfertadoCRC);
    }

    [Fact]
    public async Task ObtenerOferta_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Ofertas.ObtenerAsync(Guid.CreateVersion7());

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.OfertaNoEncontrada, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ListarOfertas_FiltraPorLicitacionYProveedor()
    {
        var (licitacionId, primerProveedor) = await PrepararLicitacionPublicadaConProveedorAsync();
        var segundo = await Proveedores.CrearAsync(new ProveedorEntrada("Empresa del Norte"));
        await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(primerProveedor, 900_000m));
        await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(segundo.Valor!.Id, 850_000m));

        var todas = await Ofertas.ListarAsync(new ParametrosConsulta(), licitacionId);
        var soloPrimero = await Ofertas.ListarAsync(new ParametrosConsulta(), licitacionId, primerProveedor);

        Assert.Equal(2, todas.TotalElementos);
        Assert.Equal(1, soloPrimero.TotalElementos);
    }

    [Fact]
    public async Task ActualizarOferta_ConLicitacionVigente_CambiaElMonto()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        var registrada = await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));

        var resultado = await Ofertas.ActualizarAsync(
            registrada.Valor!.Id,
            new OfertaActualizacion(820_000m));

        Assert.True(resultado.EsExitoso);
        Assert.Equal(820_000m, resultado.Valor!.MontoOfertadoCRC);
    }

    [Fact]
    public async Task ActualizarOferta_ConLicitacionCerrada_EsRechazado()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        var registrada = await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));
        await Licitaciones.CambiarEstadoAsync(licitacionId, TransicionLicitacion.Cerrar);

        var resultado = await Ofertas.ActualizarAsync(
            registrada.Valor!.Id,
            new OfertaActualizacion(820_000m));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.OfertaLicitacionNoPublicada, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ActualizarOferta_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Ofertas.ActualizarAsync(
            Guid.CreateVersion7(),
            new OfertaActualizacion(100m));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task EliminarOferta_ConLicitacionVigente_EsExitoso()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        var registrada = await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));

        var resultado = await Ofertas.EliminarAsync(registrada.Valor!.Id);

        Assert.True(resultado.EsExitoso);
        Assert.Empty(_almacen.Ofertas);
    }

    [Fact]
    public async Task EliminarOferta_DeLicitacionCerrada_EsRechazadoParaConservarLaEvidencia()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        var registrada = await Ofertas.RegistrarAsync(licitacionId, new OfertaEntrada(proveedorId, 900_000m));
        await Licitaciones.CambiarEstadoAsync(licitacionId, TransicionLicitacion.Cerrar);

        var resultado = await Ofertas.EliminarAsync(registrada.Valor!.Id);

        Assert.False(resultado.EsExitoso);
        Assert.Single(_almacen.Ofertas);
    }

    [Fact]
    public async Task EliminarOferta_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Ofertas.EliminarAsync(Guid.CreateVersion7());

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    // --- Niveles de aprobación ---

    [Fact]
    public async Task ListarNiveles_DevuelveLosRangosOrdenadosPorMonto()
    {
        await Niveles.CrearAsync(new NivelAprobacionEntrada(10_000_000m, null, "Junta Directiva"));
        await Niveles.CrearAsync(new NivelAprobacionEntrada(0.01m, 999_999.99m, "Encargado de área"));

        var niveles = await Niveles.ListarAsync();

        Assert.Equal(2, niveles.Count);
        Assert.Equal("Encargado de área", niveles[0].Aprobador);
        Assert.Equal("Junta Directiva", niveles[1].Aprobador);
    }

    [Fact]
    public async Task ObtenerNivel_Existente_DevuelveElDto()
    {
        var creado = await Niveles.CrearAsync(
            new NivelAprobacionEntrada(0.01m, 999_999.99m, "Encargado de área"));

        var resultado = await Niveles.ObtenerAsync(creado.Valor!.Id);

        Assert.True(resultado.EsExitoso);
        Assert.Equal("Encargado de área", resultado.Valor!.Aprobador);
    }

    [Fact]
    public async Task ObtenerNivel_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Niveles.ObtenerAsync(Guid.CreateVersion7());

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.NivelAprobacionNoEncontrado, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ActualizarNivel_AmpliandoElRangoSinTraslape_EsExitoso()
    {
        var creado = await Niveles.CrearAsync(
            new NivelAprobacionEntrada(0.01m, 500_000m, "Encargado de área"));

        var resultado = await Niveles.ActualizarAsync(
            creado.Valor!.Id,
            new NivelAprobacionEntrada(0.01m, 900_000m, "Encargado de área"));

        Assert.True(resultado.EsExitoso);
        Assert.Equal(900_000m, resultado.Valor!.MontoMaximoCRC);
    }

    [Fact]
    public async Task ActualizarNivel_ProvocandoTraslape_EsRechazado()
    {
        var primero = await Niveles.CrearAsync(
            new NivelAprobacionEntrada(0.01m, 999_999.99m, "Encargado de área"));
        await Niveles.CrearAsync(new NivelAprobacionEntrada(1_000_000m, 9_999_999.99m, "Gerencia"));

        var resultado = await Niveles.ActualizarAsync(
            primero.Valor!.Id,
            new NivelAprobacionEntrada(0.01m, 5_000_000m, "Encargado de área"));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.RangoAprobacionTraslapado, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ActualizarNivel_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Niveles.ActualizarAsync(
            Guid.CreateVersion7(),
            new NivelAprobacionEntrada(1m, 2m, "Gerencia"));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task EliminarNivel_Existente_EsExitoso()
    {
        var creado = await Niveles.CrearAsync(
            new NivelAprobacionEntrada(0.01m, 999_999.99m, "Encargado de área"));

        var resultado = await Niveles.EliminarAsync(creado.Valor!.Id);

        Assert.True(resultado.EsExitoso);
        Assert.Empty(_almacen.Niveles);
    }

    [Fact]
    public async Task EliminarNivel_Inexistente_DevuelveNoEncontrado()
    {
        var resultado = await Niveles.EliminarAsync(Guid.CreateVersion7());

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    // --- Traducción de errores ---

    [Theory]
    [InlineData(CodigosError.NombreProveedorDuplicado, TipoError.Conflicto)]
    [InlineData(CodigosError.OfertaDuplicada, TipoError.Conflicto)]
    [InlineData(CodigosError.ProveedorNoEncontrado, TipoError.NoEncontrado)]
    [InlineData(CodigosError.MontoOfertaNoPositivo, TipoError.Validacion)]
    [InlineData(CodigosError.OfertaSuperaPresupuesto, TipoError.ReglaNegocio)]
    [InlineData(CodigosError.TransicionEstadoInvalida, TipoError.ReglaNegocio)]
    [InlineData(CodigosError.ConflictoConcurrencia, TipoError.Concurrencia)]
    public void TraductorErrores_ClasificaCadaCodigoEnSuTipo(string codigo, TipoError esperado)
    {
        Assert.Equal(esperado, TraductorErrores.ClasificarCodigo(codigo));
    }

    [Fact]
    public void TraductorErrores_ConservaCodigoYMensajeDeLaExcepcion()
    {
        var excepcion = new ExcepcionDominio(CodigosError.OfertaDuplicada, "Ya ofertó.");

        var error = TraductorErrores.Traducir(excepcion);

        Assert.Equal(CodigosError.OfertaDuplicada, error.Codigo);
        Assert.Equal("Ya ofertó.", error.Mensaje);
        Assert.Equal(TipoError.Conflicto, error.Tipo);
    }

    // --- Utilidades ---

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
