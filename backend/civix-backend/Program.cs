using Microsoft.EntityFrameworkCore;
using civix_backend.Data;
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

// Configuración de controladores
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
