using Licitaciones.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Http;

/// <summary>
/// Traduce el resultado de un caso de uso a una respuesta HTTP.
/// </summary>
/// <remarks>
/// Concentrar la traducción aquí garantiza que la misma regla de negocio
/// produzca siempre el mismo código en todos los controladores, y evita que cada
/// uno improvise su propio criterio (enunciado §10.2).
/// </remarks>
public static class ResultadoHttp
{
    /// <summary>Código HTTP que corresponde a cada tipo de error.</summary>
    public static int CodigoDe(TipoError tipo) => tipo switch
    {
        TipoError.Validacion => StatusCodes.Status400BadRequest,
        TipoError.NoEncontrado => StatusCodes.Status404NotFound,
        TipoError.Conflicto => StatusCodes.Status409Conflict,
        TipoError.Concurrencia => StatusCodes.Status409Conflict,
        TipoError.ReglaNegocio => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Título legible de cada familia de error.</summary>
    private static string TituloDe(TipoError tipo) => tipo switch
    {
        TipoError.Validacion => "Datos inválidos",
        TipoError.NoEncontrado => "Recurso no encontrado",
        TipoError.Conflicto => "Conflicto con el estado actual",
        TipoError.Concurrencia => "Conflicto de concurrencia",
        TipoError.ReglaNegocio => "Regla de negocio incumplida",
        _ => "Error inesperado",
    };

    /// <summary>
    /// Construye la respuesta de error en formato <c>ProblemDetails</c>.
    /// </summary>
    /// <remarks>
    /// Solo se expone el mensaje que la capa de aplicación redactó para el
    /// cliente y un identificador de correlación. Nunca la traza, la consulta ni
    /// el nombre de la restricción de base de datos (enunciado §10.2).
    /// </remarks>
    public static ProblemDetails ProblemaDe(ErrorAplicacion error, HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(contexto);

        var problema = new ProblemDetails
        {
            Title = TituloDe(error.Tipo),
            Status = CodigoDe(error.Tipo),
            Detail = error.Mensaje,
            Instance = contexto.Request.Path,
        };

        problema.Extensions["codigoError"] = error.Codigo;
        problema.Extensions["idCorrelacion"] = contexto.TraceIdentifier;

        return problema;
    }

    /// <summary>Convierte un resultado con valor en <c>200 OK</c> o el error correspondiente.</summary>
    public static IActionResult AResultado<T>(this Resultado<T> resultado, HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        return resultado.EsExitoso
            ? new OkObjectResult(resultado.Valor)
            : Fallo(resultado.Error!, contexto);
    }

    /// <summary>
    /// Convierte un resultado de creación en <c>201 Created</c> con la cabecera
    /// <c>Location</c> apuntando al recurso nuevo.
    /// </summary>
    public static IActionResult ACreado<T>(
        this Resultado<T> resultado,
        HttpContext contexto,
        string nombreRuta,
        Func<T, object> valoresRuta)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        ArgumentNullException.ThrowIfNull(valoresRuta);

        return resultado.EsExitoso
            ? new CreatedAtRouteResult(nombreRuta, valoresRuta(resultado.Valor!), resultado.Valor)
            : Fallo(resultado.Error!, contexto);
    }

    /// <summary>Convierte un resultado sin valor en <c>204 No Content</c> o el error correspondiente.</summary>
    public static IActionResult ASinContenido(this Resultado resultado, HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        return resultado.EsExitoso
            ? new NoContentResult()
            : Fallo(resultado.Error!, contexto);
    }

    private static ObjectResult Fallo(ErrorAplicacion error, HttpContext contexto)
    {
        var problema = ProblemaDe(error, contexto);

        return new ObjectResult(problema) { StatusCode = problema.Status };
    }
}
