# 📊 Consultas SQL: Trabajadores sin Asignación de Turno

## 🎯 Problema a Resolver

```
¿Cuáles son los trabajadores que NO tienen turno asignado?
¿Cuál es su información completa (horarios, turnos, sucursal)?
```

---

## 🔍 OPCIÓN 1: Todos los Trabajadores (Con y Sin Asignación)

### Cuándo Usar
- Auditoría completa
- Reporte mensual
- Validación de datos

### Resultado
```
id_trabajador  | trabajador_nombre      | estado          | turno_nombre    | horario_activo
──────────────────────────────────────────────────────────────────────────────────────
1              | Juan Pérez             | ✅ CON TURNO    | TURNO_MAÑANA   | Sí
2              | María García           | ❌ SIN ASIGNAR  | NULL           | NULL
3              | Carlos López           | ✅ CON TURNO    | TURNO_NOCHE    | Sí
4              | Ana Rodríguez          | ❌ SIN ASIGNAR  | NULL           | NULL
5              | Roberto Martínez       | ✅ CON TURNO    | TURNO_TARDE    | Sí
```

### Query Efectivo
```sql
SELECT t.id_trabajador, p.apellidos_nombres, 
       CASE WHEN at.id IS NULL THEN '❌ SIN ASIGNAR' 
            ELSE '✅ CON TURNO' END AS estado,
       trn.nombre_codigo AS turno,
       ht.nombre_horario
FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador 
                              AND at.es_vigente = 1
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
ORDER BY estado ASC, p.apellidos_nombres;
```

---

## 🔴 OPCIÓN 2: SOLO Sin Asignación (La Más Rápida)

### Cuándo Usar
- Identificar rápidamente quiénes faltan
- Hacer asignaciones pendientes
- Validación de datos incompletos

### Resultado
```
id_trabajador  | trabajador_nombre      | dni            | sucursal      | estado
──────────────────────────────────────────────────────────────────────────────────
2              | María García           | 45678901       | Lima Centro   | ❌ SIN TURNO
4              | Ana Rodríguez          | 78901234       | Lima Norte    | ❌ SIN TURNO
7              | Patricia Sánchez       | 23456789       | Lima Este     | ❌ SIN TURNO
```

### Query Optimizada
```sql
SELECT t.id_trabajador, p.apellidos_nombres, p.dni, s.nombre AS sucursal
FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador 
                              AND at.es_vigente = 1
WHERE at.id IS NULL
ORDER BY p.apellidos_nombres;
```

---

## 📈 OPCIÓN 3: Estadísticas Generales

### Cuándo Usar
- Dashboard ejecutivo
- KPIs de cobertura
- Reportes gerenciales

### Resultado
```
total_trabajadores | con_turno_asignado | sin_turno_asignado | % Asignados | % Sin Asignar
──────────────────────────────────────────────────────────────────────────────────
50                 | 47                 | 3                  | 94.00%      | 6.00%
```

### Query de Resumen
```sql
SELECT 
    COUNT(DISTINCT t.id_trabajador) AS total_trabajadores,
    COUNT(DISTINCT CASE WHEN at.id IS NOT NULL 
                       THEN t.id_trabajador END) AS con_turno,
    COUNT(DISTINCT CASE WHEN at.id IS NULL 
                       THEN t.id_trabajador END) AS sin_turno,
    CAST(COUNT(DISTINCT CASE WHEN at.id IS NOT NULL 
                             THEN t.id_trabajador END) * 100.0 / 
         COUNT(DISTINCT t.id_trabajador) AS DECIMAL(5,2)) AS porcentaje_asignados
FROM TRABAJADORES t
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador 
                              AND at.es_vigente = 1;
```

---

## 🏢 OPCIÓN 4: Desglose por Sucursal

### Cuándo Usar
- Auditoría por sucursal
- Identificar sucursales problemáticas
- Reporte operativo

### Resultado
```
sucursal        | total_trabajadores | con_turno | sin_turno | % Asignados
─────────────────────────────────────────────────────────────────────────────
Lima Centro     | 20                 | 18        | 2         | 90.00%
Lima Norte      | 15                 | 15        | 0         | 100.00%
Lima Este       | 10                 | 10        | 0         | 100.00%
Lima Sur        | 5                  | 4         | 1         | 80.00%
─────────────────────────────────────────────────────────────────────────────
TOTAL           | 50                 | 47        | 3         | 94.00%
```

