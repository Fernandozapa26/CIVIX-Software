using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    /// <summary>
    /// Mapeo de dbo.ObraMateriales. Las columnas Estado y CotizacionId
    /// se agregan con database/scripts/004_materiales_estados_retiros.sql.
    /// </summary>
    public class ObraMaterialConfig : IEntityTypeConfiguration<ObraMaterial>
    {
        public void Configure(EntityTypeBuilder<ObraMaterial> entity)
        {
            entity.ToTable("ObraMateriales", "dbo");

            entity.HasKey(om => om.Id);

            entity.Property(om => om.Id).ValueGeneratedOnAdd();

            entity.Property(om => om.Cantidad)
                  .HasColumnType("decimal(18,3)")
                  .IsRequired();

            entity.Property(om => om.Estado)
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(om => om.FechaRegistro)
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(om => om.Obra)
                  .WithMany()
                  .HasForeignKey(om => om.ObraId)
                  .HasConstraintName("FK_ObraMateriales_Obras");

            entity.HasOne(om => om.Material)
                  .WithMany()
                  .HasForeignKey(om => om.MaterialId)
                  .HasConstraintName("FK_ObraMateriales_Materiales");

            entity.HasOne(om => om.Cotizacion)
                  .WithMany()
                  .HasForeignKey(om => om.CotizacionId)
                  .IsRequired(false)
                  .HasConstraintName("FK_ObraMateriales_Cotizaciones");

            entity.HasOne(om => om.RegistradoPor)
                  .WithMany()
                  .HasForeignKey(om => om.RegistradoPorUsuarioId)
                  .HasConstraintName("FK_ObraMateriales_Usuarios");
        }
    }
}
