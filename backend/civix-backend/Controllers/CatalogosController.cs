using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using civix_backend.Models;
using civix_backend.Services;

namespace civix_backend.Controllers
{
    /// <summary>
    /// Listas de solo lectura para los selectores del frontend.
    /// Se usa "api/catalogos" para no chocar con el futuro módulo de Obras (api/obras).
    /// </summary>
    [ApiController]
    [Route("api/catalogos")]
    [Authorize(Roles = RolesSistema.Gestion)]
    public class CatalogosController : ControllerBase
    {
        private readonly ObraMaterialesService _servicio;
        private readonly ObrasService _obras;

        public CatalogosController(ObraMaterialesService servicio, ObrasService obras)
        {
            _servicio = servicio;
            _obras = obras;
        }

        /// <summary>
        /// GET /api/catalogos/usuarios?rol=Cliente — usuarios activos de un rol
        /// (Cliente o Ingeniero) para asignar como cliente o responsable de una obra.
        /// </summary>
        [HttpGet("usuarios")]
        public async Task<IActionResult> Usuarios([FromQuery] string rol)
        {
            if (rol != RolesSistema.Cliente && rol != RolesSistema.Ingeniero)
            {
                return BadRequest(new { Mensaje = "El rol debe ser Cliente o Ingeniero." });
            }

            return Ok(await _obras.ListarUsuariosPorRolAsync(rol));
        }

        /// <summary>GET /api/catalogos/obras — obras a las que el usuario tiene acceso.</summary>
        [HttpGet("obras")]
        public async Task<IActionResult> Obras()
        {
            return Ok(await _servicio.ObtenerObrasAsync(User));
        }

        /// <summary>GET /api/catalogos/cotizaciones — cotizaciones para asociar a materiales.</summary>
        [HttpGet("cotizaciones")]
        public async Task<IActionResult> Cotizaciones()
        {
            return Ok(await _servicio.ObtenerCotizacionesAsync());
        }
    }
}
