using Licitaciones.Application.TiposCambio;
using Licitaciones.Web.Infraestructura;
using Licitaciones.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>Pantallas del tipo de cambio CRC/USD (HU-09 y HU-10).</summary>
public sealed class TiposCambioController : ControladorBase
{
    private readonly TipoCambioServicio _servicio;

    public TiposCambioController(TipoCambioServicio servicio) => _servicio = servicio;

    /// <summary>Listado de tipos de cambio, del más vigente al más antiguo.</summary>
    public async Task<IActionResult> Index(CancellationToken cancelacion) =>
        View(await _servicio.ListarAsync(cancelacion));

    [HttpGet]
    public IActionResult Crear() => View("Formulario", new TipoCambioFormulario());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(TipoCambioFormulario formulario, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ModelState.IsValid)
        {
            return View("Formulario", formulario);
        }

        var resultado = await _servicio.CrearAsync(formulario.AEntrada(), cancelacion);
        if (!resultado.EsExitoso)
        {
            RegistrarError(resultado.Error!, nameof(formulario.CRCporUSD));
            return View("Formulario", formulario);
        }

        AvisarExito(resultado.Valor!.Activo
            ? "Tipo de cambio registrado y activado."
            : "Tipo de cambio registrado. Actívelo para usarlo en la conversión.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _servicio.ObtenerAsync(id, cancelacion);

        return resultado.EsExitoso
            ? View("Formulario", TipoCambioFormulario.Desde(resultado.Valor!))
            : NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, TipoCambioFormulario formulario, CancellationToken cancelacion)
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
            RegistrarError(resultado.Error!, nameof(formulario.CRCporUSD));
            return View("Formulario", formulario);
        }

        AvisarExito("Tipo de cambio actualizado.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Marca un tipo de cambio como el activo y desactiva el anterior, en una
    /// sola transacción (enunciado §8.8).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _servicio.ActivarAsync(id, cancelacion);

        if (resultado.EsExitoso)
        {
            AvisarExito("Tipo de cambio activado.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _servicio.EliminarAsync(id, cancelacion);

        if (resultado.EsExitoso)
        {
            AvisarExito("Tipo de cambio eliminado.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction(nameof(Index));
    }
}
