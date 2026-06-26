/******************************************************************************
  DEPLOY PRODUCCION - Doble Turno + Areas/Puestos + Descanso Laborado
  ---------------------------------------------------------------------------
  Servidor destino: 10.1.2.4  /  BD: DB_RRHH
  Ejecuta en ESTE ORDEN los 7 scripts pendientes (validados contra un backup
  de produccion el 2026-06-12). Correr ANTES de desplegar el backend nuevo.

  >>> ANTES DE EJECUTAR:
      1. BACKUP de DB_RRHH (este script modifica datos: areas/puestos y catalogo).
      2. Correr desde una conexion ESTABLE a 10.1.2.4 (SSMS), de un solo tiro.
      3. Revisar los PRINT y los SELECT de verificacion de cada bloque.

  Cada bloque es idempotente o transaccional. Si algo falla, restaurar el backup.
  Orden:
    1) FIX-Areas-Puestos            (tabla PUESTOS + id_puesto + FKs)
    2) MIGRA-Areas-Puestos-Data     (puebla areas/puestos)  [COMMIT interno]
    3) FEATURE-Descanso-Laborado    (columna es_descanso_laborado)
    4) FEATURE-Doble-Turno          (id_asignacion + UQ nueva)
    5) SP-Cierre-DobleTurno-v8      (SP cierre v8)
    6) FIX-Indices-Rendimiento      (indices)
    7) FIX-Catalogo-Turnos-Horarios (limpieza catalogo)  [modifica datos]
******************************************************************************/
GO

/*============================================================================
  PASO 1 / 7 : FIX-Areas-Puestos.sql
============================================================================*/
PRINT '>>>>>> PASO 1/7: FIX-Areas-Puestos';
GO
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

GO

/*============================================================================
  PASO 2 / 7 : MIGRA-Areas-Puestos-Data.sql
============================================================================*/
PRINT '>>>>>> PASO 2/7: MIGRA-Areas-Puestos-Data';
GO
-- ============================================================
-- Migracion de datos: poblar AREAS y PUESTOS desde los textos
-- existentes en TRABAJADORES (area_departamento y cargo) y
-- asignar los ids (id_area / id_puesto).
--
-- Requisito previo: ejecutar antes FIX-Areas-Puestos.sql
-- (crea la tabla PUESTOS y la columna TRABAJADORES.id_puesto).
--
-- Idempotente: se puede ejecutar varias veces sin duplicar.
-- ============================================================

USE [DB_RRHH];
GO

-- ------------------------------------------------------------
-- PASO 0 (PREVIEW): revisar los valores distintos antes de migrar.
-- Ejecuta solo estos SELECT para detectar duplicados por
-- mayusculas / espacios / tildes.
-- ------------------------------------------------------------
-- SELECT DISTINCT LTRIM(RTRIM(area_departamento)) AS area
-- FROM dbo.TRABAJADORES
-- WHERE area_departamento IS NOT NULL AND LTRIM(RTRIM(area_departamento)) <> ''
-- ORDER BY area;

-- SELECT DISTINCT LTRIM(RTRIM(cargo)) AS puesto
-- FROM dbo.TRABAJADORES
-- WHERE cargo IS NOT NULL AND LTRIM(RTRIM(cargo)) <> ''
-- ORDER BY puesto;

-- ------------------------------------------------------------
-- MIGRACION (envuelta en transaccion: COMMIT si todo se ve bien,
-- ROLLBACK si algo sale mal).
-- ------------------------------------------------------------
BEGIN TRANSACTION;

-- 1) AREAS: insertar las areas distintas que aun no existan
INSERT INTO dbo.AREAS (nombre_area, es_activo)
SELECT DISTINCT LTRIM(RTRIM(t.area_departamento)), 1
FROM dbo.TRABAJADORES t
WHERE t.area_departamento IS NOT NULL
  AND LTRIM(RTRIM(t.area_departamento)) <> ''
  AND NOT EXISTS (SELECT 1 FROM dbo.AREAS a
                  WHERE a.nombre_area = LTRIM(RTRIM(t.area_departamento)));

