using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using civix_backend.Data;
using civix_backend.Models;

namespace civix_backend.Services
{
    /// <summary>
    /// Reglas de acceso por obra:
    /// - Administrador: todas las obras.
    /// - Ingeniero y Cliente: solo las obras asignadas en dbo.ObraUsuarios (Activo = 1).
    /// - Cliente: además, solo lectura.
    /// Los módulos de Obras, Avances y Cotizaciones deben usar este servicio
    /// antes de devolver o modificar información de una obra.
    /// </summary>
    public class AccesoObrasService
    {
        private readonly CivixDbContext _context;

        public AccesoObrasService(CivixDbContext context)
        {
            _context = context;
        }

        /// <summary>Id del usuario autenticado, tomado del claim "sub" del token.</summary>
        public static int? ObtenerUsuarioId(ClaimsPrincipal usuario)
        {
            var valor = usuario.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(valor, out var id) ? id : null;
        }

        public static bool EsAdministrador(ClaimsPrincipal usuario) =>
            usuario.IsInRole(RolesSistema.Administrador);

        /// <summary>True si el usuario solo tiene el rol Cliente (no puede crear ni editar).</summary>
        public static bool EsSoloLectura(ClaimsPrincipal usuario) =>
            usuario.IsInRole(RolesSistema.Cliente)
            && !usuario.IsInRole(RolesSistema.Administrador)
            && !usuario.IsInRole(RolesSistema.Ingeniero);

        /// <summary>
        /// Ids de las obras a las que el usuario tiene acceso.
        /// Devuelve null cuando el usuario es Administrador (acceso a todas).
        /// </summary>
        public async Task<List<int>?> ObtenerObrasPermitidasAsync(ClaimsPrincipal usuario)
        {
            if (EsAdministrador(usuario))
            {
                return null;
            }

            var usuarioId = ObtenerUsuarioId(usuario);

            if (usuarioId is null)
            {
                return new List<int>();
            }

            return await _context.ObraUsuarios
                .Where(ou => ou.UsuarioId == usuarioId.Value && ou.Activo)
                .Select(ou => ou.ObraId)
                .ToListAsync();
        }

        /// <summary>Indica si el usuario puede consultar una obra concreta.</summary>
        public async Task<bool> PuedeAccederAObraAsync(ClaimsPrincipal usuario, int obraId)
        {
            if (EsAdministrador(usuario))
            {
                return true;
            }

            var usuarioId = ObtenerUsuarioId(usuario);

            if (usuarioId is null)
            {
                return false;
            }

            return await _context.ObraUsuarios
                .AnyAsync(ou => ou.UsuarioId == usuarioId.Value && ou.ObraId == obraId && ou.Activo);
        }
    }
}
