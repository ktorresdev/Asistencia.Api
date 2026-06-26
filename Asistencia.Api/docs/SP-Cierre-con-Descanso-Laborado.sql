USE [DB_RRHH]
GO
/****** SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA v7
        v7 = revierte el "recorrer horarios del turno" de v6. PASO A vuelve a
             ISNULL(PTS, horario de la asignacion). NO se recorren otros horarios:
             rotativo = depende del PTS; fijo = solo su horario asignado.
        v6 = (revertido) PASO A recorria todos los horarios del turno.
        v5 = v4 + DESCANSO_LABORADO (trabajar en dia de descanso, sin cambio de dia)
        Cambios v5:
          - Bloque B2: descanso laborado -> fija teoricas + estado DESCANSO_LABORADO
          - Paso  F2 : DESCANSO_LABORADO sin marcacion -> vuelve a DESCANSO
          - Paso  G  : agrega 'DESCANSO_LABORADO' al NOT IN (feriados)
        REQUISITO: columna PROGRAMACION_TURNOS_SEMANAL.es_descanso_laborado (PASO 1 del
                   archivo FEATURE-Descanso-Laborado.sql).
 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA]
    @FechaProceso DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @FechaProceso IS NULL
        SET @FechaProceso = DATEADD(DAY, -1, CAST(GETDATE() AS DATE));

    DECLARE @DiaSemana VARCHAR(15) = UPPER(CASE DATEPART(WEEKDAY, @FechaProceso)
        WHEN 1 THEN 'DOMINGO'  WHEN 2 THEN 'LUNES'
        WHEN 3 THEN 'MARTES'   WHEN 4 THEN 'MIERCOLES'
        WHEN 5 THEN 'JUEVES'   WHEN 6 THEN 'VIERNES'
        WHEN 7 THEN 'SABADO' END);

    PRINT '>> SP Cierre v7 - Fecha: ' + CAST(@FechaProceso AS VARCHAR) + ' (' + @DiaSemana + ')';

    -- Idempotente
    DELETE FROM dbo.ASISTENCIA_RESUMEN_DIARIO WHERE fecha_asistencia = @FechaProceso;

    -- == A: INSERT base ==========================================================
    INSERT INTO dbo.ASISTENCIA_RESUMEN_DIARIO
    (id_trabajador, fecha_asistencia, hora_entrada_teorica, hora_salida_teorica,
     hora_entrada_real, hora_salida_real, minutos_tardanza, minutos_extra,
     estado_asistencia, es_dia_boleta)
    SELECT
        T.id_trabajador, @FechaProceso,
        DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_inicio), CAST(@FechaProceso AS DATETIME)),
        CASE WHEN HD.salida_dia_siguiente = 1
             THEN DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_fin), CAST(DATEADD(DAY,1,@FechaProceso) AS DATETIME))
             ELSE DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_fin), CAST(@FechaProceso AS DATETIME))
        END,
        NULL, NULL, 0, 0, 'PENDIENTE', ISNULL(PTS.es_dia_boleta, 0)
    FROM dbo.TRABAJADORES T
    INNER JOIN dbo.ASIGNACIONES_TURNO AST
        ON  AST.id_trabajador = T.id_trabajador AND AST.es_vigente = 1
        AND @FechaProceso >= AST.fecha_inicio_vigencia
        AND (AST.fecha_fin_vigencia IS NULL OR @FechaProceso <= AST.fecha_fin_vigencia)
    INNER JOIN dbo.HORARIOS_TURNO HT
        -- Horario del dia = lo programado en PTS (rotativos y fijos programados);
        -- si no hay PTS, el horario de la ASIGNACION (fijos = su horario asignado).
        -- NO se recorren otros horarios del turno.
        ON HT.id_horario_turno = ISNULL(
            (SELECT TOP 1 pts2.id_horario_turno
             FROM dbo.PROGRAMACION_TURNOS_SEMANAL pts2
             WHERE pts2.id_trabajador = T.id_trabajador
               AND pts2.fecha         = @FechaProceso
               AND pts2.es_descanso   = 0
               AND pts2.es_vacaciones = 0
               AND pts2.tipo_ausencia IS NULL),
            AST.id_horario_turno)
        AND HT.es_activo = 1
    INNER JOIN dbo.HORARIOS_DETALLE HD
        ON HD.id_horario_turno = HT.id_horario_turno
        AND HD.dia_semana = @DiaSemana
    LEFT JOIN dbo.PROGRAMACION_TURNOS_SEMANAL PTS
        ON PTS.id_trabajador = T.id_trabajador AND PTS.fecha = @FechaProceso
    WHERE T.id_estado = 10;

    PRINT '>> Filas base: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- == A2: Ausencias sin horario (ROT con tipo_ausencia, sin HT en PTS) =========
    INSERT INTO dbo.ASISTENCIA_RESUMEN_DIARIO
    (id_trabajador, fecha_asistencia, hora_entrada_teorica, hora_salida_teorica,
     hora_entrada_real, hora_salida_real, minutos_tardanza, minutos_extra,
     estado_asistencia, es_dia_boleta)
    SELECT T.id_trabajador, @FechaProceso,
        NULL, NULL, NULL, NULL, 0, 0,
        UPPER(PTS.tipo_ausencia), 0
    FROM dbo.TRABAJADORES T
    INNER JOIN dbo.PROGRAMACION_TURNOS_SEMANAL PTS
        ON  PTS.id_trabajador = T.id_trabajador
        AND PTS.fecha         = @FechaProceso
        AND PTS.tipo_ausencia IS NOT NULL
    WHERE T.id_estado = 10
      AND NOT EXISTS (
          SELECT 1 FROM dbo.ASISTENCIA_RESUMEN_DIARIO ARD
          WHERE ARD.id_trabajador    = T.id_trabajador
            AND ARD.fecha_asistencia = @FechaProceso);

    PRINT '>> Filas ausencias especiales: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- == B: Descansos y vacaciones desde PTS (booleans legacy) ===================
    UPDATE dbo.ASISTENCIA_RESUMEN_DIARIO SET estado_asistencia = 'DESCANSO'
    WHERE fecha_asistencia = @FechaProceso
      AND id_trabajador IN (
          SELECT id_trabajador FROM dbo.PROGRAMACION_TURNOS_SEMANAL
          WHERE fecha = @FechaProceso AND es_descanso = 1);

    UPDATE dbo.ASISTENCIA_RESUMEN_DIARIO SET estado_asistencia = 'VACACIONES'
    WHERE fecha_asistencia = @FechaProceso
      AND id_trabajador IN (
          SELECT id_trabajador FROM dbo.PROGRAMACION_TURNOS_SEMANAL
          WHERE fecha = @FechaProceso
            AND (es_vacaciones = 1 OR tipo_ausencia = 'VACACIONES'));

    -- B3: Ausencias especiales para FIJ que tienen horario base
    UPDATE R SET R.estado_asistencia = UPPER(PTS.tipo_ausencia)
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    INNER JOIN dbo.PROGRAMACION_TURNOS_SEMANAL PTS
        ON  PTS.id_trabajador = R.id_trabajador
        AND PTS.fecha         = @FechaProceso
        AND PTS.tipo_ausencia IS NOT NULL
        AND PTS.tipo_ausencia <> 'VACACIONES'
    WHERE R.fecha_asistencia = @FechaProceso;

    -- == B2: DESCANSO LABORADO (NUEVO) ===========================================
    -- Trabaja en su dia de descanso, SIN cambio de dia. El dia sigue con
    -- es_descanso=1, pero es_descanso_laborado=1 indica que se trabaja.
    -- Teoricas segun el horario elegido (PTS.id_horario_turno) o, si es NULL, el de
    -- la asignacion vigente. Se deja en DESCANSO_LABORADO para que D y E lo procesen.

    -- B2.1  Crear fila si no existe (descanso cuyo dia no tiene HorarioDetalle base)
    INSERT INTO dbo.ASISTENCIA_RESUMEN_DIARIO
    (id_trabajador, fecha_asistencia, hora_entrada_teorica, hora_salida_teorica,
     hora_entrada_real, hora_salida_real, minutos_tardanza, minutos_extra,
     estado_asistencia, es_dia_boleta)
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
                (SELECT TOP 1 ast.id_horario_turno FROM dbo.ASIGNACIONES_TURNO ast
                  WHERE ast.id_trabajador = T.id_trabajador AND ast.es_vigente = 1
                    AND @FechaProceso >= ast.fecha_inicio_vigencia
                    AND (ast.fecha_fin_vigencia IS NULL OR @FechaProceso <= ast.fecha_fin_vigencia)))
        AND HD.dia_semana = @DiaSemana
    WHERE T.id_estado = 10
      AND NOT EXISTS (SELECT 1 FROM dbo.ASISTENCIA_RESUMEN_DIARIO ARD
                      WHERE ARD.id_trabajador = T.id_trabajador AND ARD.fecha_asistencia = @FechaProceso);

    -- B2.2  Para los que ya tienen fila (paso A + B la dejaron en DESCANSO)
    UPDATE R SET
        R.hora_entrada_teorica = DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_inicio), CAST(@FechaProceso AS DATETIME)),
        R.hora_salida_teorica  = CASE WHEN HD.salida_dia_siguiente = 1
            THEN DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_fin), CAST(DATEADD(DAY,1,@FechaProceso) AS DATETIME))
            ELSE DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD.hora_fin), CAST(@FechaProceso AS DATETIME)) END,
        R.estado_asistencia = 'DESCANSO_LABORADO'
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    INNER JOIN dbo.PROGRAMACION_TURNOS_SEMANAL PTS
        ON  PTS.id_trabajador = R.id_trabajador
        AND PTS.fecha         = @FechaProceso
        AND PTS.es_descanso   = 1
        AND ISNULL(PTS.es_descanso_laborado, 0) = 1
    INNER JOIN dbo.HORARIOS_DETALLE HD
        ON  HD.id_horario_turno = ISNULL(
                PTS.id_horario_turno,
                (SELECT TOP 1 ast.id_horario_turno FROM dbo.ASIGNACIONES_TURNO ast
                  WHERE ast.id_trabajador = R.id_trabajador AND ast.es_vigente = 1
                    AND @FechaProceso >= ast.fecha_inicio_vigencia
                    AND (ast.fecha_fin_vigencia IS NULL OR @FechaProceso <= ast.fecha_fin_vigencia)))
        AND HD.dia_semana = @DiaSemana
    WHERE R.fecha_asistencia  = @FechaProceso
      AND R.estado_asistencia = 'DESCANSO';

    -- == C: Coberturas aprobadas =================================================
    UPDATE R SET R.estado_asistencia = 'FALTA_CUBIERTA', R.id_cobertura_origen = CT.id_cobertura
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    INNER JOIN dbo.COBERTURA_TURNOS CT
        ON CT.id_trabajador_ausente = R.id_trabajador AND CT.fecha = @FechaProceso
        AND CT.estado = 'APROBADO' AND CT.tipo_cobertura = 'COBERTURA'
    WHERE R.fecha_asistencia = @FechaProceso;

    UPDATE R SET R.estado_asistencia = 'COBERTURA_EXTRA', R.id_cobertura_origen = CT.id_cobertura
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    INNER JOIN dbo.COBERTURA_TURNOS CT
        ON CT.id_trabajador_cubre = R.id_trabajador AND CT.fecha = @FechaProceso
        AND CT.estado = 'APROBADO' AND CT.tipo_cobertura = 'COBERTURA'
    WHERE R.fecha_asistencia = @FechaProceso;

    UPDATE R SET
        R.hora_entrada_teorica = DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD2.hora_inicio), CAST(@FechaProceso AS DATETIME)),
        R.hora_salida_teorica  = CASE WHEN HD2.salida_dia_siguiente = 1
            THEN DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD2.hora_fin), CAST(DATEADD(DAY,1,@FechaProceso) AS DATETIME))
            ELSE DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00', HD2.hora_fin), CAST(@FechaProceso AS DATETIME)) END,
        R.estado_asistencia = 'ASISTENCIA_SWAP', R.id_cobertura_origen = CT.id_cobertura
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    INNER JOIN dbo.COBERTURA_TURNOS CT
        ON CT.id_trabajador_cubre = R.id_trabajador AND CT.fecha = @FechaProceso
        AND CT.estado = 'APROBADO' AND CT.tipo_cobertura = 'CAMBIO'
    INNER JOIN dbo.HORARIOS_DETALLE HD2
        ON HD2.id_horario_turno = CT.id_horario_turno_original AND HD2.dia_semana = @DiaSemana
    WHERE R.fecha_asistencia = @FechaProceso;

    -- DESCANSO_COMPENSATORIO: ANTICIPO + COBERTURA con fecha devolucion
    UPDATE dbo.ASISTENCIA_RESUMEN_DIARIO SET estado_asistencia = 'DESCANSO_COMPENSATORIO'
    WHERE fecha_asistencia = @FechaProceso
      AND id_trabajador IN (
          SELECT id_trabajador_cubre FROM dbo.COBERTURA_TURNOS
          WHERE fecha_swap_devolucion = @FechaProceso
            AND tipo_cobertura IN ('ANTICIPO', 'COBERTURA')
            AND estado = 'APROBADO');

    -- == D: Marcaciones reales ===================================================
    UPDATE R SET R.hora_entrada_real = (
        SELECT TOP 1 M.fecha_hora FROM dbo.MARCACIONES_ASISTENCIA M
        WHERE M.id_trabajador = R.id_trabajador AND M.tipo_marcacion = 'ENTRADA'
          AND CAST(M.fecha_hora AS DATE) = @FechaProceso ORDER BY M.fecha_hora ASC)
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    WHERE R.fecha_asistencia = @FechaProceso
      AND R.estado_asistencia NOT IN (
          'DESCANSO','VACACIONES','FALTA_CUBIERTA','DESCANSO_COMPENSATORIO',
          'DESCANSO_MEDICO','MATERNIDAD','PATERNIDAD','PERMISO_SIN_GOCE');

    UPDATE R SET R.hora_salida_real = COALESCE(
        (SELECT TOP 1 M.fecha_hora FROM dbo.MARCACIONES_ASISTENCIA M
         WHERE M.id_trabajador = R.id_trabajador AND M.tipo_marcacion = 'SALIDA'
           AND CAST(M.fecha_hora AS DATE) = @FechaProceso ORDER BY M.fecha_hora DESC),
        (SELECT TOP 1 M.fecha_hora FROM dbo.MARCACIONES_ASISTENCIA M
         INNER JOIN dbo.PROGRAMACION_TURNOS_SEMANAL PTS2
             ON PTS2.id_trabajador = M.id_trabajador AND PTS2.fecha = @FechaProceso
         INNER JOIN dbo.HORARIOS_DETALLE HD3
             ON HD3.id_horario_turno = PTS2.id_horario_turno
             AND HD3.dia_semana = @DiaSemana AND HD3.salida_dia_siguiente = 1
         WHERE M.id_trabajador = R.id_trabajador AND M.tipo_marcacion = 'SALIDA'
           AND CAST(M.fecha_hora AS DATE) = DATEADD(DAY, 1, @FechaProceso)
           AND M.fecha_hora < DATEADD(SECOND, DATEDIFF(SECOND,'00:00:00','08:00:00'),
               CAST(DATEADD(DAY,1,@FechaProceso) AS DATETIME))
         ORDER BY M.fecha_hora DESC))
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    WHERE R.fecha_asistencia = @FechaProceso
      AND R.estado_asistencia NOT IN (
          'DESCANSO','VACACIONES','FALTA_CUBIERTA','DESCANSO_COMPENSATORIO',
          'DESCANSO_MEDICO','MATERNIDAD','PATERNIDAD','PERMISO_SIN_GOCE');

    -- == E: Tardanza y horas extra (con tolerancia_ingreso del turno) =============
    UPDATE R
    SET
        minutos_tardanza = CASE
            WHEN R.hora_entrada_real > DATEADD(MINUTE, ISNULL(TU.tolerancia_ingreso, 0), R.hora_entrada_teorica)
            THEN DATEDIFF(MINUTE, R.hora_entrada_teorica, R.hora_entrada_real)
            ELSE 0
        END,
        minutos_extra = CASE
            WHEN R.hora_salida_real IS NOT NULL AND R.hora_salida_real > R.hora_salida_teorica
            THEN DATEDIFF(MINUTE, R.hora_salida_teorica, R.hora_salida_real)
            ELSE 0
        END
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    INNER JOIN dbo.ASIGNACIONES_TURNO AST
        ON  AST.id_trabajador = R.id_trabajador
        AND AST.es_vigente    = 1
        AND @FechaProceso >= AST.fecha_inicio_vigencia
        AND (AST.fecha_fin_vigencia IS NULL OR @FechaProceso <= AST.fecha_fin_vigencia)
    INNER JOIN dbo.TURNOS TU
        ON TU.id_turno = AST.id_turno
    WHERE R.fecha_asistencia   = @FechaProceso
      AND R.hora_entrada_real IS NOT NULL;

    -- == F: Estado final (solo filas PENDIENTE) ==================================
    UPDATE dbo.ASISTENCIA_RESUMEN_DIARIO
    SET estado_asistencia = CASE
        WHEN hora_entrada_real IS NULL THEN 'FALTA'
        WHEN minutos_tardanza  > 0     THEN 'TARDANZA'
        ELSE 'ASISTENCIA' END
    WHERE fecha_asistencia  = @FechaProceso
      AND estado_asistencia = 'PENDIENTE';

    -- == F2: DESCANSO LABORADO sin marcacion -> vuelve a DESCANSO (NUEVO) =========
    -- Si el trabajador no marco, no trabajo realmente su descanso: queda como DESCANSO.
    UPDATE dbo.ASISTENCIA_RESUMEN_DIARIO SET estado_asistencia = 'DESCANSO'
    WHERE fecha_asistencia  = @FechaProceso
      AND estado_asistencia = 'DESCANSO_LABORADO'
      AND hora_entrada_real IS NULL;

    -- == G: Feriados - no sobreescribir ausencias programadas ====================
    UPDATE dbo.ASISTENCIA_RESUMEN_DIARIO SET estado_asistencia = 'FERIADO'
    WHERE fecha_asistencia = @FechaProceso
      AND estado_asistencia NOT IN (
          'VACACIONES','DESCANSO','DESCANSO_COMPENSATORIO','FALTA_CUBIERTA',
          'DESCANSO_MEDICO','MATERNIDAD','PATERNIDAD','PERMISO_SIN_GOCE',
          'DESCANSO_LABORADO')   -- NUEVO
      AND EXISTS (
          SELECT 1 FROM dbo.CALENDARIO_FERIADOS
          WHERE fecha = @FechaProceso AND es_feriado = 1);

    -- == H: Cerrar coberturas ====================================================
    UPDATE dbo.COBERTURA_TURNOS SET estado = 'EJECUTADO', updated_at = SYSUTCDATETIME()
    WHERE fecha = @FechaProceso AND estado = 'APROBADO';

    UPDATE dbo.COBERTURA_TURNOS SET estado = 'DEVUELTO', updated_at = SYSUTCDATETIME()
    WHERE fecha_swap_devolucion = @FechaProceso
      AND tipo_cobertura = 'ANTICIPO' AND estado = 'EJECUTADO';

    -- == Resumen final ===========================================================
    SELECT estado_asistencia, COUNT(*) AS n
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO
    WHERE fecha_asistencia = @FechaProceso
    GROUP BY estado_asistencia
    ORDER BY estado_asistencia;

    -- == Retroactivo: corregir ausencias en dias anteriores ======================
    UPDATE R
    SET R.estado_asistencia = UPPER(P.tipo_ausencia),
        R.minutos_tardanza  = 0
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    INNER JOIN dbo.PROGRAMACION_TURNOS_SEMANAL P
        ON  P.id_trabajador = R.id_trabajador
        AND P.fecha         = R.fecha_asistencia
    WHERE P.tipo_ausencia IS NOT NULL
      AND R.fecha_asistencia < @FechaProceso
      AND R.estado_asistencia NOT IN (
            'DESCANSO_MEDICO', 'MATERNIDAD', 'PATERNIDAD',
            'VACACIONES', 'PERMISO_SIN_GOCE');

    PRINT '>> Retroactivo corregido: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' dias';
END
GO
