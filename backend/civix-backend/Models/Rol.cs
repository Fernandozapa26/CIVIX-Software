namespace civix_backend.Models
{
    /// <summary>
    /// Representa la tabla dbo.Roles (1 = Administrador, 2 = Ingeniero, 3 = Cliente).
    /// </summary>
    public class Rol
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public ICollection<UsuarioRol> UsuariosRoles { get; set; } = new List<UsuarioRol>();
    }
}
