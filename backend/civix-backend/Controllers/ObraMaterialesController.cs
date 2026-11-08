using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using civix_backend.DTOs.ObraMateriales;
using civix_backend.Models;
using civix_backend.Services;

namespace civix_backend.Controllers
{
    /// <summary>
    /// Materiales asociados a una obra (RF-006 registrar, RF-007 consultar).
    /// Actores: Administrador e Ingeniero.
    /// </summary>
    [ApiController]
    [Route("api/obras/{obraId:int}/materiales")]
    [Authorize(Roles = RolesSistema.Gestion)]
    public class ObraMaterialesController : ControllerBase
    {
        private readonly ObraMaterialesService _servicio;

        public ObraMaterialesController(ObraMaterialesService servicio)
        {
            _servicio = servicio;
        }

        /// <summary>GET /api/obras/{obraId}/materiales — RF-007.</summary>
        [HttpGet]
        public async Task<IActionResult> Listar(int obraId)
        {
            return Responder(await _servicio.ListarPorObraAsync(User, obraId));
        }

        /// <summary>POST /api/obras/{obraId}/materiales — RF-006.</summary>
        [HttpPost]
        public async Task<IActionResult> Registrar(int obraId, [FromBody] RegistrarObraMaterialDto dto)
        {
            var resultado = await _servicio.RegistrarAsync(User, obraId, dto);

            if (!resultado.Exito) return Responder(resultado);

            return Ok(new { Mensaje = "Material registrado en la obra.", Datos = resultado.Dato });
        }

        /// <summary>PUT /api/obras/{obraId}/materiales/{id} — cantidad, estado y cotización.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int obraId, int id, [FromBody] ActualizarObraMaterialDto dto)
        {
            var resultado = await _servicio.ActualizarAsync(User, obraId, id, dto);

            if (!resultado.Exito) return Responder(resultado);

            return Ok(new { Mensaje = "Material actualizado.", Datos = resultado.Dato });
        }

        /// <summary>DELETE /api/obras/{obraId}/materiales/{id} — solo si no tiene retiros.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int obraId, int id)
        {
            var resultado = await _servicio.EliminarAsync(User, obraId, id);

            if (!resultado.Exito) return Responder(resultado);

            return Ok(new { Mensaje = "Material eliminado de la obra." });
        }

        private IActionResult Responder<T>(ResultadoOperacion<T> resultado)
        {
            return resultado.Exito
                ? Ok(resultado.Dato)
                : StatusCode(resultado.CodigoError, new { Mensaje = resultado.Mensaje });
        }
    }
}
