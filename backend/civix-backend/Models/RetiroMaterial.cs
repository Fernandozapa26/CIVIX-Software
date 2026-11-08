namespace civix_backend.Models
{
    /// <summary>
    /// Tabla dbo.RetirosMaterial: cada salida de material de bodega.
    /// Guarda quién lo retiró, para qué obra y qué usuario hizo el registro.
    /// </summary>
    public class RetiroMaterial
    {
        public int Id { get; set; }

        public int ObraMaterialId { get; set; }

        public decimal Cantidad { get; set; }

        /// <summary>Nombre de la persona que recogió el material (puede no tener cuenta en CIVIX).</summary>
        public string RetiradoPor { get; set; } = string.Empty;

        /// <summary>Obra en la que se usará el material.</summary>
        public int ObraDestinoId { get; set; }

        /// <summary>Usuario autenticado que registró el retiro.</summary>
        public int RegistradoPorUsuarioId { get; set; }

        public DateTime FechaRetiro { get; set; }

        public string? Observacion { get; set; }

        public ObraMaterial ObraMaterial { get; set; } = null!;

        public Obra ObraDestino { get; set; } = null!;

        public Usuario RegistradoPor { get; set; } = null!;
    }
}
