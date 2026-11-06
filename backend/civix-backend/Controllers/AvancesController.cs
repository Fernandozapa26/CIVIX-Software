using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using civix_backend.Models;

namespace civix_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvancesController : ControllerBase
{
    private static readonly List<Avance> _avances = new();
    private static readonly object _lock = new();
    private readonly IWebHostEnvironment _env;

    public AvancesController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Avance>> ObtenerAvances([FromQuery] int? obraId)
    {
        lock (_lock)
        {
            var query = _avances.AsQueryable();
            if (obraId.HasValue)
                query = query.Where(a => a.ObraId == obraId.Value);
            return Ok(query.OrderByDescending(a => a.Fecha).ToList());
        }
    }

    [HttpGet("{id:int}")]
    public ActionResult<Avance> ObtenerAvance(int id)
    {
        lock (_lock)
        {
            var avance = _avances.FirstOrDefault(a => a.Id == id);
            if (avance is null) return NotFound();
            return Ok(avance);
        }
    }

    [HttpPost]
    public ActionResult<Avance> CrearAvance([FromBody] Avance avance)
    {
        if (avance.ObraId <= 0)
            return BadRequest("La obra es obligatoria.");
        if (string.IsNullOrWhiteSpace(avance.Descripcion))
            return BadRequest("La descripcion es obligatoria.");
        if (avance.PorcentajeAvance.HasValue && (avance.PorcentajeAvance < 0 || avance.PorcentajeAvance > 100))
            return BadRequest("El porcentaje debe estar entre 0 y 100.");

        lock (_lock)
        {
            avance.Id = _avances.Count == 0 ? 1 : _avances.Max(a => a.Id) + 1;
            if (avance.Fecha == default)
                avance.Fecha = DateTime.UtcNow;
            if (avance.Imagenes is null)
                avance.Imagenes = new();
            _avances.Add(avance);
            return CreatedAtAction(nameof(ObtenerAvance), new { id = avance.Id }, avance);
        }
    }

    [HttpPut("{id:int}")]
    public IActionResult ActualizarAvance(int id, [FromBody] Avance avance)
    {
        lock (_lock)
        {
            var existente = _avances.FirstOrDefault(a => a.Id == id);
            if (existente is null) return NotFound();

            existente.ObraId = avance.ObraId;
            existente.Fecha = avance.Fecha == default ? existente.Fecha : avance.Fecha;
            existente.Descripcion = avance.Descripcion;
            existente.PorcentajeAvance = avance.PorcentajeAvance;
            existente.Imagenes = avance.Imagenes ?? new();
            return NoContent();
        }
    }

    [HttpDelete("{id:int}")]
    public IActionResult EliminarAvance(int id)
    {
        lock (_lock)
        {
            var avance = _avances.FirstOrDefault(a => a.Id == id);
            if (avance is null) return NotFound();
            _avances.Remove(avance);
            return NoContent();
        }
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
