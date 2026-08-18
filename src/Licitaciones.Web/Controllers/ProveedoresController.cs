using Licitaciones.Application.Common;
using Licitaciones.Application.Proveedores;
using Licitaciones.Web.Infraestructura;
using Licitaciones.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>Pantallas de proveedores (HU-01 y HU-02).</summary>
public sealed class ProveedoresController : ControladorBase
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

    [HttpGet]
    public IActionResult Crear() => View("Formulario", new ProveedorFormulario());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProveedorFormulario formulario, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ModelState.IsValid)
        {
            return View("Formulario", formulario);
        }

        var resultado = await _servicio.CrearAsync(formulario.AEntrada(), cancelacion);
        if (!resultado.EsExitoso)
        {
            RegistrarError(resultado.Error!, nameof(formulario.Nombre));
            return View("Formulario", formulario);
        }

        AvisarExito($"Proveedor «{resultado.Valor!.Nombre}» registrado.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _servicio.ObtenerAsync(id, cancelacion);

        return resultado.EsExitoso
            ? View("Formulario", ProveedorFormulario.Desde(resultado.Valor!))
            : NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, ProveedorFormulario formulario, CancellationToken cancelacion)
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
            RegistrarError(resultado.Error!, nameof(formulario.Nombre));
            return View("Formulario", formulario);
        }

        AvisarExito($"Proveedor «{resultado.Valor!.Nombre}» actualizado.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Elimina el proveedor. Si tiene ofertas se aplica borrado lógico para
    /// conservarlas como evidencia (enunciado §8.9).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _servicio.EliminarAsync(id, cancelacion);

        if (resultado.EsExitoso)
        {
            AvisarExito("Proveedor eliminado.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }
}
