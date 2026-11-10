namespace civix_backend.Models
{
    /// <summary>
    /// Estados de una cotización (dbo.Cotizaciones.Estado).
    /// Borrador -> Aprobada (crea la obra) | Rechazada.
    /// "Anulada" es el borrado lógico: la cotización deja de estar activa.
    /// </summary>
    public static class EstadosCotizacion
    {
        public const string Borrador = "Borrador";
        public const string Aprobada = "Aprobada";
        public const string Rechazada = "Rechazada";
        public const string Anulada = "Anulada";
    }
}
