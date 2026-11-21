using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    /// <summary>
    /// Mapeo de dbo.Cotizaciones. Las columnas NombreProyecto, Descripcion, Ubicacion,
    /// FechaDecision y DecididaPorUsuarioId se agregan con 006_cotizaciones.sql.
    /// </summary>
    public class CotizacionConfig : IEntityTypeConfiguration<Cotizacion>
    {
        public void Configure(EntityTypeBuilder<Cotizacion> entity)
        {
            entity.ToTable("Cotizaciones", "dbo");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(c => c.Numero)
                  .HasMaxLength(30)
                  .IsRequired();

            entity.HasIndex(c => c.Numero).IsUnique();

            entity.Property(c => c.Fecha)
                  .HasColumnType("date")
                  .IsRequired();

            entity.Property(c => c.CreadoPorUsuarioId)
                  .IsRequired();

            entity.Property(c => c.NombreProyecto)
                  .HasMaxLength(150)
                  .IsRequired();

            entity.Property(c => c.Descripcion).HasMaxLength(1000);

            entity.Property(c => c.Ubicacion).HasMaxLength(250);

            entity.Property(c => c.Estado)
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(c => c.Total)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(c => c.Activo).IsRequired();

            entity.Property(c => c.FechaDecision).HasColumnType("datetime2");

            entity.HasOne(c => c.CreadoPor)
                  .WithMany()
                  .HasForeignKey(c => c.CreadoPorUsuarioId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Cotizaciones_Usuarios");

            entity.HasOne(c => c.Cliente)
                  .WithMany()
                  .HasForeignKey(c => c.ClienteId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Cotizaciones_Clientes");

            entity.HasOne(c => c.DecididaPor)
                  .WithMany()
                  .HasForeignKey(c => c.DecididaPorUsuarioId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Cotizaciones_DecididaPor");

            entity.HasOne(c => c.Obra)
                  .WithMany()
                  .HasForeignKey(c => c.ObraId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Cotizaciones_Obras");

            entity.HasMany(c => c.Detalles)
                  .WithOne(d => d.Cotizacion)
                  .HasForeignKey(d => d.CotizacionId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
