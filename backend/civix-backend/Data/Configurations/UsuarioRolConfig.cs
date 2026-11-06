using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    /// <summary>Mapeo de la tabla puente dbo.UsuariosRoles (llave compuesta UsuarioId + RolId).</summary>
    public class UsuarioRolConfig : IEntityTypeConfiguration<UsuarioRol>
    {
        public void Configure(EntityTypeBuilder<UsuarioRol> entity)
        {
            entity.ToTable("UsuariosRoles", "dbo");

            entity.HasKey(ur => new { ur.UsuarioId, ur.RolId });

            entity.HasOne(ur => ur.Usuario)
                  .WithMany(u => u.UsuariosRoles)
                  .HasForeignKey(ur => ur.UsuarioId)
                  .HasConstraintName("FK_UsuariosRoles_Usuarios");

            entity.HasOne(ur => ur.Rol)
                  .WithMany(r => r.UsuariosRoles)
                  .HasForeignKey(ur => ur.RolId)
                  .HasConstraintName("FK_UsuariosRoles_Roles");
        }
    }
}
