-- ============================================================
-- FIX COMPLETO DE HORARIOS - SCRIPT UNICO
-- Fecha: 2026-05-15
-- Descripcion: Rellena horas, corrige errores y renombra
--              horarios en HORARIOS_TURNO y HORARIOS_DETALLE
-- ============================================================
-- INSTRUCCIONES:
--   1. Ejecutar TODO el archivo de una sola vez
--   2. Revisar los SELECTs de verificacion al final
--   3. Si todo esta bien -> descomentar COMMIT y ejecutar
--   4. Si algo esta mal  -> descomentar ROLLBACK y ejecutar
-- ============================================================

BEGIN TRANSACTION;


-- ============================================================
-- BLOQUE 1: RELLENAR hora_inicio Y hora_fin
-- (los valores estaban en NULL - se toman del nombre del turno)
-- ============================================================

-- Turno 1: L-S 07:00-16:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '07:00:00', hora_fin = '16:00:00' WHERE id IN (1,2,3,4,5,6);

-- Turno 2: L-V 06:00-15:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '06:00:00', hora_fin = '15:00:00' WHERE id IN (7,8,9,10,11);

-- Turno 3: L-V 07:00-16:30
UPDATE HORARIOS_DETALLE SET hora_inicio = '07:00:00', hora_fin = '16:30:00' WHERE id IN (12,13,14,15,16);
-- Turno 3: SAB 07:00-12:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '07:00:00', hora_fin = '12:00:00' WHERE id = 17;

-- Turno 4: L-V 07:00-17:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '07:00:00', hora_fin = '17:00:00' WHERE id IN (18,19,20,21,22);
-- Turno 4: SAB 09:00-12:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '09:00:00', hora_fin = '12:00:00' WHERE id = 23;

-- Turno 5: L-V 07:00-17:30
UPDATE HORARIOS_DETALLE SET hora_inicio = '07:00:00', hora_fin = '17:30:00' WHERE id IN (24,25,26,27,28);

-- Turno 6: L-V 08:00-16:30
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '16:30:00' WHERE id IN (29,30,31,32,33);
-- Turno 6: SAB 08:00-13:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '13:00:00' WHERE id = 34;

-- Turno 7: L-V 08:00-17:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '17:00:00' WHERE id IN (35,36,37,38,39);
-- Turno 7: SAB 08:00-17:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '17:00:00' WHERE id = 40;

-- Turno 8: L-V 08:00-17:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '17:00:00' WHERE id IN (41,42,43,44,45);
-- Turno 8: SAB 08:00-13:30
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '13:30:00' WHERE id = 46;

-- Turno 9: L-V 08:00-17:30
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '17:30:00' WHERE id IN (47,48,49,50,51);
-- Turno 9: SAB 08:00-12:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '12:00:00' WHERE id = 52;

-- Turno 10: L-V 08:00-17:30
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '17:30:00' WHERE id IN (53,54,55,56,57);
-- Turno 10: SAB 08:00-13:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '13:00:00' WHERE id = 58;

-- Turno 11: L-V 08:00-18:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '18:00:00' WHERE id IN (59,60,61,62,63);
-- Turno 11: SAB 08:00-11:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '11:00:00' WHERE id = 64;

-- Turno 12: L-V 08:00-18:30
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '18:30:00' WHERE id IN (65,66,67,68,69);

-- Turno 13: L-V 09:00-19:30
UPDATE HORARIOS_DETALLE SET hora_inicio = '09:00:00', hora_fin = '19:30:00' WHERE id IN (70,71,72,73,74);

-- Turno 14: L-V 11:00-21:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '11:00:00', hora_fin = '21:00:00' WHERE id IN (75,76,77,78,79);
-- Turno 14: SAB 12:00-21:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '12:00:00', hora_fin = '21:00:00' WHERE id = 80;

-- Turno 15: M-V 11:00-21:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '11:00:00', hora_fin = '21:00:00' WHERE id IN (81,82,83,84);
-- Turno 15: SAB 08:00-20:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '20:00:00' WHERE id = 85;

