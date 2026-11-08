using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    /// <summary>Mapeo de dbo.ObraUsuarios: obras asignadas a cada ingeniero o cliente.</summary>
    public class ObraUsuarioConfig : IEntityTypeConfiguration<ObraUsuario>
    {
        public void Configure(EntityTypeBuilder<ObraUsuario> entity)
        {
            entity.ToTable("ObraUsuarios", "dbo");

            entity.HasKey(ou => new { ou.ObraId, ou.UsuarioId });

            entity.Property(ou => ou.FechaAsignacion)
                  .HasColumnType("datetime2")
                  .IsRequired();

            entity.Property(ou => ou.Activo)
                  .IsRequired();

            entity.HasOne(ou => ou.Usuario)
                  .WithMany(u => u.ObrasAsignadas)
                  .HasForeignKey(ou => ou.UsuarioId)
                  .HasConstraintName("FK_ObraUsuarios_Usuarios");
        }
    }
}
