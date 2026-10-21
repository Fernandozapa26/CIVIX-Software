# Inicio del desarrollo (Windows / PowerShell)

## Requisitos
- Git
- Node.js compatible con la versión de Angular elegida
- Angular CLI (`npm install -g @angular/cli`)
- SDK .NET compatible con la versión acordada
- SQL Server y SQL Server Management Studio (SSMS)

## 1. Crear Angular usando el CLI oficial
Desde la raíz `CIVIX`:

```powershell
cd frontend
# Antes, eliminar la carpeta civix-web que contiene solo marcadores .gitkeep
# (guardar cualquier cambio real antes de borrarla).
Remove-Item -Recurse -Force civix-web
ng new civix-web --routing --style=scss --standalone --skip-git
cd ..
```

El CLI creará los archivos reales (`angular.json`, `package.json`, etc.).
Los directorios `features/`, `core/`, `shared/` y `layouts/` se encuentran
como guía en `frontend/ESTRUCTURA.md`; créalos dentro de `frontend/civix-web/src/app/`
cuando comience el desarrollo. No crear componentes vacíos innecesarios.

## 2. Crear solución ASP.NET Core
Desde la raíz `CIVIX`:

```powershell
cd backend
dotnet new sln -n CIVIX
dotnet new webapi -n CIVIX.Api -o src/CIVIX.Api --use-controllers
dotnet new classlib -n CIVIX.Application -o src/CIVIX.Application
dotnet new classlib -n CIVIX.Domain -o src/CIVIX.Domain
dotnet new classlib -n CIVIX.Infrastructure -o src/CIVIX.Infrastructure
dotnet new xunit -n CIVIX.UnitTests -o tests/CIVIX.UnitTests
dotnet new xunit -n CIVIX.IntegrationTests -o tests/CIVIX.IntegrationTests
dotnet sln add src/CIVIX.Api src/CIVIX.Application src/CIVIX.Domain src/CIVIX.Infrastructure tests/CIVIX.UnitTests tests/CIVIX.IntegrationTests
dotnet add src/CIVIX.Api reference src/CIVIX.Application src/CIVIX.Infrastructure
dotnet add src/CIVIX.Application reference src/CIVIX.Domain
dotnet add src/CIVIX.Infrastructure reference src/CIVIX.Application src/CIVIX.Domain
dotnet add tests/CIVIX.UnitTests reference src/CIVIX.Application src/CIVIX.Domain
dotnet add tests/CIVIX.IntegrationTests reference src/CIVIX.Api
dotnet build
cd ..
```

> La extensión de solución (`.sln` o `.slnx`) depende de la versión del SDK.
> No crearla manualmente: dejar que `dotnet new sln` la genere.

## 3. Base de datos
Revisar `database/scripts/001_esquema_propuesto.sql` **antes de ejecutar**.
Ese script es un borrador de diseño y **no es compatible directamente con
ASP.NET Core Identity**. Si se usa Identity, las tablas de usuarios y roles
deben generarse con sus migraciones de EF Core, y el esquema SQL debe ajustarse.
No usar el script SQL y las migraciones para crear las mismas tablas dos veces.

## 4. Ejecución
Después de generar las aplicaciones:
```powershell
cd backend/src/CIVIX.Api
dotnet run
```
En otra terminal:
```powershell
cd frontend/civix-web
npm start
```
La integración HTTP, CORS, autenticación y la conexión SQL se configuran
posteriormente; esta plantilla no afirma que ya estén implementadas.
