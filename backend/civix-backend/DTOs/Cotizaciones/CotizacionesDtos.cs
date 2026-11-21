using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.Cotizaciones
{
    /// <summary>Ítem o concepto de la cotización (RF-008).</summary>
    public class ItemCotizacionDto
    {
        [Required(ErrorMessage = "Cada ítem necesita un concepto.")]
        [StringLength(250, ErrorMessage = "El concepto admite máximo 250 caracteres.")]
        public string Concepto { get; set; } = string.Empty;

        [Range(0.001, 999999999, ErrorMessage = "La cantidad de cada ítem debe ser mayor que cero.")]
        public decimal Cantidad { get; set; }

        [Range(0, 999999999999, ErrorMessage = "El precio unitario no puede ser negativo.")]
        public decimal PrecioUnitario { get; set; }
    }

    /// <summary>Cuerpo de POST y PUT /api/cotizaciones.</summary>
    public class GuardarCotizacionDto
    {
        [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre del proyecto admite máximo 150 caracteres.")]
        public string NombreProyecto { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona el cliente.")]
        public int ClienteId { get; set; }

        public DateTime? Fecha { get; set; }

        [StringLength(250, ErrorMessage = "La ubicación admite máximo 250 caracteres.")]
        public string? Ubicacion { get; set; }

        [StringLength(1000, ErrorMessage = "La descripción admite máximo 1000 caracteres.")]
        public string? Descripcion { get; set; }

        [MinLength(1, ErrorMessage = "Agrega al menos un ítem.")]
        public List<ItemCotizacionDto> Items { get; set; } = new();
    }

    /// <summary>Cuerpo de POST /api/cotizaciones/{id}/aprobar.</summary>
    public class AprobarCotizacionDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona el ingeniero responsable de la obra.")]
        public int ResponsableId { get; set; }

        /// <summary>Si viene vacío se usa el nombre del proyecto de la cotización.</summary>
        [StringLength(150)]
        public string? NombreObra { get; set; }

        public DateTime? FechaInicio { get; set; }

        public DateTime? FechaFinEstimada { get; set; }
    }

    /// <summary>Fila del listado de cotizaciones.</summary>
    public class CotizacionResumenDto
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string NombreProyecto { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public int CreadoPorUsuarioId { get; set; }
        public string CreadoPor { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public decimal Total { get; set; }
        public int CantidadItems { get; set; }
        public int? ObraId { get; set; }
        public string? ObraNombre { get; set; }
    }

    public class ItemCotizacionDetalleDto
    {
        public int Id { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    /// <summary>Cotización completa con sus ítems (para editar, ver y generar el PDF).</summary>
    public class CotizacionDetalleDto : CotizacionResumenDto
    {
        public string? ClienteCorreo { get; set; }
        public string? Ubicacion { get; set; }
        public string? Descripcion { get; set; }
        public DateTime? FechaDecision { get; set; }
        public string? DecididaPor { get; set; }
        public List<ItemCotizacionDetalleDto> Items { get; set; } = new();
    }
}
