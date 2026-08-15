using Licitaciones.Api.Http;
using Licitaciones.Application.Common;
using Licitaciones.Application.Proveedores;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controllers;

/// <summary>Operaciones sobre proveedores (HU-01 y HU-02).</summary>
[ApiController]
[Route("api/v1/proveedores")]
[Produces("application/json")]
public sealed class ProveedoresController : ControllerBase
{
    private readonly ProveedorServicio _servicio;

    public ProveedoresController(ProveedorServicio servicio) => _servicio = servicio;

    /// <summary>Lista proveedores con paginación, filtro por nombre y ordenamiento.</summary>
    /// <param name="consulta">Página, tamaño, filtro y criterio de orden.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaResultado<ProveedorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ParametrosConsulta consulta,
        CancellationToken cancelacion) =>
        Ok(await _servicio.ListarAsync(consulta, cancelacion));

    /// <summary>Consulta un proveedor por su identificador.</summary>
    [HttpGet("{id:guid}", Name = nameof(ObtenerProveedor))]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerProveedor(Guid id, CancellationToken cancelacion) =>
        (await _servicio.ObtenerAsync(id, cancelacion)).AResultado(HttpContext);

    /// <summary>Registra un proveedor con nombre único normalizado.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] ProveedorEntrada entrada,
        CancellationToken cancelacion) =>
        (await _servicio.CrearAsync(entrada, cancelacion))
            .ACreado(HttpContext, nameof(ObtenerProveedor), dto => new { id = dto.Id });

    /// <summary>Cambia el nombre de un proveedor existente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        Guid id,
        [FromBody] ProveedorEntrada entrada,
        CancellationToken cancelacion) =>
        (await _servicio.ActualizarAsync(id, entrada, cancelacion)).AResultado(HttpContext);

    /// <summary>
    /// Elimina un proveedor. Si tiene ofertas se aplica borrado lógico para
    /// conservarlas como evidencia.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion) =>
        (await _servicio.EliminarAsync(id, cancelacion)).ASinContenido(HttpContext);
}
