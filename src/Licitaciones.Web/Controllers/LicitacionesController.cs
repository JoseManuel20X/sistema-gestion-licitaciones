using Licitaciones.Application.Common;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Web.Infraestructura;
using Licitaciones.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>Pantallas de licitaciones (HU-03, HU-04 y HU-07).</summary>
public sealed class LicitacionesController : ControladorBase
{
    private readonly LicitacionServicio _servicio;
    private readonly OfertaServicio _ofertas;

    public LicitacionesController(LicitacionServicio servicio, OfertaServicio ofertas)
    {
        _servicio = servicio;
        _ofertas = ofertas;
    }

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

    /// <summary>Detalle con la mejor oferta y el aprobador aplicable.</summary>
    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancelacion)
    {
        var licitacion = await _servicio.ObtenerAsync(id, cancelacion);
        if (!licitacion.EsExitoso)
        {
            return NotFound();
        }

        var mejorOferta = await _servicio.ObtenerMejorOfertaAsync(id, cancelacion);
        ViewBag.MejorOferta = mejorOferta.Valor;

        // Las ofertas se listan completas: una licitación tiene pocas por
        // definición, ya que cada proveedor solo puede presentar una.
        var ofertas = await _ofertas.ListarAsync(
            new ParametrosConsulta { TamanoPagina = ParametrosConsulta.TamanoPaginaMaximo },
            id,
            null,
            cancelacion);

        ViewBag.Ofertas = ofertas.Elementos;

        return View(licitacion.Valor);
    }

    [HttpGet]
    public IActionResult Crear() => View("Formulario", new LicitacionFormulario());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(LicitacionFormulario formulario, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ModelState.IsValid)
        {
            return View("Formulario", formulario);
        }

        var resultado = await _servicio.CrearAsync(formulario.AEntrada(), cancelacion);
        if (!resultado.EsExitoso)
        {
            RegistrarError(resultado.Error!, nameof(formulario.Codigo));
            return View("Formulario", formulario);
        }

        AvisarExito($"Licitación «{resultado.Valor!.Codigo}» creada en estado Borrador.");
        return RedirectToAction(nameof(Detalle), new { id = resultado.Valor.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _servicio.ObtenerAsync(id, cancelacion);

        return resultado.EsExitoso
            ? View("Formulario", LicitacionFormulario.Desde(resultado.Valor!))
            : NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, LicitacionFormulario formulario, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        formulario.Id = id;

        if (!ModelState.IsValid)
        {
            return View("Formulario", formulario);
        }

        var resultado = await _servicio.ActualizarAsync(id, formulario.AEntrada(), cancelacion);
        if (!resultado.EsExitoso)
        {
            RegistrarError(resultado.Error!, nameof(formulario.Codigo));
            return View("Formulario", formulario);
        }

        AvisarExito($"Licitación «{resultado.Valor!.Codigo}» actualizada.");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    /// <summary>
    /// Aplica una transición del ciclo de estados. Las prohibidas las rechaza el
    /// dominio; aquí solo se muestra el motivo (enunciado §8.1).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(
        Guid id,
        TransicionLicitacion transicion,
        CancellationToken cancelacion)
    {
        var resultado = await _servicio.CambiarEstadoAsync(id, transicion, cancelacion);

        if (resultado.EsExitoso)
        {
            AvisarExito($"La licitación pasó a estado {resultado.Valor!.Estado}.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _servicio.EliminarAsync(id, cancelacion);

        if (resultado.EsExitoso)
        {
            AvisarExito("Licitación eliminada.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }
}
