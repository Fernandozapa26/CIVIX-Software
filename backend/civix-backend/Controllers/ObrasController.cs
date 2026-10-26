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
            Nombre = "Edificio Residencial Los Pinos",
            Descripcion = "Construcción de edificio de 5 pisos",
            Ubicacion = "Bogotá, Colombia",
            FechaInicio = new DateTime(2026, 1, 15),
            FechaFinEstimada = new DateTime(2026, 12, 20),
            Estado = "En ejecución",
            Presupuesto = 1_500_000_000m
        },
        new Obra
        {
            Id = 2,
            Nombre = "Puente Vehicular Sector Norte",
            Descripcion = "Puente de concreto de 120 m",
            Ubicacion = "Medellín, Colombia",
            FechaInicio = new DateTime(2026, 3, 1),
            FechaFinEstimada = new DateTime(2027, 2, 28),
            Estado = "Planificada",
            Presupuesto = 3_200_000_000m
        }
    };

    private static int _nextId = 3;
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
            obra.Id = _nextId++;
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
            existente.Ubicacion = obra.Ubicacion;
            existente.FechaInicio = obra.FechaInicio;
            existente.FechaFinEstimada = obra.FechaFinEstimada;
            existente.Estado = obra.Estado;
            existente.Presupuesto = obra.Presupuesto;

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