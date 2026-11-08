using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using civix_backend.Data;
using civix_backend.Models;

namespace civix_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ObrasController : ControllerBase
{
    private readonly CivixDbContext _context;

    public ObrasController(CivixDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Obra>>> ObtenerObras()
    {
        var obras = await _context.Obras.AsNoTracking().ToListAsync();
        return Ok(obras);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Obra>> ObtenerObra(int id)
    {
        var obra = await _context.Obras.FindAsync(id);
        if (obra is null) return NotFound();
        return Ok(obra);
    }

    [HttpPost]
    public async Task<ActionResult<Obra>> CrearObra([FromBody] Obra obra)
    {
        if (string.IsNullOrWhiteSpace(obra.Nombre))
            return BadRequest("El nombre es obligatorio.");

        if (obra.FechaCreacion == default)
            obra.FechaCreacion = DateTime.UtcNow;

        _context.Obras.Add(obra);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerObra), new { id = obra.Id }, obra);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarObra(int id, [FromBody] Obra obra)
    {
        var existente = await _context.Obras.FindAsync(id);
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

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarObra(int id)
    {
        var obra = await _context.Obras.FindAsync(id);
        if (obra is null) return NotFound();

        _context.Obras.Remove(obra);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}