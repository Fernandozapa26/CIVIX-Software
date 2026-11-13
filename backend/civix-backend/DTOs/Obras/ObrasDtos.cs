using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.Obras
{
    /// <summary>
    /// Datos para crear o editar una obra. El cliente y el responsable son usuarios
    /// del sistema y quedan asignados a la obra en dbo.ObraUsuarios.
    /// </summary>
    public class GuardarObraDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre admite máximo 150 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "La descripción admite máximo 1000 caracteres.")]
        public string? Descripcion { get; set; }

        [StringLength(250, ErrorMessage = "La ubicación admite máximo 250 caracteres.")]
        public string? Ubicacion { get; set; }

        public DateTime? FechaInicio { get; set; }

        public DateTime? FechaFinEstimada { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio.")]
        [StringLength(30)]
        public string Estado { get; set; } = "En planeación";

        [Range(0, 999999999999, ErrorMessage = "El presupuesto no puede ser negativo.")]
        public decimal Presupuesto { get; set; }

        [Range(0, 100, ErrorMessage = "El avance debe estar entre 0 y 100.")]
        public int Avance { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El personal no puede ser negativo.")]
        public int Personal { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona el cliente de la obra.")]
        public int ClienteId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona el ingeniero responsable.")]
        public int ResponsableId { get; set; }
    }

    /// <summary>Obra con su cliente y su ingeniero responsable.</summary>
    public class ObraDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Ubicacion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinEstimada { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Presupuesto { get; set; }
        public int Avance { get; set; }
        public int Personal { get; set; }
        public DateTime? FechaCreacion { get; set; }

        /// <summary>Nombre del cliente (texto guardado en dbo.Obras.Cliente).</summary>
        public string? Cliente { get; set; }

        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public int? ResponsableId { get; set; }
        public string? ResponsableNombre { get; set; }

        /// <summary>
        /// Foto representativa: la primera imagen del avance más reciente que tenga fotos.
        /// Null si la obra todavía no tiene fotos registradas.
        /// </summary>
        public string? ImagenPortada { get; set; }
    }

    /// <summary>Usuario activo para los selectores de cliente y responsable.</summary>
    public class UsuarioOpcionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
    }
}
