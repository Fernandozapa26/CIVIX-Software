
using Microsoft.EntityFrameworkCore;
using civix_backend.Data;
using civix_backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CivixDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CivixConnection")
    )
);

builder.Services.AddScoped<MaterialesService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularCivix", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AngularCivix");

app.UseAuthorization();

app.MapControllers();

app.Run();
