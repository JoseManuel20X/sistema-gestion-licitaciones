using Licitaciones.Application.Common;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>Pantallas de licitaciones (HU-03, HU-04 y HU-07).</summary>
public sealed class LicitacionesController : Controller
{
    private readonly LicitacionServicio _servicio;

    public LicitacionesController(LicitacionServicio servicio) => _servicio = servicio;

    /// <summary>Listado con paginación, filtro por código o título y ordenamiento.</summary>
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

    /// <summary>Detalle de la licitación con su mejor oferta y el aprobador aplicable.</summary>
    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancelacion)
    {
        var licitacion = await _servicio.ObtenerAsync(id, cancelacion);
        if (!licitacion.EsExitoso)
        {
            return NotFound();
        }

        var mejorOferta = await _servicio.ObtenerMejorOfertaAsync(id, cancelacion);
        ViewBag.MejorOferta = mejorOferta.Valor;

        return View(licitacion.Valor);
    }
}
