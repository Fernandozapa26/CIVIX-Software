namespace civix_backend.DTOs.Auth
{
    /// <summary>
    /// Respuesta de GET /api/auth/me: quién es el usuario del token y a qué obras tiene acceso.
    /// TodasLasObras = true para el Administrador; en ese caso ObrasPermitidas viene vacía.
    /// </summary>
    public class UsuarioActualDto
    {
        public int UsuarioId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();

        public bool SoloLectura { get; set; }

        public bool TodasLasObras { get; set; }

        public List<int> ObrasPermitidas { get; set; } = new();
    }
}
