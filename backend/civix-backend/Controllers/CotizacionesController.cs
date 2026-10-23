
using Microsoft.AspNetCore.Mvc;

namespace civix_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CotizacionesController : ControllerBase
    {
        [HttpGet]
        public IActionResult ObtenerCotizaciones()
        {
            var cotizaciones = new[]
            {
                new
                {
                    Id = 1,
                    Cliente = "Cliente de ejemplo",
                    Estado = "Pendiente",
                    Total = 1500000
                },
                new
                {
                    Id = 2,
                    Cliente = "Otro cliente",
                    Estado = "Aprobada",
                    Total = 2500000
                }
            };

            return Ok(cotizaciones);
        }
    }
}
