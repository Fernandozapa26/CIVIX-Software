using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using civix_backend.Data;
using civix_backend.DTOs.Obras;
using civix_backend.Models;

namespace civix_backend.Services
{
    /// <summary>
    /// Obras: consulta filtrada por acceso, creación, edición y eliminación.
    /// Cada obra tiene un cliente (rol Cliente) y un responsable (rol Ingeniero),
    /// ambos asignados en dbo.ObraUsuarios, que es lo que usa AccesoObrasService
    /// para que cada usuario vea solo sus obras.
    /// </summary>
    public class ObrasService
    {
        private readonly CivixDbContext _context;
        private readonly AccesoObrasService _accesoObras;

        public ObrasService(CivixDbContext context, AccesoObrasService accesoObras)
        {
            _context = context;
            _accesoObras = accesoObras;
        }

        // ---------- Consultas ----------

        public async Task<List<ObraDto>> ListarAsync(ClaimsPrincipal usuario)
        {
            var permitidas = await _accesoObras.ObtenerObrasPermitidasAsync(usuario);

            var consulta = _context.Obras.AsNoTracking();

            if (permitidas is not null)
            {
                consulta = consulta.Where(o => permitidas.Contains(o.Id));
            }

            var obras = await consulta.OrderByDescending(o => o.Id).ToListAsync();

            return await ConvertirAsync(obras);
        }

        public async Task<ResultadoOperacion<ObraDto>> ObtenerAsync(ClaimsPrincipal usuario, int id)
        {
            var obra = await _context.Obras.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);

            if (obra is null)
                return ResultadoOperacion<ObraDto>.NoEncontrado("La obra no existe.");

            if (!await _accesoObras.PuedeAccederAObraAsync(usuario, id))
                return ResultadoOperacion<ObraDto>.Prohibido("No tienes acceso a esta obra.");

            return ResultadoOperacion<ObraDto>.Ok((await ConvertirAsync(new List<Obra> { obra }))[0]);
        }

