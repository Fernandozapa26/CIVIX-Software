using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using civix_backend.DTOs.Cotizaciones;
using civix_backend.Models;
using civix_backend.Services;

namespace civix_backend.Controllers
{
    /// <summary>
    /// Cotizaciones (RF-008, RF-009). El PDF (RF-010) se genera en el frontend
    /// con los datos que devuelve GET /api/cotizaciones/{id}.
    /// - Administrador: todas; aprueba y rechaza.
    /// - Ingeniero: solo las que creó.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RolesSistema.Gestion)]
    public class CotizacionesController : ControllerBase
    {
        private readonly CotizacionesService _servicio;

        public CotizacionesController(CotizacionesService servicio)
        {
            _servicio = servicio;
        }

        /// <summary>GET /api/cotizaciones?estado=Borrador</summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? estado)
        {
            return Ok(await _servicio.ListarAsync(User, estado));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Obtener(int id)
        {
            return Responder(await _servicio.ObtenerAsync(User, id));
        }

        /// <summary>POST /api/cotizaciones — RF-008 y RF-009.</summary>
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] GuardarCotizacionDto dto)
        {
            return Responder(await _servicio.CrearAsync(User, dto), "Cotización creada.");
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarCotizacionDto dto)
        {
            return Responder(await _servicio.ActualizarAsync(User, id, dto), "Cotización actualizada.");
        }

        /// <summary>DELETE /api/cotizaciones/{id} — borrado lógico (Estado = Anulada).</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Anular(int id)
        {
            return Responder(await _servicio.AnularAsync(User, id), "Cotización anulada.");
        }

        /// <summary>POST /api/cotizaciones/{id}/aprobar — crea la obra automáticamente.</summary>
        [HttpPost("{id:int}/aprobar")]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Aprobar(int id, [FromBody] AprobarCotizacionDto dto)
        {
            return Responder(await _servicio.AprobarAsync(User, id, dto), "Cotización aprobada y obra creada.");
        }

        [HttpPost("{id:int}/rechazar")]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Rechazar(int id)
        {
            return Responder(await _servicio.RechazarAsync(User, id), "Cotización rechazada.");
        }

        private IActionResult Responder<T>(ResultadoOperacion<T> resultado, string? mensaje = null)
        {
            if (!resultado.Exito)
            {
                return StatusCode(resultado.CodigoError, new { Mensaje = resultado.Mensaje });
            }

            return mensaje is null
                ? Ok(resultado.Dato)
                : Ok(new { Mensaje = mensaje, Datos = resultado.Dato });
        }
    }
}
