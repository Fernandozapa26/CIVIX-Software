using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using civix_backend.Data;
using civix_backend.DTOs.ObraMateriales;
using civix_backend.Models;

namespace civix_backend.Services
{
    /// <summary>
    /// Materiales por obra (RF-006, RF-007), estados y retiros de bodega.
    /// Reglas:
    /// - Solo se ven y modifican obras a las que el usuario tiene acceso (AccesoObrasService).
    /// - Al registrar o editar, el estado puede ser "Pedido" o "En bodega".
    /// - Solo se retira material "En bodega" y nunca más de lo disponible.
    /// - Cuando lo disponible llega a cero, el estado pasa solo a "Retirado".
    /// </summary>
    public class ObraMaterialesService
    {
        private readonly CivixDbContext _context;
        private readonly AccesoObrasService _accesoObras;

        public ObraMaterialesService(CivixDbContext context, AccesoObrasService accesoObras)
        {
            _context = context;
            _accesoObras = accesoObras;
        }

        // ---------- Catálogos para los selectores ----------

        public async Task<List<ObraOpcionDto>> ObtenerObrasAsync(ClaimsPrincipal usuario)
        {
            var permitidas = await _accesoObras.ObtenerObrasPermitidasAsync(usuario);

            var consulta = _context.Obras.AsNoTracking();

            if (permitidas is not null)
            {
                consulta = consulta.Where(o => permitidas.Contains(o.Id));
            }

            return await consulta
                .OrderBy(o => o.Nombre)
                .Select(o => new ObraOpcionDto
                {
                    Id = o.Id,
                    Nombre = o.Nombre,
                    Estado = o.Estado,
                    Ubicacion = o.Ubicacion
                })
                .ToListAsync();
        }

        public async Task<List<CotizacionOpcionDto>> ObtenerCotizacionesAsync()
        {
            return await _context.Cotizaciones
                .AsNoTracking()
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.Id)
                .Select(c => new CotizacionOpcionDto
                {
                    Id = c.Id,
                    Fecha = c.Fecha,
                    Total = c.Total,
                    Estado = c.Estado
                })
                .ToListAsync();
        }

        // ---------- Materiales de una obra ----------

        public async Task<ResultadoOperacion<List<ObraMaterialDto>>> ListarPorObraAsync(
            ClaimsPrincipal usuario, int obraId)
        {
            var error = await ValidarObraAsync<List<ObraMaterialDto>>(usuario, obraId);
            if (error is not null) return error;

            var lista = await ProyectarMateriales(_context.ObraMateriales.Where(om => om.ObraId == obraId))
                .OrderByDescending(om => om.FechaRegistro)
                .ToListAsync();

            return ResultadoOperacion<List<ObraMaterialDto>>.Ok(lista);
        }

        public async Task<ResultadoOperacion<ObraMaterialDto>> RegistrarAsync(
            ClaimsPrincipal usuario, int obraId, RegistrarObraMaterialDto dto)
        {
            var error = await ValidarObraAsync<ObraMaterialDto>(usuario, obraId);
            if (error is not null) return error;

            var usuarioId = AccesoObrasService.ObtenerUsuarioId(usuario);
            if (usuarioId is null)
                return ResultadoOperacion<ObraMaterialDto>.Prohibido("No se pudo identificar al usuario.");

            if (!EstadosMaterial.Seleccionables.Contains(dto.Estado))
                return ResultadoOperacion<ObraMaterialDto>.Invalido("El estado debe ser \"Pedido\" o \"En bodega\".");

            var materialActivo = await _context.Materiales.AnyAsync(m => m.Id == dto.MaterialId && m.Activo);
            if (!materialActivo)
                return ResultadoOperacion<ObraMaterialDto>.Invalido("El material no existe o está desactivado.");

            if (dto.CotizacionId is not null &&
                !await _context.Cotizaciones.AnyAsync(c => c.Id == dto.CotizacionId))
                return ResultadoOperacion<ObraMaterialDto>.Invalido("La cotización seleccionada no existe.");

            var registro = new ObraMaterial
            {
                ObraId = obraId,
                MaterialId = dto.MaterialId,
                CotizacionId = dto.CotizacionId,
                Cantidad = dto.Cantidad,
                Estado = dto.Estado,
                FechaRegistro = DateTime.Now,
                RegistradoPorUsuarioId = usuarioId.Value
            };

            _context.ObraMateriales.Add(registro);
            await _context.SaveChangesAsync();

            return ResultadoOperacion<ObraMaterialDto>.Ok(await ObtenerDtoAsync(registro.Id));
        }

