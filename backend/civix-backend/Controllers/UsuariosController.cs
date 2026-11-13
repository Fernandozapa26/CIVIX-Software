using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using civix_backend.DTOs.Usuarios;
using civix_backend.Models;
using civix_backend.Services;

namespace civix_backend.Controllers
{
    /// <summary>
    /// Administración de usuarios (solo Administrador).
    /// - GET    /api/usuarios               → todos los usuarios con rol, estado y fecha de registro.
    /// - PUT    /api/usuarios/{id}          → editar nombre, correo, rol, estado y (opcional) contraseña.
    /// - DELETE /api/usuarios/{id}          → desactivar (no se borra: tiene historial asociado).
    /// - POST   /api/usuarios/{id}/reactivar → volver a activar.
    /// Para crear usuarios se usa POST /api/auth/register con el token del Administrador.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RolesSistema.Administrador)]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuariosService _servicio;

        public UsuariosController(UsuariosService servicio)
        {
            _servicio = servicio;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioAdminDto>>> Listar()
        {
            return Ok(await _servicio.ListarAsync());
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarUsuarioDto dto)
        {
            return Responder(await _servicio.ActualizarAsync(User, id, dto));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Desactivar(int id)
        {
            return Responder(await _servicio.CambiarEstadoAsync(User, id, false));
        }

        [HttpPost("{id:int}/reactivar")]
        public async Task<IActionResult> Reactivar(int id)
        {
            return Responder(await _servicio.CambiarEstadoAsync(User, id, true));
        }

        private IActionResult Responder(ResultadoOperacion<UsuarioAdminDto> resultado)
        {
            if (resultado.Exito)
            {
                return Ok(resultado.Dato);
            }

            return StatusCode(resultado.CodigoError, new { Mensaje = resultado.Mensaje });
        }
    }
}
