-- ============================================================
-- FIX: columna es_mock_location en MARCACIONES_ASISTENCIA
-- El codigo (entidad MarcacionAsistencia / feature "GPS Fake") mapea
-- EsMockLocation -> columna es_mock_location. Si la tabla no la tiene,
-- TODA marcacion falla con "Invalid column name 'es_mock_location'".
-- Idempotente.
-- ============================================================
USE [DB_RRHH];
GO
IF COL_LENGTH('dbo.MARCACIONES_ASISTENCIA','es_mock_location') IS NULL
BEGIN
    ALTER TABLE dbo.MARCACIONES_ASISTENCIA ADD es_mock_location BIT NULL;
    PRINT '>> Columna es_mock_location agregada.';
END
ELSE
    PRINT '>> Columna es_mock_location ya existe.';
GO
