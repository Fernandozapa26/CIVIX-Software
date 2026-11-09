namespace civix_backend.Models
{
    public class Cotizacion
    {
        public int Id { get; set; }

        public int? ObraId { get; set; }

        public DateTime Fecha { get; set; }

        public int CreadoPorUsuarioId { get; set; }

        public string Estado { get; set; } = "Borrador";

        public decimal Total { get; set; }

        public ICollection<CotizacionDetalle> Detalles
        { get; set; } = new List<CotizacionDetalle>();
    }
}
