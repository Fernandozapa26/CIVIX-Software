/*
CIVIX - 004 Materiales por obra: estados, cotización y retiros
Requisitos: RF-006 (registrar materiales asociados a una obra) y RF-007 (consultar por obra),
más los acuerdos del equipo:
  - Estado del material en la obra: Pedido, En bodega, Retirado.
  - Asociación opcional con una cotización.
  - Registro de cada retiro: quién retiró, cuánto, para qué obra y qué usuario lo registró.

Se ejecuta DESPUÉS de 001_creacion_inicial.sql. Es seguro ejecutarlo más de una vez.
*/
USE [CIVIX];
GO

/* 1. Estado del material en la obra */
IF COL_LENGTH('dbo.ObraMateriales', 'Estado') IS NULL
BEGIN
    ALTER TABLE dbo.ObraMateriales
        ADD Estado NVARCHAR(20) NOT NULL
            CONSTRAINT DF_ObraMateriales_Estado DEFAULT (N'Pedido') WITH VALUES;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ObraMateriales_Estado')
BEGIN
    ALTER TABLE dbo.ObraMateriales WITH CHECK
        ADD CONSTRAINT CK_ObraMateriales_Estado
            CHECK (Estado IN (N'Pedido', N'En bodega', N'Retirado'));
END
GO

/* 2. Cotización asociada (opcional) */
IF COL_LENGTH('dbo.ObraMateriales', 'CotizacionId') IS NULL
BEGIN
    ALTER TABLE dbo.ObraMateriales ADD CotizacionId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ObraMateriales_Cotizaciones')
BEGIN
    ALTER TABLE dbo.ObraMateriales WITH CHECK
        ADD CONSTRAINT FK_ObraMateriales_Cotizaciones
            FOREIGN KEY (CotizacionId) REFERENCES dbo.Cotizaciones (Id);
END
GO

/* 3. Retiros de material */
IF OBJECT_ID('dbo.RetirosMaterial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RetirosMaterial (
        Id                     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetirosMaterial PRIMARY KEY,
        ObraMaterialId         INT            NOT NULL,
        Cantidad               DECIMAL(18,3)  NOT NULL,
        RetiradoPor            NVARCHAR(150)  NOT NULL,  -- persona que recoge (puede no tener cuenta)
        ObraDestinoId          INT            NOT NULL,  -- obra donde se usará el material
        RegistradoPorUsuarioId INT            NOT NULL,  -- usuario de CIVIX que registra el retiro
        FechaRetiro            DATETIME2(7)   NOT NULL CONSTRAINT DF_RetirosMaterial_Fecha DEFAULT (SYSDATETIME()),
        Observacion            NVARCHAR(500)  NULL,

        CONSTRAINT CK_RetirosMaterial_Cantidad CHECK (Cantidad > 0),
        CONSTRAINT FK_RetirosMaterial_ObraMateriales FOREIGN KEY (ObraMaterialId) REFERENCES dbo.ObraMateriales (Id),
        CONSTRAINT FK_RetirosMaterial_Obras          FOREIGN KEY (ObraDestinoId)  REFERENCES dbo.Obras (Id),
        CONSTRAINT FK_RetirosMaterial_Usuarios       FOREIGN KEY (RegistradoPorUsuarioId) REFERENCES dbo.Usuarios (Id)
    );

    CREATE INDEX IX_RetirosMaterial_ObraMaterialId ON dbo.RetirosMaterial (ObraMaterialId);
    CREATE INDEX IX_RetirosMaterial_ObraDestinoId  ON dbo.RetirosMaterial (ObraDestinoId);
END
GO

/* 4. Índice para consultar materiales por obra (RF-007) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ObraMateriales_ObraId'
               AND object_id = OBJECT_ID('dbo.ObraMateriales'))
BEGIN
    CREATE INDEX IX_ObraMateriales_ObraId ON dbo.ObraMateriales (ObraId);
END
GO

/* 5. Datos de prueba mínimos: una obra para poder probar si la tabla está vacía */
IF NOT EXISTS (SELECT 1 FROM dbo.Obras)
BEGIN
    INSERT INTO dbo.Obras (Nombre, Descripcion, Ubicacion, FechaInicio, FechaFinEstimada, Estado)
    VALUES (N'Obra de prueba Alameda 72', N'Obra creada para probar el módulo de Materiales',
            N'Medellín', CAST(GETDATE() AS DATE), DATEADD(MONTH, 6, CAST(GETDATE() AS DATE)), N'En ejecución');
END
GO

/* 6. Catálogo inicial de materiales si está vacío */
IF NOT EXISTS (SELECT 1 FROM dbo.Materiales)
BEGIN
    INSERT INTO dbo.Materiales (Nombre, UnidadMedida, Descripcion, Activo) VALUES
        (N'Cemento gris 50 kg',      N'Bulto',   N'Cemento de uso general', 1),
        (N'Arena de río',            N'm³',      N'Arena lavada para mezcla', 1),
        (N'Varilla corrugada 1/2"',  N'Unidad',  N'Varilla de 6 m', 1),
        (N'Ladrillo tolete',         N'Unidad',  NULL, 1);
END
GO
