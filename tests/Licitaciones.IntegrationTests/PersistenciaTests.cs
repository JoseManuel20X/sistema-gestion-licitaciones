using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Licitaciones.IntegrationTests;

/// <summary>
/// Verifica contra PostgreSQL real las restricciones que no pueden comprobarse
/// con pruebas unitarias: índices únicos, claves foráneas, CHECK, precisión
/// decimal y concurrencia optimista (enunciado §12.2).
/// </summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class PersistenciaTests : IAsyncLifetime
{
    /// <summary>SQLSTATE de violación de restricción única en PostgreSQL.</summary>
    private const string ViolacionUnicidad = "23505";

    /// <summary>SQLSTATE de violación de restricción CHECK.</summary>
    private const string ViolacionCheck = "23514";

    /// <summary>SQLSTATE de violación de clave foránea.</summary>
    private const string ViolacionClaveForanea = "23503";

    private readonly BaseDatosFixture _fixture;
    private readonly RelojFijo _reloj = RelojFijo.EnInstanteBase();

    public PersistenciaTests(BaseDatosFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.LimpiarAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Migraciones_CreanElEsquemaSinCambiosPendientes()
    {
        await using var contexto = _fixture.CrearContexto();

        var pendientes = await contexto.Database.GetPendingMigrationsAsync();

        Assert.Empty(pendientes);
    }

    [Fact]
    public async Task Proveedor_SePersisteYSeRecupera()
    {
        var id = await CrearProveedorAsync("Empresa Central");

        await using var contexto = _fixture.CrearContexto();
        var recuperado = await contexto.Proveedores.FindAsync(id);

        Assert.NotNull(recuperado);
        Assert.Equal("Empresa Central", recuperado.Nombre);
        Assert.Equal("EMPRESA CENTRAL", recuperado.NombreNormalizado);
    }

    [Fact]
    public async Task IndiceUnico_RechazaNombreDeProveedorEquivalente()
    {
        await CrearProveedorAsync("Empresa Central");

        // Distinta grafía, mismo nombre normalizado: PostgreSQL debe rechazarlo
        // aunque la validación de la aplicación se hubiera omitido.
        var excepcion = await Assert.ThrowsAsync<DbUpdateException>(
            () => CrearProveedorAsync("  empresa   CENTRAL  "));

        Assert.Equal(ViolacionUnicidad, ExtraerSqlState(excepcion));
    }

    [Fact]
    public async Task IndiceUnico_PermiteReutilizarElNombreDeUnProveedorBorradoLogicamente()
    {
        var id = await CrearProveedorAsync("Empresa Central");

        await using (var contexto = _fixture.CrearContexto())
        {
            var proveedor = await contexto.Proveedores.FindAsync(id);
            proveedor!.Eliminar(_reloj);
            await contexto.SaveChangesAsync();
        }

        // El índice único es parcial (WHERE "DeletedAt" IS NULL), así que el
        // nombre queda libre tras la baja lógica.
        var nuevoId = await CrearProveedorAsync("Empresa Central");

        Assert.NotEqual(id, nuevoId);
    }

    [Fact]
    public async Task IndiceUnico_RechazaCodigoDeLicitacionEquivalente()
    {
        await CrearLicitacionAsync("LIC-2026-001");

        var excepcion = await Assert.ThrowsAsync<DbUpdateException>(
            () => CrearLicitacionAsync("  lic-2026-001 "));

        Assert.Equal(ViolacionUnicidad, ExtraerSqlState(excepcion));
    }

    [Fact]
    public async Task IndiceUnicoCompuesto_RechazaDosOfertasDelMismoProveedor()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();

        await RegistrarOfertaAsync(licitacionId, proveedorId, 900_000m);

        var excepcion = await Assert.ThrowsAsync<DbUpdateException>(
            () => RegistrarOfertaAsync(licitacionId, proveedorId, 800_000m));

        Assert.Equal(ViolacionUnicidad, ExtraerSqlState(excepcion));
    }

    [Fact]
    public async Task ClaveForanea_ImpideBorrarUnaLicitacionConOfertas()
    {
        var (licitacionId, proveedorId) = await PrepararLicitacionPublicadaConProveedorAsync();
        await RegistrarOfertaAsync(licitacionId, proveedorId, 900_000m);

        await using var contexto = _fixture.CrearContexto();
        var licitacion = await contexto.Licitaciones.FindAsync(licitacionId);
        contexto.Licitaciones.Remove(licitacion!);

        var excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Equal(ViolacionClaveForanea, ExtraerSqlState(excepcion));
    }

    [Fact]
    public async Task RestriccionCheck_RechazaUnPresupuestoNoPositivoEscritoDirectamente()
    {
        await using var contexto = _fixture.CrearContexto();

        // Se escribe con SQL crudo para saltarse el dominio y comprobar que la
        // base de datos es la última línea de defensa (enunciado §8.5).
        var excepcion = await Assert.ThrowsAsync<PostgresException>(() =>
            contexto.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO licitaciones
                    ("Id", "Codigo", "CodigoNormalizado", "Titulo", "Estado",
                     "FechaCierre", "PresupuestoEstimadoCRC", "CreatedAt", "UpdatedAt")
                VALUES
                    (gen_random_uuid(), 'LIC-X', 'LIC-X', 'Prueba', 'Borrador',
                     now(), -1, now(), now());
                """));

        Assert.Equal(ViolacionCheck, excepcion.SqlState);
    }

    [Fact]
    public async Task Montos_ConservanLaPrecisionDecimalSinErrorDeComaFlotante()
    {
        var licitacionId = await CrearLicitacionAsync("LIC-2026-DEC", 1_234_567.89m);

        await using var contexto = _fixture.CrearContexto();
        var licitacion = await contexto.Licitaciones.FindAsync(licitacionId);

        Assert.Equal(1_234_567.89m, licitacion!.PresupuestoEstimadoCRC);
    }

    [Fact]
    public async Task ConcurrenciaOptimista_DetectaLaEdicionSimultaneaDeUnProveedor()
    {
        var id = await CrearProveedorAsync("Empresa Central");

        // Dos contextos leen la misma fila: simulan dos usuarios con el
        // formulario abierto a la vez.
        await using var primerContexto = _fixture.CrearContexto();
        await using var segundoContexto = _fixture.CrearContexto();

        var primerProveedor = await primerContexto.Proveedores.FindAsync(id);
        var segundoProveedor = await segundoContexto.Proveedores.FindAsync(id);

        primerProveedor!.Renombrar("Empresa Central Uno", _reloj);
        await primerContexto.SaveChangesAsync();

        segundoProveedor!.Renombrar("Empresa Central Dos", _reloj);

        // El xmin que leyó el segundo contexto ya cambió: EF Core detecta que la
        // fila fue modificada y no sobrescribe en silencio.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => segundoContexto.SaveChangesAsync());
    }

    [Fact]
    public async Task DatosSemilla_SiembranNivelesDeAprobacionYTipoDeCambioActivo()
    {
        await using var contexto = _fixture.CrearContexto();

        await DatosSemilla.SembrarAsync(contexto, _reloj);

        var niveles = await contexto.NivelesAprobacion.OrderBy(n => n.MontoMinimoCRC).ToListAsync();
        var activos = await contexto.TiposCambio.CountAsync(t => t.Activo);

        Assert.Equal(3, niveles.Count);
        Assert.Equal("Encargado de área", niveles[0].Aprobador);
        Assert.Equal("Junta Directiva", niveles[2].Aprobador);
        Assert.Null(niveles[2].MontoMaximoCRC);
        Assert.Equal(1, activos);
    }

    [Fact]
    public async Task DatosSemilla_SonIdempotentes()
    {
        await using var contexto = _fixture.CrearContexto();

        await DatosSemilla.SembrarAsync(contexto, _reloj);
        await DatosSemilla.SembrarAsync(contexto, _reloj);

        Assert.Equal(3, await contexto.NivelesAprobacion.CountAsync());
        Assert.Equal(1, await contexto.TiposCambio.CountAsync());
    }

    [Fact]
    public async Task IndiceUnicoParcial_ImpideDosTiposDeCambioActivos()
    {
        await using var contexto = _fixture.CrearContexto();
        await DatosSemilla.SembrarAsync(contexto, _reloj);

        contexto.TiposCambio.Add(
            Domain.TiposCambio.TipoCambio.Crear(540m, _reloj.AhoraUtc, activo: true, _reloj));

        var excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Equal(ViolacionUnicidad, ExtraerSqlState(excepcion));
    }

    // --- Utilidades ---

    private static string? ExtraerSqlState(DbUpdateException excepcion) =>
        (excepcion.InnerException as PostgresException)?.SqlState;

    private async Task<Guid> CrearProveedorAsync(string nombre)
    {
        await using var contexto = _fixture.CrearContexto();

        var proveedor = Proveedor.Crear(nombre, _reloj);
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();

        return proveedor.Id;
    }

    private async Task<Guid> CrearLicitacionAsync(string codigo, decimal presupuesto = 1_000_000m)
    {
        await using var contexto = _fixture.CrearContexto();

        var licitacion = Licitacion.Crear(
            codigo,
            "Compra de equipo",
            presupuesto,
            _reloj.AhoraUtc.AddDays(30),
            _reloj);

        contexto.Licitaciones.Add(licitacion);
        await contexto.SaveChangesAsync();

        return licitacion.Id;
    }

    private async Task<(Guid LicitacionId, Guid ProveedorId)> PrepararLicitacionPublicadaConProveedorAsync()
    {
        var proveedorId = await CrearProveedorAsync("Empresa Central");
        var licitacionId = await CrearLicitacionAsync("LIC-2026-001");

        await using var contexto = _fixture.CrearContexto();
        var licitacion = await contexto.Licitaciones.FindAsync(licitacionId);
        licitacion!.Publicar(_reloj);
        await contexto.SaveChangesAsync();

        return (licitacionId, proveedorId);
    }

    private async Task RegistrarOfertaAsync(Guid licitacionId, Guid proveedorId, decimal monto)
    {
        await using var contexto = _fixture.CrearContexto();

        var licitacion = await contexto.Licitaciones.FindAsync(licitacionId);
        var oferta = Oferta.Registrar(licitacion!, proveedorId, monto, _reloj);

        contexto.Ofertas.Add(oferta);
        await contexto.SaveChangesAsync();
    }
}
