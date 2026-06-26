-- ============================================================
-- Indices de rendimiento (cierre diario y marcacion)
-- Idempotente.
-- ============================================================
USE [DB_RRHH];
GO

-- Marcaciones: filtradas por trabajador + fecha en el cierre (PASO D) y al marcar.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_MARC_Trab_Fecha' AND object_id=OBJECT_ID('dbo.MARCACIONES_ASISTENCIA'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_MARC_Trab_Fecha
        ON dbo.MARCACIONES_ASISTENCIA (id_trabajador, fecha_hora)
        INCLUDE (tipo_marcacion);
    PRINT '>> Indice IX_MARC_Trab_Fecha creado.';
END
ELSE PRINT '>> IX_MARC_Trab_Fecha ya existe.';
GO

-- Resumen diario: el cierre borra/consulta por fecha; reportes filtran por fecha.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ARD_Fecha' AND object_id=OBJECT_ID('dbo.ASISTENCIA_RESUMEN_DIARIO'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ARD_Fecha
        ON dbo.ASISTENCIA_RESUMEN_DIARIO (fecha_asistencia)
        INCLUDE (id_trabajador, estado_asistencia);
    PRINT '>> Indice IX_ARD_Fecha creado.';
END
ELSE PRINT '>> IX_ARD_Fecha ya existe.';
GO

-- Programacion semanal: el cierre y la marcacion buscan por trabajador + fecha (ya hay UQ, reforzamos por fecha).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_PTS_Fecha' AND object_id=OBJECT_ID('dbo.PROGRAMACION_TURNOS_SEMANAL'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PTS_Fecha
        ON dbo.PROGRAMACION_TURNOS_SEMANAL (fecha)
        INCLUDE (id_trabajador, id_horario_turno, es_descanso, es_vacaciones, tipo_ausencia);
    PRINT '>> Indice IX_PTS_Fecha creado.';
END
ELSE PRINT '>> IX_PTS_Fecha ya existe.';
GO
