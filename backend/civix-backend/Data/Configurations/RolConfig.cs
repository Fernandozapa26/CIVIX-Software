using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    /// <summary>Mapeo de dbo.Roles (1 = Administrador, 2 = Ingeniero, 3 = Cliente).</summary>
    public class RolConfig : IEntityTypeConfiguration<Rol>
    {
        public void Configure(EntityTypeBuilder<Rol> entity)
        {
            entity.ToTable("Roles", "dbo");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(r => r.Nombre)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.HasIndex(r => r.Nombre)
                  .IsUnique();

            entity.Property(r => r.Descripcion)
                  .HasMaxLength(200)
                  .IsRequired(false);
        }
    }
}
