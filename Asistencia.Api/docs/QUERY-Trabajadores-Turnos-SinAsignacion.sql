-- ============================================================
-- QUERY: Trabajadores con Turnos, Horarios y Detalles
-- Para identificar quiénes NO tienen asignación
-- ============================================================

-- OPCIÓN 1: TODOS LOS TRABAJADORES (Con y sin asignación)
-- ============================================================
SELECT 
    t.id_trabajador,
    p.apellidos_nombres AS trabajador_nombre,
    p.dni,
    s.nombre AS sucursal,
    CASE WHEN at.id IS NULL THEN '❌ SIN ASIGNAR' ELSE '✅ CON TURNO' END AS estado,
    
    -- Información de Asignación
    at.id AS asignacion_id,
    at.es_vigente,
    FORMAT(at.fecha_inicio_vigencia, 'yyyy-MM-dd') AS fecha_inicio_vigencia,
    FORMAT(at.fecha_fin_vigencia, 'yyyy-MM-dd') AS fecha_fin_vigencia,
    
    -- Turno
    trn.id AS turno_id,
    trn.nombre_codigo AS turno_nombre,
    trn.es_activo AS turno_activo,
    tt.nombre AS tipo_turno,
    
    -- Horario Turno
    ht.id AS horario_turno_id,
    ht.nombre_horario,
    ht.es_activo AS horario_activo,
    
    -- Horario Detalle
    hd.id AS horario_detalle_id,
    hd.dia_semana,
    FORMAT(CAST(hd.hora_entrada AS TIME), 'HH:mm') AS hora_entrada,
    FORMAT(CAST(hd.hora_salida AS TIME), 'HH:mm') AS hora_salida,
    hd.minutos_duracion,
    
    -- Auditoría
    FORMAT(at.created_at, 'yyyy-MM-dd HH:mm:ss') AS asignacion_creada_en,
    FORMAT(at.updated_at, 'yyyy-MM-dd HH:mm:ss') AS asignacion_actualizada_en

FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador AND at.es_vigente = 1
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN TIPO_TURNO tt ON trn.id_tipo_turno = tt.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
LEFT JOIN HORARIOS_DETALLE hd ON ht.id = hd.id_horario_turno

ORDER BY 
    CASE WHEN at.id IS NULL THEN 0 ELSE 1 END DESC,  -- Sin asignar primero
    p.apellidos_nombres ASC;


-- OPCIÓN 2: SOLO TRABAJADORES SIN ASIGNACIÓN
-- ============================================================
SELECT 
    t.id_trabajador,
    p.apellidos_nombres AS trabajador_nombre,
    p.dni,
    s.nombre AS sucursal,
    t.correo_trabajo,
    t.numero_empleado,
    DATEDIFF(DAY, t.fecha_ingreso, GETDATE()) AS dias_en_empresa,
    '❌ SIN TURNO ASIGNADO' AS estado

FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador AND at.es_vigente = 1

WHERE at.id IS NULL  -- ← CLAVE: Solo sin asignación

ORDER BY p.apellidos_nombres ASC;


-- OPCIÓN 3: RESUMEN ESTADÍSTICO
-- ============================================================
SELECT 
    COUNT(DISTINCT t.id_trabajador) AS total_trabajadores,
    COUNT(DISTINCT CASE WHEN at.id IS NOT NULL THEN t.id_trabajador END) AS con_turno_asignado,
    COUNT(DISTINCT CASE WHEN at.id IS NULL THEN t.id_trabajador END) AS sin_turno_asignado,
    CAST(
        COUNT(DISTINCT CASE WHEN at.id IS NOT NULL THEN t.id_trabajador END) * 100.0 / 
        COUNT(DISTINCT t.id_trabajador) 
    AS DECIMAL(5,2)) AS porcentaje_asignados,
    CAST(
        COUNT(DISTINCT CASE WHEN at.id IS NULL THEN t.id_trabajador END) * 100.0 / 
        COUNT(DISTINCT t.id_trabajador) 
    AS DECIMAL(5,2)) AS porcentaje_sin_asignar

FROM TRABAJADORES t
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador AND at.es_vigente = 1;


-- OPCIÓN 4: DESGLOSE POR SUCURSAL
-- ============================================================
SELECT 
    s.nombre AS sucursal,
    COUNT(DISTINCT t.id_trabajador) AS total_trabajadores,
    COUNT(DISTINCT CASE WHEN at.id IS NOT NULL THEN t.id_trabajador END) AS con_turno,
    COUNT(DISTINCT CASE WHEN at.id IS NULL THEN t.id_trabajador END) AS sin_turno,
    CAST(
        COUNT(DISTINCT CASE WHEN at.id IS NOT NULL THEN t.id_trabajador END) * 100.0 / 
        COUNT(DISTINCT t.id_trabajador) 
    AS DECIMAL(5,2)) AS porcentaje_asignados

