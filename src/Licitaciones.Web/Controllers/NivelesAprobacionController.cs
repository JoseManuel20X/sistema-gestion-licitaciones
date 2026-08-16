using Licitaciones.Application.Aprobaciones;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>Pantallas de los niveles de aprobación (HU-08).</summary>
public sealed class NivelesAprobacionController : Controller
{
    private readonly NivelAprobacionServicio _servicio;

    public NivelesAprobacionController(NivelAprobacionServicio servicio) => _servicio = servicio;

    /// <summary>
    /// Listado completo de rangos.
    /// </summary>
    /// <remarks>
    /// No se pagina: la tabla de aprobación tiene unas pocas filas por
    /// definición, y verla entera de una vez es justamente lo que permite
    /// comprobar que los rangos no dejan huecos ni se traslapan.
    /// </remarks>
    public async Task<IActionResult> Index(CancellationToken cancelacion) =>
        View(await _servicio.ListarAsync(cancelacion));
}
