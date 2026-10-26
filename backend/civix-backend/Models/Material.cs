namespace civix_backend.Models
{
    public class Material
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string UnidadMedida { get; set; } = string.Empty;

        public string? Descripcion { get; set; }
    }
}
