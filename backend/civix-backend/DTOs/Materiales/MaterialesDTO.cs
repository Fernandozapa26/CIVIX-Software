using System.ComponentModel.DataAnnotations;

namespace civix_backend.DTOs.Materiales
{
    public class MaterialesDTO
    {
        [Required]
        [StringLength(150)]
        public String Nombre { get; set; } = string.Empty;


        [Required]
        [StringLength(30)]
        public String UnidadMedida { get; set; } = string.Empty;

      
        // Opcional (la columna admite NULL) y hasta 500 caracteres, igual que en la BD.
        [StringLength(500)]
        public string? Descripcion { get; set; }

    }
}
