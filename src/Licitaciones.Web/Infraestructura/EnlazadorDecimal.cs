using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Licitaciones.Web.Infraestructura;

/// <summary>
/// Enlaza valores decimales aceptando tanto el punto como la coma decimal.
/// </summary>
/// <remarks>
/// La aplicación presenta los montos con cultura es-CR, que usa coma decimal.
/// Pero un <c>&lt;input type="number"&gt;</c> envía siempre el valor en formato
/// invariante, con punto, sea cual sea el idioma del navegador. Con el enlazador
/// que trae ASP.NET Core, «999999.99» se rechazaba con «no es válido» y ninguna
/// persona podía registrar un monto con céntimos desde el formulario.
///
/// Se intenta primero el formato invariante, que es el que envían los controles
/// numéricos, y se recurre a la cultura de la petición para los campos de texto
/// donde alguien pudo escribir la coma a mano.
/// </remarks>
public sealed class EnlazadorDecimal : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valores = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valores == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valores);

        var texto = valores.FirstValue;

        if (string.IsNullOrWhiteSpace(texto))
        {
            // Un campo vacío es válido cuando el modelo admite nulo; si es
            // obligatorio lo señalará la anotación [Required].
            if (Nullable.GetUnderlyingType(bindingContext.ModelType) is not null)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }

            return Task.CompletedTask;
        }

        const NumberStyles Estilos = NumberStyles.Float | NumberStyles.AllowThousands;

        if (decimal.TryParse(texto, Estilos, CultureInfo.InvariantCulture, out var invariante))
        {
            bindingContext.Result = ModelBindingResult.Success(invariante);
            return Task.CompletedTask;
        }

        if (decimal.TryParse(texto, Estilos, CultureInfo.CurrentCulture, out var local))
        {
            bindingContext.Result = ModelBindingResult.Success(local);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            $"«{texto}» no es un monto válido. Use dígitos y, si necesita céntimos, una coma o un punto decimal.");

        return Task.CompletedTask;
    }
}

/// <summary>Aplica <see cref="EnlazadorDecimal"/> a todos los decimales del modelo.</summary>
public sealed class ProveedorEnlazadorDecimal : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tipo = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;

        return tipo == typeof(decimal) ? new EnlazadorDecimal() : null;
    }
}
