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
