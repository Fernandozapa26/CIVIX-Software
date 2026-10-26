
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
    }
}
