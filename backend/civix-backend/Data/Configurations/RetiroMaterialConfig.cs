using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    /// <summary>
    /// Mapeo de dbo.RetirosMaterial (tabla creada por
    /// database/scripts/004_materiales_estados_retiros.sql).
    /// </summary>
    public class RetiroMaterialConfig : IEntityTypeConfiguration<RetiroMaterial>
    {
        public void Configure(EntityTypeBuilder<RetiroMaterial> entity)
        {
            entity.ToTable("RetirosMaterial", "dbo");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.Id).ValueGeneratedOnAdd();

            entity.Property(r => r.Cantidad)
                  .HasColumnType("decimal(18,3)")
                  .IsRequired();

            entity.Property(r => r.RetiradoPor)
                  .HasMaxLength(150)
                  .IsRequired();

            entity.Property(r => r.Observacion).HasMaxLength(500);

            entity.Property(r => r.FechaRetiro)
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(r => r.ObraMaterial)
                  .WithMany(om => om.Retiros)
                  .HasForeignKey(r => r.ObraMaterialId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_RetirosMaterial_ObraMateriales");

            entity.HasOne(r => r.ObraDestino)
                  .WithMany()
                  .HasForeignKey(r => r.ObraDestinoId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_RetirosMaterial_Obras");

            entity.HasOne(r => r.RegistradoPor)
                  .WithMany()
                  .HasForeignKey(r => r.RegistradoPorUsuarioId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_RetirosMaterial_Usuarios");
        }
    }
}
