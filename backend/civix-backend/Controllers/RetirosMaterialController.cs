using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using civix_backend.DTOs.ObraMateriales;
using civix_backend.Models;
using civix_backend.Services;

namespace civix_backend.Controllers
{
    /// <summary>Retiros de material de bodega: quién retiró, cuánto y para qué obra.</summary>
    [ApiController]
    [Route("api/retiros-material")]
    [Authorize(Roles = RolesSistema.Gestion)]
    public class RetirosMaterialController : ControllerBase
    {
        private readonly ObraMaterialesService _servicio;

        public RetirosMaterialController(ObraMaterialesService servicio)
        {
            _servicio = servicio;
        }

        /// <summary>GET /api/retiros-material?obraId=1 — historial de la obra (origen o destino).</summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] int obraId)
        {
            var resultado = await _servicio.ListarRetirosAsync(User, obraId);

            return resultado.Exito
                ? Ok(resultado.Dato)
                : StatusCode(resultado.CodigoError, new { Mensaje = resultado.Mensaje });
        }

        /// <summary>POST /api/retiros-material — registra un retiro parcial o total.</summary>
        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] RegistrarRetiroDto dto)
        {
            var resultado = await _servicio.RegistrarRetiroAsync(User, dto);

            if (!resultado.Exito)
            {
                return StatusCode(resultado.CodigoError, new { Mensaje = resultado.Mensaje });
            }

            return Ok(new { Mensaje = "Retiro registrado.", Datos = resultado.Dato });
        }
    }
}
