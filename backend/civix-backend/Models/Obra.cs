namespace civix_backend.Models
{
    /// <summary>
    /// Tabla dbo.Obras (tal como está en 001_creacion_inicial.sql).
    /// Materiales la usa en modo lectura para listar obras y validar asociaciones.
    /// </summary>
    public class Obra
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public string? Ubicacion { get; set; }

        public DateTime? FechaInicio { get; set; }

        public DateTime? FechaFinEstimada { get; set; }

        public string Estado { get; set; } = "Planeada";

        public DateTime FechaCreacion { get; set; }
    }
}
