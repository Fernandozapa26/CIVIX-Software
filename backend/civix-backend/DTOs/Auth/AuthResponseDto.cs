namespace civix_backend.DTOs.Auth
{
    /// <summary>
    /// Respuesta de login y registro. El frontend guarda el token y los datos
    /// del usuario para mostrar el menú según sus roles.
    /// </summary>
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;

        /// <summary>Fecha y hora (UTC) en que vence el token.</summary>
        public DateTime ExpiraEn { get; set; }

        public int UsuarioId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();
    }
}
