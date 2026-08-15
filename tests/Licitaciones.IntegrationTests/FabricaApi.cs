using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Licitaciones.IntegrationTests;

/// <summary>
/// Levanta la API en memoria apuntando al PostgreSQL del contenedor.
/// </summary>
/// <remarks>
/// La cadena de conexión y el entorno se pasan por variable de entorno y no por
/// <c>ConfigureAppConfiguration</c>: <c>WebApplication.CreateBuilder</c> lee la
/// configuración al principio de <c>Program</c>, antes de que
/// <c>WebApplicationFactory</c> aplique sus ajustes diferidos, de modo que un
/// origen añadido ahí llegaría tarde. Las variables de entorno están
/// disponibles desde el primer instante.
///
/// El entorno «Testing» evita que <c>Program</c> aplique migraciones al
/// arrancar: de eso ya se encarga el fixture una sola vez para toda la
/// colección.
/// </remarks>
public sealed class FabricaApi : WebApplicationFactory<Program>
{
    public FabricaApi(string cadenaConexion)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Licitaciones", cadenaConexion);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
    }
}
