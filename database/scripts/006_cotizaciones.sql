/*
CIVIX - 006 Cotizaciones
Requisitos: RF-008 (crear cotización con ítems), RF-009 (calcular total), RF-010 (PDF),
más lo asignado al equipo:
  - Editar y anular (borrado lógico) cotizaciones.
  - El Administrador ve todas; el Ingeniero solo las que creó.
  - Al aprobar una cotización se crea la obra automáticamente con su cliente.

Agrega a dbo.Cotizaciones los datos del proyecto y de la aprobación.
Se ejecuta DESPUÉS de 001 y 005. Es seguro ejecutarlo más de una vez.
*/
USE [CIVIX];
GO

/* 1. Datos del proyecto que se cotiza */
IF COL_LENGTH('dbo.Cotizaciones', 'NombreProyecto') IS NULL
    ALTER TABLE dbo.Cotizaciones ADD NombreProyecto NVARCHAR(150) NOT NULL
        CONSTRAINT DF_Cotizaciones_NombreProyecto DEFAULT (N'') WITH VALUES;
GO

IF COL_LENGTH('dbo.Cotizaciones', 'Descripcion') IS NULL
    ALTER TABLE dbo.Cotizaciones ADD Descripcion NVARCHAR(1000) NULL;
GO

IF COL_LENGTH('dbo.Cotizaciones', 'Ubicacion') IS NULL
    ALTER TABLE dbo.Cotizaciones ADD Ubicacion NVARCHAR(250) NULL;
GO

/* 2. Datos de la aprobación o el rechazo */
IF COL_LENGTH('dbo.Cotizaciones', 'FechaDecision') IS NULL
    ALTER TABLE dbo.Cotizaciones ADD FechaDecision DATETIME2(7) NULL;
GO

IF COL_LENGTH('dbo.Cotizaciones', 'DecididaPorUsuarioId') IS NULL
    ALTER TABLE dbo.Cotizaciones ADD DecididaPorUsuarioId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Cotizaciones_DecididaPor')
    ALTER TABLE dbo.Cotizaciones WITH CHECK
        ADD CONSTRAINT FK_Cotizaciones_DecididaPor
            FOREIGN KEY (DecididaPorUsuarioId) REFERENCES dbo.Usuarios (Id);
GO

/* 3. Estados permitidos */
UPDATE dbo.Cotizaciones
SET Estado = N'Borrador'
WHERE Estado NOT IN (N'Borrador', N'Aprobada', N'Rechazada', N'Anulada');
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Cotizaciones_Estado')
    ALTER TABLE dbo.Cotizaciones WITH CHECK
        ADD CONSTRAINT CK_Cotizaciones_Estado
            CHECK (Estado IN (N'Borrador', N'Aprobada', N'Rechazada', N'Anulada'));
GO

/* 4. Índice para listar las cotizaciones de cada ingeniero */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Cotizaciones_CreadoPor'
               AND object_id = OBJECT_ID('dbo.Cotizaciones'))
    CREATE INDEX IX_Cotizaciones_CreadoPor ON dbo.Cotizaciones (CreadoPorUsuarioId);
GO