        /// <summary>Usuarios activos con el rol indicado ("Cliente" o "Ingeniero").</summary>
        public async Task<List<UsuarioOpcionDto>> ListarUsuariosPorRolAsync(string rol)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Activo && u.UsuariosRoles.Any(ur => ur.Rol.Nombre == rol))
                .OrderBy(u => u.Nombre)
                .Select(u => new UsuarioOpcionDto { Id = u.Id, Nombre = u.Nombre, Correo = u.Correo })
                .ToListAsync();
        }

        // ---------- Escritura ----------

        public async Task<ResultadoOperacion<ObraDto>> CrearAsync(ClaimsPrincipal usuario, GuardarObraDto dto)
        {
            var error = ValidarFechas<ObraDto>(dto);
            if (error is not null) return error;

            await using var transaccion = await _context.Database.BeginTransactionAsync();

            var obra = new Obra { FechaCreacion = DateTime.Now };
            CopiarDatos(dto, obra);
            _context.Obras.Add(obra);
            await _context.SaveChangesAsync();

            var errorAsignacion = await AsignarClienteYResponsableAsync(obra, dto.ClienteId, dto.ResponsableId);
            if (errorAsignacion is not null)
                return ResultadoOperacion<ObraDto>.Invalido(errorAsignacion);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return await ObtenerAsync(usuario, obra.Id);
        }

        public async Task<ResultadoOperacion<ObraDto>> ActualizarAsync(
            ClaimsPrincipal usuario, int id, GuardarObraDto dto)
        {
            var error = ValidarFechas<ObraDto>(dto);
            if (error is not null) return error;

            var obra = await _context.Obras.FirstOrDefaultAsync(o => o.Id == id);

            if (obra is null)
                return ResultadoOperacion<ObraDto>.NoEncontrado("La obra no existe.");

            if (!await _accesoObras.PuedeAccederAObraAsync(usuario, id))
                return ResultadoOperacion<ObraDto>.Prohibido("No tienes acceso a esta obra.");

            await using var transaccion = await _context.Database.BeginTransactionAsync();

            CopiarDatos(dto, obra);

            var errorAsignacion = await AsignarClienteYResponsableAsync(obra, dto.ClienteId, dto.ResponsableId);
            if (errorAsignacion is not null)
                return ResultadoOperacion<ObraDto>.Invalido(errorAsignacion);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return await ObtenerAsync(usuario, obra.Id);
        }

        public async Task<ResultadoOperacion<bool>> EliminarAsync(ClaimsPrincipal usuario, int id)
        {
            var obra = await _context.Obras.FirstOrDefaultAsync(o => o.Id == id);

            if (obra is null)
                return ResultadoOperacion<bool>.NoEncontrado("La obra no existe.");

            if (!await _accesoObras.PuedeAccederAObraAsync(usuario, id))
                return ResultadoOperacion<bool>.Prohibido("No tienes acceso a esta obra.");

            var tieneAvances = await _context.Avances.AnyAsync(a => a.ObraId == id);
            var tieneMateriales = await _context.ObraMateriales.AnyAsync(m => m.ObraId == id);
            var tieneCotizaciones = await _context.Cotizaciones.AnyAsync(c => c.ObraId == id);

            if (tieneAvances || tieneMateriales || tieneCotizaciones)
                return ResultadoOperacion<bool>.Conflicto(
                    "No se puede eliminar la obra porque tiene avances, materiales o cotizaciones asociadas.");

            // Las asignaciones de cliente y responsable se borran junto con la obra.
            var asignaciones = await _context.ObraUsuarios.Where(ou => ou.ObraId == id).ToListAsync();
            _context.ObraUsuarios.RemoveRange(asignaciones);
            _context.Obras.Remove(obra);
            await _context.SaveChangesAsync();

            return ResultadoOperacion<bool>.Ok(true);
        }

        /// <summary>
        /// Asigna el cliente y el ingeniero responsable de la obra en dbo.ObraUsuarios.
        /// Si ya había otro cliente u otro responsable, se desactiva su asignación.
        /// Devuelve un mensaje de error o null si todo salió bien.
        /// La usa también Cotizaciones al crear la obra de una cotización aprobada.
        /// </summary>
        public async Task<string?> AsignarClienteYResponsableAsync(Obra obra, int clienteId, int responsableId)
        {
            var cliente = await _context.Usuarios.FirstOrDefaultAsync(u =>
                u.Id == clienteId && u.Activo && u.UsuariosRoles.Any(ur => ur.Rol.Nombre == RolesSistema.Cliente));

            if (cliente is null)
                return "El cliente seleccionado no existe o no tiene el rol Cliente.";

            var responsableValido = await _context.Usuarios.AnyAsync(u =>
                u.Id == responsableId && u.Activo && u.UsuariosRoles.Any(ur => ur.Rol.Nombre == RolesSistema.Ingeniero));

            if (!responsableValido)
                return "El responsable seleccionado no existe o no tiene el rol Ingeniero.";

            var asignaciones = await _context.ObraUsuarios
                .Include(ou => ou.Usuario).ThenInclude(u => u.UsuariosRoles).ThenInclude(ur => ur.Rol)
                .Where(ou => ou.ObraId == obra.Id)
                .ToListAsync();

            ReemplazarAsignacion(obra.Id, asignaciones, RolesSistema.Cliente, clienteId);
            ReemplazarAsignacion(obra.Id, asignaciones, RolesSistema.Ingeniero, responsableId);

            // Se conserva el nombre en la columna de texto para las vistas que ya la usan.
            obra.Cliente = cliente.Nombre;

            return null;
        }

        // ---------- Auxiliares ----------

        private void ReemplazarAsignacion(int obraId, List<ObraUsuario> asignaciones, string rol, int usuarioId)
        {
            foreach (var actual in asignaciones.Where(a =>
                         a.UsuarioId != usuarioId && a.Activo &&
                         a.Usuario.UsuariosRoles.Any(ur => ur.Rol.Nombre == rol)))
            {
                actual.Activo = false;
            }

            var existente = asignaciones.FirstOrDefault(a => a.UsuarioId == usuarioId);

            if (existente is null)
            {
                _context.ObraUsuarios.Add(new ObraUsuario
                {
                    ObraId = obraId,
                    UsuarioId = usuarioId,
                    FechaAsignacion = DateTime.Now,
                    Activo = true
                });
            }
            else if (!existente.Activo)
            {
                existente.Activo = true;
                existente.FechaAsignacion = DateTime.Now;
            }
        }

        private static void CopiarDatos(GuardarObraDto dto, Obra obra)
        {
            obra.Nombre = dto.Nombre.Trim();
            obra.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
            obra.Ubicacion = string.IsNullOrWhiteSpace(dto.Ubicacion) ? null : dto.Ubicacion.Trim();
            obra.FechaInicio = dto.FechaInicio?.Date;
            obra.FechaFinEstimada = dto.FechaFinEstimada?.Date;
            obra.Estado = dto.Estado.Trim();
            obra.Presupuesto = dto.Presupuesto;
            obra.Avance = dto.Avance;
            obra.Personal = dto.Personal;
        }

        private static ResultadoOperacion<T>? ValidarFechas<T>(GuardarObraDto dto)
        {
            if (dto.FechaInicio.HasValue && dto.FechaFinEstimada.HasValue &&
                dto.FechaFinEstimada.Value.Date < dto.FechaInicio.Value.Date)
                return ResultadoOperacion<T>.Invalido(
                    "La fecha de fin estimada no puede ser anterior a la fecha de inicio.");

            return null;
        }

        /// <summary>Agrega a cada obra su cliente y su responsable activos.</summary>
        private async Task<List<ObraDto>> ConvertirAsync(List<Obra> obras)
        {
            var ids = obras.Select(o => o.Id).ToList();

            var asignaciones = await _context.ObraUsuarios
                .AsNoTracking()
                .Where(ou => ou.Activo && ids.Contains(ou.ObraId))
                .Select(ou => new
                {
                    ou.ObraId,
                    ou.UsuarioId,
                    ou.Usuario.Nombre,
                    Roles = ou.Usuario.UsuariosRoles.Select(ur => ur.Rol.Nombre).ToList()
                })
                .ToListAsync();

            // Portada: primera foto del avance más reciente que tenga imágenes.
            // Las imágenes se guardan como texto separado por comas, por eso se filtran en memoria.
            var avancesConFotos = await _context.Avances
                .AsNoTracking()
                .Where(a => ids.Contains(a.ObraId))
                .OrderByDescending(a => a.Fecha)
                .ThenByDescending(a => a.Id)
                .Select(a => new { a.ObraId, a.Imagenes })
                .ToListAsync();

            var portadas = avancesConFotos
                .Where(a => a.Imagenes.Count > 0)
                .GroupBy(a => a.ObraId)
                .ToDictionary(g => g.Key, g => g.First().Imagenes[0]);

            return obras.Select(o =>
            {
                var cliente = asignaciones.FirstOrDefault(a => a.ObraId == o.Id && a.Roles.Contains(RolesSistema.Cliente));
                var responsable = asignaciones.FirstOrDefault(a => a.ObraId == o.Id && a.Roles.Contains(RolesSistema.Ingeniero));

                return new ObraDto
                {
                    Id = o.Id,
                    Nombre = o.Nombre,
                    Descripcion = o.Descripcion,
                    Ubicacion = o.Ubicacion,
                    FechaInicio = o.FechaInicio,
                    FechaFinEstimada = o.FechaFinEstimada,
                    Estado = o.Estado,
                    Presupuesto = o.Presupuesto,
                    Avance = o.Avance,
                    Personal = o.Personal,
                    FechaCreacion = o.FechaCreacion,
                    Cliente = o.Cliente,
                    ClienteId = cliente?.UsuarioId,
                    ClienteNombre = cliente?.Nombre ?? o.Cliente,
                    ResponsableId = responsable?.UsuarioId,
                    ResponsableNombre = responsable?.Nombre,
                    ImagenPortada = portadas.GetValueOrDefault(o.Id)
                };
            }).ToList();
        }
    }
}
