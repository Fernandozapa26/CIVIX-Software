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

## 📁 Estructura del Repositorio

```text
CIVIX/
│
├── .editorconfig
├── .env.example
├── .gitattributes
├── .gitignore
├── CONTRIBUTING.md
├── README.md
│
+---backend
│   └── civix-backend
│       ├── appsettings.json
│       ├── civix-backend.csproj
│       ├── civix-backend.http
│       ├── civix-backend.slnx
│       ├── Program.cs
│       ├── WeatherForecast.cs
│       ├── Controllers/
│       │   ├── CotizacionesController.cs
│       │   ├── MaterialesController.cs
│       │   └── WeatherForecastController.cs
│       ├── Data/
│       │   └── CivixDbContext.cs
│       ├── DTOs/
│       │   └── Materiales/
│       │       └── MaterialesDTO.cs
│       ├── Models/
│       │   └── Material.cs
│       ├── Properties/
│       │   └── launchSettings.json
│       └── Services/
│           └── MaterialesService.cs
│
+---database
│   ├── README.md
│   ├── diagrams/
│   └── scripts/
│       ├── 001_creacion_inicial.sql
│       └── 001_esquema_propuesto.sql
│
+---docs
│   ├── 01-formulacion/
│   ├── 02-gestion-proyecto/
│   ├── 03-requisitos/
│   ├── 04-backlog/
│   ├── 05-diseno-ux-ui/
│   ├── 06-arquitectura-datos/
│   ├── 07-pruebas-calidad/
│   ├── 08-manual-tecnico/
│   ├── 09-manual-usuario/
│   ├── 10-cierre-entrega/
│   ├── adr/
│   │   └── 0001-decisiones-iniciales.md
│   └── api/
│
+---frontend
│   └── civix-front
│       ├── angular.json
│       ├── package.json
│       ├── tsconfig.json
│       ├── public/
│       └── src/
│           ├── index.html
│           ├── main.ts
│           ├── styles.scss
│           └── app/
│               ├── app.config.ts
│               ├── app.routes.ts
│               ├── app.ts
│               ├── features/
│               │   ├── cotizaciones/
│               │   └── materiales/
│               └── layouts/
│                   └── main-layout/
│
└── infra
    ├── README.md
    └── docker/