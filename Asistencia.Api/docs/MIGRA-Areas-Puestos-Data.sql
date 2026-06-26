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
