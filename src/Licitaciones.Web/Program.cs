using System.Globalization;
using Licitaciones.Application;
using Licitaciones.Infrastructure;
using Licitaciones.Infrastructure.Persistencia;
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

// Comprobación de salud que verifica también la base de datos: un pod que
// responde pero no alcanza PostgreSQL no está listo para recibir tráfico. La
// usan la readinessProbe de Kubernetes y el health check de Compose (§13).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LicitacionesDbContext>("postgresql");

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

// Modo «migrar y salir», que usa el Job de Kubernetes: aplica las migraciones y
// la semilla, y termina sin levantar el servidor. Así una sola ejecución prepara
// la base y las réplicas arrancan con el esquema ya listo, en vez de competir
// entre ellas por aplicar la misma migración.
if (args.Contains("--solo-migrar", StringComparer.Ordinal))
{
    await app.Services.MigrarYSembrarAsync();
    return;
}

// Fuera de Kubernetes se migra al arrancar para que `docker compose up --build`
// no requiera pasos manuales. La operación es idempotente. En Kubernetes se
// desactiva por configuración y lo hace el Job.
var migrarAlArrancar = app.Configuration.GetValue("Migraciones:AplicarAlArrancar", true);
if (migrarAlArrancar && !app.Environment.IsEnvironment("Testing"))
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

app.MapHealthChecks("/salud");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Index}/{id?}");

await app.RunAsync();
