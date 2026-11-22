using System.Text.RegularExpressions;

namespace civix_backend.Services
{
    /// <summary>
    /// Guarda, valida y localiza las fotos de los avances.
    ///
    /// Las fotos se guardan FUERA de wwwroot (carpeta "Almacenamiento/avances"),
    /// así que no se pueden abrir con una URL pública: solo se entregan por
    /// GET /api/avances/imagenes/{nombre}, que exige token y acceso a la obra.
    ///
    /// En la base se sigue guardando la ruta "/uploads/avances/{nombre}" para no
    /// tener que migrar los avances que ya existen.
    /// </summary>
    public partial class ImagenesAvanceService
    {
        /// <summary>Prefijo con el que se guardan las rutas en dbo.AvancesObra.Imagenes.</summary>
        public const string PrefijoRuta = "/uploads/avances/";

        /// <summary>Tamaño máximo por foto: 5 MB.</summary>
        public const long TamanoMaximoBytes = 5 * 1024 * 1024;

        /// <summary>Máximo de fotos por avance.</summary>
        public const int MaximoImagenesPorAvance = 10;

        private static readonly Dictionary<string, string> TiposPorExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

        // Solo nombres generados por el servidor: un GUID más una extensión permitida.
        [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\.(jpg|jpeg|png|gif|webp)$", RegexOptions.IgnoreCase)]
        private static partial Regex NombreValidoRegex();

        private readonly string _carpeta;
        private readonly string _carpetaAnterior;

        public ImagenesAvanceService(IWebHostEnvironment env)
        {
            _carpeta = Path.Combine(env.ContentRootPath, "Almacenamiento", "avances");

            // Ubicación usada antes de este cambio (era pública). Se sigue leyendo
            // para no perder las fotos ya subidas; se recomienda moverlas a _carpeta.
            _carpetaAnterior = Path.Combine(
                env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "avances");
        }

        /// <summary>True si el nombre tiene el formato de un archivo generado por el servidor.</summary>
        public static bool EsNombreValido(string? nombre) =>
            !string.IsNullOrEmpty(nombre) && NombreValidoRegex().IsMatch(nombre);

        /// <summary>
        /// Extrae el nombre de archivo de una ruta guardada ("/uploads/avances/{nombre}").
        /// Devuelve null si la ruta no tiene el formato esperado.
        /// </summary>
        public static string? NombreDesdeRuta(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !ruta.StartsWith(PrefijoRuta, StringComparison.OrdinalIgnoreCase))
                return null;

            var nombre = ruta[PrefijoRuta.Length..];
            return EsNombreValido(nombre) ? nombre : null;
        }

        /// <summary>Ruta física de la foto, o null si no existe.</summary>
        public string? BuscarArchivo(string nombre)
        {
            if (!EsNombreValido(nombre))
                return null;

            foreach (var carpeta in new[] { _carpeta, _carpetaAnterior })
            {
                var ruta = Path.Combine(carpeta, nombre);
                if (File.Exists(ruta))
                    return ruta;
            }

            return null;
        }

        public static string TipoContenido(string nombre) =>
            TiposPorExtension.GetValueOrDefault(Path.GetExtension(nombre), "application/octet-stream");

        /// <summary>
        /// Valida el archivo (extensión, tamaño y contenido real) y lo guarda con un nombre nuevo.
        /// Devuelve la ruta para guardar en el avance, o un mensaje de error.
        /// </summary>
        public async Task<(string? Ruta, string? Error)> GuardarAsync(IFormFile archivo)
        {
            if (archivo is null || archivo.Length == 0)
                return (null, "Archivo vacío.");

            if (archivo.Length > TamanoMaximoBytes)
                return (null, "La imagen supera el tamaño máximo de 5 MB.");

            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!TiposPorExtension.ContainsKey(ext))
                return (null, "Formato de imagen no permitido. Usa JPG, PNG, GIF o WEBP.");

            // Lee la cabecera del archivo para comprobar que de verdad es una imagen
            // del tipo indicado (un .jpg renombrado que en realidad es otra cosa se rechaza).
            var cabecera = new byte[12];
            await using (var lectura = archivo.OpenReadStream())
            {
                var leidos = await lectura.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);
                if (!CoincideFirma(ext, cabecera.AsSpan(0, leidos)))
                    return (null, "El archivo no es una imagen válida.");
            }

            Directory.CreateDirectory(_carpeta);

            var nombre = $"{Guid.NewGuid()}{ext}";
            await using (var destino = File.Create(Path.Combine(_carpeta, nombre)))
            {
                await archivo.CopyToAsync(destino);
            }

            return (PrefijoRuta + nombre, null);
        }

        /// <summary>Borra la foto si existe. No lanza excepción si falla.</summary>
        public void Eliminar(string ruta)
        {
            var nombre = NombreDesdeRuta(ruta);
            if (nombre is null)
                return;

            var archivo = BuscarArchivo(nombre);
            if (archivo is null)
                return;

            try
            {
                File.Delete(archivo);
            }
            catch (IOException)
            {
                // No interrumpir la operación principal si el archivo no se puede borrar.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static ReadOnlySpan<byte> FirmaPng => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        /// <summary>Compara los primeros bytes del archivo con la firma del formato.</summary>
        private static bool CoincideFirma(string ext, ReadOnlySpan<byte> b)
        {
            return ext switch
            {
                ".jpg" or ".jpeg" => b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
                ".png" => b.Length >= 8 && b[..8].SequenceEqual(FirmaPng),
                ".gif" => b.Length >= 6 && (b[..6].SequenceEqual("GIF87a"u8) || b[..6].SequenceEqual("GIF89a"u8)),
                ".webp" => b.Length >= 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8),
                _ => false
            };
        }
    }
}
