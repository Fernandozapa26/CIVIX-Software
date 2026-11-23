using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using civix_backend.DTOs.Auth;
using civix_backend.Models;
using civix_backend.Services;

namespace civix_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AccesoObrasService _accesoObrasService;
        private readonly RecuperacionPasswordService _recuperacionPassword;

        /// <summary>Nombre de la política de límite de intentos (definida en Program.cs).</summary>
        public const string PoliticaRecuperacion = "recuperacion-password";

        public AuthController(
            AuthService authService,
            AccesoObrasService accesoObrasService,
            RecuperacionPasswordService recuperacionPassword)
        {
            _authService = authService;
            _accesoObrasService = accesoObrasService;
            _recuperacionPassword = recuperacionPassword;
        }

        /// <summary>
        /// POST /api/auth/olvide-password — envía el enlace de recuperación al correo.
        /// Siempre responde lo mismo, exista o no la cuenta, para no revelar qué correos están registrados.
        /// </summary>
        [HttpPost("olvide-password")]
        [AllowAnonymous]
        [EnableRateLimiting(PoliticaRecuperacion)]
        public async Task<IActionResult> OlvidePassword([FromBody] SolicitarRecuperacionDto dto, CancellationToken cancelacion)
        {
            await _recuperacionPassword.SolicitarAsync(dto.Correo, cancelacion);

            return Ok(new
            {
                Mensaje = "Si el correo está registrado, te enviamos un enlace para restablecer la contraseña. " +
                          "Revisa tu bandeja de entrada y la carpeta de spam."
            });
        }

        /// <summary>POST /api/auth/validar-token-recuperacion — indica si el enlace todavía sirve.</summary>
        [HttpPost("validar-token-recuperacion")]
        [AllowAnonymous]
        [EnableRateLimiting(PoliticaRecuperacion)]
        public async Task<IActionResult> ValidarTokenRecuperacion([FromBody] ValidarTokenRecuperacionDto dto, CancellationToken cancelacion)
        {
            return await _recuperacionPassword.ValidarAsync(dto.Token, cancelacion)
                ? Ok(new { Valido = true })
                : BadRequest(new { Mensaje = RecuperacionPasswordService.MensajeTokenInvalido });
        }

        /// <summary>POST /api/auth/restablecer-password — guarda la nueva contraseña si el enlace es válido.</summary>
        [HttpPost("restablecer-password")]
        [AllowAnonymous]
        [EnableRateLimiting(PoliticaRecuperacion)]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordDto dto, CancellationToken cancelacion)
        {
            var resultado = await _recuperacionPassword.RestablecerAsync(dto.Token, dto.Password, cancelacion);

            if (!resultado.Exito)
            {
                return StatusCode(resultado.CodigoError, new { Mensaje = resultado.Mensaje });
            }

            return Ok(new { Mensaje = "Tu contraseña se actualizó. Ya puedes iniciar sesión con la nueva contraseña." });
        }

        /// <summary>POST /api/auth/login — devuelve el token si las credenciales son correctas.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var respuesta = await _authService.LoginAsync(dto);

            if (respuesta is null)
            {
                return Unauthorized(new { Mensaje = "Correo o contraseña incorrectos." });
            }

            return Ok(respuesta);
        }

        /// <summary>
        /// POST /api/auth/register — registro público (queda como Cliente).
        /// Si la petición trae el token de un Administrador, se respeta el RolId enviado.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var esAdministrador = User.IsInRole(RolesSistema.Administrador);

            var resultado = await _authService.RegistrarAsync(dto, esAdministrador);

            if (resultado.Datos is null)
            {
                var error = new { Mensaje = resultado.Error };
                return resultado.EsConflicto ? Conflict(error) : BadRequest(error);
            }

            return StatusCode(StatusCodes.Status201Created, resultado.Datos);
        }

        /// <summary>GET /api/auth/me — datos del usuario del token y obras a las que tiene acceso.</summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var obras = await _accesoObrasService.ObtenerObrasPermitidasAsync(User);

            var respuesta = new UsuarioActualDto
            {
                UsuarioId = AccesoObrasService.ObtenerUsuarioId(User) ?? 0,
                Nombre = User.FindFirst(AuthService.ClaimNombre)?.Value ?? string.Empty,
                Correo = User.FindFirst(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Email)?.Value ?? string.Empty,
                Roles = User.FindAll(AuthService.ClaimRol).Select(c => c.Value).ToList(),
                SoloLectura = AccesoObrasService.EsSoloLectura(User),
                TodasLasObras = obras is null,
                ObrasPermitidas = obras ?? new List<int>()
            };

            return Ok(respuesta);
        }
    }
}
