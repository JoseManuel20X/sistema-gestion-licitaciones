using Licitaciones.Api.Http;
using Licitaciones.Application.Common;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controllers;

/// <summary>Cuerpo de la solicitud de cambio de estado.</summary>
/// <param name="Transicion">Transición solicitada: <c>Publicar</c> o <c>Cerrar</c>.</param>
public sealed record CambioEstadoSolicitud(TransicionLicitacion Transicion);

/// <summary>Operaciones sobre licitaciones (HU-03, HU-04 y HU-07).</summary>
[ApiController]
[Route("api/v1/licitaciones")]
[Produces("application/json")]
public sealed class LicitacionesController : ControllerBase
{
    private readonly LicitacionServicio _servicio;
    private readonly OfertaServicio _ofertas;

    public LicitacionesController(LicitacionServicio servicio, OfertaServicio ofertas)
    {
        _servicio = servicio;
        _ofertas = ofertas;
    }

    /// <summary>Lista licitaciones con paginación, filtro y ordenamiento.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaResultado<LicitacionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ParametrosConsulta consulta,
        CancellationToken cancelacion) =>
        Ok(await _servicio.ListarAsync(consulta, cancelacion));

    /// <summary>Consulta una licitación por su identificador.</summary>
    [HttpGet("{id:guid}", Name = nameof(ObtenerLicitacion))]
    [ProducesResponseType(typeof(LicitacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerLicitacion(Guid id, CancellationToken cancelacion) =>
        (await _servicio.ObtenerAsync(id, cancelacion)).AResultado(HttpContext);

    /// <summary>Crea una licitación en estado Borrador.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LicitacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] LicitacionEntrada entrada,
        CancellationToken cancelacion) =>
        (await _servicio.CrearAsync(entrada, cancelacion))
            .ACreado(HttpContext, nameof(ObtenerLicitacion), dto => new { id = dto.Id });

    /// <summary>Actualiza los datos editables de una licitación no cerrada.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LicitacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] LicitacionEntrada entrada,
        CancellationToken cancelacion) =>
        (await _servicio.ActualizarAsync(id, entrada, cancelacion)).AResultado(HttpContext);

    /// <summary>
    /// Aplica una transición del ciclo de estados: Borrador → Publicada,
    /// Borrador → Cerrada o Publicada → Cerrada.
    /// </summary>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(LicitacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CambiarEstado(
        Guid id,
        [FromBody] CambioEstadoSolicitud solicitud,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        return (await _servicio.CambiarEstadoAsync(id, solicitud.Transicion, cancelacion))
            .AResultado(HttpContext);
    }

    /// <summary>Elimina una licitación, con borrado lógico si tiene ofertas.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion) =>
        (await _servicio.EliminarAsync(id, cancelacion)).ASinContenido(HttpContext);

    /// <summary>Lista las ofertas presentadas en la licitación.</summary>
    [HttpGet("{id:guid}/ofertas")]
    [ProducesResponseType(typeof(PaginaResultado<OfertaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarOfertas(
        Guid id,
        [FromQuery] ParametrosConsulta consulta,
        CancellationToken cancelacion) =>
        Ok(await _ofertas.ListarAsync(consulta, id, null, cancelacion));

    /// <summary>Registra una oferta en la licitación.</summary>
    [HttpPost("{id:guid}/ofertas")]
    [ProducesResponseType(typeof(OfertaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarOferta(
        Guid id,
        [FromBody] OfertaEntrada entrada,
        CancellationToken cancelacion) =>
        (await _ofertas.RegistrarAsync(id, entrada, cancelacion))
            .ACreado(HttpContext, nameof(OfertasController.ObtenerOferta), dto => new { id = dto.Id });

    /// <summary>
    /// Devuelve la mejor oferta de la licitación con su clasificación de ahorro y
    /// el aprobador que corresponde al monto.
    /// </summary>
    [HttpGet("{id:guid}/mejor-oferta")]
    [ProducesResponseType(typeof(MejorOfertaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MejorOferta(Guid id, CancellationToken cancelacion) =>
        (await _servicio.ObtenerMejorOfertaAsync(id, cancelacion)).AResultado(HttpContext);
}
