using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using civix_backend.Configuration;
using civix_backend.Data;
using civix_backend.Models;

namespace civix_backend.Services
{
    /// <summary>
    /// Flujo "¿Olvidaste tu contraseña?" sin tablas nuevas en la base de datos.
    ///
    /// El enlace lleva un token firmado con HMAC-SHA256 que contiene:
    ///   id del usuario : fecha de vencimiento : sello de la contraseña actual
    /// - La firma impide que alguien lo invente o lo modifique.
    /// - El vencimiento lo limita a unos minutos (RecuperacionPassword:ExpiracionMinutos).
    /// - El sello es un resumen del hash de la contraseña actual: en cuanto la contraseña
    ///   cambia, el sello deja de coincidir y el enlace ya no sirve (un solo uso).
    /// </summary>
    public class RecuperacionPasswordService
    {
        public const string MensajeTokenInvalido =
            "El enlace no es válido o ya venció. Solicita uno nuevo desde \"¿Olvidaste tu contraseña?\".";

        private readonly CivixDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly IEnviadorCorreo _enviadorCorreo;
        private readonly RecuperacionPasswordOptions _opciones;
        private readonly IHostEnvironment _entorno;
        private readonly ILogger<RecuperacionPasswordService> _logger;
        private readonly byte[] _claveFirma;

        public RecuperacionPasswordService(
            CivixDbContext context,
            IPasswordHasher<Usuario> passwordHasher,
            IEnviadorCorreo enviadorCorreo,
            RecuperacionPasswordOptions opciones,
            JwtOptions jwt,
            IHostEnvironment entorno,
            ILogger<RecuperacionPasswordService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _enviadorCorreo = enviadorCorreo;
            _opciones = opciones;
            _entorno = entorno;
            _logger = logger;

            // Clave propia para estos tokens, derivada de Jwt:Key (no se reutiliza la misma clave tal cual).
            _claveFirma = SHA256.HashData(Encoding.UTF8.GetBytes("civix-recuperacion-password:" + jwt.Key));
        }

        /// <summary>
        /// Si el correo pertenece a un usuario activo, le envía el enlace de recuperación.
        /// Nunca indica si el correo existe: el controlador siempre responde lo mismo.
        /// </summary>
        public async Task SolicitarAsync(string correo, CancellationToken cancelacion)
        {
            var correoNormalizado = correo.Trim().ToLowerInvariant();

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Correo == correoNormalizado && u.Activo, cancelacion);

            if (usuario is null)
            {
                _logger.LogInformation("Recuperación solicitada para un correo no registrado o inactivo.");
                return;
            }

            var token = GenerarToken(usuario);
            var enlace = $"{_opciones.UrlFrontend.TrimEnd('/')}/restablecer-password?token={Uri.EscapeDataString(token)}";

            try
            {
                var enviado = await _enviadorCorreo.EnviarAsync(
                    usuario.Correo,
                    "Restablece tu contraseña de CIVIX",
                    ConstruirCorreo(usuario.Nombre, enlace),
                    cancelacion);

                if (!enviado)
                {
                    if (_entorno.IsDevelopment())
                    {
                        // Sin servidor SMTP configurado: en desarrollo el enlace se muestra en la consola
                        // para poder probar el flujo completo.
                        _logger.LogWarning(
                            "[CIVIX] SMTP no configurado. Enlace de recuperación para {Correo}:\n{Enlace}",
                            usuario.Correo, enlace);
                    }
                    else
                    {
                        _logger.LogError("No hay servidor SMTP configurado: no se pudo enviar el correo de recuperación.");
                    }
                }
            }
            catch (Exception ex)
            {
                // No se revela el error al usuario (evita confirmar que el correo existe); queda en el registro.
                _logger.LogError(ex, "No se pudo enviar el correo de recuperación a {Correo}.", usuario.Correo);
            }
        }

        /// <summary>Indica si el token sigue siendo válido (para mostrar o no el formulario).</summary>
        public async Task<bool> ValidarAsync(string token, CancellationToken cancelacion)
        {
            return await ObtenerUsuarioDelTokenAsync(token, cancelacion) is not null;
        }

        /// <summary>Cambia la contraseña si el token es válido. El token queda inutilizado.</summary>
        public async Task<ResultadoOperacion<bool>> RestablecerAsync(
            string token, string password, CancellationToken cancelacion)
        {
            var usuario = await ObtenerUsuarioDelTokenAsync(token, cancelacion);

            if (usuario is null)
            {
                return ResultadoOperacion<bool>.Invalido(MensajeTokenInvalido);
            }

            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, password);
            await _context.SaveChangesAsync(cancelacion);