-- Turno 16 ROTATIVO M-T-N 06-14-22: MAÑANA
UPDATE HORARIOS_DETALLE SET hora_inicio = '06:00:00', hora_fin = '14:00:00' WHERE id IN (86,87,88,89,90,91,92);
-- Turno 16: TARDE
UPDATE HORARIOS_DETALLE SET hora_inicio = '14:00:00', hora_fin = '22:00:00' WHERE id IN (93,94,95,96,97,98,99);
-- Turno 16: NOCHE (cruza medianoche - salida_dia_siguiente ya es 1)
UPDATE HORARIOS_DETALLE SET hora_inicio = '22:00:00', hora_fin = '06:00:00' WHERE id IN (100,101,102,103,104,105,106);

-- Turno 17 ROTATIVO M-N 07-19: MAÑANA
UPDATE HORARIOS_DETALLE SET hora_inicio = '07:00:00', hora_fin = '19:00:00' WHERE id IN (107,108,109,110,111,112,113);
-- Turno 17: NOCHE (cruza medianoche - salida_dia_siguiente ya es 1)
UPDATE HORARIOS_DETALLE SET hora_inicio = '19:00:00', hora_fin = '07:00:00' WHERE id IN (114,115,116,117,118,119,120);

-- Turno 18 ROTATIVO M-T-N 07-15-23: MAÑANA
UPDATE HORARIOS_DETALLE SET hora_inicio = '07:00:00', hora_fin = '15:00:00' WHERE id IN (121,122,123,124,125,126,127);
-- Turno 18: TARDE
UPDATE HORARIOS_DETALLE SET hora_inicio = '15:00:00', hora_fin = '23:00:00' WHERE id IN (128,129,130,131,132,133,134);
-- Turno 18: NOCHE (cruza medianoche - salida_dia_siguiente ya es 1)
UPDATE HORARIOS_DETALLE SET hora_inicio = '23:00:00', hora_fin = '07:00:00' WHERE id IN (135,136,137,138,139,140,141);

-- Turno 19 NO FISCALIZADO 2x2 06-18: DIURNO
UPDATE HORARIOS_DETALLE SET hora_inicio = '06:00:00', hora_fin = '18:00:00' WHERE id IN (142,143,144,145,146,147,148);
-- Turno 19: NOCTURNO (cruza medianoche - salida_dia_siguiente ya es 1)
UPDATE HORARIOS_DETALLE SET hora_inicio = '18:00:00', hora_fin = '06:00:00' WHERE id IN (149,150,151,152,153,154,155);

-- Turno 20 NO FISCALIZADO 2x2 08-20: DIURNO
UPDATE HORARIOS_DETALLE SET hora_inicio = '08:00:00', hora_fin = '20:00:00' WHERE id IN (156,157,158,159,160,161,162);
-- Turno 20: NOCTURNO (cruza medianoche - salida_dia_siguiente ya es 1)
UPDATE HORARIOS_DETALLE SET hora_inicio = '20:00:00', hora_fin = '08:00:00' WHERE id IN (163,164,165,166,167,168,169);

-- Turno 22 HORARIO VARIABLE: NOCHE 12PM-7AM (cruza medianoche, hora_inicio era 00:00 - error)
UPDATE HORARIOS_DETALLE SET hora_inicio = '12:00:00', hora_fin = '07:00:00' WHERE id IN (170,171,172,173,174);

-- Turno 22: MADRUGADA 17:00-01:00 (cruza medianoche - salida_dia_siguiente ya es 1)
UPDATE HORARIOS_DETALLE SET hora_inicio = '17:00:00', hora_fin = '01:00:00' WHERE id IN (175,176,177,178,179,180);

-- Turno 22: MAÑANA 09:00-17:00 (salida_dia_siguiente estaba en 1 - error, no cruza medianoche)
UPDATE HORARIOS_DETALLE SET hora_inicio = '09:00:00', hora_fin = '17:00:00', salida_dia_siguiente = 0
WHERE id IN (181,182,183,184,185,186);

-- Turno 22: Tarde-Madrugada 19:00-07:00 (cruza medianoche - salida_dia_siguiente ya es 1)
UPDATE HORARIOS_DETALLE SET hora_inicio = '19:00:00', hora_fin = '07:00:00' WHERE id IN (187,188,189,190,191,192);

-- Turno 22: MAÑANA 05:00-13:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '05:00:00', hora_fin = '13:00:00' WHERE id IN (193,194,195,196,197,198);

-- Turno 22: TARDE 11:00-19:00
UPDATE HORARIOS_DETALLE SET hora_inicio = '11:00:00', hora_fin = '19:00:00' WHERE id IN (199,200,201,202,203,204);


