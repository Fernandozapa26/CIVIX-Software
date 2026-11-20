using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.Auth
{
    /// <summary>Cuerpo de POST /api/auth/olvide-password.</summary>
    public class SolicitarRecuperacionDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(255)]
        public string Correo { get; set; } = string.Empty;
    }
}
