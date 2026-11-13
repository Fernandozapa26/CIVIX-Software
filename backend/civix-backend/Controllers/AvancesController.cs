using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using civix_backend.Data;
using civix_backend.DTOs.Avances;
using civix_backend.Models;
using civix_backend.Services;

namespace civix_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.Todos)]
public class AvancesController : ControllerBase
{
    private readonly CivixDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly AccesoObrasService _accesoObras;

    public AvancesController(CivixDbContext context, IWebHostEnvironment env, AccesoObrasService accesoObras)
    {
        _context = context;
        _env = env;
        _accesoObras = accesoObras;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AvanceDto>>> ObtenerAvances([FromQuery] int? obraId)
    {
        var query = _context.Avances.AsNoTracking().AsQueryable();
        if (obraId.HasValue)
            query = query.Where(a => a.ObraId == obraId.Value);

        // Ingeniero y Cliente solo ven los avances de sus obras asignadas.
        var permitidas = await _accesoObras.ObtenerObrasPermitidasAsync(User);
        if (permitidas is not null)
            query = query.Where(a => permitidas.Contains(a.ObraId));

        var avances = await query.OrderByDescending(a => a.Fecha).ToListAsync();

        // Nombre de quien registró cada avance (una sola consulta para todos).
        var nombres = await NombresDeUsuariosAsync(avances.Select(a => a.RegistradoPorUsuarioId));

        return Ok(avances.Select(a => AvanceDto.Desde(a, nombres.GetValueOrDefault(a.RegistradoPorUsuarioId))));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AvanceDto>> ObtenerAvance(int id)
    {
        var avance = await _context.Avances.FindAsync(id);
        if (avance is null) return NotFound();
        if (!await _accesoObras.PuedeAccederAObraAsync(User, avance.ObraId))
            return Forbid();

        var nombres = await NombresDeUsuariosAsync(new[] { avance.RegistradoPorUsuarioId });
        return Ok(AvanceDto.Desde(avance, nombres.GetValueOrDefault(avance.RegistradoPorUsuarioId)));
    }

    [HttpPost]
    [Authorize(Roles = RolesSistema.Gestion)]
    public async Task<ActionResult<Avance>> CrearAvance([FromBody] Avance avance)
    {
        if (avance.ObraId <= 0)
            return BadRequest("La obra es obligatoria.");

        var obraExiste = await _context.Obras.AnyAsync(o => o.Id == avance.ObraId);
        if (!obraExiste)
            return BadRequest("La obra indicada no existe.");

        if (!await _accesoObras.PuedeAccederAObraAsync(User, avance.ObraId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(avance.Descripcion))
            return BadRequest("La descripcion es obligatoria.");
        if (avance.PorcentajeAvance.HasValue && (avance.PorcentajeAvance < 0 || avance.PorcentajeAvance > 100))
            return BadRequest("El porcentaje debe estar entre 0 y 100.");

        if (avance.Fecha == default)
            avance.Fecha = DateTime.Today;
        else
            avance.Fecha = NormalizarFecha(avance.Fecha);
        if (avance.Imagenes is null)
            avance.Imagenes = new();

        var usuarioId = AccesoObrasService.ObtenerUsuarioId(User);
        if (usuarioId is null)
            return Unauthorized("No se pudo identificar al usuario autenticado.");
        avance.RegistradoPorUsuarioId = usuarioId.Value;

        _context.Avances.Add(avance);
        await _context.SaveChangesAsync();

        await RecalcularAvanceObra(avance.ObraId);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerAvance), new { id = avance.Id }, avance);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Gestion)]
    public async Task<IActionResult> ActualizarAvance(int id, [FromBody] Avance avance)
    {
        if (avance.ObraId <= 0)
            return BadRequest("La obra es obligatoria.");

        var obraExiste = await _context.Obras.AnyAsync(o => o.Id == avance.ObraId);
        if (!obraExiste)
            return BadRequest("La obra indicada no existe.");

        if (!await _accesoObras.PuedeAccederAObraAsync(User, avance.ObraId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(avance.Descripcion))
            return BadRequest("La descripcion es obligatoria.");
        if (avance.PorcentajeAvance.HasValue && (avance.PorcentajeAvance < 0 || avance.PorcentajeAvance > 100))
            return BadRequest("El porcentaje debe estar entre 0 y 100.");

        var existente = await _context.Avances.FindAsync(id);
        if (existente is null) return NotFound();
        if (!await _accesoObras.PuedeAccederAObraAsync(User, existente.ObraId))
            return Forbid();

        var obraAnteriorId = existente.ObraId;

        existente.ObraId = avance.ObraId;
        existente.Fecha = avance.Fecha == default ? existente.Fecha : NormalizarFecha(avance.Fecha);
        existente.Descripcion = avance.Descripcion;
        existente.PorcentajeAvance = avance.PorcentajeAvance;
        existente.Imagenes = avance.Imagenes ?? new();
        // RegistradoPorUsuarioId no se cambia: queda el usuario que registró el avance.

        await _context.SaveChangesAsync();

        await RecalcularAvanceObra(avance.ObraId);
        if (obraAnteriorId != avance.ObraId)
            await RecalcularAvanceObra(obraAnteriorId);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolesSistema.Gestion)]
    public async Task<IActionResult> EliminarAvance(int id)
    {
        var avance = await _context.Avances.FindAsync(id);
        if (avance is null) return NotFound();
        if (!await _accesoObras.PuedeAccederAObraAsync(User, avance.ObraId))
            return Forbid();

        var obraId = avance.ObraId;

        foreach (var imagen in avance.Imagenes ?? new List<string>())
        {
            if (!imagen.StartsWith("/uploads/avances/", StringComparison.OrdinalIgnoreCase))
                continue;

            var nombreArchivo = Path.GetFileName(imagen);
            if (string.IsNullOrWhiteSpace(nombreArchivo))
                continue;

            var carpeta = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "avances");
            var ruta = Path.Combine(carpeta, nombreArchivo);

            try
            {
                if (System.IO.File.Exists(ruta))
                    System.IO.File.Delete(ruta);
            }
            catch
            {
                // No interrumpir la eliminación del avance si falla el borrado del archivo.
            }
        }

        _context.Avances.Remove(avance);
        await _context.SaveChangesAsync();

        await RecalcularAvanceObra(obraId);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Subida de imagenes. Devuelve la URL relativa que puede guardarse en Avance.Imagenes.
    [HttpPost("imagenes")]
    [Authorize(Roles = RolesSistema.Gestion)]
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

    /// <summary>Diccionario Id → Nombre de los usuarios indicados.</summary>
    private async Task<Dictionary<int, string>> NombresDeUsuariosAsync(IEnumerable<int> usuarioIds)
    {
        var ids = usuarioIds.Distinct().ToList();

        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nombre);
    }

    private async Task RecalcularAvanceObra(int obraId)
    {
        if (obraId <= 0) return;

        var obra = await _context.Obras.FindAsync(obraId);
        if (obra is null) return;

        var ultimo = await _context.Avances
            .AsNoTracking()
            .Where(a => a.ObraId == obraId)
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync();

        if (ultimo is null || !ultimo.PorcentajeAvance.HasValue)
        {
            obra.Avance = 0;
        }
        else
        {
            obra.Avance = (int)Math.Round(ultimo.PorcentajeAvance.Value);
        }
    }

    private static DateTime NormalizarFecha(DateTime value)
    {
        if (value == default) return value;
        return value.Kind == DateTimeKind.Utc ? value.ToLocalTime().Date : value.Date;
    }
}