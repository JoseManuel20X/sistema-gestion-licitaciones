using Licitaciones.Application.Abstracciones;
using Licitaciones.Domain.Common;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Infrastructure.Persistencia.Repositorios;
using Licitaciones.Infrastructure.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Licitaciones.Infrastructure;

/// <summary>Registro de la infraestructura en el contenedor de dependencias.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra el contexto de datos, los repositorios y el reloj del sistema.
    /// </summary>
    /// <param name="cadenaConexion">
    /// Cadena de conexión a PostgreSQL. Debe provenir de una variable de entorno
    /// o de un secreto: el enunciado §11 prohíbe credenciales reales en el
    /// repositorio.
    /// </param>
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios,
        string cadenaConexion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentException.ThrowIfNullOrWhiteSpace(cadenaConexion);

        servicios.AddDbContext<LicitacionesDbContext>(opciones =>
            opciones.UseNpgsql(cadenaConexion, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(LicitacionesDbContext).Assembly.FullName);

                // Reintenta ante fallos transitorios de red o de arranque del
                // contenedor de base de datos.
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, TimeSpan.FromSeconds(10), null);
            }));

        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<IProveedorRepositorio, ProveedorRepositorio>();
        servicios.AddScoped<ILicitacionRepositorio, LicitacionRepositorio>();
        servicios.AddScoped<IOfertaRepositorio, OfertaRepositorio>();
        servicios.AddScoped<INivelAprobacionRepositorio, NivelAprobacionRepositorio>();
        servicios.AddScoped<ITipoCambioRepositorio, TipoCambioRepositorio>();

        servicios.AddSingleton<IReloj, RelojSistema>();

        return servicios;
    }

    /// <summary>
    /// Aplica las migraciones pendientes y siembra los datos iniciales.
    /// </summary>
    /// <remarks>
    /// Se invoca al arrancar la aplicación para que <c>docker compose up --build</c>
    /// deje el sistema listo sin pasos manuales (enunciado §13.1). En Kubernetes
    /// conviene ejecutarlo desde un Job o un initContainer para que no compitan
    /// varias réplicas.
    /// </remarks>
    public static async Task MigrarYSembrarAsync(
        this IServiceProvider proveedorServicios,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(proveedorServicios);

        using var ambito = proveedorServicios.CreateScope();

        var contexto = ambito.ServiceProvider.GetRequiredService<LicitacionesDbContext>();
        var reloj = ambito.ServiceProvider.GetRequiredService<IReloj>();

        await contexto.Database.MigrateAsync(cancelacion);
        await DatosSemilla.SembrarAsync(contexto, reloj, cancelacion);
    }
}