        public async Task<ResultadoOperacion<ObraMaterialDto>> ActualizarAsync(
            ClaimsPrincipal usuario, int obraId, int id, ActualizarObraMaterialDto dto)
        {
            var error = await ValidarObraAsync<ObraMaterialDto>(usuario, obraId);
            if (error is not null) return error;

            var registro = await _context.ObraMateriales
                .FirstOrDefaultAsync(om => om.Id == id && om.ObraId == obraId);

            if (registro is null)
                return ResultadoOperacion<ObraMaterialDto>.NoEncontrado("El material no está registrado en esta obra.");

            if (!EstadosMaterial.Seleccionables.Contains(dto.Estado))
                return ResultadoOperacion<ObraMaterialDto>.Invalido("El estado debe ser \"Pedido\" o \"En bodega\".");

            if (dto.CotizacionId is not null &&
                !await _context.Cotizaciones.AnyAsync(c => c.Id == dto.CotizacionId))
                return ResultadoOperacion<ObraMaterialDto>.Invalido("La cotización seleccionada no existe.");

            var retirado = await _context.RetirosMaterial
                .Where(r => r.ObraMaterialId == id)
                .SumAsync(r => (decimal?)r.Cantidad) ?? 0m;

            if (retirado > 0 && dto.Estado == EstadosMaterial.Pedido)
                return ResultadoOperacion<ObraMaterialDto>.Invalido(
                    "Este material ya tiene retiros: no puede volver a estado \"Pedido\".");

            if (dto.Cantidad < retirado)
                return ResultadoOperacion<ObraMaterialDto>.Invalido(
                    $"La cantidad no puede ser menor que lo ya retirado ({retirado:0.###}).");

            registro.Cantidad = dto.Cantidad;
            registro.CotizacionId = dto.CotizacionId;
            registro.Estado = retirado > 0 && dto.Cantidad == retirado
                ? EstadosMaterial.Retirado
                : dto.Estado;

            await _context.SaveChangesAsync();

            return ResultadoOperacion<ObraMaterialDto>.Ok(await ObtenerDtoAsync(registro.Id));
        }

        public async Task<ResultadoOperacion<bool>> EliminarAsync(ClaimsPrincipal usuario, int obraId, int id)
        {
            var error = await ValidarObraAsync<bool>(usuario, obraId);
            if (error is not null) return error;

            var registro = await _context.ObraMateriales
                .FirstOrDefaultAsync(om => om.Id == id && om.ObraId == obraId);

            if (registro is null)
                return ResultadoOperacion<bool>.NoEncontrado("El material no está registrado en esta obra.");

            if (await _context.RetirosMaterial.AnyAsync(r => r.ObraMaterialId == id))
                return ResultadoOperacion<bool>.Invalido(
                    "No se puede eliminar: el material ya tiene retiros registrados.");

            _context.ObraMateriales.Remove(registro);
            await _context.SaveChangesAsync();

            return ResultadoOperacion<bool>.Ok(true);
        }

        // ---------- Retiros ----------

        /// <summary>Retiros en los que la obra participa como origen o como destino.</summary>
        public async Task<ResultadoOperacion<List<RetiroMaterialDto>>> ListarRetirosAsync(
            ClaimsPrincipal usuario, int obraId)
        {
            var error = await ValidarObraAsync<List<RetiroMaterialDto>>(usuario, obraId);
            if (error is not null) return error;

            var lista = await _context.RetirosMaterial
                .AsNoTracking()
                .Where(r => r.ObraMaterial.ObraId == obraId || r.ObraDestinoId == obraId)
                .OrderByDescending(r => r.FechaRetiro)
                .Select(r => new RetiroMaterialDto
                {
                    Id = r.Id,
                    ObraMaterialId = r.ObraMaterialId,
                    MaterialNombre = r.ObraMaterial.Material.Nombre,
                    UnidadMedida = r.ObraMaterial.Material.UnidadMedida,
                    ObraOrigenId = r.ObraMaterial.ObraId,
                    ObraOrigenNombre = r.ObraMaterial.Obra.Nombre,
                    ObraDestinoId = r.ObraDestinoId,
                    ObraDestinoNombre = r.ObraDestino.Nombre,
                    Cantidad = r.Cantidad,
                    RetiradoPor = r.RetiradoPor,
                    RegistradoPor = r.RegistradoPor.Nombre,
                    FechaRetiro = r.FechaRetiro,
                    Observacion = r.Observacion
                })
                .ToListAsync();

            return ResultadoOperacion<List<RetiroMaterialDto>>.Ok(lista);
        }

