namespace Licitaciones.Domain.Common;

/// <summary>
/// Base de las entidades persistidas: identidad generada y sellos de auditoría
/// (enunciado §7 y §11).
/// </summary>
public abstract class EntidadAuditable
{
    /// <summary>
    /// Identificador generado por el sistema. Se usa UUID v7 porque incorpora
    /// una marca temporal y mantiene la localidad del índice al insertar, a
    /// diferencia de un UUID v4 completamente aleatorio.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    /// <summary>Instante de creación en UTC.</summary>
    public DateTimeOffset CreatedAt { get; protected set; }

    /// <summary>Instante de la última modificación en UTC.</summary>
    public DateTimeOffset UpdatedAt { get; protected set; }

    /// <summary>Registra los sellos de auditoría iniciales.</summary>
    protected void MarcarCreacion(IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        CreatedAt = reloj.AhoraUtc;
        UpdatedAt = CreatedAt;
    }

    /// <summary>Actualiza el sello de modificación.</summary>
    protected void MarcarActualizacion(IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        UpdatedAt = reloj.AhoraUtc;
    }
}
