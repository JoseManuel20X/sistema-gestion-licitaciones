using Licitaciones.Domain.TiposCambio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

internal sealed class TipoCambioConfiguracion : IEntityTypeConfiguration<TipoCambio>
{
    public void Configure(EntityTypeBuilder<TipoCambio> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "tipos_cambio",
            tabla => tabla.HasCheckConstraint("ck_tipos_cambio_positivo", "\"CRCporUSD\" > 0"));

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        // Cuatro decimales: el tipo de cambio se cotiza con más precisión que un
        // monto, y redondearlo a dos distorsionaría conversiones grandes.
        builder.Property(t => t.CRCporUSD).IsRequired().HasColumnType("numeric(18,4)");
        builder.Property(t => t.FechaVigencia).IsRequired();
        builder.Property(t => t.Activo).IsRequired();

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        // Índice único parcial: solo puede haber una fila con Activo = true
        // (enunciado §8.8). La regla la impone PostgreSQL, no la aplicación.
        builder.HasIndex(t => t.Activo)
            .IsUnique()
            .HasFilter("\"Activo\" = true")
            .HasDatabaseName("ix_tipos_cambio_activo_unico");
    }
}
