using Licitaciones.Application.TiposCambio;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>Pantallas del tipo de cambio CRC/USD (HU-09 y HU-10).</summary>
public sealed class TiposCambioController : Controller
{
    private readonly TipoCambioServicio _servicio;

    public TiposCambioController(TipoCambioServicio servicio) => _servicio = servicio;

    /// <summary>Listado de tipos de cambio, del más vigente al más antiguo.</summary>
    public async Task<IActionResult> Index(CancellationToken cancelacion) =>
        View(await _servicio.ListarAsync(cancelacion));
}
