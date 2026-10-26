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

      
        [StringLength(50)]
        public String Descripcion { get; set; } = string.Empty;

    }
}
