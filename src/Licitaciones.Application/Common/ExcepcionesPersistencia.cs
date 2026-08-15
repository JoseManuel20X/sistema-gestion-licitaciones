namespace Licitaciones.Application.Common;

/// <summary>
/// Otro proceso modificó el registro entre la lectura y la escritura.
/// </summary>
/// <remarks>
/// La capa de infraestructura traduce la excepción de concurrencia de Entity
/// Framework Core a esta, para que la capa de aplicación no dependa del ORM
/// (enunciado §11).
/// </remarks>
public sealed class ExcepcionConcurrencia : Exception
{
    public ExcepcionConcurrencia(string mensaje)
        : base(mensaje)
    {
    }

    public ExcepcionConcurrencia(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }

    public ExcepcionConcurrencia()
        : base("El registro fue modificado por otro proceso.")
    {
    }
}

/// <summary>
/// La base de datos rechazó la operación por una restricción de integridad,
/// típicamente un índice único que otra transacción ganó por carrera.
/// </summary>
public sealed class ExcepcionConflictoPersistencia : Exception
{
    public ExcepcionConflictoPersistencia(string codigo, string mensaje)
        : base(mensaje) => Codigo = codigo;

    public ExcepcionConflictoPersistencia(string codigo, string mensaje, Exception innerException)
        : base(mensaje, innerException) => Codigo = codigo;

    public ExcepcionConflictoPersistencia()
        : base("La operación infringe una restricción de integridad.") =>
        Codigo = Domain.Common.CodigosError.ViolacionIntegridad;

    /// <summary>Código estable del conflicto detectado.</summary>
    public string Codigo { get; }
}