        public async Task<ResultadoOperacion<ObraMaterialDto>> RegistrarRetiroAsync(
            ClaimsPrincipal usuario, RegistrarRetiroDto dto)
        {
            var usuarioId = AccesoObrasService.ObtenerUsuarioId(usuario);
            if (usuarioId is null)
                return ResultadoOperacion<ObraMaterialDto>.Prohibido("No se pudo identificar al usuario.");

            // Serializable: dos retiros simultáneos no pueden sacar más de lo disponible.
            await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var registro = await _context.ObraMateriales.FirstOrDefaultAsync(om => om.Id == dto.ObraMaterialId);

            if (registro is null)
                return ResultadoOperacion<ObraMaterialDto>.NoEncontrado("El material a retirar no existe.");

            var errorOrigen = await ValidarObraAsync<ObraMaterialDto>(usuario, registro.ObraId);
            if (errorOrigen is not null) return errorOrigen;

            var errorDestino = await ValidarObraAsync<ObraMaterialDto>(usuario, dto.ObraDestinoId);
            if (errorDestino is not null) return errorDestino;

            if (registro.Estado != EstadosMaterial.EnBodega)
                return ResultadoOperacion<ObraMaterialDto>.Invalido(
                    "Solo se puede retirar material que esté \"En bodega\".");

            var retirado = await _context.RetirosMaterial
                .Where(r => r.ObraMaterialId == registro.Id)
                .SumAsync(r => (decimal?)r.Cantidad) ?? 0m;

            var disponible = registro.Cantidad - retirado;

            if (dto.Cantidad > disponible)
                return ResultadoOperacion<ObraMaterialDto>.Invalido(
                    $"Solo hay {disponible:0.###} disponibles en bodega.");

            _context.RetirosMaterial.Add(new RetiroMaterial
            {
                ObraMaterialId = registro.Id,
                Cantidad = dto.Cantidad,
                RetiradoPor = dto.RetiradoPor.Trim(),
                ObraDestinoId = dto.ObraDestinoId,
                RegistradoPorUsuarioId = usuarioId.Value,
                FechaRetiro = DateTime.Now,
                Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim()
            });

            if (dto.Cantidad == disponible)
            {
                registro.Estado = EstadosMaterial.Retirado;
            }

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion<ObraMaterialDto>.Ok(await ObtenerDtoAsync(registro.Id));
        }

        // ---------- Auxiliares ----------

        private async Task<ResultadoOperacion<T>?> ValidarObraAsync<T>(ClaimsPrincipal usuario, int obraId)
        {
            if (!await _context.Obras.AnyAsync(o => o.Id == obraId))
                return ResultadoOperacion<T>.NoEncontrado("La obra no existe.");

            if (!await _accesoObras.PuedeAccederAObraAsync(usuario, obraId))
                return ResultadoOperacion<T>.Prohibido("No tienes acceso a esta obra.");

            return null;
        }

        private async Task<ObraMaterialDto> ObtenerDtoAsync(int id)
        {
            return await ProyectarMateriales(_context.ObraMateriales.Where(om => om.Id == id)).FirstAsync();
        }

        private static IQueryable<ObraMaterialDto> ProyectarMateriales(IQueryable<ObraMaterial> consulta)
        {
            return consulta
                .AsNoTracking()
                .Select(om => new ObraMaterialDto
                {
                    Id = om.Id,
                    ObraId = om.ObraId,
                    ObraNombre = om.Obra.Nombre,
                    MaterialId = om.MaterialId,
                    MaterialNombre = om.Material.Nombre,
                    UnidadMedida = om.Material.UnidadMedida,
                    CotizacionId = om.CotizacionId,
                    Cantidad = om.Cantidad,
                    CantidadRetirada = om.Retiros.Sum(r => (decimal?)r.Cantidad) ?? 0m,
                    CantidadDisponible = om.Cantidad - (om.Retiros.Sum(r => (decimal?)r.Cantidad) ?? 0m),
                    Estado = om.Estado,
                    FechaRegistro = om.FechaRegistro,
                    RegistradoPor = om.RegistradoPor.Nombre,
                    TotalRetiros = om.Retiros.Count()
                });
        }
    }
}
