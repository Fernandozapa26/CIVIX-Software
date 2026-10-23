
using Microsoft.AspNetCore.Mvc;

namespace civix_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaterialesController : ControllerBase
    {
        [HttpGet]
        public IActionResult ObtenerMateriales()
        {
            var material = new[]
            {
                new
                {
                    Id = 1,
                    Nombre = "Cemento",
                    Cantidad = 50,
                    Estado = "En bodega"
                },
                new
                {
                    Id = 2,
                    Nombre = "Varilla de acero",
                    Cantidad = 20,
                    Estado = "Pedido"
                }
            };

            return Ok(material);
        }

        [HttpGet("prueba")]
        public IActionResult MaterialesCivix()
        {
            return Ok("Se abrió correctamente el controlador de materiales");
        }
    }
}
