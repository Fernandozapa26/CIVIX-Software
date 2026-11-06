/*
CIVIX - 002: Cuentas de prueba para el módulo de autenticación
Requiere: 001_creacion_inicial.sql ya ejecutado (tablas y roles 1, 2 y 3).
Es idempotente: se puede ejecutar varias veces sin duplicar datos.

Cuentas (las mismas que muestra la pantalla de login):
  admin@civix.co      / Admin123      -> Administrador
  ingeniero@civix.co  / Ingeniero123  -> Ingeniero
  cliente@civix.co    / Cliente123    -> Cliente

Los hashes usan el formato de ASP.NET Core PasswordHasher v3
(PBKDF2 con HMAC-SHA512, 100.000 iteraciones, sal de 16 bytes).
SOLO PARA DESARROLLO: no ejecutar en producción.
*/
USE [CIVIX];
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

-- 1. Usuarios de prueba
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Correo = N'admin@civix.co')
    INSERT INTO dbo.Usuarios (Nombre, Correo, PasswordHash, Activo, FechaCreacion)
    VALUES (N'Administrador CIVIX', N'admin@civix.co',
            N'AQAAAAIAAYagAAAAEKPOOm4yzmcrmCqJlkJX4FoNcR89zXBZPtL1+wPNa8TwRPBujDWkadT090S22NkWIw==',
            1, SYSDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Correo = N'ingeniero@civix.co')
    INSERT INTO dbo.Usuarios (Nombre, Correo, PasswordHash, Activo, FechaCreacion)
    VALUES (N'Ingeniero de Obra', N'ingeniero@civix.co',
            N'AQAAAAIAAYagAAAAEJgRMZ71zi6uqepHIJcK0IOmbe3PAgOBnZqqHBs20fKZjHazeUYC4qgG8c7Gm9ZjuQ==',
            1, SYSDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Correo = N'cliente@civix.co')
    INSERT INTO dbo.Usuarios (Nombre, Correo, PasswordHash, Activo, FechaCreacion)
    VALUES (N'Cliente Demo', N'cliente@civix.co',
            N'AQAAAAIAAYagAAAAED2/3YTXnuBoF7gI7NSlClwm3SvuDuUodr+iTIuemwemnuoGeyo7uAqLXtdetRD7Bg==',
            1, SYSDATETIME());

-- 2. Asignación de roles (1 = Administrador, 2 = Ingeniero, 3 = Cliente)
INSERT INTO dbo.UsuariosRoles (UsuarioId, RolId)
SELECT u.Id, v.RolId
FROM (VALUES (N'admin@civix.co', 1),
             (N'ingeniero@civix.co', 2),
             (N'cliente@civix.co', 3)) AS v (Correo, RolId)
JOIN dbo.Usuarios u ON u.Correo = v.Correo
WHERE NOT EXISTS (SELECT 1 FROM dbo.UsuariosRoles ur
                  WHERE ur.UsuarioId = u.Id AND ur.RolId = v.RolId);

-- 3. Obra de demostración para probar el acceso restringido por obra
IF NOT EXISTS (SELECT 1 FROM dbo.Obras WHERE Nombre = N'Obra demostración CIVIX')
    INSERT INTO dbo.Obras (Nombre, Descripcion, Ubicacion, FechaInicio, FechaFinEstimada, Estado, FechaCreacion)
    VALUES (N'Obra demostración CIVIX', N'Obra de prueba para validar permisos por rol.',
            N'Medellín, Antioquia', CONVERT(date, GETDATE()), DATEADD(MONTH, 6, CONVERT(date, GETDATE())),
            N'En ejecución', SYSDATETIME());

-- 4. El ingeniero y el cliente quedan asignados a la obra de demostración
INSERT INTO dbo.ObraUsuarios (ObraId, UsuarioId, FechaAsignacion, Activo)
SELECT o.Id, u.Id, SYSDATETIME(), 1
FROM dbo.Obras o
JOIN dbo.Usuarios u ON u.Correo IN (N'ingeniero@civix.co', N'cliente@civix.co')
WHERE o.Nombre = N'Obra demostración CIVIX'
  AND NOT EXISTS (SELECT 1 FROM dbo.ObraUsuarios ou
                  WHERE ou.ObraId = o.Id AND ou.UsuarioId = u.Id);

COMMIT TRANSACTION;
GO

-- Verificación rápida
SELECT u.Id, u.Nombre, u.Correo, r.Nombre AS Rol, u.Activo
FROM dbo.Usuarios u
JOIN dbo.UsuariosRoles ur ON ur.UsuarioId = u.Id
JOIN dbo.Roles r ON r.Id = ur.RolId
WHERE u.Correo LIKE N'%@civix.co'
ORDER BY r.Id;
GO
