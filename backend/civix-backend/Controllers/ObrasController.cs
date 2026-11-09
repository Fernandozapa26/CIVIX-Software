using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using civix_backend.DTOs.Obras;
using civix_backend.Models;
using civix_backend.Services;

namespace civix_backend.Controllers;

/// <summary>
/// CRUD de obras.
/// - Consultar: Administrador (todas), Ingeniero y Cliente (solo sus obras asignadas).
/// - Crear, editar y eliminar: Administrador e Ingeniero (el ingeniero solo sus obras).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.Todos)]
public class ObrasController : ControllerBase
{
    private readonly ObrasService _servicio;

    public ObrasController(ObrasService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ObraDto>>> ObtenerObras()
    {
        return Ok(await _servicio.ListarAsync(User));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerObra(int id)
    {
        return Responder(await _servicio.ObtenerAsync(User, id));
    }

    [HttpPost]
    [Authorize(Roles = RolesSistema.Gestion)]
    public async Task<IActionResult> CrearObra([FromBody] GuardarObraDto dto)
    {
        var resultado = await _servicio.CrearAsync(User, dto);

        if (!resultado.Exito) return Responder(resultado);

        return CreatedAtAction(nameof(ObtenerObra), new { id = resultado.Dato!.Id }, resultado.Dato);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Gestion)]
    public async Task<IActionResult> ActualizarObra(int id, [FromBody] GuardarObraDto dto)
    {
        return Responder(await _servicio.ActualizarAsync(User, id, dto));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RolesSistema.Gestion)]
    public async Task<IActionResult> EliminarObra(int id)
    {
        var resultado = await _servicio.EliminarAsync(User, id);

        return resultado.Exito ? NoContent() : Responder(resultado);
    }

    private IActionResult Responder<T>(ResultadoOperacion<T> resultado)
    {
        return resultado.Exito
            ? Ok(resultado.Dato)
            : StatusCode(resultado.CodigoError, new { Mensaje = resultado.Mensaje });
    }
}
