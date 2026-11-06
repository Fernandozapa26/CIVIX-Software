
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using civix_backend.Models;

namespace civix_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RolesSistema.Todos)]
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