-- 2) PUESTOS: insertar los cargos distintos que aun no existan
INSERT INTO dbo.PUESTOS (nombre_puesto, es_activo)
SELECT DISTINCT LTRIM(RTRIM(t.cargo)), 1
FROM dbo.TRABAJADORES t
WHERE t.cargo IS NOT NULL
  AND LTRIM(RTRIM(t.cargo)) <> ''
  AND NOT EXISTS (SELECT 1 FROM dbo.PUESTOS p
                  WHERE p.nombre_puesto = LTRIM(RTRIM(t.cargo)));

-- 3) Asignar id_area en TRABAJADORES segun el nombre del area
UPDATE t
SET t.id_area = a.id_area
FROM dbo.TRABAJADORES t
INNER JOIN dbo.AREAS a ON a.nombre_area = LTRIM(RTRIM(t.area_departamento))
WHERE t.area_departamento IS NOT NULL AND LTRIM(RTRIM(t.area_departamento)) <> '';

-- 4) Asignar id_puesto en TRABAJADORES segun el nombre del cargo
UPDATE t
SET t.id_puesto = p.id_puesto
FROM dbo.TRABAJADORES t
INNER JOIN dbo.PUESTOS p ON p.nombre_puesto = LTRIM(RTRIM(t.cargo))
WHERE t.cargo IS NOT NULL AND LTRIM(RTRIM(t.cargo)) <> '';

-- 5) (OPCIONAL) Vincular cada PUESTO con su area mas frecuente.
--    Si un mismo cargo existe en varias areas, igual queda bien
--    porque TRABAJADORES.id_area manda en cada trabajador.
UPDATE p
SET p.id_area = x.id_area
FROM dbo.PUESTOS p
CROSS APPLY (
    SELECT TOP 1 t.id_area
    FROM dbo.TRABAJADORES t
    WHERE t.id_puesto = p.id_puesto AND t.id_area IS NOT NULL
    GROUP BY t.id_area
    ORDER BY COUNT(*) DESC
) x
WHERE p.id_area IS NULL;

COMMIT;   -- cambiar por ROLLBACK si la verificacion sale mal
GO

-- ------------------------------------------------------------
-- VERIFICACION: ambos contadores deben dar 0.
-- Si quedan registros sin asignar suele ser por tildes o
-- variantes de escritura; corrige el texto y re-ejecuta 3-4.
-- ------------------------------------------------------------
SELECT
  SUM(CASE WHEN area_departamento IS NOT NULL AND LTRIM(RTRIM(area_departamento)) <> '' AND id_area   IS NULL THEN 1 ELSE 0 END) AS area_sin_asignar,
  SUM(CASE WHEN cargo             IS NOT NULL AND LTRIM(RTRIM(cargo))             <> '' AND id_puesto IS NULL THEN 1 ELSE 0 END) AS puesto_sin_asignar
FROM dbo.TRABAJADORES;
GO

GO

/*============================================================================
  PASO 3 / 7 : FEATURE-Descanso-Laborado.sql
============================================================================*/
PRINT '>>>>>> PASO 3/7: FEATURE-Descanso-Laborado';
GO
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

GO

/*============================================================================
  PASO 4 / 7 : FEATURE-Doble-Turno.sql
============================================================================*/
PRINT '>>>>>> PASO 4/7: FEATURE-Doble-Turno';
GO
USE [DB_RRHH]
GO
/******************************************************************************
  FEATURE: DOBLE TURNO  ─  Fase 1 (esquema)
  ---------------------------------------------------------------------------
  Permite que un trabajador tenga 2 (o más) ASIGNACIONES_TURNO vigentes a la
  vez y que el cierre diario genere UNA FILA POR ASIGNACION en
  ASISTENCIA_RESUMEN_DIARIO, en lugar de abortar por la UQ (id_trabajador,fecha).

  Cambios:
    1. Columna ASISTENCIA_RESUMEN_DIARIO.id_asignacion  (FK a ASIGNACIONES_TURNO)
    2. Backfill de filas históricas (1 asignación vigente por fecha)
    3. FK id_asignacion -> ASIGNACIONES_TURNO(id_asignacion)
    4. Reemplazo de la UQ (id_trabajador, fecha_asistencia)
                  por la UQ (id_trabajador, fecha_asistencia, id_asignacion)

  Idempotente: se puede correr varias veces. Requiere el SP de cierre v8
  (SP-Cierre-con-Descanso-Laborado.sql) para poblar la nueva columna en
  adelante. Correr ESTE script ANTES de desplegar el backend que mapea la
  columna y ANTES del SP v8.
******************************************************************************/
SET NOCOUNT ON;
GO

