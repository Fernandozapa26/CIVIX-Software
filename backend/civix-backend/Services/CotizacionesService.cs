using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using civix_backend.Data;
using civix_backend.DTOs.Cotizaciones;
using civix_backend.Models;

namespace civix_backend.Services
{
    /// <summary>
    /// Cotizaciones (RF-008 crear con ítems, RF-009 calcular total).
    /// Reglas:
    /// - Administrador: ve, aprueba y rechaza todas.
    /// - Ingeniero: solo ve, edita y anula las que él creó.
    /// - Solo se edita una cotización en Borrador.
    /// - Anular es el borrado lógico (Estado = Anulada, Activo = 0); no aplica a las aprobadas.
    /// - Al aprobar se crea la obra con el cliente de la cotización y el responsable elegido.
    /// </summary>
    public class CotizacionesService
    {
        private const string EstadoObraInicial = "En planeación";

        private readonly CivixDbContext _context;
        private readonly ObrasService _obras;

        public CotizacionesService(CivixDbContext context, ObrasService obras)
        {
            _context = context;
            _obras = obras;
        }

        // ---------- Consultas ----------

        /// <param name="estado">Borrador, Aprobada, Rechazada o Anulada. Vacío: todas menos las anuladas.</param>
        public async Task<List<CotizacionResumenDto>> ListarAsync(ClaimsPrincipal usuario, string? estado)
        {
            var consulta = FiltrarVisibles(usuario, _context.Cotizaciones.AsNoTracking());

            consulta = string.IsNullOrWhiteSpace(estado)
                ? consulta.Where(c => c.Estado != EstadosCotizacion.Anulada)
                : consulta.Where(c => c.Estado == estado);

            return await consulta
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.Id)
                .Select(c => new CotizacionResumenDto
                {
                    Id = c.Id,
                    Numero = c.Numero,
                    Fecha = c.Fecha,
                    NombreProyecto = c.NombreProyecto,
                    ClienteId = c.ClienteId,
                    ClienteNombre = c.Cliente != null ? c.Cliente.Nombre : null,
                    CreadoPorUsuarioId = c.CreadoPorUsuarioId,
                    CreadoPor = c.CreadoPor.Nombre,
                    Estado = c.Estado,
                    Activo = c.Activo,
                    Total = c.Total,
                    CantidadItems = c.Detalles.Count(),
                    ObraId = c.ObraId,
                    ObraNombre = c.Obra != null ? c.Obra.Nombre : null
                })
                .ToListAsync();
        }

