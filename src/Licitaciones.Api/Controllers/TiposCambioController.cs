using Licitaciones.Api.Http;
using Licitaciones.Application.TiposCambio;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controllers;

/// <summary>Operaciones sobre el tipo de cambio CRC/USD (HU-09 y HU-10).</summary>
[ApiController]
[Route("api/v1/tipos-cambio")]
[Produces("application/json")]
public sealed class TiposCambioController : ControllerBase
{
    private readonly TipoCambioServicio _servicio;

    public TiposCambioController(TipoCambioServicio servicio) => _servicio = servicio;

    /// <summary>Lista los tipos de cambio, del más vigente al más antiguo.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TipoCambioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancelacion) =>
        Ok(await _servicio.ListarAsync(cancelacion));

    /// <summary>Devuelve el tipo de cambio vigente para la operación ordinaria.</summary>
    [HttpGet("activo")]
    [ProducesResponseType(typeof(TipoCambioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ObtenerActivo(CancellationToken cancelacion) =>
        (await _servicio.ObtenerActivoAsync(cancelacion)).AResultado(HttpContext);

    /// <summary>
    /// Convierte un monto en colones a dólares con el tipo de cambio activo.
    /// </summary>
    /// <remarks>
    /// La conversión es una representación calculada: los valores oficiales se
    /// almacenan solo en colones y no se modifican (enunciado §8.8).
    /// </remarks>
    [HttpGet("convertir")]
    [ProducesResponseType(typeof(MontoConvertidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Convertir(
        [FromQuery] decimal montoCRC,
        CancellationToken cancelacion) =>
        (await _servicio.ConvertirAsync(montoCRC, cancelacion)).AResultado(HttpContext);

    /// <summary>Consulta un tipo de cambio por su identificador.</summary>
    [HttpGet("{id:guid}", Name = nameof(ObtenerTipoCambio))]
    [ProducesResponseType(typeof(TipoCambioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerTipoCambio(Guid id, CancellationToken cancelacion) =>
        (await _servicio.ObtenerAsync(id, cancelacion)).AResultado(HttpContext);

    /// <summary>Registra un tipo de cambio. El primero queda activo automáticamente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TipoCambioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(
        [FromBody] TipoCambioEntrada entrada,
        CancellationToken cancelacion) =>
        (await _servicio.CrearAsync(entrada, cancelacion))
            .ACreado(HttpContext, nameof(ObtenerTipoCambio), dto => new { id = dto.Id });

    /// <summary>Actualiza el valor y la fecha de vigencia.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TipoCambioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] TipoCambioEntrada entrada,
        CancellationToken cancelacion) =>
        (await _servicio.ActualizarAsync(id, entrada, cancelacion)).AResultado(HttpContext);

    /// <summary>
    /// Marca un tipo de cambio como el activo y desactiva el anterior, en una
    /// sola transacción.
    /// </summary>
    [HttpPatch("{id:guid}/activar")]
    [ProducesResponseType(typeof(TipoCambioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activar(Guid id, CancellationToken cancelacion) =>
        (await _servicio.ActivarAsync(id, cancelacion)).AResultado(HttpContext);

    /// <summary>Elimina un tipo de cambio que no esté activo.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion) =>
        (await _servicio.EliminarAsync(id, cancelacion)).ASinContenido(HttpContext);
}
