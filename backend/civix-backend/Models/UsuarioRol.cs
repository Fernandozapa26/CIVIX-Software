namespace civix_backend.Models
{
    /// <summary>
    /// Tabla puente dbo.UsuariosRoles (relación muchos a muchos entre Usuarios y Roles).
    /// Su llave primaria es compuesta: (UsuarioId, RolId).
    /// </summary>
    public class UsuarioRol
    {
        public int UsuarioId { get; set; }

        public int RolId { get; set; }

        public Usuario Usuario { get; set; } = null!;

        public Rol Rol { get; set; } = null!;
    }
}