-- ============================================================
-- BLOQUE 2: ELIMINAR DUPLICADO
-- ============================================================

-- Detalle 205: Sabado duplicado en turno 6 (correcto es detalle 34)
DELETE FROM HORARIOS_DETALLE WHERE id = 205;


-- ============================================================
-- BLOQUE 3: NORMALIZAR dia_semana
-- Title Case con tilde -> MAYUSCULAS sin tilde
-- ============================================================

UPDATE HORARIOS_DETALLE SET dia_semana = 'LUNES'     WHERE dia_semana = 'Lunes';
UPDATE HORARIOS_DETALLE SET dia_semana = 'MARTES'    WHERE dia_semana = 'Martes';
UPDATE HORARIOS_DETALLE SET dia_semana = 'MIERCOLES' WHERE dia_semana IN ('Miércoles','Miercoles','MIÉRCOLES');
UPDATE HORARIOS_DETALLE SET dia_semana = 'JUEVES'    WHERE dia_semana = 'Jueves';
UPDATE HORARIOS_DETALLE SET dia_semana = 'VIERNES'   WHERE dia_semana = 'Viernes';
UPDATE HORARIOS_DETALLE SET dia_semana = 'SABADO'    WHERE dia_semana IN ('Sábado','Sabado','SÁBADO');
UPDATE HORARIOS_DETALLE SET dia_semana = 'DOMINGO'   WHERE dia_semana = 'Domingo';


-- ============================================================
-- BLOQUE 4: RENOMBRAR HORARIOS_TURNO
-- Los nombres "HORARIO L-V" / "HORARIO SABADO" no dicen nada
-- Se renombran con las horas reales para que el admin los entienda
-- ============================================================

-- Turno 1
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-S 07:00-16:00'
WHERE id_turno = 1 AND nombre_horario = 'HORARIO L-S';

-- Turno 2
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 06:00-15:00'
WHERE id_turno = 2 AND nombre_horario = 'HORARIO L-V';

-- Turno 3
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 07:00-16:30'
WHERE id_turno = 3 AND nombre_horario = 'HORARIO L-V';
UPDATE HORARIOS_TURNO SET nombre_horario = 'SAB 07:00-12:00'
WHERE id_turno = 3 AND nombre_horario = 'HORARIO SABADO';

-- Turno 4
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 07:00-17:00'
WHERE id_turno = 4 AND nombre_horario = 'HORARIO L-V';
UPDATE HORARIOS_TURNO SET nombre_horario = 'SAB 09:00-12:00'
WHERE id_turno = 4 AND nombre_horario = 'HORARIO SABADO';

-- Turno 5
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 07:00-17:30'
WHERE id_turno = 5 AND nombre_horario = 'HORARIO L-V';

-- Turno 6
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-16:30'
WHERE id_turno = 6 AND nombre_horario = 'HORARIO L-V';
UPDATE HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-13:00'
WHERE id_turno = 6 AND nombre_horario = 'HORARIO SABADO';

-- Turno 7
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-17:00'
WHERE id_turno = 7 AND nombre_horario = 'HORARIO L-V';
UPDATE HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-17:00'
WHERE id_turno = 7 AND nombre_horario = 'HORARIO SABADO';

-- Turno 8
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-17:00'
WHERE id_turno = 8 AND nombre_horario = 'HORARIO L-V';
UPDATE HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-13:30'
WHERE id_turno = 8 AND nombre_horario = 'HORARIO SABADO';

-- Turno 9
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-17:30'
WHERE id_turno = 9 AND nombre_horario = 'HORARIO L-V';
UPDATE HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-12:00'
WHERE id_turno = 9 AND nombre_horario = 'HORARIO SABADO';

-- Turno 10
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-17:30'
WHERE id_turno = 10 AND nombre_horario = 'HORARIO L-V';
UPDATE HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-13:00'
WHERE id_turno = 10 AND nombre_horario = 'HORARIO SABADO';

-- Turno 11
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-18:00'
WHERE id_turno = 11 AND nombre_horario = 'HORARIO L-V';
UPDATE HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-11:00'
WHERE id_turno = 11 AND nombre_horario = 'HORARIO SABADO';

-- Turno 12
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 08:00-18:30'
WHERE id_turno = 12 AND nombre_horario = 'HORARIO L-V';

-- Turno 13
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 09:00-19:30'
WHERE id_turno = 13 AND nombre_horario = 'HORARIO L-V';

