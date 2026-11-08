using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    /// <summary>Mapeo de dbo.Obras (la tabla ya existe; esta clase no la crea).</summary>
    public class ObraConfig : IEntityTypeConfiguration<Obra>
    {
        public void Configure(EntityTypeBuilder<Obra> entity)
        {
            entity.ToTable("Obras", "dbo");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.Id).ValueGeneratedOnAdd();

            entity.Property(o => o.Nombre).HasMaxLength(150).IsRequired();

            entity.Property(o => o.Descripcion).HasMaxLength(1000);

            entity.Property(o => o.Ubicacion).HasMaxLength(250);

            entity.Property(o => o.FechaInicio).HasColumnType("date");

            entity.Property(o => o.FechaFinEstimada).HasColumnType("date");

            entity.Property(o => o.Estado).HasMaxLength(30).IsRequired();

            entity.Property(o => o.FechaCreacion)
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("SYSDATETIME()");

            // Propiedades propias del modulo de Obras (no persistidas en dbo.Obras)
            entity.Ignore(o => o.Cliente);
            entity.Ignore(o => o.Presupuesto);
            entity.Ignore(o => o.Avance);
            entity.Ignore(o => o.Personal);
        }
    }
}
