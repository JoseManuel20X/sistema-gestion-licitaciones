using Licitaciones.Application.Common;
using Licitaciones.Application.Proveedores;
using Licitaciones.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>Pantallas de proveedores (HU-01 y HU-02).</summary>
public sealed class ProveedoresController : Controller
{
    private readonly ProveedorServicio _servicio;

    public ProveedoresController(ProveedorServicio servicio) => _servicio = servicio;

    /// <summary>Listado con paginación, filtro por nombre y ordenamiento.</summary>
    public async Task<IActionResult> Index(
        int pagina = 1,
        string? filtro = null,
        string? ordenarPor = null,
        bool descendente = false,
        CancellationToken cancelacion = default)
    {
        var consulta = new ParametrosConsulta
        {
            Pagina = pagina,
            Filtro = filtro,
            OrdenarPor = ordenarPor,
            Descendente = descendente,
        };

        var resultado = await _servicio.ListarAsync(consulta, cancelacion);

        ViewBag.Paginacion = PaginacionViewModel.Desde(resultado, consulta);
        ViewBag.Consulta = consulta;

        return View(resultado.Elementos);
    }
}
