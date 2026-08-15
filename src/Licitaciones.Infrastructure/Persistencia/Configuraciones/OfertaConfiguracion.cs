using Licitaciones.Domain.Ofertas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

internal sealed class OfertaConfiguracion : IEntityTypeConfiguration<Oferta>
{
    public void Configure(EntityTypeBuilder<Oferta> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "ofertas",
            tabla => tabla.HasCheckConstraint(
                "ck_ofertas_monto_positivo",
                "\"MontoOfertadoCRC\" > 0"));

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.LicitacionId).IsRequired();
        builder.Property(o => o.ProveedorId).IsRequired();

        builder.Property(o => o.MontoOfertadoCRC)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(o => o.FechaRegistro).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        builder.Property(o => o.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Las relaciones se declaran aquí, desde el lado que posee la clave
        // foránea. Licitación y Proveedor no exponen colección de ofertas: se
        // consultan por repositorio, de modo que ninguna regla dependa de que el
        // ORM haya cargado la navegación.
        builder.HasOne(o => o.Licitacion)
            .WithMany()
            .HasForeignKey(o => o.LicitacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Proveedor)
            .WithMany()
            .HasForeignKey(o => o.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un proveedor no puede presentar dos ofertas en la misma licitación
        // (enunciado §8.3). La restricción vive en la base de datos para que
        // resista peticiones concurrentes.
        builder.HasIndex(o => new { o.LicitacionId, o.ProveedorId })
            .IsUnique()
            .HasDatabaseName("ix_ofertas_licitacion_proveedor");

        // La mejor oferta se busca por monto ascendente dentro de una licitación.
        builder.HasIndex(o => new { o.LicitacionId, o.MontoOfertadoCRC })
            .HasDatabaseName("ix_ofertas_licitacion_monto");
    }
}
