USE [DB_RRHH]
GO
/****** SP_PROCESAR_CIERRE_DIARIO_ASISTENCIA v8  ── DOBLE TURNO
  ---------------------------------------------------------------------------
  v8 = soporte de DOBLE TURNO (1 fila por ASIGNACION vigente, no 1 por día).
       REQUISITO: FEATURE-Doble-Turno.sql (columna id_asignacion + UQ triple)
       ejecutado ANTES que este SP.

  Cambios respecto a v7:
    - PASO A : graba R.id_asignacion = AST.id_asignacion. Si el trabajador tiene
               >1 asignación vigente (doble turno) se IGNORA el PTS y se usa el
               horario de CADA asignación (AST.id_horario_turno). Con una sola
               asignación se mantiene la prioridad de PTS (rotativos).
    - PASO B2: descanso laborado también graba id_asignacion.
    - PASO D : MODELO DE PRESENCIA CONTINUA. Soporta doble turno CONTIGUO (08-16 y
               16-00, presencia continua con 2 marcas) y CON HUECO (marca cada turno).
               El límite teórico (carried-in/out) se aplica SOLO en la frontera entre
               dos turnos contiguos del mismo trabajador; en los bordes exteriores usa
               las marcas reales (no altera turno único). Resuelve también el cruce
               nocturno (la salida_teorica overnight ya cae al día siguiente).
    - PASO E : tardanza por R.id_asignacion (tolerancia del turno correcto).
               HORAS EXTRA: pendiente de definir → 0 para doble turno; turno único
               conserva el cálculo existente.

  Mantiene de v5/v7: descanso laborado, ausencias, coberturas, feriados, retroactivo.
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

    PRINT '>> SP Cierre v8 (doble turno) - Fecha: ' + CAST(@FechaProceso AS VARCHAR) + ' (' + @DiaSemana + ')';

    -- Idempotente
    DELETE FROM dbo.ASISTENCIA_RESUMEN_DIARIO WHERE fecha_asistencia = @FechaProceso;

    -- == A: INSERT base (1 fila por ASIGNACION vigente) ==========================
    INSERT INTO dbo.ASISTENCIA_RESUMEN_DIARIO
    (id_trabajador, id_asignacion, fecha_asistencia, hora_entrada_teorica, hora_salida_teorica,
     hora_entrada_real, hora_salida_real, minutos_tardanza, minutos_extra,
     estado_asistencia, es_dia_boleta)
    SELECT
        T.id_trabajador, AST.id_asignacion, @FechaProceso,
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
    -- ¿cuántas asignaciones vigentes tiene hoy? (para decidir si manda el PTS)
    CROSS APPLY (
        SELECT COUNT(*) AS n_vigentes
        FROM dbo.ASIGNACIONES_TURNO v
        WHERE v.id_trabajador = T.id_trabajador AND v.es_vigente = 1
          AND @FechaProceso >= v.fecha_inicio_vigencia
          AND (v.fecha_fin_vigencia IS NULL OR @FechaProceso <= v.fecha_fin_vigencia)
    ) CNT
    INNER JOIN dbo.HORARIOS_TURNO HT
        -- 1 sola asignación  -> horario del PTS del día y, si no hay, el de la asignación.
        -- 2+ asignaciones     -> el horario de CADA asignación (doble turno, sin PTS).
        ON HT.id_horario_turno = CASE
             WHEN CNT.n_vigentes > 1 THEN AST.id_horario_turno
             ELSE ISNULL(
                    (SELECT TOP 1 pts2.id_horario_turno
                     FROM dbo.PROGRAMACION_TURNOS_SEMANAL pts2
                     WHERE pts2.id_trabajador = T.id_trabajador
                       AND pts2.fecha         = @FechaProceso
                       AND pts2.es_descanso   = 0
                       AND pts2.es_vacaciones = 0
                       AND pts2.tipo_ausencia IS NULL),
                    AST.id_horario_turno)
           END
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
    (id_trabajador, id_asignacion, fecha_asistencia, hora_entrada_teorica, hora_salida_teorica,
     hora_entrada_real, hora_salida_real, minutos_tardanza, minutos_extra,
     estado_asistencia, es_dia_boleta)
    SELECT T.id_trabajador, NULL, @FechaProceso,
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

    -- == B: Descansos y vacaciones desde PTS (aplican a TODAS las filas del día) ==
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

    -- == B2: DESCANSO LABORADO (graba también id_asignacion) =====================
    -- B2.1  Crear fila si no existe
    INSERT INTO dbo.ASISTENCIA_RESUMEN_DIARIO
    (id_trabajador, id_asignacion, fecha_asistencia, hora_entrada_teorica, hora_salida_teorica,
     hora_entrada_real, hora_salida_real, minutos_tardanza, minutos_extra,
     estado_asistencia, es_dia_boleta)
    SELECT
        T.id_trabajador, AV.id_asignacion, @FechaProceso,
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
    OUTER APPLY (
        SELECT TOP 1 ast.id_asignacion, ast.id_horario_turno
        FROM dbo.ASIGNACIONES_TURNO ast
        WHERE ast.id_trabajador = T.id_trabajador AND ast.es_vigente = 1
          AND @FechaProceso >= ast.fecha_inicio_vigencia
          AND (ast.fecha_fin_vigencia IS NULL OR @FechaProceso <= ast.fecha_fin_vigencia)
    ) AV
    INNER JOIN dbo.HORARIOS_DETALLE HD
        ON  HD.id_horario_turno = ISNULL(PTS.id_horario_turno, AV.id_horario_turno)
        AND HD.dia_semana = @DiaSemana
    WHERE T.id_estado = 10
      AND NOT EXISTS (SELECT 1 FROM dbo.ASISTENCIA_RESUMEN_DIARIO ARD
                      WHERE ARD.id_trabajador = T.id_trabajador AND ARD.fecha_asistencia = @FechaProceso);

    -- B2.2  Para los que ya tienen fila (paso A + B la dejaron en DESCANSO)
    UPDATE R SET
        R.id_asignacion = ISNULL(R.id_asignacion, AV.id_asignacion),
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
    OUTER APPLY (
        SELECT TOP 1 ast.id_asignacion, ast.id_horario_turno
        FROM dbo.ASIGNACIONES_TURNO ast
        WHERE ast.id_trabajador = R.id_trabajador AND ast.es_vigente = 1
          AND @FechaProceso >= ast.fecha_inicio_vigencia
          AND (ast.fecha_fin_vigencia IS NULL OR @FechaProceso <= ast.fecha_fin_vigencia)
    ) AV
    INNER JOIN dbo.HORARIOS_DETALLE HD
        ON  HD.id_horario_turno = ISNULL(PTS.id_horario_turno, AV.id_horario_turno)
        AND HD.dia_semana = @DiaSemana
    WHERE R.fecha_asistencia  = @FechaProceso
      AND R.estado_asistencia = 'DESCANSO';

    -- == C: Coberturas aprobadas (por trabajador/día) ============================
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

    -- DESCANSO_COMPENSATORIO
    UPDATE dbo.ASISTENCIA_RESUMEN_DIARIO SET estado_asistencia = 'DESCANSO_COMPENSATORIO'
    WHERE fecha_asistencia = @FechaProceso
      AND id_trabajador IN (
          SELECT id_trabajador_cubre FROM dbo.COBERTURA_TURNOS
          WHERE fecha_swap_devolucion = @FechaProceso
            AND tipo_cobertura IN ('ANTICIPO', 'COBERTURA')
            AND estado = 'APROBADO');

    -- == D: Marcaciones reales — MODELO DE PRESENCIA CONTINUA ====================
    -- Atribuye marcas a cada fila/turno soportando AMBOS casos:
    --   • CON HUECO  : el trabajador se va y vuelve → marca cada turno por separado.
    --   • CONTIGUOS  : ej. 08-16 y 16-00, presencia continua con solo 2 marcas.
    -- El "límite teórico" (carried-in / carried-out) se aplica SOLO en la frontera
    -- entre dos turnos contiguos del MISMO trabajador (filas hermanas cuya
    -- salida_teorica = entrada_teorica de la otra). En los bordes exteriores se usan
    -- las marcas reales → NO altera el comportamiento de turno único.
    --   entrada: si hay turno contiguo ANTES y seguía DENTRO justo antes del inicio
    --            (última marca = ENTRADA) ⇒ entrada = inicio (límite, sin tardanza);
    --            si no, su ENTRADA real del turno.
    --   salida : si hay turno contiguo DESPUÉS y seguía DENTRO al llegar al fin
    --            ⇒ salida = fin (límite); si no, su SALIDA real del turno.
    DECLARE @diaIni DATETIME = CAST(@FechaProceso AS DATETIME);

    UPDATE R SET R.hora_entrada_real = X.entrada
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    CROSS APPLY (
        SELECT entrada = CASE
            WHEN EXISTS (SELECT 1 FROM dbo.ASISTENCIA_RESUMEN_DIARIO R2
                         WHERE R2.id_trabajador = R.id_trabajador
                           AND R2.fecha_asistencia = @FechaProceso
                           AND R2.id_resumen <> R.id_resumen
                           AND R2.hora_salida_teorica = R.hora_entrada_teorica)
                 AND (SELECT TOP 1 m.tipo_marcacion FROM dbo.MARCACIONES_ASISTENCIA m
                      WHERE m.id_trabajador = R.id_trabajador
                        AND m.fecha_hora >= @diaIni
                        AND m.fecha_hora <  R.hora_entrada_teorica
                      ORDER BY m.fecha_hora DESC) = 'ENTRADA'
                 THEN R.hora_entrada_teorica          -- carried-in (turno contiguo previo)
            ELSE (SELECT TOP 1 m.fecha_hora FROM dbo.MARCACIONES_ASISTENCIA m
                  WHERE m.id_trabajador = R.id_trabajador
                    AND m.tipo_marcacion = 'ENTRADA'
                    AND m.fecha_hora >= DATEADD(HOUR,-2,R.hora_entrada_teorica)
                    AND m.fecha_hora <  R.hora_salida_teorica
                  ORDER BY m.fecha_hora ASC)
        END
    ) X
    WHERE R.fecha_asistencia = @FechaProceso
      AND R.hora_entrada_teorica IS NOT NULL
      AND R.estado_asistencia NOT IN (
          'DESCANSO','VACACIONES','FALTA_CUBIERTA','DESCANSO_COMPENSATORIO',
          'DESCANSO_MEDICO','MATERNIDAD','PATERNIDAD','PERMISO_SIN_GOCE');

    UPDATE R SET R.hora_salida_real = X.salida
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    CROSS APPLY (
        SELECT salida = CASE
            WHEN EXISTS (SELECT 1 FROM dbo.ASISTENCIA_RESUMEN_DIARIO R2
                         WHERE R2.id_trabajador = R.id_trabajador
                           AND R2.fecha_asistencia = @FechaProceso
                           AND R2.id_resumen <> R.id_resumen
                           AND R2.hora_entrada_teorica = R.hora_salida_teorica)
                 AND (SELECT TOP 1 m.tipo_marcacion FROM dbo.MARCACIONES_ASISTENCIA m
                      WHERE m.id_trabajador = R.id_trabajador
                        AND m.fecha_hora >= @diaIni
                        AND m.fecha_hora <= R.hora_salida_teorica
                      ORDER BY m.fecha_hora DESC) = 'ENTRADA'
                 THEN R.hora_salida_teorica           -- carried-out (turno contiguo siguiente)
            ELSE (SELECT TOP 1 m.fecha_hora FROM dbo.MARCACIONES_ASISTENCIA m
                  WHERE m.id_trabajador = R.id_trabajador
                    AND m.tipo_marcacion = 'SALIDA'
                    AND m.fecha_hora >= R.hora_entrada_teorica
                    AND m.fecha_hora <= DATEADD(HOUR,2,R.hora_salida_teorica)
                  ORDER BY m.fecha_hora DESC)
        END
    ) X
    WHERE R.fecha_asistencia = @FechaProceso
      AND R.hora_entrada_teorica IS NOT NULL
      AND R.estado_asistencia NOT IN (
          'DESCANSO','VACACIONES','FALTA_CUBIERTA','DESCANSO_COMPENSATORIO',
          'DESCANSO_MEDICO','MATERNIDAD','PATERNIDAD','PERMISO_SIN_GOCE');

    -- == E: Tardanza (por ASIGNACION → tolerancia del turno correcto) ============
    -- HORAS EXTRA: PENDIENTE DE DEFINIR. Para trabajadores con doble turno se deja
    -- minutos_extra = 0 (no se calcula hasta definir la regla). Turno único conserva
    -- el cálculo existente.
    UPDATE R
    SET
        minutos_tardanza = CASE
            WHEN R.hora_entrada_real > DATEADD(MINUTE, ISNULL(TU.tolerancia_ingreso, 0), R.hora_entrada_teorica)
            THEN DATEDIFF(MINUTE, R.hora_entrada_teorica, R.hora_entrada_real)
            ELSE 0
        END,
        minutos_extra = CASE
            WHEN (SELECT COUNT(*) FROM dbo.ASISTENCIA_RESUMEN_DIARIO R2
                  WHERE R2.id_trabajador = R.id_trabajador
                    AND R2.fecha_asistencia = @FechaProceso) > 1
                 THEN 0   -- doble turno: horas extra pendientes de definir
            WHEN R.hora_salida_real IS NOT NULL AND R.hora_salida_real > R.hora_salida_teorica
                 THEN DATEDIFF(MINUTE, R.hora_salida_teorica, R.hora_salida_real)
            ELSE 0
        END
    FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
    INNER JOIN dbo.ASIGNACIONES_TURNO AST
        ON AST.id_asignacion = R.id_asignacion
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

    -- == F2: DESCANSO LABORADO sin marcación -> vuelve a DESCANSO =================
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
          'DESCANSO_LABORADO')
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

    -- == Retroactivo: corregir ausencias en días anteriores ======================
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
