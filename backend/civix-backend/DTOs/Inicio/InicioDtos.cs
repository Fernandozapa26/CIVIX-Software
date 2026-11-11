namespace civix_backend.DTOs.Inicio
{
    /// <summary>
    /// Resumen de la pantalla de Inicio. Cada usuario ve solo los datos de sus obras;
    /// Cotizaciones y Materiales vienen en null para el rol Cliente.
    /// </summary>
    public class ResumenInicioDto
    {
        public int ObrasTotal { get; set; }
        public decimal AvancePromedio { get; set; }
        public List<ConteoEstadoDto> ObrasPorEstado { get; set; } = new();
        public List<ObraInicioDto> Obras { get; set; } = new();
        public List<AvanceInicioDto> AvancesRecientes { get; set; } = new();
        public CotizacionesInicioDto? Cotizaciones { get; set; }
        public MaterialesInicioDto? Materiales { get; set; }
    }

    public class ConteoEstadoDto
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class ObraInicioDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string? Ubicacion { get; set; }
        public int Avance { get; set; }
    }

    public class AvanceInicioDto
    {
        public int Id { get; set; }
        public int ObraId { get; set; }
        public string ObraNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal? PorcentajeAvance { get; set; }
        /// <summary>Primera foto del avance (ruta relativa, ej. /uploads/avances/x.jpg).</summary>
        public string? Imagen { get; set; }
    }

    public class CotizacionesInicioDto
    {
        public int Borrador { get; set; }
        public int Aprobadas { get; set; }
        public int Rechazadas { get; set; }
        public decimal ValorEnBorrador { get; set; }
        /// <summary>Cotizaciones en Borrador más recientes (para el Administrador: pendientes de aprobar).</summary>
        public List<CotizacionInicioDto> Pendientes { get; set; } = new();
    }

    public class CotizacionInicioDto
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string NombreProyecto { get; set; } = string.Empty;
        public string? ClienteNombre { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
    }

    public class MaterialesInicioDto
    {
        public int Pedidos { get; set; }
        public int EnBodega { get; set; }
        public int Retirados { get; set; }
        public int RetirosUltimos7Dias { get; set; }
    }
}
