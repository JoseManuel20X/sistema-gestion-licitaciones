using Licitaciones.Application.Common;
using Licitaciones.Application.Ofertas;
using Licitaciones.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>Pantallas de ofertas (HU-05 y HU-06).</summary>
public sealed class OfertasController : Controller
{
    private readonly OfertaServicio _servicio;

    public OfertasController(OfertaServicio servicio) => _servicio = servicio;

    /// <summary>Listado con paginación y filtro por licitación y proveedor.</summary>
    public async Task<IActionResult> Index(
        int pagina = 1,
        Guid? licitacionId = null,
        Guid? proveedorId = null,
        string? ordenarPor = null,
        bool descendente = false,
        CancellationToken cancelacion = default)
    {
        var consulta = new ParametrosConsulta
        {
            Pagina = pagina,
            OrdenarPor = ordenarPor,
            Descendente = descendente,
        };

        var resultado = await _servicio.ListarAsync(consulta, licitacionId, proveedorId, cancelacion);

        ViewBag.Paginacion = PaginacionViewModel.Desde(resultado, consulta);
        ViewBag.LicitacionId = licitacionId;
        ViewBag.ProveedorId = proveedorId;

        return View(resultado.Elementos);
    }
}
