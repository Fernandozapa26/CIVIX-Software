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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Material>(entity =>
            {
                entity.ToTable("Materiales", "dbo");

                entity.HasKey(m => m.Id);

                entity.Property(m => m.Id)
                      .ValueGeneratedOnAdd();

                entity.Property(m => m.Nombre)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(m => m.UnidadMedida)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(m => m.Descripcion)
                      .HasMaxLength(500)
                      .IsRequired(false);
            });
        }
    }
}