-- Turno 14
UPDATE HORARIOS_TURNO SET nombre_horario = 'L-V 11:00-21:00'
WHERE id_turno = 14 AND nombre_horario = 'HORARIO L-V';
UPDATE HORARIOS_TURNO SET nombre_horario = 'SAB 12:00-21:00'
WHERE id_turno = 14 AND nombre_horario = 'HORARIO SABADO';

-- Turno 15
UPDATE HORARIOS_TURNO SET nombre_horario = 'M-V 11:00-21:00'
WHERE id_turno = 15 AND nombre_horario = 'HORARIO M-V';
UPDATE HORARIOS_TURNO SET nombre_horario = 'SAB 08:00-20:00'
WHERE id_turno = 15 AND nombre_horario = 'HORARIO SABADO';

-- Turno 22 HORARIO VARIABLE: limpiar nombres de sub-horarios
UPDATE HORARIOS_TURNO SET nombre_horario = 'MAÑANA 05:00-13:00'
WHERE id_turno = 22 AND nombre_horario LIKE '%05:00%';

UPDATE HORARIOS_TURNO SET nombre_horario = 'TARDE 11:00-19:00'
WHERE id_turno = 22 AND nombre_horario LIKE '%11:00%';

UPDATE HORARIOS_TURNO SET nombre_horario = 'MADRUGADA 17:00-01:00'
WHERE id_turno = 22 AND nombre_horario LIKE '%MADRUGADA%';

UPDATE HORARIOS_TURNO SET nombre_horario = 'MAÑANA 09:00-17:00'
WHERE id_turno = 22 AND nombre_horario LIKE '%MAÑANA 09%';

UPDATE HORARIOS_TURNO SET nombre_horario = 'NOCHE 12:00-07:00'
WHERE id_turno = 22 AND nombre_horario LIKE '%12PM%';

UPDATE HORARIOS_TURNO SET nombre_horario = 'NOCHE 19:00-07:00'
WHERE id_turno = 22 AND nombre_horario LIKE '%19 pm%';

-- Deshabilitar "VARIABLE" que no tiene detalles
UPDATE HORARIOS_TURNO SET es_activo = 0
WHERE id_turno = 22 AND nombre_horario = 'VARIABLE';


-- ============================================================
-- VERIFICACION FINAL
-- ============================================================

-- 1) No debe quedar ningun detalle con hora NULL o errores conocidos
SELECT
    'ERRORES EN DETALLES (debe ser 0)' AS check_nombre,
    COUNT(*) AS cant
FROM HORARIOS_DETALLE
WHERE hora_inicio IS NULL
   OR hora_fin    IS NULL
   OR (id IN (170,171,172,173,174) AND hora_inicio = '00:00:00')
   OR (id IN (181,182,183,184,185,186) AND salida_dia_siguiente = 1)
   OR id = 205
   OR dia_semana IN ('Lunes','Martes','Miércoles','Miercoles',
                     'Jueves','Viernes','Sábado','Sabado','Domingo');

-- 2) Vista completa de horarios con nombres nuevos y cantidad de detalles
SELECT
    ht.id            AS horario_id,
    trn.id           AS turno_id,
    trn.nombre_codigo AS turno,
    ht.nombre_horario,
    ht.es_activo,
    COUNT(hd.id)     AS cant_detalles
FROM HORARIOS_TURNO ht
INNER JOIN TURNOS trn ON trn.id = ht.id_turno
LEFT JOIN  HORARIOS_DETALLE hd ON hd.id_horario_turno = ht.id
GROUP BY ht.id, trn.id, trn.nombre_codigo, ht.nombre_horario, ht.es_activo
ORDER BY trn.nombre_codigo, ht.nombre_horario;

-- 3) Detalle de los IDs criticos corregidos
SELECT
    id,
    dia_semana,
    CONVERT(varchar(8), hora_inicio, 108) AS hora_inicio,
    CONVERT(varchar(8), hora_fin,    108) AS hora_fin,
    salida_dia_siguiente
FROM HORARIOS_DETALLE
WHERE id IN (170,171,172,173,174, 181,182,183,184,185,186)
ORDER BY id;


-- ============================================================
-- DECISION FINAL
-- Si check 1 = 0 y los nombres en check 2 se ven bien:
COMMIT;
-- Si algo esta mal:
-- ROLLBACK;
-- ============================================================