-- == 1. Columna id_asignacion ===============================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ASISTENCIA_RESUMEN_DIARIO')
      AND name = 'id_asignacion')
BEGIN
    ALTER TABLE dbo.ASISTENCIA_RESUMEN_DIARIO ADD id_asignacion INT NULL;
    PRINT '>> Columna id_asignacion agregada.';
END
ELSE
    PRINT '>> Columna id_asignacion ya existe (skip).';
GO

-- == 2. Backfill =============================================================
-- A cada fila histórica se le asigna la ASIGNACION_TURNO vigente del trabajador
-- en esa fecha. Si hubiera más de una vigente (no debería en datos previos a
-- esta feature) se toma la de fecha_inicio_vigencia más reciente.
UPDATE R
SET R.id_asignacion = X.id_asignacion
FROM dbo.ASISTENCIA_RESUMEN_DIARIO R
CROSS APPLY (
    SELECT TOP 1 AST.id_asignacion
    FROM dbo.ASIGNACIONES_TURNO AST
    WHERE AST.id_trabajador = R.id_trabajador
      AND R.fecha_asistencia >= AST.fecha_inicio_vigencia
      AND (AST.fecha_fin_vigencia IS NULL OR R.fecha_asistencia <= AST.fecha_fin_vigencia)
    ORDER BY AST.es_vigente DESC, AST.fecha_inicio_vigencia DESC
) X
WHERE R.id_asignacion IS NULL;

PRINT '>> Backfill id_asignacion: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' filas.';
GO

-- == 3. FK id_asignacion -> ASIGNACIONES_TURNO ==============================
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ARD_Asignacion')
BEGIN
    ALTER TABLE dbo.ASISTENCIA_RESUMEN_DIARIO WITH NOCHECK
        ADD CONSTRAINT FK_ARD_Asignacion
        FOREIGN KEY (id_asignacion) REFERENCES dbo.ASIGNACIONES_TURNO(id_asignacion);
    PRINT '>> FK_ARD_Asignacion creada.';
END
ELSE
    PRINT '>> FK_ARD_Asignacion ya existe (skip).';
GO

-- == 4. Reemplazo de la clave única =========================================
-- 4.1  Eliminar CUALQUIER índice/constraint único que esté EXACTAMENTE sobre
--      (id_trabajador, fecha_asistencia) -- es el creado por EF.
DECLARE @ixName SYSNAME, @sql NVARCHAR(MAX);

SELECT @ixName = i.name
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.ASISTENCIA_RESUMEN_DIARIO')
  AND i.is_unique = 1
  AND (
      SELECT COUNT(*) FROM sys.index_columns ic
      WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
  ) = 2
  AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c
                ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                AND c.name = 'id_trabajador')
  AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c
                ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                AND c.name = 'fecha_asistencia');

IF @ixName IS NOT NULL
BEGIN
    -- ¿es constraint (unique key) o índice?
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = @ixName)
        SET @sql = 'ALTER TABLE dbo.ASISTENCIA_RESUMEN_DIARIO DROP CONSTRAINT ' + QUOTENAME(@ixName) + ';';
    ELSE
        SET @sql = 'DROP INDEX ' + QUOTENAME(@ixName) + ' ON dbo.ASISTENCIA_RESUMEN_DIARIO;';
    EXEC sp_executesql @sql;
    PRINT '>> UQ vieja (id_trabajador, fecha_asistencia) eliminada: ' + @ixName;
END
ELSE
    PRINT '>> No se encontró UQ vieja de 2 columnas (probablemente ya migrada).';
GO