            _logger.LogInformation("Contraseña restablecida para el usuario {UsuarioId}.", usuario.Id);
            return ResultadoOperacion<bool>.Ok(true);
        }

        // ------------------------------------------------------------------ Token

        private string GenerarToken(Usuario usuario)
        {
            var vence = DateTimeOffset.UtcNow.AddMinutes(_opciones.ExpiracionMinutos).ToUnixTimeSeconds();
            var datos = Encoding.UTF8.GetBytes($"{usuario.Id}:{vence}:{Sello(usuario.PasswordHash)}");

            return $"{WebEncoders.Base64UrlEncode(datos)}.{WebEncoders.Base64UrlEncode(Firmar(datos))}";
        }

        /// <summary>Devuelve el usuario (con seguimiento de EF) si el token es auténtico, vigente y de un solo uso.</summary>
        private async Task<Usuario?> ObtenerUsuarioDelTokenAsync(string token, CancellationToken cancelacion)
        {
            var partes = (token ?? string.Empty).Trim().Split('.');
            if (partes.Length != 2)
            {
                return null;
            }

            byte[] datos;
            byte[] firma;

            try
            {
                datos = WebEncoders.Base64UrlDecode(partes[0]);
                firma = WebEncoders.Base64UrlDecode(partes[1]);
            }
            catch (FormatException)
            {
                return null;
            }

            // Comparación en tiempo constante para no filtrar información por tiempos de respuesta.
            if (!CryptographicOperations.FixedTimeEquals(Firmar(datos), firma))
            {
                return null;
            }

            var campos = Encoding.UTF8.GetString(datos).Split(':');
            if (campos.Length != 3
                || !int.TryParse(campos[0], out var usuarioId)
                || !long.TryParse(campos[1], out var vence))
            {
                return null;
            }

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > vence)
            {
                return null;
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, cancelacion);
            if (usuario is null || !usuario.Activo)
            {
                return null;
            }

            // Si la contraseña ya cambió (el enlace se usó o se cambió por otro medio), el sello no coincide.
            var selloActual = Encoding.UTF8.GetBytes(Sello(usuario.PasswordHash));
            if (!CryptographicOperations.FixedTimeEquals(selloActual, Encoding.UTF8.GetBytes(campos[2])))
            {
                return null;
            }

            return usuario;
        }

        private byte[] Firmar(byte[] datos) => HMACSHA256.HashData(_claveFirma, datos);

        /// <summary>Resumen corto del hash de la contraseña actual (no revela la contraseña).</summary>
        private static string Sello(string passwordHash) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)))[..16];

        // ------------------------------------------------------------------ Correo

        private string ConstruirCorreo(string nombre, string enlace)
        {
            var nombreSeguro = WebUtility.HtmlEncode(nombre);
            var enlaceSeguro = WebUtility.HtmlEncode(enlace);
            var minutos = _opciones.ExpiracionMinutos;

            return $"""
                <!doctype html>
                <html lang="es">
                <body style="margin:0;padding:0;background:#F1F2F0;font-family:Segoe UI,Arial,sans-serif;color:#24333B;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#F1F2F0;padding:32px 12px;">
                    <tr><td align="center">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:#FFFFFF;border:1px solid #E0E5E8;border-radius:12px;overflow:hidden;">
                        <tr><td style="background:#102A35;padding:22px 28px;">
                          <span style="display:inline-block;background:#E6A126;color:#102A35;font-weight:800;padding:6px 10px;border-radius:6px;">CIVIX</span>
                          <span style="color:#C4D2D8;font-size:12px;letter-spacing:2px;margin-left:10px;">CONTROL DE OBRA</span>
                        </td></tr>
                        <tr><td style="padding:28px;">
                          <h1 style="margin:0 0 12px;font-size:22px;color:#102A35;">Restablece tu contraseña</h1>
                          <p style="margin:0 0 16px;line-height:1.6;">Hola, {nombreSeguro}:</p>
                          <p style="margin:0 0 24px;line-height:1.6;">
                            Recibimos una solicitud para restablecer la contraseña de tu cuenta de CIVIX.
                            Haz clic en el botón para crear una nueva. El enlace vence en {minutos} minutos y solo se puede usar una vez.
                          </p>
                          <p style="margin:0 0 24px;">
                            <a href="{enlaceSeguro}" style="display:inline-block;background:#E6A126;color:#102A35;font-weight:700;text-decoration:none;padding:12px 22px;border-radius:8px;">Crear nueva contraseña</a>
                          </p>
                          <p style="margin:0 0 8px;font-size:13px;color:#5F6D76;">Si el botón no funciona, copia este enlace en tu navegador:</p>
                          <p style="margin:0 0 24px;font-size:12px;word-break:break-all;color:#294653;">{enlaceSeguro}</p>
                          <p style="margin:0;font-size:13px;color:#5F6D76;line-height:1.6;">
                            Si tú no pediste este cambio, ignora este mensaje: tu contraseña actual seguirá funcionando.
                          </p>
                        </td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;
        }
    }
}
