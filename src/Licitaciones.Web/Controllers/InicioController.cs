using System.Diagnostics;
using Licitaciones.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>Página inicial y manejo de errores (HU-11).</summary>
public sealed class InicioController : Controller
{
    /// <summary>
    /// Landing page: explica el propósito del sistema y el flujo completo de una
    /// licitación (enunciado §5.1).
    /// </summary>
    public IActionResult Index() => View();

    /// <summary>Página de error con el identificador de correlación de la petición.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
        });
}
