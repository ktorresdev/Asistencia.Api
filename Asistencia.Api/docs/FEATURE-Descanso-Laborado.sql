-- ============================================================
-- FEATURE: DESCANSO LABORADO
-- ------------------------------------------------------------
-- Caso: el trabajador trabaja en su DIA DE DESCANSO, sin que sea
-- un cambio de dia (swap) ni una cobertura. El admin lo asigna
-- (durante la semana o el mismo dia) sobre un dia ya marcado como
-- descanso. Debe tomar sus marcaciones y reflejarse en el reporte
-- con estado propio: 'DESCANSO_LABORADO'.
--
-- OJO: esto es DISTINTO de:
--   - Cambio de descanso (swap de dia)        -> ya funciona
--   - DESCANSO_COMPENSATORIO (devolucion swap) -> ya funciona
--
-- Contenido:
--   PASO 1  -> columna es_descanso_laborado en PROGRAMACION_TURNOS_SEMANAL  (ejecutable)
--   PASO 2  -> parche al SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA               (insertar bloque)
--   PASO 3  -> verificacion
-- ============================================================

USE [DB_RRHH];
GO

-- ============================================================
-- PASO 1: Columna nueva en PROGRAMACION_TURNOS_SEMANAL (idempotente)
-- El dia sigue con es_descanso = 1; este flag indica que se trabaja.
-- El horario del dia se toma de id_horario_turno (o, si es NULL,
-- del horario de la asignacion vigente).
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = 'es_descanso_laborado'
                 AND Object_ID = Object_ID('dbo.PROGRAMACION_TURNOS_SEMANAL'))
BEGIN
    ALTER TABLE dbo.PROGRAMACION_TURNOS_SEMANAL
        ADD es_descanso_laborado BIT NOT NULL
            CONSTRAINT DF_PTS_EsDescansoLaborado DEFAULT(0);
    PRINT 'Columna es_descanso_laborado agregada.';
END
ELSE
    PRINT 'Columna es_descanso_laborado ya existe.';
GO

-- ============================================================
-- PASO 2: Parche al Stored Procedure SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA
-- ------------------------------------------------------------
-- NO se reescribe el SP completo aqui. Aplicar estos 2 cambios
-- dentro del cuerpo del SP (abrirlo con ALTER PROCEDURE en SSMS):
--
--   (2.A) INSERTAR el bloque "B2" JUSTO DESPUES del paso
--         "%% B :  Descansos y vacaciones desde PTS" y ANTES del
--         "%% C :  Coberturas aprobadas".
--
--   (2.B) AGREGAR 'DESCANSO_LABORADO' a la lista NOT IN del paso
--         "%% G :  Feriados" (para que un feriado no pise el estado).
--
-- Los pasos D (marcaciones reales) y E (tardanza/extra) NO requieren
-- cambios: como 'DESCANSO_LABORADO' NO esta en sus listas de exclusion,
-- automaticamente toman las marcaciones y calculan tardanza/extra.
-- El paso F solo toca filas 'PENDIENTE', asi que no pisa el estado.
-- ============================================================

