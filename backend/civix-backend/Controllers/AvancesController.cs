using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using civix_backend.Data;
using civix_backend.Models;

namespace civix_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvancesController : ControllerBase
{
    private readonly CivixDbContext _context;
    private readonly IWebHostEnvironment _env;

    public AvancesController(CivixDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Avance>>> ObtenerAvances([FromQuery] int? obraId)
    {
        var query = _context.Avances.AsNoTracking().AsQueryable();
        if (obraId.HasValue)
            query = query.Where(a => a.ObraId == obraId.Value);

        var avances = await query.OrderByDescending(a => a.Fecha).ToListAsync();
        return Ok(avances);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Avance>> ObtenerAvance(int id)
    {
        var avance = await _context.Avances.FindAsync(id);
        if (avance is null) return NotFound();
        return Ok(avance);
    }

    [HttpPost]
    public async Task<ActionResult<Avance>> CrearAvance([FromBody] Avance avance)
    {
        if (avance.ObraId <= 0)
            return BadRequest("La obra es obligatoria.");
        if (string.IsNullOrWhiteSpace(avance.Descripcion))
            return BadRequest("La descripcion es obligatoria.");
        if (avance.PorcentajeAvance.HasValue && (avance.PorcentajeAvance < 0 || avance.PorcentajeAvance > 100))
            return BadRequest("El porcentaje debe estar entre 0 y 100.");

        if (avance.Fecha == default)
            avance.Fecha = DateTime.UtcNow;
        if (avance.Imagenes is null)
            avance.Imagenes = new();
        if (avance.RegistradoPorUsuarioId == 0)
            avance.RegistradoPorUsuarioId = 1;

        _context.Avances.Add(avance);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerAvance), new { id = avance.Id }, avance);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarAvance(int id, [FromBody] Avance avance)
    {
        var existente = await _context.Avances.FindAsync(id);
        if (existente is null) return NotFound();

        existente.ObraId = avance.ObraId;
        existente.Fecha = avance.Fecha == default ? existente.Fecha : avance.Fecha;
        existente.Descripcion = avance.Descripcion;
        existente.PorcentajeAvance = avance.PorcentajeAvance;
        existente.Imagenes = avance.Imagenes ?? new();
        if (avance.RegistradoPorUsuarioId != 0)
            existente.RegistradoPorUsuarioId = avance.RegistradoPorUsuarioId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarAvance(int id)
    {
        var avance = await _context.Avances.FindAsync(id);
        if (avance is null) return NotFound();

        _context.Avances.Remove(avance);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Subida de imagenes. Devuelve la URL relativa que puede guardarse en Avance.Imagenes.
    [HttpPost("imagenes")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> SubirImagen([FromForm] IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest("Archivo vacio.");

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!extensionesPermitidas.Contains(ext))
            return BadRequest("Formato de imagen no permitido.");

        var carpeta = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "avances");
        Directory.CreateDirectory(carpeta);

        var nombreArchivo = $"{Guid.NewGuid()}{ext}";
        var rutaCompleta = Path.Combine(carpeta, nombreArchivo);
        using (var stream = System.IO.File.Create(rutaCompleta))
        {
            await archivo.CopyToAsync(stream);
        }

        var url = $"/uploads/avances/{nombreArchivo}";
        return Ok(new { url });
    }
}