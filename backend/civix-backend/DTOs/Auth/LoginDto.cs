using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.Auth
{
    /// <summary>
    /// Datos que envía el formulario de inicio de sesión.
    /// [ApiController] valida estas reglas antes de entrar al método del controlador.
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(255)]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }
}
