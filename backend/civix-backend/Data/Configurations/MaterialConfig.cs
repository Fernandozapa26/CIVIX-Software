using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    public class MaterialConfig : IEntityTypeConfiguration<Material>
    {
        public void Configure(EntityTypeBuilder<Material> entity)
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
        }
    }
}
