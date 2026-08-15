using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Construye el contexto cuando las herramientas de Entity Framework Core lo
/// necesitan en tiempo de diseño, por ejemplo al ejecutar
/// <c>dotnet ef migrations add</c>.
/// </summary>
/// <remarks>
/// Evita tener que apuntar a un proyecto de arranque para generar migraciones.
/// La cadena de conexión sale de la variable de entorno
/// <c>LICITACIONES_CONNECTION</c>; el valor de respaldo apunta a la base local de
/// desarrollo y no contiene credenciales reales, que según el enunciado §11
/// nunca deben versionarse.
/// </remarks>
public sealed class FabricaDbContextDisenio : IDesignTimeDbContextFactory<LicitacionesDbContext>
{
    /// <summary>Variable de entorno consultada en tiempo de diseño.</summary>
    public const string VariableCadenaConexion = "LICITACIONES_CONNECTION";

    private const string CadenaDesarrolloLocal =
        "Host=localhost;Port=5432;Database=licitaciones;Username=postgres;Password=postgres";

    public LicitacionesDbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable(VariableCadenaConexion) ?? CadenaDesarrolloLocal;

        var opciones = new DbContextOptionsBuilder<LicitacionesDbContext>()
            .UseNpgsql(cadena)
            .Options;

        return new LicitacionesDbContext(opciones);
    }
}
