using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    public class CotizacionDetalleConfig : IEntityTypeConfiguration<CotizacionDetalle>
    {
        public void Configure(EntityTypeBuilder<CotizacionDetalle> entity)
        {
            entity.ToTable("CotizacionDetalles", "dbo");

            entity.HasKey(d => d.Id);

            entity.Property(d => d.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(d => d.Concepto)
                  .HasMaxLength(250)
                  .IsRequired();

            entity.Property(d => d.Cantidad)
                  .HasColumnType("decimal(18,3)")
                  .IsRequired();

            entity.Property(d => d.PrecioUnitario)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(d => d.Subtotal)
                  .HasComputedColumnSql(
                      "CONVERT(DECIMAL(18,2), [Cantidad] * [PrecioUnitario])",
                      stored: true
                  );
        }
    }
}
