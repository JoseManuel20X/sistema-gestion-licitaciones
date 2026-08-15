using Licitaciones.Domain.Common;

namespace Licitaciones.Application.Common;

/// <summary>
/// Traduce una violación de regla del dominio al resultado que devuelve el caso de uso.
/// </summary>
/// <remarks>
/// Concentrar aquí la clasificación evita que cada servicio decida por su cuenta
/// si un error es 400, 409 o 422, y garantiza que la API responda de forma
/// coherente ante la misma regla.
/// </remarks>
public static class TraductorErrores
{
    /// <summary>Códigos que representan un choque con el estado actual (HTTP 409).</summary>
    private static readonly HashSet<string> Conflictos =
    [
        CodigosError.NombreProveedorDuplicado,
        CodigosError.CodigoLicitacionDuplicado,
        CodigosError.OfertaDuplicada,
        CodigosError.RangoAprobacionTraslapado,
        CodigosError.RangoAbiertoDuplicado,
        CodigosError.ViolacionIntegridad,
    ];

    /// <summary>Códigos que representan datos mal formados o ausentes (HTTP 400).</summary>
    private static readonly HashSet<string> Validaciones =
    [
        CodigosError.NombreProveedorVacio,
        CodigosError.NombreProveedorCaracteresInvalidos,
        CodigosError.CodigoLicitacionVacio,
        CodigosError.TituloLicitacionVacio,
        CodigosError.PresupuestoNoPositivo,
        CodigosError.MontoOfertaNoPositivo,
        CodigosError.TipoCambioNoPositivo,
        CodigosError.RangoAprobacionInvalido,
        CodigosError.AprobadorVacio,
    ];

    /// <summary>Códigos que indican un recurso inexistente (HTTP 404).</summary>
    private static readonly HashSet<string> NoEncontrados =
    [
        CodigosError.ProveedorNoEncontrado,
        CodigosError.LicitacionNoEncontrada,
        CodigosError.OfertaNoEncontrada,
        CodigosError.NivelAprobacionNoEncontrado,
        CodigosError.TipoCambioNoEncontrado,
    ];

    /// <summary>Clasifica una excepción del dominio.</summary>
    public static ErrorAplicacion Traducir(ExcepcionDominio excepcion)
    {
        ArgumentNullException.ThrowIfNull(excepcion);

        return new ErrorAplicacion(excepcion.Codigo, excepcion.Message, ClasificarCodigo(excepcion.Codigo));
    }

    /// <summary>Clasifica un código de error del dominio.</summary>
    public static TipoError ClasificarCodigo(string codigo)
    {
        if (codigo == CodigosError.ConflictoConcurrencia)
        {
            return TipoError.Concurrencia;
        }

        if (Conflictos.Contains(codigo))
        {
            return TipoError.Conflicto;
        }

        if (NoEncontrados.Contains(codigo))
        {
            return TipoError.NoEncontrado;
        }

        if (Validaciones.Contains(codigo))
        {
            return TipoError.Validacion;
        }

        // El resto son reglas de negocio: los datos son correctos pero el estado
        // del sistema no admite la operación (HTTP 422).
        return TipoError.ReglaNegocio;
    }
}
