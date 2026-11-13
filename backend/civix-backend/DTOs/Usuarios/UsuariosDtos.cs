using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.Usuarios
{
    /// <summary>Usuario tal como lo ve el Administrador en el módulo de Usuarios.</summary>
    public class UsuarioAdminDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        /// <summary>Rol principal (el primero asignado). Los usuarios de CIVIX tienen un rol.</summary>
        public int? RolId { get; set; }

        public List<string> Roles { get; set; } = new();

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        /// <summary>Obras a las que el usuario está asignado (activas en dbo.ObraUsuarios).</summary>
        public int ObrasAsignadas { get; set; }
    }

    /// <summary>
    /// Datos para editar un usuario. La contraseña es opcional:
    /// si viene vacía (null) se conserva la actual.
    /// </summary>
    public class ActualizarUsuarioDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(255)]
        public string Correo { get; set; } = string.Empty;

        [Range(1, 3, ErrorMessage = "El rol indicado no existe.")]
        public int RolId { get; set; }

        public bool Activo { get; set; } = true;

        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "La contraseña debe incluir letras y números.")]
        public string? Password { get; set; }
    }
}