### Query Agrupada
```sql
SELECT 
    s.nombre AS sucursal,
    COUNT(DISTINCT t.id_trabajador) AS total,
    COUNT(DISTINCT CASE WHEN at.id IS NOT NULL 
                       THEN t.id_trabajador END) AS con_turno,
    COUNT(DISTINCT CASE WHEN at.id IS NULL 
                       THEN t.id_trabajador END) AS sin_turno,
    CAST(COUNT(DISTINCT CASE WHEN at.id IS NOT NULL 
                             THEN t.id_trabajador END) * 100.0 / 
         COUNT(DISTINCT t.id_trabajador) AS DECIMAL(5,2)) AS porcentaje
FROM TRABAJADORES t
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador 
                              AND at.es_vigente = 1
GROUP BY s.id_sucursal, s.nombre
ORDER BY sin_turno DESC;
```

---

## 📋 OPCIÓN 5: Horarios Detallados (Una Fila por Día)

### Cuándo Usar
- Visualizar horarios completos
- Auditoría de horarios
- Validación de configuración

### Resultado
```
id_trab | trabajador_nombre | turno        | dia_semana | entrada | salida | estado
─────────────────────────────────────────────────────────────────────────────────
1       | Juan Pérez        | TURNO_MAÑANA | Lunes      | 09:00   | 17:00  | ✅ VIGENTE
1       | Juan Pérez        | TURNO_MAÑANA | Martes     | 09:00   | 17:00  | ✅ VIGENTE
1       | Juan Pérez        | TURNO_MAÑANA | Miércoles  | 09:00   | 17:00  | ✅ VIGENTE
...
2       | María García      | SIN ASIGNAR  | N/A        | N/A     | N/A    | ❌ SIN ASIGNAR
3       | Carlos López      | TURNO_NOCHE  | Lunes      | 21:00   | 05:00  | ✅ VIGENTE
```

### Query Expandida
```sql
SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    ISNULL(trn.nombre_codigo, '❌ SIN ASIGNAR') AS turno,
    CASE 
        WHEN hd.dia_semana = 1 THEN 'Lunes'
        WHEN hd.dia_semana = 2 THEN 'Martes'
        -- ...más días...
        ELSE 'N/A'
    END AS dia_semana,
    FORMAT(CAST(hd.hora_entrada AS TIME), 'HH:mm') AS entrada,
    FORMAT(CAST(hd.hora_salida AS TIME), 'HH:mm') AS salida,
    CASE 
        WHEN at.id IS NULL THEN '❌ SIN ASIGNAR'
        WHEN at.es_vigente = 1 THEN '✅ VIGENTE'
        ELSE '⚠️ VENCIDA'
    END AS estado
FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
LEFT JOIN HORARIOS_DETALLE hd ON ht.id = hd.id_horario_turno
ORDER BY p.apellidos_nombres, hd.dia_semana;
```

---

## ⚠️ OPCIÓN 6: Detectar Asignaciones Duplicadas

### Cuándo Usar
- Validación de integridad
- Detección de errores
- Limpieza de datos

### Resultado
```
id_trabajador | trabajador_nombre | cantidad_asignaciones | Problema
──────────────────────────────────────────────────────────────────────
15            | Roberto Martínez  | 2                     | ⚠️ DUPLICADO
22            | Carmen López      | 3                     | ⚠️ CRÍTICO
```

### Query de Validación
```sql
SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    COUNT(at.id) AS cantidad_asignaciones,
    STRING_AGG(
        CONCAT(
            'Turno: ', trn.nombre_codigo,
            ' (', FORMAT(at.fecha_inicio_vigencia, 'dd/MM'),
            ' - ', ISNULL(FORMAT(at.fecha_fin_vigencia, 'dd/MM'), 'Indef'), ')'
        ),
        ' | '
    ) AS asignaciones
FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
GROUP BY t.id_trabajador, p.apellidos_nombres
HAVING COUNT(at.id) > 1
ORDER BY COUNT(at.id) DESC;
```

---

## 🎯 Recomendación por Caso

| Necesidad | Opción | Por Qué |
|-----------|--------|--------|
| **Ver quiénes NO tienen turno** | 2 | Más rápida y precisa |
| **Auditoría completa** | 1 | Muestra todo junto |
| **Estadísticas/KPIs** | 3 | Resumen ejecutivo |
| **Analizar por sucursal** | 4 | Datos operativos |
| **Ver horarios completos** | 5 | Detalle máximo |
| **Detectar errores/duplicados** | 6 | Control de calidad |

---

## 📌 Columnas Clave Explicadas

```
LEFT JOIN → Mantiene trabajadores sin asignación (NULL en turno/horario)

WHERE at.id IS NULL → Filtra SOLO los que NO tienen asignación

at.es_vigente = 1 → Considera SOLO asignaciones activas (no vencidas)

FORMAT(CAST(...AS TIME), 'HH:mm') → Convierte tiempo a formato legible

CASE WHEN ... THEN ... → Muestra iconos/textos para mejor visualización

STRING_AGG(...) → Agrupa múltiples valores en una sola celda
```

---

**Usa la OPCIÓN 2 para rapidez, OPCIÓN 1 para auditoría completa.** 🚀
