namespace civix_backend.Configuration
{
    /// <summary>Configuración del flujo "¿Olvidaste tu contraseña?" (sección "RecuperacionPassword").</summary>
    public class RecuperacionPasswordOptions
    {
        public const string Seccion = "RecuperacionPassword";

        /// <summary>Dirección del frontend; el enlace del correo apunta a {UrlFrontend}/restablecer-password.</summary>
        public string UrlFrontend { get; set; } = "http://localhost:4200";

        /// <summary>Minutos que dura válido el enlace de recuperación.</summary>
        public int ExpiracionMinutos { get; set; } = 30;
    }
}
