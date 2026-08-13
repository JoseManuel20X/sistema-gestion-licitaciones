using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Licitaciones.Domain.Common;

/// <summary>
/// Reglas de normalización de texto usadas para comparar unicidad (enunciado §8.3).
/// </summary>
/// <remarks>
/// El valor normalizado se persiste en su propia columna con índice único. Así la
/// unicidad queda garantizada por PostgreSQL y no depende de que toda consulta
/// recuerde aplicar la misma transformación.
/// </remarks>
public static partial class Normalizador
{
    /// <summary>Caracteres admitidos en el nombre de un proveedor (enunciado §8.4).</summary>
    [GeneratedRegex(@"^[\p{L}\p{N} .,\(\)]+$")]
    private static partial Regex NombreProveedorPermitido();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();

    /// <summary>
    /// Normaliza el nombre de un proveedor: recorta espacios laterales, colapsa
    /// espacios repetidos, aplica normalización Unicode NFC y pasa a mayúsculas
    /// invariantes.
    /// </summary>
    /// <remarks>
    /// No se eliminan diacríticos de forma deliberada: "Mas" y "Más" son nombres
    /// distintos y tratarlos como duplicados impediría registrar proveedores
    /// legítimos. El enunciado solo exige ignorar mayúsculas, espacios y forma
    /// Unicode.
    /// </remarks>
    public static string NormalizarNombreProveedor(string nombre)
    {
        ArgumentNullException.ThrowIfNull(nombre);

        var colapsado = EspaciosRepetidos().Replace(nombre.Trim(), " ");
        return colapsado.Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    /// <summary>
    /// Normaliza el código de una licitación ignorando espacios laterales y
    /// diferencias entre mayúsculas y minúsculas.
    /// </summary>
    public static string NormalizarCodigoLicitacion(string codigo)
    {
        ArgumentNullException.ThrowIfNull(codigo);

        return codigo.Trim().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    /// <summary>Indica si el nombre solo contiene los caracteres permitidos.</summary>
    public static bool EsNombreProveedorValido(string nombre) =>
        !string.IsNullOrWhiteSpace(nombre) && NombreProveedorPermitido().IsMatch(nombre.Trim());

    /// <summary>Recorta y colapsa espacios conservando el texto tal como lo escribió el usuario.</summary>
    public static string LimpiarEspacios(string valor)
    {
        ArgumentNullException.ThrowIfNull(valor);

        return EspaciosRepetidos().Replace(valor.Trim(), " ");
    }

    /// <summary>Cultura de presentación para montos en colones costarricenses.</summary>
    public static CultureInfo CulturaCostaRica { get; } = CultureInfo.GetCultureInfo("es-CR");
}