        public async Task<ResultadoOperacion<CotizacionDetalleDto>> ObtenerAsync(ClaimsPrincipal usuario, int id)
        {
            var dto = await FiltrarVisibles(usuario, _context.Cotizaciones.AsNoTracking())
                .Where(c => c.Id == id)
                .Select(c => new CotizacionDetalleDto
                {
                    Id = c.Id,
                    Numero = c.Numero,
                    Fecha = c.Fecha,
                    NombreProyecto = c.NombreProyecto,
                    ClienteId = c.ClienteId,
                    ClienteNombre = c.Cliente != null ? c.Cliente.Nombre : null,
                    ClienteCorreo = c.Cliente != null ? c.Cliente.Correo : null,
                    CreadoPorUsuarioId = c.CreadoPorUsuarioId,
                    CreadoPor = c.CreadoPor.Nombre,
                    Estado = c.Estado,
                    Activo = c.Activo,
                    Total = c.Total,
                    CantidadItems = c.Detalles.Count(),
                    ObraId = c.ObraId,
                    ObraNombre = c.Obra != null ? c.Obra.Nombre : null,
                    Ubicacion = c.Ubicacion,
                    Descripcion = c.Descripcion,
                    FechaDecision = c.FechaDecision,
                    DecididaPor = c.DecididaPor != null ? c.DecididaPor.Nombre : null,
                    Items = c.Detalles
                        .OrderBy(d => d.Id)
                        .Select(d => new ItemCotizacionDetalleDto
                        {
                            Id = d.Id,
                            Concepto = d.Concepto,
                            Cantidad = d.Cantidad,
                            PrecioUnitario = d.PrecioUnitario,
                            Subtotal = d.Subtotal
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (dto is null)
            {
                return await _context.Cotizaciones.AnyAsync(c => c.Id == id)
                    ? ResultadoOperacion<CotizacionDetalleDto>.Prohibido("No tienes acceso a esta cotización.")
                    : ResultadoOperacion<CotizacionDetalleDto>.NoEncontrado("La cotización no existe.");
            }

            return ResultadoOperacion<CotizacionDetalleDto>.Ok(dto);
        }

        // ---------- Crear y editar ----------

        public async Task<ResultadoOperacion<CotizacionDetalleDto>> CrearAsync(
            ClaimsPrincipal usuario, GuardarCotizacionDto dto)
        {
            var usuarioId = AccesoObrasService.ObtenerUsuarioId(usuario);
            if (usuarioId is null)
                return ResultadoOperacion<CotizacionDetalleDto>.Prohibido("No se pudo identificar al usuario.");

            var error = await ValidarAsync<CotizacionDetalleDto>(dto);
            if (error is not null) return error;

            // Serializable: dos cotizaciones creadas a la vez no reciben el mismo número.
            await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var fecha = (dto.Fecha ?? DateTime.Today).Date;

            var cotizacion = new Cotizacion
            {
                Numero = await GenerarNumeroAsync(fecha.Year),
                Fecha = fecha,
                CreadoPorUsuarioId = usuarioId.Value,
                Estado = EstadosCotizacion.Borrador,
                Activo = true
            };

            CopiarDatos(dto, cotizacion);

            _context.Cotizaciones.Add(cotizacion);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return await ObtenerAsync(usuario, cotizacion.Id);
        }

        public async Task<ResultadoOperacion<CotizacionDetalleDto>> ActualizarAsync(
            ClaimsPrincipal usuario, int id, GuardarCotizacionDto dto)
        {
            var cotizacion = await _context.Cotizaciones
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.Id == id);

            var errorAcceso = ValidarAcceso<CotizacionDetalleDto>(usuario, cotizacion);
            if (errorAcceso is not null) return errorAcceso;

            if (cotizacion!.Estado != EstadosCotizacion.Borrador)
                return ResultadoOperacion<CotizacionDetalleDto>.Invalido(
                    $"Solo se puede editar una cotización en Borrador (esta está {cotizacion.Estado}).");

            var error = await ValidarAsync<CotizacionDetalleDto>(dto);
            if (error is not null) return error;

            if (dto.Fecha.HasValue)
            {
                cotizacion.Fecha = dto.Fecha.Value.Date;
            }

            // Los ítems se reemplazan completos por los que vienen del formulario.
            var anteriores = cotizacion.Detalles.ToList();
            cotizacion.Detalles.Clear();
            _context.RemoveRange(anteriores);

            CopiarDatos(dto, cotizacion);

            await _context.SaveChangesAsync();

            return await ObtenerAsync(usuario, cotizacion.Id);
        }

        // ---------- Cambios de estado ----------

        /// <summary>Borrado lógico: Estado = Anulada y Activo = 0.</summary>
        public async Task<ResultadoOperacion<CotizacionDetalleDto>> AnularAsync(ClaimsPrincipal usuario, int id)
        {
            var cotizacion = await _context.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id);

            var errorAcceso = ValidarAcceso<CotizacionDetalleDto>(usuario, cotizacion);
            if (errorAcceso is not null) return errorAcceso;

            if (cotizacion!.Estado == EstadosCotizacion.Aprobada)
                return ResultadoOperacion<CotizacionDetalleDto>.Invalido(
                    "No se puede anular una cotización aprobada: ya tiene una obra creada.");

            if (cotizacion.Estado == EstadosCotizacion.Anulada)
                return ResultadoOperacion<CotizacionDetalleDto>.Invalido("La cotización ya está anulada.");

            cotizacion.Estado = EstadosCotizacion.Anulada;
            cotizacion.Activo = false;

            await _context.SaveChangesAsync();

            return await ObtenerAsync(usuario, cotizacion.Id);
        }

        public async Task<ResultadoOperacion<CotizacionDetalleDto>> RechazarAsync(ClaimsPrincipal usuario, int id)
        {
            var cotizacion = await _context.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id);

            var errorAcceso = ValidarAcceso<CotizacionDetalleDto>(usuario, cotizacion);
            if (errorAcceso is not null) return errorAcceso;

            if (cotizacion!.Estado != EstadosCotizacion.Borrador)
                return ResultadoOperacion<CotizacionDetalleDto>.Invalido("Solo se puede rechazar una cotización en Borrador.");

            cotizacion.Estado = EstadosCotizacion.Rechazada;
            cotizacion.FechaDecision = DateTime.Now;
            cotizacion.DecididaPorUsuarioId = AccesoObrasService.ObtenerUsuarioId(usuario);

            await _context.SaveChangesAsync();

            return await ObtenerAsync(usuario, cotizacion.Id);
        }

        /// <summary>
        /// Aprueba la cotización y crea la obra automáticamente:
        /// nombre, descripción y ubicación de la cotización, presupuesto = total,
        /// cliente de la cotización y el ingeniero responsable elegido.
        /// </summary>
        public async Task<ResultadoOperacion<CotizacionDetalleDto>> AprobarAsync(
            ClaimsPrincipal usuario, int id, AprobarCotizacionDto dto)
        {
            var cotizacion = await _context.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id);

            var errorAcceso = ValidarAcceso<CotizacionDetalleDto>(usuario, cotizacion);
            if (errorAcceso is not null) return errorAcceso;

            if (cotizacion!.Estado != EstadosCotizacion.Borrador)
                return ResultadoOperacion<CotizacionDetalleDto>.Invalido("Solo se puede aprobar una cotización en Borrador.");

            if (cotizacion.ClienteId is null)
                return ResultadoOperacion<CotizacionDetalleDto>.Invalido(
                    "La cotización no tiene cliente. Edítala y asígnale uno antes de aprobarla.");

            if (dto.FechaInicio.HasValue && dto.FechaFinEstimada.HasValue &&
                dto.FechaFinEstimada.Value.Date < dto.FechaInicio.Value.Date)
                return ResultadoOperacion<CotizacionDetalleDto>.Invalido(
                    "La fecha de fin estimada no puede ser anterior a la fecha de inicio.");

            await using var transaccion = await _context.Database.BeginTransactionAsync();

            var obra = new Obra
            {
                Nombre = string.IsNullOrWhiteSpace(dto.NombreObra) ? cotizacion.NombreProyecto : dto.NombreObra.Trim(),
                Descripcion = cotizacion.Descripcion,
                Ubicacion = cotizacion.Ubicacion,
                FechaInicio = dto.FechaInicio?.Date,
                FechaFinEstimada = dto.FechaFinEstimada?.Date,
                Estado = EstadoObraInicial,
                Presupuesto = cotizacion.Total,
                Avance = 0,
                Personal = 0,
                FechaCreacion = DateTime.Now
            };

            _context.Obras.Add(obra);
            await _context.SaveChangesAsync();

            var errorAsignacion = await _obras.AsignarClienteYResponsableAsync(
                obra, cotizacion.ClienteId.Value, dto.ResponsableId);

            if (errorAsignacion is not null)
                return ResultadoOperacion<CotizacionDetalleDto>.Invalido(errorAsignacion);

            cotizacion.ObraId = obra.Id;
            cotizacion.Estado = EstadosCotizacion.Aprobada;
            cotizacion.FechaDecision = DateTime.Now;
            cotizacion.DecididaPorUsuarioId = AccesoObrasService.ObtenerUsuarioId(usuario);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return await ObtenerAsync(usuario, cotizacion.Id);
        }

