namespace civix_backend.Models;

public class Avance
{
    public int Id { get; set; }
    public int ObraId { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Descripcion { get; set; } = string.Empty;
    public decimal? PorcentajeAvance { get; set; }
    public List<string> Imagenes { get; set; } = new();
}
