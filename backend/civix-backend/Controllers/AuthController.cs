using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public AuthController(AuthService authService, AccesoObrasService accesoObrasService)
        {
            _authService = authService;
            _accesoObrasService = accesoObrasService;
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
