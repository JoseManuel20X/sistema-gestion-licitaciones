using Licitaciones.Domain.Proveedores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

internal sealed class ProveedorConfiguracion : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("proveedores");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(p => p.NombreNormalizado).IsRequired().HasMaxLength(200);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
        builder.Property(p => p.DeletedAt);

        // Concurrencia optimista mediante la columna de sistema xmin de
        // PostgreSQL: no hace falta una columna propia y la base la mantiene
        // sola en cada UPDATE (enunciado §11).
        builder.Property(p => p.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // La unicidad se garantiza en la base de datos, no solo en el servidor:
        // dos peticiones simultáneas no pueden crear el mismo proveedor.
        // El filtro excluye los borrados lógicos para que un nombre pueda
        // reutilizarse tras dar de baja al proveedor anterior.
        builder.HasIndex(p => p.NombreNormalizado)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL")
            .HasDatabaseName("ix_proveedores_nombre_normalizado");

        // La relación se configura desde el lado de Oferta; ver OfertaConfiguracion.
    }
}
