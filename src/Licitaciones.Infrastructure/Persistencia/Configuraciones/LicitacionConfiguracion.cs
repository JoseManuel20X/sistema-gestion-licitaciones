using Licitaciones.Domain.Licitaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

internal sealed class LicitacionConfiguracion : IEntityTypeConfiguration<Licitacion>
{
    public void Configure(EntityTypeBuilder<Licitacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "licitaciones",
            tabla => tabla.HasCheckConstraint(
                "ck_licitaciones_presupuesto_positivo",
                "\"PresupuestoEstimadoCRC\" > 0"));

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Codigo).IsRequired().HasMaxLength(50);
        builder.Property(l => l.CodigoNormalizado).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Titulo).IsRequired().HasMaxLength(300);

        // El estado se guarda como texto: una migración que reordene el enum no
        // puede corromper los datos existentes, y las consultas son legibles.
        builder.Property(l => l.Estado)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(l => l.FechaCierre).IsRequired();

        // Los montos usan numeric(18,2); nunca float o double (enunciado §7).
        builder.Property(l => l.PresupuestoEstimadoCRC)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.UpdatedAt).IsRequired();
        builder.Property(l => l.DeletedAt);

        builder.Property(l => l.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(l => l.CodigoNormalizado)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL")
            .HasDatabaseName("ix_licitaciones_codigo_normalizado");

        // Acelera los listados filtrados por estado, que son los más frecuentes.
        builder.HasIndex(l => l.Estado).HasDatabaseName("ix_licitaciones_estado");

        // La relación se configura desde el lado de Oferta; ver OfertaConfiguracion.
    }
}
