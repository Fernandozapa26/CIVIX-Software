using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.Auth
{
    /// <summary>
    /// Datos para registrar un usuario.
    /// RolId solo se respeta cuando quien registra es un Administrador autenticado;
    /// en el registro público el usuario siempre queda como Cliente.
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(255)]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "La contraseña debe incluir letras y números.")]
        public string Password { get; set; } = string.Empty;

        [Range(1, 3, ErrorMessage = "El rol indicado no existe.")]
        public int? RolId { get; set; }
    }
}
