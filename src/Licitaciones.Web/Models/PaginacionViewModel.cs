using Licitaciones.Application.Common;

namespace Licitaciones.Web.Models;

/// <summary>
/// Datos que necesita el control de paginación para dibujarse y conservar el
/// filtro y el orden al cambiar de página.
/// </summary>
/// <param name="Pagina">Página que se está mostrando.</param>
/// <param name="TotalPaginas">Cantidad total de páginas.</param>
/// <param name="TotalElementos">Cantidad total de elementos que cumplen el filtro.</param>
/// <param name="Accion">Acción del controlador a la que apuntan los enlaces.</param>
/// <param name="Filtro">Filtro vigente, que debe conservarse al navegar.</param>
/// <param name="OrdenarPor">Campo de ordenamiento vigente.</param>
/// <param name="Descendente">Sentido del ordenamiento vigente.</param>
public sealed record PaginacionViewModel(
    int Pagina,
    int TotalPaginas,
    int TotalElementos,
    string Accion,
    string? Filtro,
    string? OrdenarPor,
    bool Descendente)
{
    /// <summary>Construye el modelo a partir de una página de resultados.</summary>
    public static PaginacionViewModel Desde<T>(
        PaginaResultado<T> pagina,
        ParametrosConsulta consulta,
        string accion = "Index")
    {
        ArgumentNullException.ThrowIfNull(pagina);
        ArgumentNullException.ThrowIfNull(consulta);

        return new PaginacionViewModel(
            pagina.Pagina,
            pagina.TotalPaginas,
            pagina.TotalElementos,
            accion,
            consulta.Filtro,
            consulta.OrdenarPor,
            consulta.Descendente);
    }
}
