
using Microsoft.AspNetCore.Mvc;
using civix_backend.DTOs.Materiales;
using civix_backend.Services;

namespace civix_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaterialesController : ControllerBase
    {
        private readonly MaterialesService _materialesService;

        public MaterialesController(MaterialesService materialesService)
        {
            _materialesService = materialesService;
        }

        [HttpPost]
        public async Task<IActionResult> GuardarMaterial(
            [FromBody] MaterialesDTO dto)
        {
            var material = await _materialesService.GuardarMaterialAsync(dto);

            return Ok(new
            {
                Mensaje = "Material guardado correctamente",
                Datos = material
            });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerMateriales()
        {
            var materiales = await _materialesService.ObtenerMaterialesAsync();

            return Ok(materiales);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerMaterialPorId(int id)
        {
            var material = await _materialesService.ObtenerMaterialPorIdAsync(id);

            if (material == null)
            {
                return NotFound(new
                {
                    Mensaje = "Material no encontrado"
                });
            }

            return Ok(material);

        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarMaterial(int id, [FromBody] MaterialesDTO dto)
        {
            var material = await _materialesService.ActualizarMaterialAsync(id, dto);

            if (material == null)
            {
                return NotFound(new
                {
                    Mensaje = "Material no encontrado"
                });
            }

            return Ok(new
            {
                Mensaje = "Material actualizado correctamente",
                Datos = material
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DesactivarMaterial(int id)
        {
            var desactivado = await _materialesService.DesactivarMaterialAsync(id);

            if (!desactivado)
            {
                return NotFound(new
                {
                    Mensaje = "El material no existe o ya está desactivado"
                });
            }

            return Ok(new
            {
                Mensaje = "Material desactivado correctamente"
            });
        }

    }
}
