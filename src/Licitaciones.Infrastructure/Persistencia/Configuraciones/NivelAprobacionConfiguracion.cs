using Licitaciones.Domain.Aprobaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

internal sealed class NivelAprobacionConfiguracion : IEntityTypeConfiguration<NivelAprobacion>
{
    public void Configure(EntityTypeBuilder<NivelAprobacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "niveles_aprobacion",
            tabla =>
            {
                tabla.HasCheckConstraint(
                    "ck_niveles_minimo_positivo",
                    "\"MontoMinimoCRC\" > 0");

                // El máximo puede ser nulo (rango abierto), pero si existe debe
                // superar al mínimo.
                tabla.HasCheckConstraint(
                    "ck_niveles_rango_coherente",
                    "\"MontoMaximoCRC\" IS NULL OR \"MontoMaximoCRC\" > \"MontoMinimoCRC\"");
            });

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.MontoMinimoCRC).IsRequired().HasColumnType("numeric(18,2)");
        builder.Property(n => n.MontoMaximoCRC).HasColumnType("numeric(18,2)");
        builder.Property(n => n.Aprobador).IsRequired().HasMaxLength(150);

        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.UpdatedAt).IsRequired();

        // Dos rangos no pueden empezar en el mismo monto. No cubre todo el
        // traslape (eso lo valida el dominio), pero descarta el caso más común
        // desde la propia base de datos.
        builder.HasIndex(n => n.MontoMinimoCRC)
            .IsUnique()
            .HasDatabaseName("ix_niveles_aprobacion_minimo");
    }
}
