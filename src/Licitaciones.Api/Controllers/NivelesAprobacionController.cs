using Licitaciones.Api.Http;
using Licitaciones.Application.Aprobaciones;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controllers;

/// <summary>Operaciones sobre los niveles de aprobación (HU-08).</summary>
[ApiController]
[Route("api/v1/niveles-aprobacion")]
[Produces("application/json")]
public sealed class NivelesAprobacionController : ControllerBase
{
    private readonly NivelAprobacionServicio _servicio;

    public NivelesAprobacionController(NivelAprobacionServicio servicio) => _servicio = servicio;

    /// <summary>Lista los niveles ordenados por monto mínimo.</summary>
    /// <remarks>
    /// No se pagina: la tabla de aprobación tiene unas pocas filas por definición
    /// y paginarla solo complicaría al cliente sin resolver ningún problema real.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NivelAprobacionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancelacion) =>
        Ok(await _servicio.ListarAsync(cancelacion));

    /// <summary>Consulta un nivel por su identificador.</summary>
    [HttpGet("{id:guid}", Name = nameof(ObtenerNivel))]
    [ProducesResponseType(typeof(NivelAprobacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerNivel(Guid id, CancellationToken cancelacion) =>
        (await _servicio.ObtenerAsync(id, cancelacion)).AResultado(HttpContext);

    /// <summary>Resuelve el aprobador que corresponde a un monto en colones.</summary>
    [HttpGet("aprobador")]
    [ProducesResponseType(typeof(NivelAprobacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResolverAprobador(
        [FromQuery] decimal montoCRC,
        CancellationToken cancelacion) =>
        (await _servicio.ResolverAprobadorAsync(montoCRC, cancelacion)).AResultado(HttpContext);

    /// <summary>Crea un nivel comprobando que no se traslape con los existentes.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(NivelAprobacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] NivelAprobacionEntrada entrada,
        CancellationToken cancelacion) =>
        (await _servicio.CrearAsync(entrada, cancelacion))
            .ACreado(HttpContext, nameof(ObtenerNivel), dto => new { id = dto.Id });

    /// <summary>Actualiza un nivel manteniendo la consistencia del conjunto.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NivelAprobacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] NivelAprobacionEntrada entrada,
        CancellationToken cancelacion) =>
        (await _servicio.ActualizarAsync(id, entrada, cancelacion)).AResultado(HttpContext);

    /// <summary>Elimina un nivel de aprobación.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion) =>
        (await _servicio.EliminarAsync(id, cancelacion)).ASinContenido(HttpContext);
}
