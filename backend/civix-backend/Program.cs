using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using civix_backend.Configuration;
using civix_backend.Data;
using civix_backend.Models;
using civix_backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la conexión a SQL Server
builder.Services.AddDbContext<CivixDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CivixConnection")
    )
);

// Registro del servicio de materiales
builder.Services.AddScoped<MaterialesService>();
builder.Services.AddScoped<ObraMaterialesService>();
builder.Services.AddScoped<ObrasService>();
builder.Services.AddScoped<CotizacionesService>();
builder.Services.AddScoped<InicioService>();

// --- Autenticación y autorización ---

// Lee la sección "Jwt" de la configuración.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Falta la configuración Jwt:Key. Defínela con User Secrets o con la variable de entorno Jwt__Key.");
    }

    // Solo en desarrollo: si nadie configuró una clave, se genera una aleatoria.
    // Los tokens dejan de servir al reiniciar la API y el frontend vuelve a pedir login.
    jwtOptions.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    Console.WriteLine("[CIVIX] Jwt:Key no configurada: se usa una clave temporal de desarrollo.");
}

if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AccesoObrasService>();
builder.Services.AddScoped<UsuariosService>();
builder.Services.AddScoped<ImagenesAvanceService>();

// --- Recuperación de contraseña ("¿Olvidaste tu contraseña?") ---
builder.Services.AddSingleton(
    builder.Configuration.GetSection(SmtpOptions.Seccion).Get<SmtpOptions>() ?? new SmtpOptions());
builder.Services.AddSingleton(
    builder.Configuration.GetSection(RecuperacionPasswordOptions.Seccion).Get<RecuperacionPasswordOptions>()
    ?? new RecuperacionPasswordOptions());
builder.Services.AddSingleton<IEnviadorCorreo, SmtpEnviadorCorreo>();
builder.Services.AddScoped<RecuperacionPasswordService>();

// Límite de intentos por IP para los endpoints de recuperación (evita abuso y envío masivo de correos).
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(civix_backend.Controllers.AuthController.PoliticaRecuperacion, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    options.OnRejected = async (contexto, cancelacion) =>
    {
        contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await contexto.HttpContext.Response.WriteAsJsonAsync(
            new { mensaje = "Demasiados intentos. Espera unos minutos e intenta de nuevo." },
            cancelacion);
    };
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Conserva los nombres cortos de los claims ("sub", "role", "name").
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = AuthService.ClaimNombre,
            RoleClaimType = AuthService.ClaimRol
        };
    });

builder.Services.AddAuthorization();

// CORS: permite que el frontend Angular (otro puerto) llame a la API.
const string PoliticaCorsFrontend = "CivixFrontend";

var origenesPermitidos = builder.Configuration
    .GetSection("Cors:OrigenesPermitidos")
    .Get<string[]>() ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(PoliticaCorsFrontend, policy =>
        policy.WithOrigins(origenesPermitidos)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Configuración de controladores
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// En desarrollo no se redirige a HTTPS: el frontend llama a http://localhost:5067 y,
// si la API redirige, el navegador descarta el token y la petición termina en 401.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// No se usa UseStaticFiles(): las fotos de los avances son privadas y solo se entregan
// por GET /api/avances/imagenes/{nombre}, que valida el token y el acceso a la obra.

// El orden importa: CORS -> Autenticación (quién eres) -> Autorización (qué puedes hacer).

app.UseCors(PoliticaCorsFrontend);

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
