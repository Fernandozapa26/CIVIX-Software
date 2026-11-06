namespace civix_backend.Models
{
    /// <summary>
    /// Representa la tabla dbo.Usuarios. Guarda el hash de la contraseña,
    /// nunca la contraseña en texto plano.
    /// </summary>
    public class Usuario
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; }

        public ICollection<UsuarioRol> UsuariosRoles { get; set; } = new List<UsuarioRol>();

        public ICollection<ObraUsuario> ObrasAsignadas { get; set; } = new List<ObraUsuario>();
    }
}
