using Licitaciones.Application.Common;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Licitaciones.Web.Infraestructura;
using Licitaciones.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Licitaciones.Web.Controllers;

/// <summary>Pantallas de ofertas (HU-05 y HU-06).</summary>
public sealed class OfertasController : ControladorBase
{
    private readonly OfertaServicio _servicio;
    private readonly LicitacionServicio _licitaciones;
    private readonly ProveedorServicio _proveedores;

    public OfertasController(
        OfertaServicio servicio,
        LicitacionServicio licitaciones,
        ProveedorServicio proveedores)
    {
        _servicio = servicio;
        _licitaciones = licitaciones;
        _proveedores = proveedores;
    }

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

    /// <summary>
    /// Formulario de registro de una oferta dentro de una licitación.
    /// </summary>
    /// <remarks>
    /// Una oferta solo existe dentro de una licitación, así que el alta siempre
    /// parte de una concreta y no de un formulario suelto.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Crear(Guid licitacionId, CancellationToken cancelacion)
    {
        var licitacion = await _licitaciones.ObtenerAsync(licitacionId, cancelacion);
        if (!licitacion.EsExitoso)
        {
            return NotFound();
        }

        if (!licitacion.Valor!.AceptaOfertas)
        {
            AvisarError("La licitación no admite ofertas: no está publicada o ya alcanzó su fecha de cierre.");
            return RedirectToAction("Detalle", "Licitaciones", new { id = licitacionId });
        }

        await CargarProveedoresAsync(cancelacion);

        return View("Formulario", new OfertaFormulario
        {
            LicitacionId = licitacionId,
            CodigoLicitacion = licitacion.Valor.Codigo,
            PresupuestoCRC = licitacion.Valor.PresupuestoEstimadoCRC,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(OfertaFormulario formulario, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ModelState.IsValid)
        {
            await CargarProveedoresAsync(cancelacion);
            return View("Formulario", formulario);
        }

        var resultado = await _servicio.RegistrarAsync(
            formulario.LicitacionId,
            formulario.AEntrada(),
            cancelacion);

        if (!resultado.EsExitoso)
        {
            RegistrarError(resultado.Error!, nameof(formulario.MontoOfertadoCRC));
            await CargarProveedoresAsync(cancelacion);
            return View("Formulario", formulario);
        }

        AvisarExito("Oferta registrada.");
        return RedirectToAction("Detalle", "Licitaciones", new { id = formulario.LicitacionId });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        var oferta = await _servicio.ObtenerAsync(id, cancelacion);
        if (!oferta.EsExitoso)
        {
            return NotFound();
        }

        var licitacion = await _licitaciones.ObtenerAsync(oferta.Valor!.LicitacionId, cancelacion);

        await CargarProveedoresAsync(cancelacion);

        return View("Formulario", new OfertaFormulario
        {
            Id = oferta.Valor.Id,
            LicitacionId = oferta.Valor.LicitacionId,
            CodigoLicitacion = licitacion.Valor?.Codigo,
            PresupuestoCRC = licitacion.Valor?.PresupuestoEstimadoCRC ?? 0m,
            ProveedorId = oferta.Valor.ProveedorId,
            MontoOfertadoCRC = oferta.Valor.MontoOfertadoCRC,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, OfertaFormulario formulario, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        formulario.Id = id;

        if (!ModelState.IsValid)
        {
            await CargarProveedoresAsync(cancelacion);
            return View("Formulario", formulario);
        }

        var resultado = await _servicio.ActualizarAsync(id, formulario.AActualizacion(), cancelacion);
        if (!resultado.EsExitoso)
        {
            RegistrarError(resultado.Error!, nameof(formulario.MontoOfertadoCRC));
            await CargarProveedoresAsync(cancelacion);
            return View("Formulario", formulario);
        }

        AvisarExito("Oferta actualizada.");
        return RedirectToAction("Detalle", "Licitaciones", new { id = formulario.LicitacionId });
    }

    /// <summary>
    /// Elimina una oferta. Solo procede mientras la licitación siga publicada y
    /// vigente: las de licitaciones cerradas se conservan como evidencia (§8.9).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id, Guid licitacionId, CancellationToken cancelacion)
    {
        var resultado = await _servicio.EliminarAsync(id, cancelacion);

        if (resultado.EsExitoso)
        {
            AvisarExito("Oferta eliminada.");
        }
        else
        {
            AvisarError(resultado.Error!.Mensaje);
        }

        return RedirectToAction("Detalle", "Licitaciones", new { id = licitacionId });
    }

    /// <summary>
    /// Carga los proveedores para el selector del formulario.
    /// </summary>
    /// <remarks>
    /// Se pide el tamaño máximo de página: el selector debe ofrecerlos todos, y
    /// paginar un desplegable no tendría sentido para la persona usuaria.
    /// </remarks>
    private async Task CargarProveedoresAsync(CancellationToken cancelacion)
    {
        var proveedores = await _proveedores.ListarAsync(
            new ParametrosConsulta { TamanoPagina = ParametrosConsulta.TamanoPaginaMaximo },
            cancelacion);

        ViewBag.Proveedores = proveedores.Elementos
            .Select(p => new SelectListItem(p.Nombre, p.Id.ToString()))
            .ToList();
    }
}
