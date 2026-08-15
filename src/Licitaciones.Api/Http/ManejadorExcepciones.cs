using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Http;

/// <summary>
/// Convierte cualquier excepción no prevista en una respuesta 500 controlada.
/// </summary>
/// <remarks>
/// El enunciado §10.2 exige que el error 500 llegue como <c>ProblemDetails</c> y
/// prohíbe exponer trazas, rutas internas, consultas o secretos. El detalle real
/// se registra en el log del servidor junto al identificador de correlación, de
/// modo que sea rastreable sin filtrarlo al cliente.
/// </remarks>
public sealed partial class ManejadorExcepciones : IExceptionHandler
{
    private readonly ILogger<ManejadorExcepciones> _log;

    public ManejadorExcepciones(ILogger<ManejadorExcepciones> log) => _log = log;

    // Delegado generado en compilación: evita construir el mensaje y encajonar
    // los argumentos cuando el nivel de registro está desactivado.
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Error,
        Message = "Error no controlado en {Metodo} {Ruta}. Correlación: {IdCorrelacion}")]
    private partial void RegistrarErrorNoControlado(
        Exception excepcion,
        string metodo,
        string ruta,
        string idCorrelacion);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        RegistrarErrorNoControlado(
            exception,
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        var problema = new ProblemDetails
        {
            Title = "Error inesperado",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Ocurrió un error inesperado al procesar la solicitud. "
                     + "Comunique el identificador de correlación para su seguimiento.",
            Instance = httpContext.Request.Path,
        };

        problema.Extensions["codigoError"] = "ERROR_INESPERADO";
        problema.Extensions["idCorrelacion"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problema, cancellationToken);

        return true;
    }
}
