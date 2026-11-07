namespace civix_backend.Models
{
    /// <summary>
    /// Estados de un material asociado a una obra (dbo.ObraMateriales.Estado).
    /// Pedido -> En bodega -> Retirado. "Retirado" lo asigna el sistema cuando
    /// la cantidad disponible llega a cero por medio de retiros.
    /// </summary>
    public static class EstadosMaterial
    {
        public const string Pedido = "Pedido";
        public const string EnBodega = "En bodega";
        public const string Retirado = "Retirado";

        /// <summary>Estados que el usuario puede elegir al registrar o editar.</summary>
        public static readonly string[] Seleccionables = { Pedido, EnBodega };

        public static readonly string[] Todos = { Pedido, EnBodega, Retirado };
    }
}