-- 4.2  Crear la UQ nueva de 3 columnas (incluye id_asignacion).
--      NOTA: en SQL Server un índice único trata los NULL como iguales, por lo
--      que solo se permite UNA fila con id_asignacion NULL por (trabajador,fecha)
--      -- correcto: una ausencia sin asignación es única en el día.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_ARD_Trab_Fecha_Asignacion'
      AND object_id = OBJECT_ID('dbo.ASISTENCIA_RESUMEN_DIARIO'))
BEGIN
    CREATE UNIQUE INDEX UQ_ARD_Trab_Fecha_Asignacion
        ON dbo.ASISTENCIA_RESUMEN_DIARIO (id_trabajador, fecha_asistencia, id_asignacion);
    PRINT '>> UQ nueva (id_trabajador, fecha_asistencia, id_asignacion) creada.';
END
ELSE
    PRINT '>> UQ nueva ya existe (skip).';
GO

-- == Verificación ===========================================================
PRINT '----------------------------------------------------------------';
PRINT 'Verificación:';
SELECT
    (SELECT COUNT(*) FROM sys.columns
       WHERE object_id = OBJECT_ID('dbo.ASISTENCIA_RESUMEN_DIARIO') AND name='id_asignacion') AS tiene_columna,
    (SELECT COUNT(*) FROM sys.foreign_keys WHERE name='FK_ARD_Asignacion')                     AS tiene_fk,
    (SELECT COUNT(*) FROM sys.indexes WHERE name='UQ_ARD_Trab_Fecha_Asignacion')               AS tiene_uq_nueva,
    (SELECT COUNT(*) FROM dbo.ASISTENCIA_RESUMEN_DIARIO WHERE id_asignacion IS NULL)            AS filas_sin_asignacion;
GO

GO

/*============================================================================
  PASO 5 / 7 : SP-Cierre-DobleTurno-v8.sql
============================================================================*/
PRINT '>>>>>> PASO 5/7: SP-Cierre-DobleTurno-v8';
GO
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

GO

/*============================================================================
  PASO 6 / 7 : FIX-Indices-Rendimiento.sql
============================================================================*/
PRINT '>>>>>> PASO 6/7: FIX-Indices-Rendimiento';
GO
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

GO

/*============================================================================
  PASO 7 / 7 : FIX-Catalogo-Turnos-Horarios.sql
============================================================================*/
PRINT '>>>>>> PASO 7/7: FIX-Catalogo-Turnos-Horarios';
GO
-- ============================================================
-- LIMPIEZA / ESTANDARIZACION DE CATALOGO: TURNOS Y HORARIOS
-- Servidor objetivo: el que tenga la BD DB_RRHH activa.
--
-- Hace (transaccional, revisar y luego COMMIT):
--   PASO 1: normaliza dia_semana a MAYUSCULA SIN TILDE (arregla el cierre del turno 22)
--   PASO 2: corrige salida_dia_siguiente segun las horas reales
--   PASO 3: consolida a TURNO ROTATIVO y elimina el tipo "HORARIO VARIABLE"
--   PASO 4: DESACTIVA (es_activo=0) turnos basura 23-26 y el horario 39 (NO se eliminan)
--   PASO 5: renombra nombre_horario al estandar  {DIAS} {HH:MM}-{HH:MM}[ (+1)]
--   VERIFICACION final
-- ============================================================
USE [DB_RRHH];
GO

BEGIN TRANSACTION;

-- ── PASO 1: normalizar dia_semana (MAYUS sin tilde) ─────────────
-- El SP de cierre compara dia_semana = 'MIERCOLES' (mayus, sin tilde).
-- Los horarios del turno 22 tenian 'Miércoles', 'Sábado' -> no hacian match.
UPDATE dbo.HORARIOS_DETALLE
SET dia_semana =
    CASE
        WHEN UPPER(dia_semana) LIKE 'LUN%' THEN 'LUNES'
        WHEN UPPER(dia_semana) LIKE 'MAR%' THEN 'MARTES'
        WHEN UPPER(dia_semana) LIKE 'MI%'  THEN 'MIERCOLES'
        WHEN UPPER(dia_semana) LIKE 'JUE%' THEN 'JUEVES'
        WHEN UPPER(dia_semana) LIKE 'VIE%' THEN 'VIERNES'
        WHEN UPPER(dia_semana) LIKE 'S%'   THEN 'SABADO'
        WHEN UPPER(dia_semana) LIKE 'DOM%' THEN 'DOMINGO'
        ELSE UPPER(dia_semana)
    END
