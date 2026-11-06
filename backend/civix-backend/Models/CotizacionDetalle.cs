namespace civix_backend.Models
{
  public class CotizacionDetalle
  {
    public int Id { get; set; }

    public int CotizacionId { get; set; }

    public string Concepto { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; private set; }

    public Cotizacion Cotizacion { get; set; } = null!;
  }
}
