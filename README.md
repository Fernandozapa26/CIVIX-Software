# CIVIX — Estructura oficial inicial (v3)

Repositorio base para el MVP de CIVIX: Usuarios y seguridad, Obras, Materiales, Cotizaciones y Avances.

**Tecnologías previstas:** Angular + TypeScript, ASP.NET Core Web API (C#), SQL Server, Git/GitHub.

## Qué incluye
- Árbol de carpetas completo para frontend y backend por capas, con `.gitkeep` para que GitHub conserve las carpetas vacías.
- Script SQL preliminar en `database/scripts/001_esquema_propuesto.sql` (10 tablas, pendiente de aprobación).
- Documentación y plantillas de colaboración.

## Qué NO incluye todavía
No contiene una aplicación Angular ni una API .NET compilables. **No se inventaron** archivos como `angular.json`, `package.json`, `.csproj`, `CIVIX.slnx`, `Program.cs`, `appsettings.json`, `CivixDbContext.cs` ni workflows funcionales. Se generan al inicializar los proyectos y elegir versiones.

## Inicialización correcta
1. Antes de crear los proyectos, revisar `docs/08-manual-tecnico/INICIO.md`.
2. **Angular:** la carpeta `frontend/civix-web` ya existe con marcadores `.gitkeep`; para ejecutar `ng new civix-web --routing --style=scss --standalone --skip-git` desde `frontend`, eliminar primero esa carpeta de marcadores o generar en un directorio temporal y trasladar los archivos. No sobrescribir a ciegas.
3. **.NET:** crear solución y proyectos con los comandos indicados en la guía; los directorios vacíos existentes pueden requerir retirar `.gitkeep` antes de ejecutar `dotnet new` o usar `--force` solo tras verificar que no hay código.
4. **SQL Server:** no ejecutar simultáneamente el script de creación manual y migraciones EF Core para las mismas tablas. Si se usa ASP.NET Core Identity, adaptar el esquema de seguridad.
5. Registrar versiones, autenticación, librería PDF y demás decisiones en `docs/adr/0001-decisiones-iniciales.md`.

## Roles propuestos
- Fernando: Obras, Avances, coordinación e integración.
- Londoño: Usuarios, seguridad, arquitectura y coordinación de datos.
- Paniagua: Materiales, Cotizaciones y UX/UI.
Todos participan en frontend, backend, pruebas y revisiones.