WHERE dia_semana <> CASE
        WHEN UPPER(dia_semana) LIKE 'LUN%' THEN 'LUNES'
        WHEN UPPER(dia_semana) LIKE 'MAR%' THEN 'MARTES'
        WHEN UPPER(dia_semana) LIKE 'MI%'  THEN 'MIERCOLES'
        WHEN UPPER(dia_semana) LIKE 'JUE%' THEN 'JUEVES'
        WHEN UPPER(dia_semana) LIKE 'VIE%' THEN 'VIERNES'
        WHEN UPPER(dia_semana) LIKE 'S%'   THEN 'SABADO'
        WHEN UPPER(dia_semana) LIKE 'DOM%' THEN 'DOMINGO'
        ELSE UPPER(dia_semana)
    END;
PRINT '>> dia_semana normalizados: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- ── PASO 2: corregir salida_dia_siguiente ──────────────────────
-- Regla: es del dia siguiente solo si la hora_fin <= hora_inicio (cruza medianoche).
-- Corrige horario 40 (00:00-07:00, estaba en 1) y 42 (09:00-17:00, estaba en 1).
UPDATE dbo.HORARIOS_DETALLE
SET salida_dia_siguiente = CASE WHEN hora_fin <= hora_inicio THEN 1 ELSE 0 END
WHERE salida_dia_siguiente <> CASE WHEN hora_fin <= hora_inicio THEN 1 ELSE 0 END;
PRINT '>> salida_dia_siguiente corregidos: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- ── PASO 3: CONSOLIDAR a ROTATIVO (eliminar el tipo "HORARIO VARIABLE") ──────────
-- 1=TURNO FIJO, 2=TURNO ROTATIVO, 3=NO FISCALIZADO, 4=HORARIO VARIABLE
-- El sistema SOLO distingue por nombre que contenga "ROT": todo lo que se programa
-- dia a dia debe ser TURNO ROTATIVO. "HORARIO VARIABLE" se comportaba como FIJO (bug).
-- Por eso: cualquier turno con tipo VARIABLE pasa a ROTATIVO y se elimina el tipo 4.
-- (Turno 22 ya estaba como ROTATIVO; se asegura aqui de todos modos.)
UPDATE dbo.TURNOS SET id_tipo_turno = 2 WHERE id_tipo_turno = 4; -- VARIABLE -> ROTATIVO
UPDATE dbo.TURNOS SET id_tipo_turno = 2 WHERE id_turno = 22;      -- asegurar turno 22 = ROTATIVO

-- Eliminar el tipo HORARIO VARIABLE ya que no se usa (despues del UPDATE de arriba).
-- Si quedara referenciado, este DELETE no se ejecuta (guarda).
IF NOT EXISTS (SELECT 1 FROM dbo.TURNOS WHERE id_tipo_turno = 4)
    DELETE FROM dbo.TIPO_TURNO WHERE id_tipo_turno = 4;

-- ── PASO 4: DESACTIVAR basura (NO eliminar) ────────────────────
UPDATE dbo.TURNOS SET es_activo = 0 WHERE id_turno IN (23, 24, 25, 26); -- sin horarios ni uso
UPDATE dbo.HORARIOS_TURNO SET es_activo = 0 WHERE id_horario_turno = 39; -- VARIABLE sin detalle

