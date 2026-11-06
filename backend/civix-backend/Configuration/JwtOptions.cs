namespace civix_backend.Configuration
{
    /// <summary>
    /// Configuración de los tokens JWT, leída de la sección "Jwt" de appsettings.
    /// La clave (Key) NO se guarda en appsettings.json: va en appsettings.Development.json
    /// (ignorado por Git), en User Secrets o en variables de entorno.
    /// </summary>
    public class JwtOptions
    {
        public const string Seccion = "Jwt";

        public string Issuer { get; set; } = "civix-backend";

        public string Audience { get; set; } = "civix-front";

        /// <summary>Clave secreta para firmar los tokens (mínimo 32 caracteres).</summary>
        public string Key { get; set; } = string.Empty;

        public int ExpiracionMinutos { get; set; } = 480;
    }
}
