using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using civix_backend.Models;
using civix_backend.Services;

namespace civix_backend.Controllers
{
    /// <summary>Resumen de la pantalla de Inicio, según el rol del usuario.</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RolesSistema.Todos)]
    public class InicioController : ControllerBase
    {
        private readonly InicioService _servicio;

        public InicioController(InicioService servicio)
        {
            _servicio = servicio;
        }

        /// <summary>GET /api/inicio</summary>
        [HttpGet]
        public async Task<IActionResult> Resumen()
        {
            return Ok(await _servicio.ObtenerResumenAsync(User));
        }
    }
}
