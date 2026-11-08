namespace civix_backend.Models
{
    /// <summary>
    /// Nombres e identificadores de los roles tal como están en dbo.Roles.
    /// Se usan en [Authorize(Roles = ...)] y en la lógica de registro,
    /// para no repetir textos sueltos por todo el código.
    /// </summary>
    public static class RolesSistema
    {
        public const string Administrador = "Administrador";
        public const string Ingeniero = "Ingeniero";
        public const string Cliente = "Cliente";

        public const int AdministradorId = 1;
        public const int IngenieroId = 2;
        public const int ClienteId = 3;

        /// <summary>Roles que pueden crear o modificar información.</summary>
        public const string Gestion = Administrador + "," + Ingeniero;

        /// <summary>Todos los roles del sistema (incluye consulta del cliente).</summary>
        public const string Todos = Administrador + "," + Ingeniero + "," + Cliente;
    }
}
