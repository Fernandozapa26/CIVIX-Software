-- 004_columnas_obras_avances.sql
-- Agrega las columnas nuevas usadas por los modulos de Obras y Avances
-- sobre la base de datos CIVIX ya existente (sin recrearla).

USE [CIVIX];
GO

IF COL_LENGTH('dbo.Obras', 'Cliente') IS NULL
    ALTER TABLE dbo.Obras ADD Cliente nvarchar(150) NULL;
GO

IF COL_LENGTH('dbo.Obras', 'Presupuesto') IS NULL
    ALTER TABLE dbo.Obras ADD Presupuesto decimal(18,2) NOT NULL DEFAULT(0);
GO

IF COL_LENGTH('dbo.Obras', 'Avance') IS NULL
    ALTER TABLE dbo.Obras ADD Avance int NOT NULL DEFAULT(0);
GO

IF COL_LENGTH('dbo.Obras', 'Personal') IS NULL
    ALTER TABLE dbo.Obras ADD Personal int NOT NULL DEFAULT(0);
GO

IF COL_LENGTH('dbo.AvancesObra', 'Imagenes') IS NULL
    ALTER TABLE dbo.AvancesObra ADD Imagenes nvarchar(max) NULL;
GO