FROM TRABAJADORES t
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador AND at.es_vigente = 1

GROUP BY s.id_sucursal, s.nombre

ORDER BY sin_turno DESC;


-- OPCIÓN 5: TABLA PIVOTE - CADA HORARIO DETALLE EN UNA FILA
-- ============================================================
SELECT 
    t.id_trabajador,
    p.apellidos_nombres AS trabajador_nombre,
    s.nombre AS sucursal,
    
    -- Turno asignado
    ISNULL(trn.nombre_codigo, '❌ SIN ASIGNAR') AS turno,
    ISNULL(ht.nombre_horario, 'N/A') AS horario,
    
    -- Desglose por día (solo si tiene asignación)
    CASE 
        WHEN hd.dia_semana = 0 THEN 'Domingo'
        WHEN hd.dia_semana = 1 THEN 'Lunes'
        WHEN hd.dia_semana = 2 THEN 'Martes'
        WHEN hd.dia_semana = 3 THEN 'Miércoles'
        WHEN hd.dia_semana = 4 THEN 'Jueves'
        WHEN hd.dia_semana = 5 THEN 'Viernes'
        WHEN hd.dia_semana = 6 THEN 'Sábado'
        ELSE 'N/A'
    END AS dia_semana,
    
    FORMAT(CAST(hd.hora_entrada AS TIME), 'HH:mm') AS entrada,
    FORMAT(CAST(hd.hora_salida AS TIME), 'HH:mm') AS salida,
    hd.minutos_duracion AS minutos,
    
    -- Estado
    CASE 
        WHEN at.id IS NULL THEN '❌ SIN ASIGNAR'
        WHEN at.es_vigente = 1 THEN '✅ VIGENTE'
        ELSE '⚠️ VENCIDA'
    END AS estado

FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
LEFT JOIN HORARIOS_DETALLE hd ON ht.id = hd.id_horario_turno

ORDER BY 
    CASE WHEN at.id IS NULL THEN 0 ELSE 1 END DESC,
    p.apellidos_nombres ASC,
    hd.dia_semana ASC;


-- OPCIÓN 6: ASIGNACIONES DUPLICADAS O CON CONFLICTOS
-- ============================================================
SELECT 
    t.id_trabajador,
    p.apellidos_nombres AS trabajador_nombre,
    COUNT(at.id) AS cantidad_asignaciones,
    STRING_AGG(
        CONCAT(
            'Turno: ', trn.nombre_codigo, 
            ' | Periodo: ', FORMAT(at.fecha_inicio_vigencia, 'dd/MM/yyyy'),
            ' - ', ISNULL(FORMAT(at.fecha_fin_vigencia, 'dd/MM/yyyy'), 'Indefinido'),
            ' | Vigente: ', CASE WHEN at.es_vigente = 1 THEN 'SÍ' ELSE 'NO' END
        ),
        ' || '
    ) AS asignaciones

FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador
LEFT JOIN TURNOS trn ON at.id_turno = trn.id

GROUP BY t.id_trabajador, p.apellidos_nombres

HAVING COUNT(at.id) > 1 OR (COUNT(DISTINCT at.es_vigente) > 1 AND COUNT(at.id) > 0)

ORDER BY COUNT(at.id) DESC;


-- ============================================================
-- NOTAS IMPORTANTES
-- ============================================================
/*
1. LEFT JOIN garantiza que aparezcan TODOS los trabajadores
   - Si no tienen asignación, las columnas de turno/horario serán NULL

2. WHERE at.id IS NULL identifica trabajadores SIN asignación
   
3. CASE WHEN at.id IS NULL muestra un indicador visual (❌ vs ✅)

4. at.es_vigente = 1 filtra solo asignaciones activas
   - Sin este filtro, podrías ver asignaciones vencidas

5. HORARIOS_DETALLE tiene una fila por cada día del horario
   - Un horario turno puede tener 7 filas (L-D) o menos

6. Para ver SOLO los que NO tienen:
   - Usa OPCIÓN 2 (más eficiente)

7. Para auditoria completa:
   - Usa OPCIÓN 1 (muestra todo)

8. Para estadísticas:
   - Usa OPCIÓN 3 o 4
*/
