using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using civix_backend.Models;

namespace civix_backend.Data.Configurations
{
    /// <summary>
    /// Mapeo de dbo.Usuarios. La tabla ya existe (001_creacion_inicial.sql):
    /// esta configuración solo describe cómo leerla y escribirla, no la crea.
    /// </summary>
    public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> entity)
        {
            entity.ToTable("Usuarios", "dbo");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(u => u.Nombre)
                  .HasMaxLength(150)
                  .IsRequired();

            entity.Property(u => u.Correo)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.HasIndex(u => u.Correo)
                  .IsUnique();

            entity.Property(u => u.PasswordHash)
                  .HasMaxLength(500)
                  .IsRequired();

            entity.Property(u => u.Activo)
                  .IsRequired();

            entity.Property(u => u.FechaCreacion)
                  .HasColumnType("datetime2")
                  .IsRequired();
        }
    }
}
