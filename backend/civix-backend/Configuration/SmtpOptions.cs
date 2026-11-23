namespace civix_backend.Configuration
{
    /// <summary>
    /// Servidor de correo para enviar los enlaces de recuperación de contraseña.
    /// Se lee de la sección "Smtp". El usuario y la contraseña NO van en appsettings.json:
    /// van en appsettings.Development.json (ignorado por Git), User Secrets o variables de entorno.
    /// Si Host está vacío, el enlace se escribe en la consola de la API (modo desarrollo).
    /// </summary>
    public class SmtpOptions
    {
        public const string Seccion = "Smtp";

        /// <summary>Servidor SMTP. Ej.: smtp.gmail.com. Vacío = no se envían correos.</summary>
        public string Host { get; set; } = string.Empty;

        public int Puerto { get; set; } = 587;

        /// <summary>STARTTLS / SSL. Gmail y Outlook lo exigen.</summary>
        public bool UsarSsl { get; set; } = true;

        public string Usuario { get; set; } = string.Empty;

        /// <summary>Con Gmail debe ser una "contraseña de aplicación", no la contraseña normal.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Dirección que aparece como remitente. Si está vacía se usa Usuario.</summary>
        public string Remitente { get; set; } = string.Empty;

        public string NombreRemitente { get; set; } = "CIVIX · Control de obra";

        public bool EstaConfigurado => !string.IsNullOrWhiteSpace(Host);
    }
}
