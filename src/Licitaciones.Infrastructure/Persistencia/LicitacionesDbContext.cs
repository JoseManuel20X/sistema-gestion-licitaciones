using Licitaciones.Domain.Aprobaciones;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;
using Licitaciones.Domain.TiposCambio;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>Contexto de Entity Framework Core sobre PostgreSQL.</summary>
public sealed class LicitacionesDbContext : DbContext
{
    public LicitacionesDbContext(DbContextOptions<LicitacionesDbContext> opciones)
        : base(opciones)
    {
    }

    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    public DbSet<Licitacion> Licitaciones => Set<Licitacion>();

    public DbSet<Oferta> Ofertas => Set<Oferta>();

    public DbSet<NivelAprobacion> NivelesAprobacion => Set<NivelAprobacion>();

    public DbSet<TipoCambio> TiposCambio => Set<TipoCambio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        // Las configuraciones viven en clases separadas por entidad para que el
        // contexto no se convierta en un archivo enorme difícil de revisar.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LicitacionesDbContext).Assembly);
    }
}
