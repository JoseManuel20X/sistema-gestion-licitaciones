using Licitaciones.Domain.Common;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Licitaciones.IntegrationTests;

/// <summary>
/// Levanta un PostgreSQL real en contenedor para toda la colección de pruebas.
/// </summary>
/// <remarks>
/// El enunciado §11 y §12.2 exigen PostgreSQL real y prohíben sustituirlo por
/// SQLite: solo así se verifican los índices únicos parciales, las restricciones
/// CHECK y la concurrencia optimista basada en <c>xmin</c>, que no existen ni se
/// comportan igual en un proveedor en memoria.
/// El contenedor se comparte entre las pruebas de la colección porque arrancarlo
/// es lento; cada prueba limpia sus propios datos.
/// </remarks>
public sealed class BaseDatosFixture : IAsyncLifetime
{
    // La imagen se fija en el constructor y con versión exacta: el enunciado §11
    // exige PostgreSQL 16 o superior, y anclar la etiqueta evita que la prueba
    // cambie de comportamiento cuando se publique una versión nueva.
    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("licitaciones_pruebas")
        .WithUsername("pruebas")
        .WithPassword("pruebas")
        .Build();

    /// <summary>Cadena de conexión al contenedor ya arrancado.</summary>
    public string CadenaConexion => _contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        // Se aplican las migraciones reales, no EnsureCreated: así la prueba
        // verifica también que las migraciones versionadas funcionan.
        await using var contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _contenedor.DisposeAsync();

    /// <summary>Crea un contexto nuevo apuntando al contenedor.</summary>
    public LicitacionesDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<LicitacionesDbContext>()
            .UseNpgsql(CadenaConexion)
            .Options;

        return new LicitacionesDbContext(opciones);
    }

    /// <summary>Borra los datos de todas las tablas conservando el esquema.</summary>
    public async Task LimpiarAsync()
    {
        await using var contexto = CrearContexto();

        await contexto.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE ofertas, licitaciones, proveedores, niveles_aprobacion, tipos_cambio
            RESTART IDENTITY CASCADE;
            """);
    }

    /// <summary>
    /// Deja la base limpia pero con los datos iniciales del §11: niveles de
    /// aprobación y tipo de cambio activo.
    /// </summary>
    /// <remarks>
    /// Necesario para las pruebas de la API, que se apoyan en la semilla igual
    /// que la aplicación real. No se hace dentro de <see cref="LimpiarAsync"/>
    /// porque las pruebas de persistencia sí necesitan partir de tablas vacías
    /// para verificar restricciones sobre datos que ellas mismas insertan.
    /// </remarks>
    public async Task LimpiarYSembrarAsync(IReloj reloj)
    {
        await LimpiarAsync();

        await using var contexto = CrearContexto();
        await DatosSemilla.SembrarAsync(contexto, reloj);
    }
}

/// <summary>Agrupa las pruebas que comparten el mismo contenedor de PostgreSQL.</summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionBaseDatos : ICollectionFixture<BaseDatosFixture>
{
    public const string Nombre = "PostgreSQL";
}

/// <summary>Reloj controlado, replicado aquí para no acoplar los proyectos de prueba.</summary>
public sealed class RelojFijo : IReloj
{
    public RelojFijo(DateTimeOffset instante) => AhoraUtc = instante;

    public DateTimeOffset AhoraUtc { get; private set; }

    public static RelojFijo EnInstanteBase() => new(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));

    public void Avanzar(TimeSpan intervalo) => AhoraUtc = AhoraUtc.Add(intervalo);
}
