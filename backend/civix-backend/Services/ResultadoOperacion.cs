namespace civix_backend.Services
{
    /// <summary>
    /// Resultado de una operación de servicio: el dato si salió bien,
    /// o el código HTTP y el mensaje que el controlador debe devolver.
    /// </summary>
    public class ResultadoOperacion<T>
    {
        public T? Dato { get; private init; }
        public int CodigoError { get; private init; }
        public string? Mensaje { get; private init; }
        public bool Exito => CodigoError == 0;

        public static ResultadoOperacion<T> Ok(T dato) => new() { Dato = dato };

        public static ResultadoOperacion<T> NoEncontrado(string mensaje) =>
            new() { CodigoError = StatusCodes.Status404NotFound, Mensaje = mensaje };

        public static ResultadoOperacion<T> Invalido(string mensaje) =>
            new() { CodigoError = StatusCodes.Status400BadRequest, Mensaje = mensaje };

        public static ResultadoOperacion<T> Prohibido(string mensaje) =>
            new() { CodigoError = StatusCodes.Status403Forbidden, Mensaje = mensaje };
    }
}
