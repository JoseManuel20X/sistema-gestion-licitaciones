using Licitaciones.Api.Http;
using Licitaciones.Application.Common;
using Licitaciones.Application.Ofertas;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controllers;

/// <summary>Operaciones sobre ofertas (HU-05 y HU-06).</summary>
/// <remarks>
/// El alta se hace desde <c>POST /api/v1/licitaciones/{id}/ofertas</c>, porque
/// una oferta solo existe dentro de una licitación.
/// </remarks>
[ApiController]
[Route("api/v1/ofertas")]
[Produces("application/json")]
public sealed class OfertasController : ControllerBase
{
    private readonly OfertaServicio _servicio;

    public OfertasController(OfertaServicio servicio) => _servicio = servicio;

    /// <summary>Lista ofertas con paginación y filtro por licitación y proveedor.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaResultado<OfertaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ParametrosConsulta consulta,
        [FromQuery] Guid? licitacionId,
        [FromQuery] Guid? proveedorId,
        CancellationToken cancelacion) =>
        Ok(await _servicio.ListarAsync(consulta, licitacionId, proveedorId, cancelacion));

    /// <summary>Consulta una oferta por su identificador.</summary>
    [HttpGet("{id:guid}", Name = nameof(ObtenerOferta))]
    [ProducesResponseType(typeof(OfertaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerOferta(Guid id, CancellationToken cancelacion) =>
        (await _servicio.ObtenerAsync(id, cancelacion)).AResultado(HttpContext);

    /// <summary>Modifica el monto de una oferta mientras la licitación siga vigente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OfertaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] OfertaActualizacion entrada,
        CancellationToken cancelacion) =>
        (await _servicio.ActualizarAsync(id, entrada, cancelacion)).AResultado(HttpContext);

    /// <summary>
    /// Elimina una oferta. Solo procede mientras la licitación siga publicada y
    /// vigente: las de licitaciones cerradas se conservan como evidencia.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion) =>
        (await _servicio.EliminarAsync(id, cancelacion)).ASinContenido(HttpContext);
}
