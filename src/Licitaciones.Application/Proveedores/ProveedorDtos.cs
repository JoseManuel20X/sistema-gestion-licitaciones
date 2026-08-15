using Licitaciones.Domain.Proveedores;

namespace Licitaciones.Application.Proveedores;

/// <summary>Datos necesarios para registrar o editar un proveedor.</summary>
/// <param name="Nombre">Nombre comercial del proveedor.</param>
public sealed record ProveedorEntrada(string Nombre);

/// <summary>Representación de un proveedor hacia el exterior.</summary>
/// <remarks>
/// La API expone este DTO y nunca la entidad de Entity Framework Core, para que
/// un cambio en el modelo de datos no rompa el contrato publicado
/// (enunciado §10).
/// </remarks>
public sealed record ProveedorDto(
    Guid Id,
    string Nombre,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool Eliminado)
{
    /// <summary>Proyecta la entidad al DTO.</summary>
    public static ProveedorDto Desde(Proveedor proveedor)
    {
        ArgumentNullException.ThrowIfNull(proveedor);

        return new ProveedorDto(
            proveedor.Id,
            proveedor.Nombre,
            proveedor.CreatedAt,
            proveedor.UpdatedAt,
            proveedor.EstaEliminado);
    }
}
