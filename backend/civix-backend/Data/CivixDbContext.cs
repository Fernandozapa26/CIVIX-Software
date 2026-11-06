using Microsoft.EntityFrameworkCore;
using civix_backend.Models;

namespace civix_backend.Data
{
    public class CivixDbContext : DbContext
    {
        public CivixDbContext(
            DbContextOptions<CivixDbContext> options)
            : base(options)
        {
        }

        public DbSet<Material> Materiales { get; set; } = null!;

        // --- Usuarios y seguridad ---
        public DbSet<Usuario> Usuarios { get; set; } = null!;

        public DbSet<Rol> Roles { get; set; } = null!;

        public DbSet<UsuarioRol> UsuariosRoles { get; set; } = null!;

        public DbSet<ObraUsuario> ObraUsuarios { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Material>(entity =>
            {
                entity.ToTable("Materiales", "dbo");

                entity.HasKey(m => m.Id);

                entity.Property(m => m.Id)
                      .ValueGeneratedOnAdd();

                entity.Property(m => m.Nombre)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(m => m.UnidadMedida)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(m => m.Descripcion)
                      .HasMaxLength(500)
                      .IsRequired(false);
            });

            // Las tablas ya existen (script 001_creacion_inicial.sql); este mapeo
            // solo le dice a EF Core cómo leerlas y escribirlas, no las crea.
            modelBuilder.Entity<Usuario>(entity =>
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
            });

            modelBuilder.Entity<Rol>(entity =>
            {
                entity.ToTable("Roles", "dbo");

                entity.HasKey(r => r.Id);

                entity.Property(r => r.Id)
                      .ValueGeneratedOnAdd();

                entity.Property(r => r.Nombre)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.HasIndex(r => r.Nombre)
                      .IsUnique();

                entity.Property(r => r.Descripcion)
                      .HasMaxLength(200)
                      .IsRequired(false);
            });

            modelBuilder.Entity<UsuarioRol>(entity =>
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
            });

            modelBuilder.Entity<ObraUsuario>(entity =>
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
            });
        }
    }
}
