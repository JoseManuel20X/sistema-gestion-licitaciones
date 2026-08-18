using System.Globalization;
using Licitaciones.Application;
using Licitaciones.Infrastructure;
using Licitaciones.Web.Infraestructura;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión llega por variable de entorno o secreto; el repositorio
// nunca contiene credenciales reales (enunciado §11).
var cadenaConexion = builder.Configuration.GetConnectionString("Licitaciones")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings__Licitaciones'. "
        + "Defínala como variable de entorno o secreto.");

builder.Services.AgregarAplicacion();
builder.Services.AgregarInfraestructura(cadenaConexion);

builder.Services.AddControllersWithViews(opciones =>
    // Debe ir primero para tener prioridad sobre el enlazador de decimales que
    // trae el marco, que solo entiende la cultura de la petición.
    opciones.ModelBinderProviders.Insert(0, new ProveedorEnlazadorDecimal()));

var app = builder.Build();

// Los montos se presentan en colones con formato es-CR y las fechas en la zona
// de Costa Rica (enunciado §8.2 y §9). La cultura se fija para toda la
// aplicación en lugar de formatear a mano en cada vista.
var culturaCostaRica = new CultureInfo("es-CR");
CultureInfo.DefaultThreadCurrentCulture = culturaCostaRica;
CultureInfo.DefaultThreadCurrentUICulture = culturaCostaRica;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culturaCostaRica),
    SupportedCultures = [culturaCostaRica],
    SupportedUICultures = [culturaCostaRica],
});

// Migraciones y semilla al arrancar, igual que la API, para que levantar la
// solución no requiera pasos manuales. La operación es idempotente: el segundo
// proceso en arrancar encuentra el trabajo ya hecho.
// En Kubernetes esto se moverá a un Job, para que varias réplicas no compitan
// por aplicar la misma migración.
if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.MigrarYSembrarAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Inicio/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Index}/{id?}");

await app.RunAsync();
