
using Microsoft.EntityFrameworkCore;
using civix_backend.Models;

namespace civix_backend.Data
{
    public class CivixDbContext : DbContext
    {
        public CivixDbContext(
            DbContextOptions<CivixDbContext> options)
            : base(options)
        {
        }

        public DbSet<Material> Materiales { get; set; } = null!;

        public DbSet<Cotizacion> Cotizaciones { get; set; } = null!;

        public DbSet<CotizacionDetalle> CotizacionDetalles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(CivixDbContext).Assembly
            );
        }
    }
}
