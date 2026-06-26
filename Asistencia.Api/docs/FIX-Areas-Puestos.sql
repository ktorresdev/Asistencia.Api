-- ============================================================
-- Modulo Areas / Puestos / Jefaturas
-- AREAS y TRABAJADORES.id_area YA EXISTEN en la BD.
-- Este script agrega la tabla PUESTOS, la columna
-- TRABAJADORES.id_puesto y las llaves foraneas faltantes.
-- Idempotente: se puede ejecutar varias veces sin error.
-- ============================================================

-- 1. Tabla PUESTOS
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PUESTOS')
BEGIN
    CREATE TABLE dbo.PUESTOS (
        id_puesto     INT IDENTITY(1,1) NOT NULL,
        nombre_puesto VARCHAR(60) NOT NULL,
        id_area       INT NULL,
        es_activo     BIT NOT NULL CONSTRAINT DF_Puestos_EsActivo DEFAULT(1),
        CONSTRAINT PK_Puestos PRIMARY KEY CLUSTERED (id_puesto ASC),
        CONSTRAINT UQ_Puestos_Nombre UNIQUE (nombre_puesto),
        CONSTRAINT FK_Puestos_Areas FOREIGN KEY (id_area) REFERENCES dbo.AREAS(id_area)
    );
    PRINT 'Tabla PUESTOS creada.';
END
ELSE
    PRINT 'Tabla PUESTOS ya existe.';
GO

-- 2. Columna id_puesto en TRABAJADORES
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = 'id_puesto' AND Object_ID = Object_ID('dbo.TRABAJADORES'))
BEGIN
    ALTER TABLE dbo.TRABAJADORES ADD id_puesto INT NULL;
    PRINT 'Columna TRABAJADORES.id_puesto agregada.';
END
ELSE
    PRINT 'Columna TRABAJADORES.id_puesto ya existe.';
GO

-- 3. FK TRABAJADORES.id_puesto -> PUESTOS
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trabajadores_Puestos')
BEGIN
    ALTER TABLE dbo.TRABAJADORES
        ADD CONSTRAINT FK_Trabajadores_Puestos
            FOREIGN KEY (id_puesto) REFERENCES dbo.PUESTOS(id_puesto);
    PRINT 'FK FK_Trabajadores_Puestos creada.';
END
ELSE
    PRINT 'FK FK_Trabajadores_Puestos ya existe.';
GO

-- 4. FK TRABAJADORES.id_area -> AREAS (por si la columna se agrego sin FK)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Trabajadores_Areas')
BEGIN
    ALTER TABLE dbo.TRABAJADORES
        ADD CONSTRAINT FK_Trabajadores_Areas
            FOREIGN KEY (id_area) REFERENCES dbo.AREAS(id_area);
    PRINT 'FK FK_Trabajadores_Areas creada.';
END
ELSE
    PRINT 'FK FK_Trabajadores_Areas ya existe.';
GO
