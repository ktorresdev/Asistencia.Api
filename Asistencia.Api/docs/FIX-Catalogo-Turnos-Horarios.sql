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
