using Licitaciones.Application.Aprobaciones;
using Licitaciones.Web.Infraestructura;
using Licitaciones.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>Pantallas de los niveles de aprobación (HU-08).</summary>
public sealed class NivelesAprobacionController : ControladorBase
{
    private readonly NivelAprobacionServicio _servicio;

    public NivelesAprobacionController(NivelAprobacionServicio servicio) => _servicio = servicio;

    /// <summary>
    /// Listado completo de rangos.
    /// </summary>
    /// <remarks>
    /// No se pagina: son pocas filas por definición, y verlas juntas es lo que
    /// permite comprobar de un vistazo que no se traslapan ni dejan huecos.
    /// </remarks>
    public async Task<IActionResult> Index(CancellationToken cancelacion) =>
        View(await _servicio.ListarAsync(cancelacion));

    [HttpGet]
    public IActionResult Crear() => View("Formulario", new NivelAprobacionFormulario());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(NivelAprobacionFormulario formulario, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ModelState.IsValid)
        {
            return View("Formulario", formulario);
        }

        var resultado = await _servicio.CrearAsync(formulario.AEntrada(), cancelacion);
        if (!resultado.EsExitoso)
        {
            RegistrarError(resultado.Error!, nameof(formulario.MontoMinimoCRC));
            return View("Formulario", formulario);
        }

        AvisarExito($"Nivel de aprobación «{resultado.Valor!.Aprobador}» creado.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _servicio.ObtenerAsync(id, cancelacion);

        return resultado.EsExitoso
            ? View("Formulario", NivelAprobacionFormulario.Desde(resultado.Valor!))
            : NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid id,
        NivelAprobacionFormulario formulario,
        CancellationToken cancelacion)
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
            RegistrarError(resultado.Error!, nameof(formulario.MontoMinimoCRC));
            return View("Formulario", formulario);
        }

        AvisarExito($"Nivel de aprobación «{resultado.Valor!.Aprobador}» actualizado.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _servicio.EliminarAsync(id, cancelacion);

        if (resultado.EsExitoso)
        {
            AvisarExito("Nivel de aprobación eliminado.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }
}
