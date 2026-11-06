namespace civix_backend.Models
{
    /// <summary>
    /// Tabla dbo.ObraUsuarios: indica qué usuarios (ingenieros o clientes)
    /// están asignados a cada obra. Es la base del acceso restringido por obra.
    /// </summary>
    public class ObraUsuario
    {
        public int ObraId { get; set; }

        public int UsuarioId { get; set; }

        public DateTime FechaAsignacion { get; set; }

        public bool Activo { get; set; } = true;

        public Usuario Usuario { get; set; } = null!;
    }
}
