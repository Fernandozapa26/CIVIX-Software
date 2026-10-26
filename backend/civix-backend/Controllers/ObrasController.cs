using Microsoft.AspNetCore.Mvc;
using civix_backend.Models;

namespace civix_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ObrasController : ControllerBase
{
    private static readonly List<Obra> _obras = new()
    {
        new Obra
        {
            Id = 1,
            Nombre = "Alameda 72",
            Descripcion = "Conjunto residencial de 3 torres",
            Cliente = "Inmobiliaria Horizonte",
            Ubicacion = "El Poblado, Medellín",
            FechaInicio = new DateTime(2025, 9, 1),
            FechaFinEstimada = new DateTime(2026, 8, 30),
            Estado = "En ejecución",
            Presupuesto = 2_480_000_000m,
            Avance = 68,
            Personal = 42
        },
        new Obra
        {
            Id = 2,
            Nombre = "Torre Norte 14",
            Descripcion = "Edificio corporativo de 18 pisos",
            Cliente = "Grupo Empresarial Andino",
            Ubicacion = "Laureles, Medellín",
            FechaInicio = new DateTime(2026, 1, 10),
            FechaFinEstimada = new DateTime(2027, 6, 30),
            Estado = "En planeación",
            Presupuesto = 4_120_000_000m,
            Avance = 12,
            Personal = 18
        },
        new Obra
        {
            Id = 3,
            Nombre = "Puente Vehicular Sector Norte",
            Descripcion = "Puente de concreto de 120 m",
            Cliente = "Alcaldía de Medellín",
            Ubicacion = "Bello, Antioquia",
            FechaInicio = new DateTime(2026, 3, 1),
            FechaFinEstimada = new DateTime(2027, 2, 28),
            Estado = "En planeación",
            Presupuesto = 3_200_000_000m,
            Avance = 5,
            Personal = 24
        },
        new Obra
        {
            Id = 4,
            Nombre = "Centro Logístico Sur",
            Descripcion = "Bodega industrial de 12.000 m²",
            Cliente = "Logística del Valle",
            Ubicacion = "Itagüí, Antioquia",
            FechaInicio = new DateTime(2024, 6, 15),
            FechaFinEstimada = new DateTime(2025, 12, 20),
            Estado = "Finalizada",
            Presupuesto = 1_890_000_000m,
            Avance = 100,
            Personal = 0
        }
    };

    private static readonly object _lock = new();

    [HttpGet]
    public ActionResult<IEnumerable<Obra>> ObtenerObras()
    {
        lock (_lock)
        {
            return Ok(_obras.ToList());
        }
    }

    [HttpGet("{id:int}")]
    public ActionResult<Obra> ObtenerObra(int id)
    {
        lock (_lock)
        {
            var obra = _obras.FirstOrDefault(o => o.Id == id);
            if (obra is null) return NotFound();
            return Ok(obra);
        }
    }

    [HttpPost]
    public ActionResult<Obra> CrearObra([FromBody] Obra obra)
    {
        if (string.IsNullOrWhiteSpace(obra.Nombre))
            return BadRequest("El nombre es obligatorio.");

        lock (_lock)
        {
            obra.Id = _obras.Count == 0 ? 1 : _obras.Max(o => o.Id) + 1;
            if (string.IsNullOrWhiteSpace(obra.Estado))
                obra.Estado = "Planificada";
            _obras.Add(obra);
            return CreatedAtAction(nameof(ObtenerObra), new { id = obra.Id }, obra);
        }
    }

    [HttpPut("{id:int}")]
    public IActionResult ActualizarObra(int id, [FromBody] Obra obra)
    {
        lock (_lock)
        {
            var existente = _obras.FirstOrDefault(o => o.Id == id);
            if (existente is null) return NotFound();

            existente.Nombre = obra.Nombre;
            existente.Descripcion = obra.Descripcion;
            existente.Cliente = obra.Cliente;
            existente.Ubicacion = obra.Ubicacion;
            existente.FechaInicio = obra.FechaInicio;
            existente.FechaFinEstimada = obra.FechaFinEstimada;
            existente.Estado = obra.Estado;
            existente.Presupuesto = obra.Presupuesto;
            existente.Avance = obra.Avance;
            existente.Personal = obra.Personal;

            return NoContent();
        }
    }

    [HttpDelete("{id:int}")]
    public IActionResult EliminarObra(int id)
    {
        lock (_lock)
        {
            var obra = _obras.FirstOrDefault(o => o.Id == id);
            if (obra is null) return NotFound();
            _obras.Remove(obra);
            return NoContent();
        }
    }
}