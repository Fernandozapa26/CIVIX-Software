using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using civix_backend.Data;
using civix_backend.Models;

namespace civix_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.Todos)]
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
    [Authorize(Roles = RolesSistema.Gestion)]
    public async Task<ActionResult<Obra>> CrearObra([FromBody] Obra obra)
    {
        if (string.IsNullOrWhiteSpace(obra.Nombre))
            return BadRequest("El nombre es obligatorio.");

        if (obra.FechaFinEstimada.HasValue && obra.FechaInicio != default &&
            obra.FechaFinEstimada.Value < obra.FechaInicio)
            return BadRequest("La fecha de fin estimada no puede ser anterior a la fecha de inicio.");

        if (obra.Presupuesto < 0)
            return BadRequest("El presupuesto no puede ser negativo.");

        if (obra.Personal < 0)
            return BadRequest("El personal no puede ser negativo.");

        if (obra.FechaCreacion == default)
            obra.FechaCreacion = DateTime.Now;

        _context.Obras.Add(obra);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerObra), new { id = obra.Id }, obra);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Gestion)]
    public async Task<IActionResult> ActualizarObra(int id, [FromBody] Obra obra)
    {
        if (string.IsNullOrWhiteSpace(obra.Nombre))
            return BadRequest("El nombre es obligatorio.");

        if (obra.FechaFinEstimada.HasValue && obra.FechaInicio != default &&
            obra.FechaFinEstimada.Value < obra.FechaInicio)
            return BadRequest("La fecha de fin estimada no puede ser anterior a la fecha de inicio.");

        if (obra.Presupuesto < 0)
            return BadRequest("El presupuesto no puede ser negativo.");

        if (obra.Personal < 0)
            return BadRequest("El personal no puede ser negativo.");

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
    [Authorize(Roles = RolesSistema.Gestion)]
    public async Task<IActionResult> EliminarObra(int id)
    {
        var obra = await _context.Obras.FindAsync(id);
        if (obra is null) return NotFound();

        var tieneAvances = await _context.Avances.AnyAsync(a => a.ObraId == id);
        var tieneMateriales = await _context.ObraMateriales.AnyAsync(m => m.ObraId == id);
        var tieneCotizaciones = await _context.Cotizaciones.AnyAsync(c => c.ObraId == id);

        if (tieneAvances || tieneMateriales || tieneCotizaciones)
            return Conflict("No se puede eliminar la obra porque tiene avances, materiales o cotizaciones asociadas.");

        _context.Obras.Remove(obra);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}