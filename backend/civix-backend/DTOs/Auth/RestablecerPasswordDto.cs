using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.Auth
{
    /// <summary>Cuerpo de POST /api/auth/restablecer-password.</summary>
    public class RestablecerPasswordDto
    {
        /// <summary>Token que llegó en el enlace del correo.</summary>
        [Required(ErrorMessage = "El enlace de recuperación no es válido.")]
        [StringLength(1000)]
        public string Token { get; set; } = string.Empty;

        /// <summary>Mismas reglas que el registro: mínimo 8 caracteres, con letras y números.</summary>
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "La contraseña debe incluir letras y números.")]
        public string Password { get; set; } = string.Empty;
    }
}
