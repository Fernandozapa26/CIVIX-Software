namespace civix_backend.Models;

public class Obra
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFinEstimada { get; set; }
    public string Estado { get; set; } = "Planificada";
    public decimal Presupuesto { get; set; }
    public int Avance { get; set; }
    public int Personal { get; set; }
}