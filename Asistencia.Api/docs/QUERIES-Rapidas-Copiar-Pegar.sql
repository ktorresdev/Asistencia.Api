-- ============================================================
-- QUERIES RÁPIDAS - COPIAR Y PEGAR
-- ============================================================

-- 🔴 LA MÁS IMPORTANTE: TRABAJADORES SIN TURNO
-- ============================================================
SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    p.dni,
    s.nombre AS sucursal,
    t.correo_trabajo,
    FORMAT(t.fecha_ingreso, 'dd/MM/yyyy') AS fecha_ingreso
FROM TRABAJADORES t
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador AND at.es_vigente = 1
WHERE at.id IS NULL
ORDER BY p.apellidos_nombres;


-- ============================================================
-- TODOS CON ESTADO (✅ ó ❌)
-- ============================================================
SELECT TOP 100
    t.id_trabajador,
    p.apellidos_nombres,
    s.nombre AS sucursal,
    ISNULL(trn.nombre_codigo, '❌ SIN ASIGNAR') AS turno,
    ISNULL(ht.nombre_horario, '-') AS horario,
    CASE 
        WHEN at.id IS NULL THEN '❌ SIN ASIGNAR'
        WHEN at.es_vigente = 1 THEN '✅ ASIGNADO'
        ELSE '⚠️ VENCIDO'
    END AS estado,
    FORMAT(at.fecha_inicio_vigencia, 'dd/MM/yyyy') AS desde,
    ISNULL(FORMAT(at.fecha_fin_vigencia, 'dd/MM/yyyy'), 'Indefinido') AS hasta
FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
ORDER BY CASE WHEN at.id IS NULL THEN 0 ELSE 1 END DESC, p.apellidos_nombres;


-- ============================================================
-- ESTADÍSTICA RÁPIDA
-- ============================================================
SELECT 
    (SELECT COUNT(*) FROM TRABAJADORES) AS total_trabajadores,
    (SELECT COUNT(DISTINCT id_trabajador) FROM ASIGNACION_TURNO WHERE es_vigente = 1) AS con_turno,
    (SELECT COUNT(*) FROM TRABAJADORES) - 
    (SELECT COUNT(DISTINCT id_trabajador) FROM ASIGNACION_TURNO WHERE es_vigente = 1) 
    AS sin_turno;


-- ============================================================
-- POR SUCURSAL (CON PORCENTAJE)
-- ============================================================
SELECT 
    s.nombre,
    COUNT(*) AS total,
    ISNULL(SUM(CASE WHEN at.id IS NOT NULL THEN 1 ELSE 0 END), 0) AS con_turno,
    COUNT(*) - ISNULL(SUM(CASE WHEN at.id IS NOT NULL THEN 1 ELSE 0 END), 0) AS sin_turno,
    CAST(
        ISNULL(SUM(CASE WHEN at.id IS NOT NULL THEN 1 ELSE 0 END), 0) * 100.0 / COUNT(*) 
    AS NUMERIC(5,1)) AS porcentaje
FROM TRABAJADORES t
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador AND at.es_vigente = 1
GROUP BY s.id_sucursal, s.nombre
ORDER BY sin_turno DESC;


-- ============================================================
-- HORARIOS DETALLADOS (CADA DÍA EN UNA FILA)
-- ============================================================
SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    ISNULL(trn.nombre_codigo, '❌ SIN TURNO') AS turno,
    CASE hd.dia_semana
        WHEN 1 THEN 'Lunes'
        WHEN 2 THEN 'Martes'
        WHEN 3 THEN 'Miércoles'
        WHEN 4 THEN 'Jueves'
        WHEN 5 THEN 'Viernes'
        WHEN 6 THEN 'Sábado'
        WHEN 0 THEN 'Domingo'
        ELSE 'N/A'
    END AS dia,
    ISNULL(FORMAT(CAST(hd.hora_entrada AS TIME), 'HH:mm'), '-') AS entrada,
    ISNULL(FORMAT(CAST(hd.hora_salida AS TIME), 'HH:mm'), '-') AS salida,
    ISNULL(hd.minutos_duracion, 0) AS minutos
FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador AND at.es_vigente = 1
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
LEFT JOIN HORARIOS_DETALLE hd ON ht.id = hd.id_horario_turno
WHERE t.id_trabajador IN (
    SELECT id_trabajador FROM TRABAJADORES 
    WHERE id_trabajador NOT IN (
        SELECT id_trabajador FROM ASIGNACION_TURNO WHERE es_vigente = 1
    )
)
OR at.id IS NOT NULL
ORDER BY p.apellidos_nombres, hd.dia_semana;


-- ============================================================
-- LISTA SIMPLE (COPIAR A EXCEL)
-- ============================================================
SELECT 
    t.id_trabajador AS ID,
    p.apellidos_nombres AS NOMBRE,
    p.dni AS DNI,
    s.nombre AS SUCURSAL,
    ISNULL(trn.nombre_codigo, 'SIN ASIGNAR') AS TURNO,
    ISNULL(ht.nombre_horario, '-') AS HORARIO,
    FORMAT(at.fecha_inicio_vigencia, 'dd/MM/yyyy') AS DESDE,
    ISNULL(FORMAT(at.fecha_fin_vigencia, 'dd/MM/yyyy'), 'VIGENTE') AS HASTA
FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
ORDER BY 
    CASE WHEN at.id IS NULL THEN 0 ELSE 1 END DESC,
    p.apellidos_nombres;


-- ============================================================
-- ASIGNACIONES VIGENTES (SOLO LOS QUE TIENEN)
-- ============================================================
SELECT TOP 50
    t.id_trabajador,
    p.apellidos_nombres,
    s.nombre AS sucursal,
    trn.nombre_codigo AS turno,
    ht.nombre_horario,
    FORMAT(at.fecha_inicio_vigencia, 'dd/MM/yyyy') AS desde,
    ISNULL(FORMAT(at.fecha_fin_vigencia, 'dd/MM/yyyy'), 'Indefinido') AS hasta,
    CASE WHEN GETDATE() > at.fecha_fin_vigencia THEN '⚠️ VENCIDA' ELSE '✅ VIGENTE' END AS estado
FROM ASIGNACION_TURNO at
INNER JOIN TRABAJADORES t ON at.id_trabajador = t.id_trabajador
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
INNER JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
WHERE at.es_vigente = 1
ORDER BY p.apellidos_nombres;


-- ============================================================
-- VERIFICAR INTEGRIDAD (DUPLICADOS, VENCIDAS, ETC)
-- ============================================================
SELECT 
    '⚠️ ASIGNACIONES VIGENTES Y VENCIDAS SIMULTÁNEAMENTE' AS problema,
    t.id_trabajador,
    p.apellidos_nombres,
    COUNT(*) AS cantidad
FROM ASIGNACION_TURNO at
INNER JOIN TRABAJADORES t ON at.id_trabajador = t.id_trabajador
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
GROUP BY t.id_trabajador, p.apellidos_nombres
HAVING COUNT(*) > 1

UNION ALL

SELECT 
    '❌ TURNOS INACTIVOS ASIGNADOS',
    t.id_trabajador,
    p.apellidos_nombres,
    COUNT(*) AS cantidad
FROM ASIGNACION_TURNO at
INNER JOIN TRABAJADORES t ON at.id_trabajador = t.id_trabajador
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
INNER JOIN TURNOS trn ON at.id_turno = trn.id
WHERE trn.es_activo = 0 AND at.es_vigente = 1
GROUP BY t.id_trabajador, p.apellidos_nombres;
