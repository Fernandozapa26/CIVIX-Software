using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    private readonly AccesoObrasService _accesoObras;
    private readonly ImagenesAvanceService _imagenes;

    public AvancesController(CivixDbContext context, AccesoObrasService accesoObras, ImagenesAvanceService imagenes)
    {
        _context = context;
        _accesoObras = accesoObras;
        _imagenes = imagenes;
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

        var avances = await query.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id).ToListAsync();

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
    public async Task<ActionResult<AvanceDto>> CrearAvance([FromBody] GuardarAvanceDto dto)
    {
        var error = await ValidarAsync(dto);
        if (error is not null) return error;

        var usuarioId = AccesoObrasService.ObtenerUsuarioId(User);
        if (usuarioId is null)
            return Unauthorized("No se pudo identificar al usuario autenticado.");

        var avance = new Avance
        {
            ObraId = dto.ObraId,
            Fecha = dto.Fecha is null || dto.Fecha == default ? DateTime.Today : NormalizarFecha(dto.Fecha.Value),
            Descripcion = dto.Descripcion.Trim(),
            PorcentajeAvance = dto.PorcentajeAvance,
            Imagenes = dto.Imagenes ?? new(),
            // Lo asigna el servidor: no se acepta desde el cuerpo de la petición.
            RegistradoPorUsuarioId = usuarioId.Value
        };

        _context.Avances.Add(avance);
        await _context.SaveChangesAsync();

        await RecalcularAvanceObra(avance.ObraId);
        await _context.SaveChangesAsync();

        var nombres = await NombresDeUsuariosAsync(new[] { avance.RegistradoPorUsuarioId });
        return CreatedAtAction(nameof(ObtenerAvance), new { id = avance.Id },
            AvanceDto.Desde(avance, nombres.GetValueOrDefault(avance.RegistradoPorUsuarioId)));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Gestion)]
    public async Task<IActionResult> ActualizarAvance(int id, [FromBody] GuardarAvanceDto dto)
    {
        var existente = await _context.Avances.FindAsync(id);
        if (existente is null) return NotFound();
        if (!await _accesoObras.PuedeAccederAObraAsync(User, existente.ObraId))
            return Forbid();

        var error = await ValidarAsync(dto);
        if (error is not null) return error;

        var obraAnteriorId = existente.ObraId;

        existente.ObraId = dto.ObraId;
        existente.Fecha = dto.Fecha is null || dto.Fecha == default ? existente.Fecha : NormalizarFecha(dto.Fecha.Value);
        existente.Descripcion = dto.Descripcion.Trim();
        existente.PorcentajeAvance = dto.PorcentajeAvance;
        existente.Imagenes = dto.Imagenes ?? new();
        // RegistradoPorUsuarioId no se cambia: queda el usuario que registró el avance.

        await _context.SaveChangesAsync();

        await RecalcularAvanceObra(dto.ObraId);
        if (obraAnteriorId != dto.ObraId)
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
        var imagenes = avance.Imagenes ?? new List<string>();

        _context.Avances.Remove(avance);
        await _context.SaveChangesAsync();

        // Los archivos se borran después de confirmar el borrado en la base.
        foreach (var imagen in imagenes)
            _imagenes.Eliminar(imagen);

        await RecalcularAvanceObra(obraId);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Subida de imágenes. Devuelve la ruta que debe enviarse en Imagenes al crear o editar el avance.
    /// </summary>
    [HttpPost("imagenes")]
    [Authorize(Roles = RolesSistema.Gestion)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImagenesAvanceService.TamanoMaximoBytes + 64 * 1024)]
    public async Task<ActionResult<object>> SubirImagen([FromForm] IFormFile archivo)
    {
        var (ruta, error) = await _imagenes.GuardarAsync(archivo);
        if (error is not null)
            return BadRequest(new { Mensaje = error });

        return Ok(new { url = ruta });
    }

    /// <summary>
    /// GET /api/avances/imagenes/{nombre} — entrega una foto solo si el usuario tiene acceso
    /// a la obra del avance que la contiene. Una foto recién subida que todavía no está en
    /// ningún avance solo la puede ver un Administrador o Ingeniero (quien la está adjuntando).
    /// </summary>
    [HttpGet("imagenes/{nombre}")]
    public async Task<IActionResult> ObtenerImagen(string nombre)
    {
        if (!ImagenesAvanceService.EsNombreValido(nombre))
            return NotFound();

        var archivo = _imagenes.BuscarArchivo(nombre);
        if (archivo is null)
            return NotFound();

        if (!AccesoObrasService.EsAdministrador(User))
        {
            // El nombre ya se validó (GUID + extensión), así que no contiene comodines de LIKE.
            var patron = "%" + ImagenesAvanceService.PrefijoRuta + nombre + "%";
            var obrasConLaImagen = await _context.Database
                .SqlQuery<int>($"SELECT ObraId AS Value FROM dbo.AvancesObra WHERE Imagenes LIKE {patron}")
                .ToListAsync();

            bool permitido;
            if (obrasConLaImagen.Count == 0)
            {
                permitido = User.IsInRole(RolesSistema.Ingeniero);
            }
            else
            {
                var permitidas = await _accesoObras.ObtenerObrasPermitidasAsync(User) ?? new List<int>();
                permitido = obrasConLaImagen.Any(permitidas.Contains);
            }

            // 404 en vez de 403 para no revelar qué fotos existen.
            if (!permitido)
                return NotFound();
        }

        Response.Headers.CacheControl = "private, max-age=3600";
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(archivo, ImagenesAvanceService.TipoContenido(nombre));
    }

    /// <summary>
    /// Validaciones comunes de crear y editar. Devuelve null si todo está bien.
    /// (Los atributos de GuardarAvanceDto ya validan obra obligatoria, descripción y porcentaje.)
    /// </summary>
    private async Task<ActionResult?> ValidarAsync(GuardarAvanceDto dto)
    {
        var obraExiste = await _context.Obras.AnyAsync(o => o.Id == dto.ObraId);
        if (!obraExiste)
            return BadRequest(new { Mensaje = "La obra indicada no existe." });

        if (!await _accesoObras.PuedeAccederAObraAsync(User, dto.ObraId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest(new { Mensaje = "La descripcion es obligatoria." });

        var imagenes = dto.Imagenes ?? new List<string>();
        if (imagenes.Count > ImagenesAvanceService.MaximoImagenesPorAvance)
            return BadRequest(new { Mensaje = $"Un avance admite como máximo {ImagenesAvanceService.MaximoImagenesPorAvance} fotos." });

        // Solo se aceptan rutas devueltas por POST /api/avances/imagenes y que existan en el servidor.
        foreach (var ruta in imagenes)
        {
            var nombre = ImagenesAvanceService.NombreDesdeRuta(ruta);
            if (nombre is null || _imagenes.BuscarArchivo(nombre) is null)
                return BadRequest(new { Mensaje = "Una de las imágenes no es válida. Vuelve a subirla." });
        }

        return null;
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
