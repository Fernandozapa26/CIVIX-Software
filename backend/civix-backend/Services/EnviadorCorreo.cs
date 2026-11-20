using System.Net;
using System.Net.Mail;
using civix_backend.Configuration;

namespace civix_backend.Services
{
    /// <summary>Envía correos electrónicos. Se usa una interfaz para poder cambiar de proveedor.</summary>
    public interface IEnviadorCorreo
    {
        /// <summary>
        /// Envía el correo. Devuelve false si no hay servidor SMTP configurado
        /// (en ese caso quien llama decide qué hacer, por ejemplo escribir el enlace en la consola).
        /// </summary>
        Task<bool> EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancelacion = default);
    }

    /// <summary>Envío por SMTP con System.Net.Mail (incluido en .NET, sin paquetes extra).</summary>
    public class SmtpEnviadorCorreo : IEnviadorCorreo
    {
        private readonly SmtpOptions _opciones;

        public SmtpEnviadorCorreo(SmtpOptions opciones)
        {
            _opciones = opciones;
        }

        public async Task<bool> EnviarAsync(
            string destinatario, string asunto, string cuerpoHtml, CancellationToken cancelacion = default)
        {
            if (!_opciones.EstaConfigurado)
            {
                return false;
            }

            var remitente = string.IsNullOrWhiteSpace(_opciones.Remitente) ? _opciones.Usuario : _opciones.Remitente;

            using var mensaje = new MailMessage
            {
                From = new MailAddress(remitente, _opciones.NombreRemitente),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };
            mensaje.To.Add(destinatario);

            using var cliente = new SmtpClient(_opciones.Host, _opciones.Puerto)
            {
                EnableSsl = _opciones.UsarSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = string.IsNullOrWhiteSpace(_opciones.Usuario)
                    ? null
                    : new NetworkCredential(_opciones.Usuario, _opciones.Password)
            };

            await cliente.SendMailAsync(mensaje, cancelacion);
            return true;
        }
    }
}
