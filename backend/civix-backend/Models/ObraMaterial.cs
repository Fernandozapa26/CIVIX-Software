namespace civix_backend.Models
{
    /// <summary>
    /// Tabla dbo.ObraMateriales: material registrado para una obra (RF-006),
    /// opcionalmente ligado a una cotización, con su estado y sus retiros.
    /// </summary>
    public class ObraMaterial
    {
        public int Id { get; set; }

        public int ObraId { get; set; }

        public int MaterialId { get; set; }

        public int? CotizacionId { get; set; }

        public decimal Cantidad { get; set; }

        public string Estado { get; set; } = EstadosMaterial.Pedido;

        public DateTime FechaRegistro { get; set; }

        public int RegistradoPorUsuarioId { get; set; }

        public Obra Obra { get; set; } = null!;

        public Material Material { get; set; } = null!;

        public Cotizacion? Cotizacion { get; set; }

        public Usuario RegistradoPor { get; set; } = null!;

        public ICollection<RetiroMaterial> Retiros { get; set; } = new List<RetiroMaterial>();
    }
}
