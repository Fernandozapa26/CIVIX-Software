using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using civix_backend.Data;
using civix_backend.DTOs.Usuarios;
using civix_backend.Models;

namespace civix_backend.Services
{
    /// <summary>
    /// Administración de usuarios (solo Administrador): listar, editar, desactivar y reactivar.
    /// Los usuarios no se borran físicamente porque están relacionados con obras, avances,
    /// cotizaciones y materiales; "eliminar" los deja inactivos y ya no pueden iniciar sesión.
    /// La creación sigue usando POST /api/auth/register.
    /// </summary>
    public class UsuariosService
    {
        private readonly CivixDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public UsuariosService(CivixDbContext context, IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<List<UsuarioAdminDto>> ListarAsync()
        {
            return await _context.Usuarios
                .AsNoTracking()
                .OrderBy(u => u.Nombre)
                .Select(u => new UsuarioAdminDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Correo = u.Correo,
                    RolId = u.UsuariosRoles.OrderBy(ur => ur.RolId).Select(ur => (int?)ur.RolId).FirstOrDefault(),
                    Roles = u.UsuariosRoles.OrderBy(ur => ur.RolId).Select(ur => ur.Rol.Nombre).ToList(),
                    Activo = u.Activo,
                    FechaCreacion = u.FechaCreacion,
                    ObrasAsignadas = u.ObrasAsignadas.Count(ou => ou.Activo)
                })
                .ToListAsync();
        }

        public async Task<ResultadoOperacion<UsuarioAdminDto>> ActualizarAsync(
            ClaimsPrincipal actor, int id, ActualizarUsuarioDto dto)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.UsuariosRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario is null)
            {
                return ResultadoOperacion<UsuarioAdminDto>.NoEncontrado("El usuario no existe.");
            }

            var esElMismo = AccesoObrasService.ObtenerUsuarioId(actor) == id;

            if (esElMismo && dto.RolId != RolesSistema.AdministradorId)
            {
                return ResultadoOperacion<UsuarioAdminDto>.Invalido(
                    "No puedes quitarte el rol de Administrador a ti mismo.");
            }

            if (esElMismo && !dto.Activo)
            {
                return ResultadoOperacion<UsuarioAdminDto>.Invalido("No puedes desactivar tu propia cuenta.");
            }

            var correo = dto.Correo.Trim().ToLowerInvariant();

            if (await _context.Usuarios.AnyAsync(u => u.Correo == correo && u.Id != id))
            {
                return ResultadoOperacion<UsuarioAdminDto>.Conflicto("Ya existe otra cuenta con ese correo.");
            }

            if (!await _context.Roles.AnyAsync(r => r.Id == dto.RolId))
            {
                return ResultadoOperacion<UsuarioAdminDto>.Invalido("El rol indicado no existe.");
            }

            usuario.Nombre = dto.Nombre.Trim();
            usuario.Correo = correo;
            usuario.Activo = dto.Activo;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                usuario.PasswordHash = _passwordHasher.HashPassword(usuario, dto.Password);
            }

            // Cada usuario de CIVIX tiene un solo rol: se quitan los demás y se agrega el nuevo
            // solo si no lo tenía (quitar y volver a agregar la misma fila confundiría a EF Core).
            var rolesSobrantes = usuario.UsuariosRoles.Where(ur => ur.RolId != dto.RolId).ToList();
            _context.UsuariosRoles.RemoveRange(rolesSobrantes);

            if (!usuario.UsuariosRoles.Any(ur => ur.RolId == dto.RolId))
            {
                _context.UsuariosRoles.Add(new UsuarioRol { UsuarioId = usuario.Id, RolId = dto.RolId });
            }

            await _context.SaveChangesAsync();

            return ResultadoOperacion<UsuarioAdminDto>.Ok(await ObtenerDtoAsync(usuario.Id));
        }

        /// <summary>Activa o desactiva la cuenta (un usuario inactivo no puede iniciar sesión).</summary>
        public async Task<ResultadoOperacion<UsuarioAdminDto>> CambiarEstadoAsync(
            ClaimsPrincipal actor, int id, bool activo)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

            if (usuario is null)
            {
                return ResultadoOperacion<UsuarioAdminDto>.NoEncontrado("El usuario no existe.");
            }

            if (!activo && AccesoObrasService.ObtenerUsuarioId(actor) == id)
            {
                return ResultadoOperacion<UsuarioAdminDto>.Invalido("No puedes desactivar tu propia cuenta.");
            }

            usuario.Activo = activo;
            await _context.SaveChangesAsync();

            return ResultadoOperacion<UsuarioAdminDto>.Ok(await ObtenerDtoAsync(usuario.Id));
        }

        private async Task<UsuarioAdminDto> ObtenerDtoAsync(int id)
        {
            return (await ListarAsync()).First(u => u.Id == id);
        }
    }
}
