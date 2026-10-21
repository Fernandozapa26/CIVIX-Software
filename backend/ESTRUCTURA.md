# Organización sugerida de ASP.NET Core
- `CIVIX.Api`: controladores HTTP, middleware, configuración.
- `CIVIX.Application`: casos de uso, servicios, DTO e interfaces.
- `CIVIX.Domain`: entidades, enumeraciones y reglas de negocio.
- `CIVIX.Infrastructure`: DbContext, configuraciones EF Core, migraciones, repositorios.
- `tests/`: pruebas unitarias y de integración.

El SDK generará proyectos reales con los comandos de `docs/08-manual-tecnico/INICIO.md`.
No se incluyen archivos `.csproj` de relleno.
