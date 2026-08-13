namespace Licitaciones.Domain.Common;

/// <summary>
/// Violación de una regla de negocio detectada por el propio dominio.
/// </summary>
/// <remarks>
/// Las entidades protegen sus invariantes lanzando esta excepción, de modo que
/// una regla no pueda incumplirse aunque se invoque el dominio desde una capa
/// nueva. La capa de aplicación la traduce a un resultado controlado y la API
/// a <c>ProblemDetails</c>; nunca escapa como error 500 sin tratar.
/// </remarks>
public sealed class ExcepcionDominio : Exception
{
    /// <summary>Código estable del error, definido en <see cref="CodigosError"/>.</summary>
    public string Codigo { get; }

    public ExcepcionDominio(string codigo, string mensaje)
        : base(mensaje)
    {
        Codigo = codigo;
    }

    /// <summary>Lanza la excepción cuando <paramref name="condicion"/> es verdadera.</summary>
    public static void SiCumple(bool condicion, string codigo, string mensaje)
    {
        if (condicion)
        {
            throw new ExcepcionDominio(codigo, mensaje);
        }
    }
}