-- ── PASO 5: renombrar horarios al estandar ─────────────────────
-- Fijos: {DIAS} {HH:MM}-{HH:MM}
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-S 07:00-16:00' WHERE id_horario_turno = 1;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 06:00-15:00' WHERE id_horario_turno = 2;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 07:00-16:30' WHERE id_horario_turno = 3;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SAB 07:00-12:00' WHERE id_horario_turno = 4;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 07:00-17:00' WHERE id_horario_turno = 5;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SAB 09:00-12:00' WHERE id_horario_turno = 6;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 07:00-17:30' WHERE id_horario_turno = 7;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-16:30' WHERE id_horario_turno = 8;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-13:00' WHERE id_horario_turno = 9;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-17:00' WHERE id_horario_turno = 10;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-17:00' WHERE id_horario_turno = 11;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-17:00' WHERE id_horario_turno = 12;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-13:30' WHERE id_horario_turno = 13;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-17:30' WHERE id_horario_turno = 14;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-12:00' WHERE id_horario_turno = 15;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-17:30' WHERE id_horario_turno = 16;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-13:00' WHERE id_horario_turno = 17;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-18:00' WHERE id_horario_turno = 18;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-11:00' WHERE id_horario_turno = 19;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-18:30' WHERE id_horario_turno = 20;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 09:00-19:30' WHERE id_horario_turno = 21;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'L-V 11:00-21:00' WHERE id_horario_turno = 22;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SAB 12:00-21:00' WHERE id_horario_turno = 23;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'M-V 11:00-21:00' WHERE id_horario_turno = 24;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-20:00' WHERE id_horario_turno = 25;
-- Rotativos / jornadas: {ETIQUETA} {HH:MM}-{HH:MM}[ (+1)]
-- NOTA: los nombres con Ñ/Ó usan NCHAR() para evitar corrupcion de encoding
-- (NCHAR(209)=Ñ, NCHAR(211)=Ó) si el script se ejecuta sin -f 65001.
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'MA'+NCHAR(209)+'ANA 06:00-14:00'  WHERE id_horario_turno = 26;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'TARDE 14:00-22:00'       WHERE id_horario_turno = 27;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'NOCHE 22:00-06:00 (+1)'  WHERE id_horario_turno = 28;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'MA'+NCHAR(209)+'ANA 07:00-19:00'  WHERE id_horario_turno = 29;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'NOCHE 19:00-07:00 (+1)'  WHERE id_horario_turno = 30;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'MA'+NCHAR(209)+'ANA 07:00-15:00'  WHERE id_horario_turno = 31;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'TARDE 15:00-23:00'       WHERE id_horario_turno = 32;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'NOCHE 23:00-07:00 (+1)'  WHERE id_horario_turno = 33;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'DIURNO 06:00-18:00'      WHERE id_horario_turno = 34;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'NOCTURNO 18:00-06:00 (+1)' WHERE id_horario_turno = 35;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'DIURNO 08:00-20:00'      WHERE id_horario_turno = 36;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'NOCTURNO 20:00-08:00 (+1)' WHERE id_horario_turno = 37;
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'SIN MARCACI'+NCHAR(211)+'N' WHERE id_horario_turno = 38;
-- Corregidos de nombres engañosos (turno 22 HORARIO VARIABLE):
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'NOCHE 00:00-07:00'       WHERE id_horario_turno = 40; -- era "NOCHE 12PM - 7AM"
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'NOCHE 17:00-01:00 (+1)'  WHERE id_horario_turno = 41; -- era "MADRUGADA 17:00 A 01:00 AM"
UPDATE dbo.HORARIOS_TURNO SET nombre_horario = 'MA'+NCHAR(209)+'ANA 09:00-17:00'  WHERE id_horario_turno = 42;

COMMIT;   -- cambiar por ROLLBACK si la verificacion sale mal
GO

-- ── VERIFICACION ───────────────────────────────────────────────
-- Dias deben quedar todos en MAYUS sin tilde:
SELECT DISTINCT dia_semana FROM dbo.HORARIOS_DETALLE ORDER BY dia_semana;

-- Horarios renombrados:
SELECT ht.id_horario_turno, t.nombre_codigo AS turno, ht.nombre_horario, ht.es_activo
FROM dbo.HORARIOS_TURNO ht JOIN dbo.TURNOS t ON t.id_turno = ht.id_turno
ORDER BY ht.id_turno, ht.id_horario_turno;

-- Turnos desactivados:
SELECT id_turno, nombre_codigo, es_activo FROM dbo.TURNOS WHERE es_activo = 0 ORDER BY id_turno;
GO

GO
PRINT '====== DEPLOY COMPLETO (7/7) ======';
GO
