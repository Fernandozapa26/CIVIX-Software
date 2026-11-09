using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    public class AvanceConfig : IEntityTypeConfiguration<Avance>
    {
        public void Configure(EntityTypeBuilder<Avance> entity)
        {
            entity.ToTable("AvancesObra", "dbo");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.Id).ValueGeneratedOnAdd();

            entity.Property(a => a.ObraId).IsRequired();

            entity.Property(a => a.RegistradoPorUsuarioId).IsRequired();

            entity.Property(a => a.Fecha).HasColumnType("date").IsRequired();

            entity.Property(a => a.Descripcion).HasMaxLength(1500).IsRequired();

            entity.Property(a => a.PorcentajeAvance).HasColumnType("decimal(5,2)");

            entity.Property(a => a.Imagenes)
                  .HasColumnType("nvarchar(max)")
                  .HasConversion(
                      v => string.Join(",", v ?? new List<string>()),
                      v => string.IsNullOrWhiteSpace(v)
                          ? new List<string>()
                          : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

            entity.HasOne<Obra>()
                  .WithMany()
                  .HasForeignKey(a => a.ObraId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}