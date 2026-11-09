using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.ObraMateriales
{
    /// <summary>Cuerpo de POST /api/obras/{obraId}/materiales (RF-006).</summary>
    public class RegistrarObraMaterialDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un material.")]
        public int MaterialId { get; set; }

        [Range(0.001, 999999999, ErrorMessage = "La cantidad debe ser mayor que cero.")]
        public decimal Cantidad { get; set; }

        /// <summary>"Pedido" o "En bodega".</summary>
        [Required(ErrorMessage = "Selecciona el estado.")]
        public string Estado { get; set; } = string.Empty;

        public int? CotizacionId { get; set; }
    }

    /// <summary>Cuerpo de PUT /api/obras/{obraId}/materiales/{id}.</summary>
    public class ActualizarObraMaterialDto
    {
        [Range(0.001, 999999999, ErrorMessage = "La cantidad debe ser mayor que cero.")]
        public decimal Cantidad { get; set; }

        [Required(ErrorMessage = "Selecciona el estado.")]
        public string Estado { get; set; } = string.Empty;

        public int? CotizacionId { get; set; }
    }

    /// <summary>Material de una obra con sus cantidades calculadas (RF-007).</summary>
    public class ObraMaterialDto
    {
        public int Id { get; set; }
        public int ObraId { get; set; }
        public string ObraNombre { get; set; } = string.Empty;
        public int MaterialId { get; set; }
        public string MaterialNombre { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public int? CotizacionId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CantidadRetirada { get; set; }
        public decimal CantidadDisponible { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string RegistradoPor { get; set; } = string.Empty;
        public int TotalRetiros { get; set; }
    }

    /// <summary>Cuerpo de POST /api/retiros-material.</summary>
    public class RegistrarRetiroDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona el material a retirar.")]
        public int ObraMaterialId { get; set; }

        [Range(0.001, 999999999, ErrorMessage = "La cantidad debe ser mayor que cero.")]
        public decimal Cantidad { get; set; }

        [Required(ErrorMessage = "Indica quién retira el material.")]
        [StringLength(150, ErrorMessage = "El nombre admite máximo 150 caracteres.")]
        public string RetiradoPor { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona la obra de destino.")]
        public int ObraDestinoId { get; set; }

        [StringLength(500, ErrorMessage = "La observación admite máximo 500 caracteres.")]
        public string? Observacion { get; set; }
    }

    public class RetiroMaterialDto
    {
        public int Id { get; set; }
        public int ObraMaterialId { get; set; }
        public string MaterialNombre { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public int ObraOrigenId { get; set; }
        public string ObraOrigenNombre { get; set; } = string.Empty;
        public int ObraDestinoId { get; set; }
        public string ObraDestinoNombre { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string RetiradoPor { get; set; } = string.Empty;
        public string RegistradoPor { get; set; } = string.Empty;
        public DateTime FechaRetiro { get; set; }
        public string? Observacion { get; set; }
    }

    /// <summary>Opción para el selector de obras.</summary>
    public class ObraOpcionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string? Ubicacion { get; set; }
    }

    /// <summary>Opción para el selector de cotizaciones.</summary>
    public class CotizacionOpcionDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