        // ---------- Auxiliares ----------

        /// <summary>El Administrador ve todas; el Ingeniero solo las que creó.</summary>
        private static IQueryable<Cotizacion> FiltrarVisibles(ClaimsPrincipal usuario, IQueryable<Cotizacion> consulta)
        {
            if (AccesoObrasService.EsAdministrador(usuario))
            {
                return consulta;
            }

            var usuarioId = AccesoObrasService.ObtenerUsuarioId(usuario) ?? 0;
            return consulta.Where(c => c.CreadoPorUsuarioId == usuarioId);
        }

        private static ResultadoOperacion<T>? ValidarAcceso<T>(ClaimsPrincipal usuario, Cotizacion? cotizacion)
        {
            if (cotizacion is null)
                return ResultadoOperacion<T>.NoEncontrado("La cotización no existe.");

            if (!AccesoObrasService.EsAdministrador(usuario) &&
                cotizacion.CreadoPorUsuarioId != AccesoObrasService.ObtenerUsuarioId(usuario))
                return ResultadoOperacion<T>.Prohibido("Solo puedes gestionar las cotizaciones que creaste.");

            return null;
        }

        private async Task<ResultadoOperacion<T>?> ValidarAsync<T>(GuardarCotizacionDto dto)
        {
            if (dto.Items.Count == 0)
                return ResultadoOperacion<T>.Invalido("Agrega al menos un ítem a la cotización.");

            var clienteValido = await _context.Usuarios.AnyAsync(u =>
                u.Id == dto.ClienteId && u.Activo &&
                u.UsuariosRoles.Any(ur => ur.Rol.Nombre == RolesSistema.Cliente));

            if (!clienteValido)
                return ResultadoOperacion<T>.Invalido("El cliente seleccionado no existe o no tiene el rol Cliente.");

            return null;
        }

        /// <summary>Copia los datos generales y los ítems, y calcula el total (RF-009).</summary>
        private static void CopiarDatos(GuardarCotizacionDto dto, Cotizacion cotizacion)
        {
            cotizacion.NombreProyecto = dto.NombreProyecto.Trim();
            cotizacion.ClienteId = dto.ClienteId;
            cotizacion.Ubicacion = string.IsNullOrWhiteSpace(dto.Ubicacion) ? null : dto.Ubicacion.Trim();
            cotizacion.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();

            foreach (var item in dto.Items)
            {
                cotizacion.Detalles.Add(new CotizacionDetalle
                {
                    Concepto = item.Concepto.Trim(),
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario
                });
            }

            // Igual que la columna calculada Subtotal: cada subtotal se redondea a 2 decimales.
            cotizacion.Total = dto.Items.Sum(i =>
                Math.Round(i.Cantidad * i.PrecioUnitario, 2, MidpointRounding.AwayFromZero));
        }

        /// <summary>Siguiente número del año: COT-2026-0001, COT-2026-0002…</summary>
        private async Task<string> GenerarNumeroAsync(int anio)
        {
            var prefijo = $"COT-{anio}-";

            var numeros = await _context.Cotizaciones
                .Where(c => c.Numero.StartsWith(prefijo))
                .Select(c => c.Numero)
                .ToListAsync();

            var ultimo = numeros
                .Select(n => int.TryParse(n.Substring(prefijo.Length), out var valor) ? valor : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"{prefijo}{ultimo + 1:0000}";
        }
    }
}
