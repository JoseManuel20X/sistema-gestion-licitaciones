using System.Reflection;
using System.Text.Json.Serialization;
using Licitaciones.Api.Http;
using Licitaciones.Application;
using Licitaciones.Infrastructure;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión llega por variable de entorno o secreto; el repositorio
// nunca contiene credenciales reales (enunciado §11).
var cadenaConexion = builder.Configuration.GetConnectionString("Licitaciones")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings__Licitaciones'. "
        + "Defínala como variable de entorno o secreto.");

builder.Services.AgregarAplicacion();
builder.Services.AgregarInfraestructura(cadenaConexion);

builder.Services.AddControllers()
    .AddJsonOptions(opciones =>
    {
        // Los enumerados viajan como texto: "Publicada" es legible en el contrato
        // y no se rompe si algún día cambia el orden del enum.
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Las validaciones de formato del propio ASP.NET Core también deben responder
// ProblemDetails, para que el cliente reciba siempre la misma forma de error.
builder.Services.AddProblemDetails();
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    opciones.InvalidModelStateResponseFactory = contexto =>
    {
        var problema = new ValidationProblemDetails(contexto.ModelState)
        {
            Title = "Datos inválidos",
            Status = StatusCodes.Status400BadRequest,
            Instance = contexto.HttpContext.Request.Path,
        };

        problema.Extensions["codigoError"] = "SOLICITUD_MAL_FORMADA";
        problema.Extensions["idCorrelacion"] = contexto.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problema);
    };
});

builder.Services.AddExceptionHandler<ManejadorExcepciones>();

// Comprobación de salud que verifica también la base de datos: un contenedor
// que responde pero no alcanza PostgreSQL no está listo para recibir tráfico.
// La usan el health check de Docker Compose y las probes de Kubernetes (§13).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LicitacionesDbContext>("postgresql");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "API de Gestión de Licitaciones",
        Description = "Operaciones sobre licitaciones, proveedores, ofertas, "
                      + "niveles de aprobación y tipo de cambio CRC/USD.",
    });

    // Incorpora la documentación XML de los controladores al contrato publicado,
    // de modo que Swagger describa cada operación sin duplicar los textos.
    var archivoXml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var rutaXml = Path.Combine(AppContext.BaseDirectory, archivoXml);
    if (File.Exists(rutaXml))
    {
        opciones.IncludeXmlComments(rutaXml);
    }
});

var app = builder.Build();

app.UseExceptionHandler();

// Las migraciones y la semilla se aplican al arrancar para que
// `docker compose up --build` deje el sistema listo sin pasos manuales.
// En Kubernetes conviene moverlo a un Job para que no compitan las réplicas.
if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.MigrarYSembrarAsync();
}

app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Licitaciones v1");
    opciones.DocumentTitle = "API de Gestión de Licitaciones";
});

app.MapHealthChecks("/salud");
app.MapControllers();

await app.RunAsync();

/// <summary>
/// Punto de entrada expuesto para que <c>WebApplicationFactory</c> pueda
/// levantar la API en las pruebas de integración.
/// </summary>
public partial class Program
{
    protected Program()
    {
    }
}
