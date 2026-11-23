using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.Auth
{
    /// <summary>Cuerpo de POST /api/auth/validar-token-recuperacion.</summary>
    public class ValidarTokenRecuperacionDto
    {
        [Required(ErrorMessage = "El enlace de recuperación no es válido.")]
        [StringLength(1000)]
        public string Token { get; set; } = string.Empty;
    }
}
