using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    public class CotizacionConfig : IEntityTypeConfiguration<Cotizacion>
    {
        public void Configure(EntityTypeBuilder<Cotizacion> entity)
        {
            entity.ToTable("Cotizaciones", "dbo");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(c => c.Fecha)
                  .HasColumnType("date")
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(c => c.CreadoPorUsuarioId)
                  .IsRequired();

            entity.Property(c => c.Estado)
                  .HasMaxLength(30)
                  .HasDefaultValue("Borrador")
                  .IsRequired();

            entity.Property(c => c.Total)
                  .HasColumnType("decimal(18,2)")
                  .HasDefaultValue(0m)
                  .IsRequired();

            entity.HasMany(c => c.Detalles)
                  .WithOne(d => d.Cotizacion)
                  .HasForeignKey(d => d.CotizacionId);
        }
    }
}
