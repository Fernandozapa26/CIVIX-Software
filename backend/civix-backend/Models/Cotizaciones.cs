namespace civix_backend.Models
{
    /// <summary>
    /// Tabla dbo.Cotizaciones. Los ítems están en dbo.CotizacionDetalles.
    /// </summary>
    public class Cotizacion
    {
        public int Id { get; set; }

        /// <summary>Número visible, por ejemplo COT-2026-0001 (único).</summary>
        public string Numero { get; set; } = string.Empty;

        /// <summary>Obra creada al aprobar la cotización.</summary>
        public int? ObraId { get; set; }

        public DateTime Fecha { get; set; }

        public int CreadoPorUsuarioId { get; set; }

        /// <summary>Usuario con rol Cliente al que se le hace la cotización.</summary>
        public int? ClienteId { get; set; }

        public string NombreProyecto { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public string? Ubicacion { get; set; }

        public string Estado { get; set; } = EstadosCotizacion.Borrador;

        public decimal Total { get; set; }

        /// <summary>Borrado lógico: false cuando la cotización se anula.</summary>
        public bool Activo { get; set; } = true;

        public DateTime? FechaDecision { get; set; }

        public int? DecididaPorUsuarioId { get; set; }

        public Usuario CreadoPor { get; set; } = null!;

        public Usuario? Cliente { get; set; }

        public Usuario? DecididaPor { get; set; }

        public Obra? Obra { get; set; }

        public ICollection<CotizacionDetalle> Detalles
        { get; set; } = new List<CotizacionDetalle>();
    }
}
