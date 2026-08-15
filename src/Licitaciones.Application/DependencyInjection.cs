using Licitaciones.Application.Aprobaciones;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Microsoft.Extensions.DependencyInjection;

namespace Licitaciones.Application;

/// <summary>Registro de los casos de uso en el contenedor de dependencias.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios de aplicación con ámbito por petición, igual que el
    /// <c>DbContext</c> del que dependen.
    /// </summary>
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddScoped<ProveedorServicio>();
        servicios.AddScoped<LicitacionServicio>();
        servicios.AddScoped<OfertaServicio>();
        servicios.AddScoped<NivelAprobacionServicio>();

        return servicios;
    }
}
