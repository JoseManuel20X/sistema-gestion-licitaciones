using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Licitaciones.Application.TiposCambio;

namespace Licitaciones.IntegrationTests;

/// <summary>
/// Contratos HTTP de la API REST v1 (HU-13), verificados contra la aplicación
/// real y PostgreSQL real.
/// </summary>
/// <remarks>
/// Comprueban lo que el enunciado §10.2 exige del transporte —códigos correctos,
/// <c>ProblemDetails</c> con código de error e identificador de correlación,
/// cabecera <c>Location</c> al crear— y que ningún mensaje filtre detalles
/// internos. La lógica de negocio ya está cubierta por las pruebas unitarias:
/// aquí se verifica la traducción a HTTP, no la regla.
/// </remarks>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class ApiTests : IAsyncLifetime, IDisposable
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly BaseDatosFixture _fixture;
    private FabricaApi _fabrica = null!;
    private HttpClient _cliente = null!;

    public ApiTests(BaseDatosFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        // La API se apoya en la semilla del §11 —niveles de aprobación y tipo de
        // cambio activo—, así que cada prueba parte del mismo estado inicial que
        // la aplicación real recién desplegada.
        await _fixture.LimpiarYSembrarAsync(RelojFijo.EnInstanteBase());
        _fabrica = new FabricaApi(_fixture.CadenaConexion);
        _cliente = _fabrica.CreateClient();
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    /// <summary>Libera el cliente y la aplicación levantada para esta prueba.</summary>
    public void Dispose()
    {
        _cliente?.Dispose();
        _fabrica?.Dispose();
    }

    private async Task<ProveedorDto> CrearProveedorAsync(string nombre)
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/proveedores", new ProveedorEntrada(nombre));
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<ProveedorDto>(OpcionesJson))!;
    }

    private async Task<LicitacionDto> CrearLicitacionPublicadaAsync(string codigo, decimal presupuesto = 1_000_000m)
    {
        var creacion = await _cliente.PostAsJsonAsync(
            "/api/v1/licitaciones",
            new LicitacionEntrada(codigo, "Compra de equipo", presupuesto, DateTimeOffset.UtcNow.AddDays(30)));
        creacion.EnsureSuccessStatusCode();

        var licitacion = (await creacion.Content.ReadFromJsonAsync<LicitacionDto>(OpcionesJson))!;

        var publicacion = await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/estado",
            new { transicion = "Publicar" });
        publicacion.EnsureSuccessStatusCode();

        return (await publicacion.Content.ReadFromJsonAsync<LicitacionDto>(OpcionesJson))!;
    }

    [Fact]
    public async Task PostProveedor_DevuelveCreatedConCabeceraLocation()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new ProveedorEntrada("Empresa Central"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(respuesta.Headers.Location);

        // La cabecera Location debe apuntar a un recurso que exista de verdad.
        var seguimiento = await _cliente.GetAsync(respuesta.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, seguimiento.StatusCode);
    }

    [Fact]
    public async Task PostProveedor_ConNombreDuplicadoNormalizado_DevuelveConflict()
    {
        await CrearProveedorAsync("Empresa Central");

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new ProveedorEntrada("  empresa   CENTRAL  "));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PROVEEDOR_NOMBRE_DUPLICADO", problema.GetProperty("codigoError").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problema.GetProperty("idCorrelacion").GetString()));
    }

    [Fact]
    public async Task PostProveedor_ConCaracteresNoPermitidos_DevuelveBadRequest()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new ProveedorEntrada("Empresa @ Central"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task GetProveedor_Inexistente_DevuelveNotFoundConProblemDetails()
    {
        var respuesta = await _cliente.GetAsync($"/api/v1/proveedores/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PROVEEDOR_NO_ENCONTRADO", problema.GetProperty("codigoError").GetString());
    }

    [Fact]
    public async Task DeleteProveedor_SinOfertas_DevuelveNoContent()
    {
        var proveedor = await CrearProveedorAsync("Empresa Central");

        var respuesta = await _cliente.DeleteAsync($"/api/v1/proveedores/{proveedor.Id}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task GetProveedores_DevuelvePaginaConTotales()
    {
        await CrearProveedorAsync("Alfa");
        await CrearProveedorAsync("Beta");
        await CrearProveedorAsync("Gamma");

        var pagina = await _cliente.GetFromJsonAsync<JsonElement>(
            "/api/v1/proveedores?pagina=1&tamanoPagina=2");

        Assert.Equal(3, pagina.GetProperty("totalElementos").GetInt32());
        Assert.Equal(2, pagina.GetProperty("totalPaginas").GetInt32());
        Assert.Equal(2, pagina.GetProperty("elementos").GetArrayLength());
        Assert.True(pagina.GetProperty("tienePaginaSiguiente").GetBoolean());
    }

    [Fact]
    public async Task PatchEstado_PublicaLaLicitacion()
    {
        var licitacion = await CrearLicitacionPublicadaAsync("LIC-001");

        Assert.Equal("Publicada", licitacion.Estado);
        Assert.True(licitacion.AceptaOfertas);
    }

    [Fact]
    public async Task PatchEstado_TransicionProhibida_DevuelveUnprocessableEntity()
    {
        var licitacion = await CrearLicitacionPublicadaAsync("LIC-002");

        // Publicada → Publicada no es una transición válida (§8.1).
        var respuesta = await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/estado",
            new { transicion = "Publicar" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("LICITACION_TRANSICION_INVALIDA", problema.GetProperty("codigoError").GetString());
    }

    [Fact]
    public async Task PostOferta_SuperiorAlPresupuesto_DevuelveUnprocessableEntity()
    {
        var licitacion = await CrearLicitacionPublicadaAsync("LIC-003", 1_000_000m);
        var proveedor = await CrearProveedorAsync("Empresa Central");

        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/ofertas",
            new OfertaEntrada(proveedor.Id, 1_000_000.01m));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("OFERTA_SUPERA_PRESUPUESTO", problema.GetProperty("codigoError").GetString());
    }

    [Fact]
    public async Task PostOferta_DuplicadaDelMismoProveedor_DevuelveConflict()
    {
        var licitacion = await CrearLicitacionPublicadaAsync("LIC-004");
        var proveedor = await CrearProveedorAsync("Empresa Central");

        var primera = await _cliente.PostAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/ofertas",
            new OfertaEntrada(proveedor.Id, 900_000m));
        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);

        var segunda = await _cliente.PostAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/ofertas",
            new OfertaEntrada(proveedor.Id, 800_000m));

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);

        var problema = await segunda.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("OFERTA_DUPLICADA", problema.GetProperty("codigoError").GetString());
    }

    [Fact]
    public async Task GetMejorOferta_DevuelveClasificacionYAprobador()
    {
        var licitacion = await CrearLicitacionPublicadaAsync("LIC-005", 1_000_000m);
        var alfa = await CrearProveedorAsync("Alfa");
        var beta = await CrearProveedorAsync("Beta");

        await _cliente.PostAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/ofertas",
            new OfertaEntrada(alfa.Id, 950_000m));
        await _cliente.PostAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/ofertas",
            new OfertaEntrada(beta.Id, 800_000m));

        var mejor = await _cliente.GetFromJsonAsync<MejorOfertaDto>(
            $"/api/v1/licitaciones/{licitacion.Id}/mejor-oferta", OpcionesJson);

        Assert.NotNull(mejor);
        Assert.Equal(800_000m, mejor.MontoMejorOfertaCRC);
        Assert.Equal(20m, mejor.PorcentajeAhorro);
        Assert.Equal("Oferta conveniente", mejor.Clasificacion);
        // 800.000 cae en el primer rango de la semilla (§8.7).
        Assert.Equal("Encargado de área", mejor.Aprobador);
    }

    [Fact]
    public async Task GetMejorOferta_SinOfertas_DevuelveSinOfertasValidas()
    {
        var licitacion = await CrearLicitacionPublicadaAsync("LIC-006");

        var mejor = await _cliente.GetFromJsonAsync<MejorOfertaDto>(
            $"/api/v1/licitaciones/{licitacion.Id}/mejor-oferta", OpcionesJson);

        Assert.NotNull(mejor);
        Assert.Null(mejor.MejorOfertaId);
        Assert.Equal("Sin ofertas válidas", mejor.Clasificacion);
    }

    [Fact]
    public async Task GetTipoCambioActivo_DevuelveElDeLaSemilla()
    {
        var activo = await _cliente.GetFromJsonAsync<TipoCambioDto>(
            "/api/v1/tipos-cambio/activo", OpcionesJson);

        Assert.NotNull(activo);
        Assert.True(activo.Activo);
        Assert.True(activo.CRCporUSD > 0);
    }

    [Fact]
    public async Task PatchActivar_CambiaElActivoYDejaSoloUno()
    {
        var creado = await _cliente.PostAsJsonAsync(
            "/api/v1/tipos-cambio",
            new TipoCambioEntrada(535.50m, DateTimeOffset.UtcNow));
        creado.EnsureSuccessStatusCode();
        var nuevo = (await creado.Content.ReadFromJsonAsync<TipoCambioDto>(OpcionesJson))!;

        var activacion = await _cliente.PatchAsync($"/api/v1/tipos-cambio/{nuevo.Id}/activar", null);

        Assert.Equal(HttpStatusCode.OK, activacion.StatusCode);

        var todos = await _cliente.GetFromJsonAsync<List<TipoCambioDto>>(
            "/api/v1/tipos-cambio", OpcionesJson);

        Assert.NotNull(todos);
        Assert.Single(todos, t => t.Activo);
        Assert.Equal(nuevo.Id, todos.Single(t => t.Activo).Id);
    }

    [Fact]
    public async Task GetConvertir_UsaElTipoDeCambioActivo()
    {
        var conversion = await _cliente.GetFromJsonAsync<MontoConvertidoDto>(
            "/api/v1/tipos-cambio/convertir?montoCRC=1040000", OpcionesJson);

        Assert.NotNull(conversion);
        Assert.Equal(1_040_000m, conversion.MontoCRC);
        Assert.Equal(2_000m, conversion.MontoUSD);
    }

    [Fact]
    public async Task GetNivelesAprobacion_DevuelveLaSemillaDelEnunciado()
    {
        var niveles = await _cliente.GetFromJsonAsync<JsonElement>("/api/v1/niveles-aprobacion");

        Assert.Equal(3, niveles.GetArrayLength());
    }

    [Fact]
    public async Task GetAprobador_ResuelveSegunElMonto()
    {
        var nivel = await _cliente.GetFromJsonAsync<JsonElement>(
            "/api/v1/niveles-aprobacion/aprobador?montoCRC=5000000");

        Assert.Equal("Gerencia", nivel.GetProperty("aprobador").GetString());
    }

    [Fact]
    public async Task ProblemDetails_NoFiltraDetallesInternos()
    {
        await CrearProveedorAsync("Empresa Central");

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new ProveedorEntrada("Empresa Central"));

        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        // El §10.2 prohíbe exponer trazas, nombres de restricciones, SQL o rutas
        // internas al cliente.
        Assert.DoesNotContain("ix_proveedores", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at Licitaciones.", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Swagger_PublicaElContratoDeLaVersionV1()
    {
        var respuesta = await _cliente.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var documento = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var rutas = documento.GetProperty("paths");

        Assert.True(rutas.TryGetProperty("/api/v1/licitaciones", out _));
        Assert.True(rutas.TryGetProperty("/api/v1/licitaciones/{id}/mejor-oferta", out _));
        Assert.True(rutas.TryGetProperty("/api/v1/tipos-cambio/{id}/activar", out _));
    }
}
