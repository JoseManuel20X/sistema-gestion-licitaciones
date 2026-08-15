namespace Licitaciones.Application.Common;

/// <summary>
/// Parámetros de paginación, filtrado y ordenamiento comunes a todos los listados
/// (enunciado §9 y §10.2).
/// </summary>
public sealed record ParametrosConsulta
{
    /// <summary>Tamaño máximo admitido, para que un cliente no pueda pedir la tabla completa.</summary>
    public const int TamanoPaginaMaximo = 100;

    public const int TamanoPaginaPorDefecto = 20;

    private readonly int _pagina = 1;
    private readonly int _tamanoPagina = TamanoPaginaPorDefecto;

    /// <summary>Número de página, empezando en 1.</summary>
    public int Pagina
    {
        get => _pagina;
        init => _pagina = value < 1 ? 1 : value;
    }

    /// <summary>Cantidad de elementos por página, acotada a <see cref="TamanoPaginaMaximo"/>.</summary>
    public int TamanoPagina
    {
        get => _tamanoPagina;
        init => _tamanoPagina = value switch
        {
            < 1 => TamanoPaginaPorDefecto,
            > TamanoPaginaMaximo => TamanoPaginaMaximo,
            _ => value,
        };
    }

    /// <summary>Texto de búsqueda libre; su significado depende del listado.</summary>
    public string? Filtro { get; init; }

    /// <summary>Campo por el que ordenar; cada listado admite un conjunto propio.</summary>
    public string? OrdenarPor { get; init; }

    /// <summary>Indica si el ordenamiento es descendente.</summary>
    public bool Descendente { get; init; }

    /// <summary>Elementos que deben omitirse para llegar a la página solicitada.</summary>
    public int Omitir => (Pagina - 1) * TamanoPagina;
}

/// <summary>Página de resultados con la información necesaria para navegar el listado.</summary>
/// <param name="Elementos">Elementos de la página actual.</param>
/// <param name="Pagina">Número de página devuelta.</param>
/// <param name="TamanoPagina">Cantidad de elementos por página.</param>
/// <param name="TotalElementos">Total de elementos que cumplen el filtro.</param>
public sealed record PaginaResultado<T>(
    IReadOnlyList<T> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos)
{
    /// <summary>Cantidad total de páginas disponibles.</summary>
    public int TotalPaginas => TamanoPagina == 0 ? 0 : (int)Math.Ceiling(TotalElementos / (double)TamanoPagina);

    public bool TienePaginaAnterior => Pagina > 1;

    public bool TienePaginaSiguiente => Pagina < TotalPaginas;

    /// <summary>Proyecta los elementos a otro tipo conservando los datos de paginación.</summary>
    public PaginaResultado<TDestino> Proyectar<TDestino>(Func<T, TDestino> proyeccion)
    {
        ArgumentNullException.ThrowIfNull(proyeccion);

        return new PaginaResultado<TDestino>(
            [.. Elementos.Select(proyeccion)],
            Pagina,
            TamanoPagina,
            TotalElementos);
    }
}
