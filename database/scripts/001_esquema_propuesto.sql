/*
CIVIX - BORRADOR de esquema SQL Server
NO EJECUTAR sin validar la estrategia de autenticación:
ASP.NET Core Identity requiere su propio esquema de usuarios y roles.
Recomendación final: migraciones de Entity Framework Core como fuente de verdad.
*/
CREATE DATABASE CIVIX;
GO
USE CIVIX;
GO

CREATE TABLE dbo.Roles (
 Id INT IDENTITY(1,1) PRIMARY KEY,
 Nombre NVARCHAR(50) NOT NULL UNIQUE,
 Descripcion NVARCHAR(200) NULL
);
CREATE TABLE dbo.Usuarios (
 Id INT IDENTITY(1,1) PRIMARY KEY,
 Nombre NVARCHAR(150) NOT NULL,
 Correo NVARCHAR(255) NOT NULL UNIQUE,
 PasswordHash NVARCHAR(500) NOT NULL,
 Activo BIT NOT NULL DEFAULT 1,
 FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE TABLE dbo.UsuariosRoles (
 UsuarioId INT NOT NULL REFERENCES dbo.Usuarios(Id),
 RolId INT NOT NULL REFERENCES dbo.Roles(Id),
 CONSTRAINT PK_UsuariosRoles PRIMARY KEY (UsuarioId,RolId)
);
CREATE TABLE dbo.Obras (
 Id INT IDENTITY(1,1) PRIMARY KEY,
 Nombre NVARCHAR(150) NOT NULL,
 Descripcion NVARCHAR(1000),
 Ubicacion NVARCHAR(250),
 FechaInicio DATE,
 FechaFinEstimada DATE,
 Estado NVARCHAR(30) NOT NULL DEFAULT N'Planeada',
 FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
 CONSTRAINT CK_Obras_Fechas CHECK
 (FechaInicio IS NULL OR FechaFinEstimada IS NULL OR FechaFinEstimada >= FechaInicio)
);
CREATE TABLE dbo.ObraUsuarios (
 ObraId INT NOT NULL REFERENCES dbo.Obras(Id),
 UsuarioId INT NOT NULL REFERENCES dbo.Usuarios(Id),
 FechaAsignacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
 Activo BIT NOT NULL DEFAULT 1,
 CONSTRAINT PK_ObraUsuarios PRIMARY KEY (ObraId,UsuarioId)
);
CREATE TABLE dbo.Materiales (
 Id INT IDENTITY(1,1) PRIMARY KEY,
 Nombre NVARCHAR(150) NOT NULL,
 UnidadMedida NVARCHAR(30) NOT NULL,
 Descripcion NVARCHAR(500),
 Activo BIT NOT NULL DEFAULT 1
);
CREATE TABLE dbo.ObraMateriales (
 Id INT IDENTITY(1,1) PRIMARY KEY,
 ObraId INT NOT NULL REFERENCES dbo.Obras(Id),
 MaterialId INT NOT NULL REFERENCES dbo.Materiales(Id),
 Cantidad DECIMAL(18,3) NOT NULL CHECK (Cantidad > 0),
 FechaRegistro DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
 RegistradoPorUsuarioId INT NOT NULL REFERENCES dbo.Usuarios(Id)
);
CREATE TABLE dbo.Cotizaciones (
 Id INT IDENTITY(1,1) PRIMARY KEY,
 Numero NVARCHAR(30) NOT NULL UNIQUE,
 Fecha DATE NOT NULL DEFAULT CONVERT(DATE,GETDATE()),
 ObraId INT NULL REFERENCES dbo.Obras(Id),
 CreadoPorUsuarioId INT NOT NULL REFERENCES dbo.Usuarios(Id),
 Estado NVARCHAR(30) NOT NULL DEFAULT N'Borrador',
 Total DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (Total >= 0)
);
CREATE TABLE dbo.CotizacionDetalles (
 Id INT IDENTITY(1,1) PRIMARY KEY,
 CotizacionId INT NOT NULL REFERENCES dbo.Cotizaciones(Id),
 Concepto NVARCHAR(250) NOT NULL,
 Cantidad DECIMAL(18,3) NOT NULL CHECK (Cantidad > 0),
 PrecioUnitario DECIMAL(18,2) NOT NULL CHECK (PrecioUnitario >= 0),
 Subtotal AS CAST(Cantidad * PrecioUnitario AS DECIMAL(18,2)) PERSISTED
);
CREATE TABLE dbo.AvancesObra (
 Id INT IDENTITY(1,1) PRIMARY KEY,
 ObraId INT NOT NULL REFERENCES dbo.Obras(Id),
 RegistradoPorUsuarioId INT NOT NULL REFERENCES dbo.Usuarios(Id),
 Fecha DATE NOT NULL,
 Descripcion NVARCHAR(1500) NOT NULL,
 PorcentajeAvance DECIMAL(5,2) NULL
   CHECK (PorcentajeAvance IS NULL OR PorcentajeAvance BETWEEN 0 AND 100)
);
GO
