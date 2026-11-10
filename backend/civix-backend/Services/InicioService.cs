using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using civix_backend.Data;
using civix_backend.DTOs.Inicio;
using civix_backend.Models;

namespace civix_backend.Services
{
    /// <summary>
    /// Arma el resumen de la pantalla de Inicio según el rol:
    /// - Administrador: todas las obras, cotizaciones y materiales.
    /// - Ingeniero: sus obras asignadas, sus cotizaciones y los materiales de sus obras.
    /// - Cliente: sus obras y sus avances (sin cotizaciones ni materiales).
    /// </summary>
    public class InicioService
    {
        private const int MaximoObras = 6;
        private const int MaximoAvances = 5;
        private const int MaximoCotizaciones = 5;

        private readonly CivixDbContext _context;
        private readonly AccesoObrasService _accesoObras;

        public InicioService(CivixDbContext context, AccesoObrasService accesoObras)
        {
            _context = context;
            _accesoObras = accesoObras;
        }

        public async Task<ResumenInicioDto> ObtenerResumenAsync(ClaimsPrincipal usuario)
        {
            var permitidas = await _accesoObras.ObtenerObrasPermitidasAsync(usuario);

            var obras = _context.Obras.AsNoTracking();
            var avances = _context.Avances.AsNoTracking();
            var materiales = _context.ObraMateriales.AsNoTracking();
            var retiros = _context.RetirosMaterial.AsNoTracking();

            if (permitidas is not null)
            {
                obras = obras.Where(o => permitidas.Contains(o.Id));
                avances = avances.Where(a => permitidas.Contains(a.ObraId));
                materiales = materiales.Where(m => permitidas.Contains(m.ObraId));
                retiros = retiros.Where(r => permitidas.Contains(r.ObraMaterial.ObraId));
            }

            var resumen = new ResumenInicioDto
            {
                ObrasTotal = await obras.CountAsync(),
                AvancePromedio = await obras.AnyAsync()
                    ? Math.Round((decimal)await obras.AverageAsync(o => o.Avance), 1)
                    : 0m,
                ObrasPorEstado = await obras
                    .GroupBy(o => o.Estado)
                    .Select(g => new ConteoEstadoDto { Estado = g.Key, Cantidad = g.Count() })
                    .OrderByDescending(c => c.Cantidad)
                    .ToListAsync(),
                Obras = await obras
                    .OrderByDescending(o => o.FechaCreacion)
                    .ThenByDescending(o => o.Id)
                    .Take(MaximoObras)
                    .Select(o => new ObraInicioDto
                    {
                        Id = o.Id,
                        Nombre = o.Nombre,
                        Estado = o.Estado,
                        Ubicacion = o.Ubicacion,
                        Avance = o.Avance
                    })
                    .ToListAsync(),
                AvancesRecientes = await ObtenerAvancesRecientesAsync(avances)
            };

            if (!EsCliente(usuario))
            {
                resumen.Cotizaciones = await ObtenerCotizacionesAsync(usuario);
                resumen.Materiales = new MaterialesInicioDto
                {
                    Pedidos = await materiales.CountAsync(m => m.Estado == EstadosMaterial.Pedido),
                    EnBodega = await materiales.CountAsync(m => m.Estado == EstadosMaterial.EnBodega),
                    Retirados = await materiales.CountAsync(m => m.Estado == EstadosMaterial.Retirado),
                    RetirosUltimos7Dias = await retiros.CountAsync(r => r.FechaRetiro >= DateTime.Now.AddDays(-7))
                };
            }

            return resumen;
        }

        private async Task<List<AvanceInicioDto>> ObtenerAvancesRecientesAsync(IQueryable<Avance> avances)
        {
            var recientes = await avances
                .OrderByDescending(a => a.Fecha)
                .ThenByDescending(a => a.Id)
                .Take(MaximoAvances)
                .ToListAsync();

            var obraIds = recientes.Select(a => a.ObraId).Distinct().ToList();

            var nombres = await _context.Obras
                .AsNoTracking()
                .Where(o => obraIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.Nombre);

            return recientes.Select(a => new AvanceInicioDto
            {
                Id = a.Id,
                ObraId = a.ObraId,
                ObraNombre = nombres.GetValueOrDefault(a.ObraId, string.Empty),
                Fecha = a.Fecha,
                Descripcion = a.Descripcion,
                PorcentajeAvance = a.PorcentajeAvance,
                Imagen = a.Imagenes.FirstOrDefault()
            }).ToList();
        }

        /// <summary>El Administrador ve todas; el Ingeniero solo las que creó. Sin anuladas.</summary>
        private async Task<CotizacionesInicioDto> ObtenerCotizacionesAsync(ClaimsPrincipal usuario)
        {
            var cotizaciones = _context.Cotizaciones.AsNoTracking().Where(c => c.Activo);

            if (!AccesoObrasService.EsAdministrador(usuario))
            {
                var usuarioId = AccesoObrasService.ObtenerUsuarioId(usuario) ?? 0;
                cotizaciones = cotizaciones.Where(c => c.CreadoPorUsuarioId == usuarioId);
            }

            var borradores = cotizaciones.Where(c => c.Estado == EstadosCotizacion.Borrador);

            return new CotizacionesInicioDto
            {
                Borrador = await borradores.CountAsync(),
                Aprobadas = await cotizaciones.CountAsync(c => c.Estado == EstadosCotizacion.Aprobada),
                Rechazadas = await cotizaciones.CountAsync(c => c.Estado == EstadosCotizacion.Rechazada),
                ValorEnBorrador = await borradores.SumAsync(c => (decimal?)c.Total) ?? 0m,
                Pendientes = await borradores
                    .OrderByDescending(c => c.Fecha)
                    .ThenByDescending(c => c.Id)
                    .Take(MaximoCotizaciones)
                    .Select(c => new CotizacionInicioDto
                    {
                        Id = c.Id,
                        Numero = c.Numero,
                        NombreProyecto = c.NombreProyecto,
                        ClienteNombre = c.Cliente != null ? c.Cliente.Nombre : null,
                        Fecha = c.Fecha,
                        Total = c.Total
                    })
                    .ToListAsync()
            };
        }

        private static bool EsCliente(ClaimsPrincipal usuario)
        {
            return !AccesoObrasService.EsAdministrador(usuario) &&
                   !usuario.IsInRole(RolesSistema.Ingeniero) &&
                   usuario.IsInRole(RolesSistema.Cliente);
        }
    }
}
