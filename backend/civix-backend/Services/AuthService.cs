using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using civix_backend.Configuration;
using civix_backend.Data;
using civix_backend.DTOs.Auth;
using civix_backend.Models;

namespace civix_backend.Services
{
    /// <summary>
    /// Lógica de autenticación: verifica credenciales, registra usuarios
    /// y genera el token JWT que el frontend envía en cada petición.
    /// </summary>
    public class AuthService
    {
        /// <summary>Nombre del claim donde viaja cada rol dentro del token.</summary>
        public const string ClaimRol = "role";

        /// <summary>Nombre del claim con el nombre visible del usuario.</summary>
        public const string ClaimNombre = "name";

        private const string MensajeCorreoDuplicado = "Ya existe una cuenta registrada con ese correo.";

        // Usuario ficticio usado solo para calcular un hash cuando el correo no existe,
        // así la respuesta tarda lo mismo y no revela qué correos están registrados.
        private static readonly Usuario UsuarioFicticio = new();

        private readonly CivixDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly JwtOptions _jwt;

        public AuthService(
            CivixDbContext context,
            IPasswordHasher<Usuario> passwordHasher,
            JwtOptions jwt)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwt = jwt;
        }

        /// <summary>
        /// Valida correo y contraseña. Devuelve null si las credenciales no son válidas
        /// o si el usuario está inactivo (el controlador responde 401 en ambos casos).
        /// </summary>
        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var correo = NormalizarCorreo(dto.Correo);

            var usuario = await _context.Usuarios
                .Include(u => u.UsuariosRoles)
                    .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.Correo == correo);

            if (usuario is null)
            {
                _passwordHasher.HashPassword(UsuarioFicticio, dto.Password);
                return null;
            }

            var resultado = VerificarPassword(usuario, dto.Password);

            if (resultado == PasswordVerificationResult.Failed || !usuario.Activo)
            {
                return null;
            }

            // Si el hash se creó con parámetros antiguos, se actualiza al formato actual.
            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.PasswordHash = _passwordHasher.HashPassword(usuario, dto.Password);
                await _context.SaveChangesAsync();
            }

            return GenerarRespuesta(usuario);
        }

        /// <summary>
        /// Crea un usuario nuevo. Si quien lo solicita no es Administrador,
        /// el rol se fuerza a Cliente sin importar el RolId recibido.
        /// </summary>
        public async Task<ResultadoRegistro> RegistrarAsync(RegisterDto dto, bool solicitadoPorAdministrador)
        {
            var correo = NormalizarCorreo(dto.Correo);

            if (await _context.Usuarios.AnyAsync(u => u.Correo == correo))
            {
                return ResultadoRegistro.Conflicto(MensajeCorreoDuplicado);
            }

            var rolId = solicitadoPorAdministrador && dto.RolId.HasValue
                ? dto.RolId.Value
                : RolesSistema.ClienteId;

            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == rolId);

            if (rol is null)
            {
                return ResultadoRegistro.Invalido("El rol indicado no existe.");
            }

            var usuario = new Usuario
            {
                Nombre = dto.Nombre.Trim(),
                Correo = correo,
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, dto.Password);
            usuario.UsuariosRoles.Add(new UsuarioRol { Rol = rol });

            _context.Usuarios.Add(usuario);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Dos registros simultáneos con el mismo correo: el índice UNIQUE de la base lo detiene.
                return ResultadoRegistro.Conflicto(MensajeCorreoDuplicado);
            }

            return ResultadoRegistro.Exito(GenerarRespuesta(usuario));
        }

        private PasswordVerificationResult VerificarPassword(Usuario usuario, string password)
        {
            try
            {
                return _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
            }
            catch (FormatException)
            {
                // El valor guardado no es un hash válido (por ejemplo, una contraseña en texto plano).
                return PasswordVerificationResult.Failed;
            }
        }

        private AuthResponseDto GenerarRespuesta(Usuario usuario)
        {
            var roles = usuario.UsuariosRoles
                .Select(ur => ur.Rol.Nombre)
                .Distinct()
                .ToList();

            var expiraEn = DateTime.UtcNow.AddMinutes(_jwt.ExpiracionMinutos);

            return new AuthResponseDto
            {
                Token = GenerarToken(usuario, roles, expiraEn),
                ExpiraEn = expiraEn,
                UsuarioId = usuario.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Roles = roles
            };
        }

        private string GenerarToken(Usuario usuario, IEnumerable<string> roles, DateTime expiraEn)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, usuario.Correo),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimNombre, usuario.Nombre)
            };

            claims.AddRange(roles.Select(rol => new Claim(ClaimRol, rol)));

            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _jwt.Issuer,
                Audience = _jwt.Audience,
                Expires = expiraEn,
                SigningCredentials = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256)
            };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }

        private static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Resultado del registro: o trae los datos del usuario creado, o un mensaje de error.
    /// EsConflicto distingue "correo ya registrado" (409) de otros errores (400).
    /// </summary>
    public record ResultadoRegistro(AuthResponseDto? Datos, string? Error, bool EsConflicto)
    {
        public static ResultadoRegistro Exito(AuthResponseDto datos) => new(datos, null, false);

        public static ResultadoRegistro Conflicto(string error) => new(null, error, true);

        public static ResultadoRegistro Invalido(string error) => new(null, error, false);
    }
}
