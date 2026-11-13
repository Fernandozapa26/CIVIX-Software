using civix_backend.Models;

namespace civix_backend.DTOs.Avances
{
    /// <summary>
    /// Avance tal como lo consume el frontend: los mismos campos de dbo.AvancesObra
    /// más el nombre del usuario que lo registró.
    /// </summary>
    public class AvanceDto
    {
        public int Id { get; set; }

        public int ObraId { get; set; }

        public int RegistradoPorUsuarioId { get; set; }

        /// <summary>Nombre de quien registró el avance (responsable del registro).</summary>
        public string? RegistradoPorNombre { get; set; }

        public DateTime Fecha { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public decimal? PorcentajeAvance { get; set; }

        public List<string> Imagenes { get; set; } = new();

        public static AvanceDto Desde(Avance avance, string? registradoPorNombre) => new()
        {
            Id = avance.Id,
            ObraId = avance.ObraId,
            RegistradoPorUsuarioId = avance.RegistradoPorUsuarioId,
            RegistradoPorNombre = registradoPorNombre,
            Fecha = avance.Fecha,
            Descripcion = avance.Descripcion,
            PorcentajeAvance = avance.PorcentajeAvance,
            Imagenes = avance.Imagenes ?? new List<string>()
        };
    }
}