-- ------------------------------------------------------------
-- (2.A)  BLOQUE  B2  — pegar despues del paso B, antes del paso C
-- ------------------------------------------------------------
/*
        -- %% B2: DESCANSO LABORADO (trabaja en su dia de descanso, sin cambio de dia) %%

        -- B2.1  Crear fila para descansos laborados que AUN no tienen fila
        --       (descanso cuyo dia no tiene HorarioDetalle base, p.ej. domingo)
        INSERT INTO dbo.ASISTENCIA_RESUMEN_DIARIO
        ( id_trabajador, fecha_asistencia, hora_entrada_teorica, hora_salida_teorica,
          hora_entrada_real, hora_salida_real, minutos_tardanza, minutos_extra,
          estado_asistencia, es_dia_boleta )
        SELECT
            T.id_trabajador, @FechaProceso,
            DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_inicio), CAST(@FechaProceso AS DATETIME)),
            CASE WHEN HD.salida_dia_siguiente = 1
                 THEN DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_fin), CAST(DATEADD(DAY,1,@FechaProceso) AS DATETIME))
                 ELSE DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_fin), CAST(@FechaProceso AS DATETIME)) END,
            NULL, NULL, 0, 0, 'DESCANSO_LABORADO', 0
        FROM dbo.TRABAJADORES T
        INNER JOIN dbo.PROGRAMACION_TURNOS_SEMANAL PTS
            ON  PTS.id_trabajador = T.id_trabajador
            AND PTS.fecha         = @FechaProceso
            AND PTS.es_descanso   = 1
            AND ISNULL(PTS.es_descanso_laborado, 0) = 1
        INNER JOIN dbo.HORARIOS_DETALLE HD
            ON  HD.id_horario_turno = ISNULL(
                    PTS.id_horario_turno,
                    (SELECT TOP 1 ast.id_horario_turno
                       FROM dbo.ASIGNACIONES_TURNO ast
                      WHERE ast.id_trabajador = T.id_trabajador
                        AND ast.es_vigente = 1
                        AND @FechaProceso >= ast.fecha_inicio_vigencia
                        AND (ast.fecha_fin_vigencia IS NULL OR @FechaProceso <= ast.fecha_fin_vigencia)))
            AND HD.dia_semana = @DiaSemana
        WHERE T.id_estado = 10
          AND NOT EXISTS (
                SELECT 1 FROM dbo.ASISTENCIA_RESUMEN_DIARIO ARD
                WHERE ARD.id_trabajador = T.id_trabajador
                  AND ARD.fecha_asistencia = @FechaProceso);

        -- B2.2  Para los que SI tienen fila (paso A la creo y paso B la puso en DESCANSO):
        --       fijar teoricas segun el horario del dia y marcar DESCANSO_LABORADO
        UPDATE R SET
            R.hora_entrada_teorica = DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_inicio), CAST(@FechaProceso AS DATETIME)),
            R.hora_salida_teorica  = CASE WHEN HD.salida_dia_siguiente = 1
                 THEN DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_fin), CAST(DATEADD(DAY,1,@FechaProceso) AS DATETIME))
                 ELSE DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_fin), CAST(@FechaProceso AS DATETIME)) END,
            R.estado_asistencia    = 'DESCANSO_LABORADO'
        FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
        INNER JOIN dbo.PROGRAMACION_TURNOS_SEMANAL PTS
            ON  PTS.id_trabajador = R.id_trabajador
            AND PTS.fecha         = @FechaProceso
            AND PTS.es_descanso   = 1
            AND ISNULL(PTS.es_descanso_laborado, 0) = 1
        INNER JOIN dbo.HORARIOS_DETALLE HD
            ON  HD.id_horario_turno = ISNULL(
                    PTS.id_horario_turno,
                    (SELECT TOP 1 ast.id_horario_turno
                       FROM dbo.ASIGNACIONES_TURNO ast
                      WHERE ast.id_trabajador = R.id_trabajador
                        AND ast.es_vigente = 1
                        AND @FechaProceso >= ast.fecha_inicio_vigencia
                        AND (ast.fecha_fin_vigencia IS NULL OR @FechaProceso <= ast.fecha_fin_vigencia)))
            AND HD.dia_semana = @DiaSemana
        WHERE R.fecha_asistencia   = @FechaProceso
          AND R.estado_asistencia  = 'DESCANSO';
*/

-- ------------------------------------------------------------
-- (2.B)  Paso G (Feriados): agregar 'DESCANSO_LABORADO' al NOT IN.
--        Queda asi (se agrega solo el ultimo valor):
-- ------------------------------------------------------------
/*
        UPDATE dbo.ASISTENCIA_RESUMEN_DIARIO SET estado_asistencia = 'FERIADO'
        WHERE fecha_asistencia = @FechaProceso
            AND estado_asistencia NOT IN (
                    'VACACIONES','DESCANSO','DESCANSO_COMPENSATORIO','FALTA_CUBIERTA',
                    'DESCANSO_MEDICO','MATERNIDAD','PATERNIDAD','PERMISO_SIN_GOCE',
                    'DESCANSO_LABORADO')   -- <== NUEVO
            AND EXISTS (
                    SELECT 1 FROM dbo.CALENDARIO_FERIADOS
                    WHERE fecha = @FechaProceso AND es_feriado = 1);
*/

-- ============================================================
-- NOTA DE POLITICA (revisar con RRHH):
-- minutos_extra se calcula como tiempo trabajado MAS ALLA de la hora
-- teorica del horario elegido (paso E estandar). Si para nomina el dia
-- de descanso laborado debe pagarse COMPLETO como extra/recargo, eso se
-- resuelve en el calculo de planilla usando el estado 'DESCANSO_LABORADO'
-- como marca; el SP solo registra horas reales + el estado.
-- ============================================================

-- ============================================================
-- PASO 3: Verificacion (despues de aplicar y correr un cierre)
-- ============================================================
-- Reprocesar un dia puntual:
--   EXEC dbo.SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA @FechaProceso = '2026-06-09';
--
-- Ver descansos laborados de una fecha:
--   SELECT id_trabajador, fecha_asistencia, hora_entrada_real, hora_salida_real,
--          minutos_tardanza, minutos_extra, estado_asistencia
--   FROM dbo.ASISTENCIA_RESUMEN_DIARIO
--   WHERE fecha_asistencia = '2026-06-09'
--     AND estado_asistencia = 'DESCANSO_LABORADO';
GO
