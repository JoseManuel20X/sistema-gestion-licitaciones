using Licitaciones.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Infraestructura;

/// <summary>
/// Base de los controladores de pantallas, con la traducción de resultados a la
/// interfaz.
/// </summary>
/// <remarks>
/// Concentrar aquí la traducción evita que cada controlador decida por su cuenta
/// dónde mostrar un error, y garantiza que la misma regla de negocio se presente
/// siempre igual (enunciado §9).
/// </remarks>
public abstract class ControladorBase : Controller
{
    /// <summary>Clave de TempData para el mensaje de éxito.</summary>
    protected const string ClaveExito = "MensajeExito";

    /// <summary>Clave de TempData para el mensaje de error.</summary>
    protected const string ClaveError = "MensajeError";

    /// <summary>
    /// Registra el mensaje que se mostrará tras la redirección.
    /// </summary>
    /// <remarks>
    /// Las altas y bajas redirigen tras guardar (patrón POST-Redirect-GET) para
    /// que recargar la página no repita la operación. TempData sobrevive
    /// exactamente a esa redirección.
    /// </remarks>
    protected void AvisarExito(string mensaje) => TempData[ClaveExito] = mensaje;

    protected void AvisarError(string mensaje) => TempData[ClaveError] = mensaje;

    /// <summary>
    /// Coloca el error de un caso de uso donde la persona pueda entenderlo.
    /// </summary>
    /// <param name="error">Error devuelto por el caso de uso.</param>
    /// <param name="campo">
    /// Campo del formulario al que corresponde, si lo hay. Un error de validación
    /// se muestra junto a su campo (§9); una regla de negocio o un conflicto se
    /// muestran como resumen, porque no pertenecen a un campo concreto.
    /// </param>
    protected void RegistrarError(ErrorAplicacion error, string? campo = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        var destino = error.Tipo is TipoError.Validacion or TipoError.Conflicto && campo is not null
            ? campo
            : string.Empty;

        ModelState.AddModelError(destino, error.Mensaje);
    }
}
