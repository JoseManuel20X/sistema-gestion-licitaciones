using Licitaciones.Domain.Ofertas;

namespace Licitaciones.Application.Ofertas;

/// <summary>Datos necesarios para registrar una oferta.</summary>
public sealed record OfertaEntrada(Guid ProveedorId, decimal MontoOfertadoCRC);

/// <summary>Datos necesarios para editar el monto de una oferta.</summary>
public sealed record OfertaActualizacion(decimal MontoOfertadoCRC);

/// <summary>Representación de una oferta hacia el exterior.</summary>
public sealed record OfertaDto(
    Guid Id,
    Guid LicitacionId,
    Guid ProveedorId,
    string? NombreProveedor,
    decimal MontoOfertadoCRC,
    DateTimeOffset FechaRegistro,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Proyecta la entidad al DTO.</summary>
    public static OfertaDto Desde(Oferta oferta)
    {
        ArgumentNullException.ThrowIfNull(oferta);

        return new OfertaDto(
            oferta.Id,
            oferta.LicitacionId,
            oferta.ProveedorId,
            oferta.Proveedor?.Nombre,
            oferta.MontoOfertadoCRC,
            oferta.FechaRegistro,
            oferta.UpdatedAt);
    }
}
