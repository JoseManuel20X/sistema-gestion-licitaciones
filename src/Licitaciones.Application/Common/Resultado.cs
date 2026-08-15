namespace Licitaciones.Application.Common;

/// <summary>Naturaleza del error, que determina el código HTTP con que responde la API.</summary>
public enum TipoError
{
    /// <summary>Datos mal formados o ausentes. Se traduce a 400.</summary>
    Validacion,

    /// <summary>Datos bien formados que infringen una regla de negocio. Se traduce a 422.</summary>
    ReglaNegocio,

    /// <summary>Choque con el estado actual, como un duplicado. Se traduce a 409.</summary>
    Conflicto,

    /// <summary>El recurso solicitado no existe. Se traduce a 404.</summary>
    NoEncontrado,

    /// <summary>Otro proceso modificó el registro primero. Se traduce a 409.</summary>
    Concurrencia,
}

/// <summary>Error de un caso de uso, con código estable y mensaje seguro para el cliente.</summary>
/// <param name="Codigo">Código estable definido en <c>CodigosError</c>.</param>
/// <param name="Mensaje">Texto comprensible, sin detalles internos del sistema.</param>
/// <param name="Tipo">Naturaleza del error.</param>
public sealed record ErrorAplicacion(string Codigo, string Mensaje, TipoError Tipo);

/// <summary>
/// Resultado de un caso de uso.
/// </summary>
/// <remarks>
/// Los casos de uso devuelven éxito o error en lugar de propagar excepciones:
/// el fallo esperado (un duplicado, un monto inválido) es parte del contrato y
/// obliga a quien invoca a tratarlo, en vez de depender de un <c>catch</c>.
/// </remarks>
public class Resultado
{
    protected Resultado(ErrorAplicacion? error) => Error = error;

    /// <summary>Error ocurrido, o <c>null</c> si la operación fue exitosa.</summary>
    public ErrorAplicacion? Error { get; }

    /// <summary>Indica si la operación se completó correctamente.</summary>
    public bool EsExitoso => Error is null;

    public static Resultado Exito() => new(null);

    public static Resultado Fallo(ErrorAplicacion error) => new(error);

    public static Resultado Fallo(string codigo, string mensaje, TipoError tipo) =>
        new(new ErrorAplicacion(codigo, mensaje, tipo));
}

/// <summary>Resultado de un caso de uso que devuelve un valor.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1000:No declarar miembros estáticos en tipos genéricos",
    Justification = "Las fábricas estáticas son la forma idiomática de construir un resultado " +
                    "y permiten inferir el tipo en el punto de uso: Resultado<ProveedorDto>.Exito(dto).")]
public sealed class Resultado<T> : Resultado
{
    private Resultado(T? valor, ErrorAplicacion? error)
        : base(error) => Valor = valor;

    /// <summary>Valor producido, disponible solo cuando <see cref="Resultado.EsExitoso"/> es verdadero.</summary>
    public T? Valor { get; }

    public static Resultado<T> Exito(T valor) => new(valor, null);

    public static new Resultado<T> Fallo(ErrorAplicacion error) => new(default, error);

    public static new Resultado<T> Fallo(string codigo, string mensaje, TipoError tipo) =>
        new(default, new ErrorAplicacion(codigo, mensaje, tipo));
}
