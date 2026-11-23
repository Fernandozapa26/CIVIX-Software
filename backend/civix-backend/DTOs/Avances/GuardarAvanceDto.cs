using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.Avances
{
    /// <summary>
    /// Datos que el frontend envía para crear o editar un avance.
    /// Solo incluye los campos que el usuario puede cambiar: el Id y
    /// RegistradoPorUsuarioId los asigna el servidor.
    /// </summary>
    public class GuardarAvanceDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "La obra es obligatoria.")]
        public int ObraId { get; set; }

        /// <summary>Si no se envía, se usa la fecha de hoy (al crear) o la existente (al editar).</summary>
        public DateTime? Fecha { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "La descripcion es obligatoria.")]
        [MaxLength(1500, ErrorMessage = "La descripcion no puede superar 1500 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "El porcentaje debe estar entre 0 y 100.")]
        public decimal? PorcentajeAvance { get; set; }

        /// <summary>Rutas devueltas por POST /api/avances/imagenes.</summary>
        public List<string>? Imagenes { get; set; }
    }
}
