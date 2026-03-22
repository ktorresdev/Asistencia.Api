# 📊 RESUMEN: Queries SQL para Trabajadores sin Turno

## 🎯 Necesidad Identificada

```
Necesito ver:
✅ Todos los trabajadores
✅ Sus turnos asignados (si tienen)
✅ Sus horarios y detalles
✅ ESPECIALMENTE: Quiénes NO tienen turno
```

---

## 📁 Archivos Creados

| Archivo | Contenido | Usar Para |
|---------|-----------|-----------|
| **QUERY-Trabajadores-Turnos-SinAsignacion.sql** | 6 queries completas | Análisis completo |
| **QUERIES-Rapidas-Copiar-Pegar.sql** | 8 queries prácticas | Copiar directamente |
| **GUIA-Consultas-Trabajadores-Turnos.md** | Explicación + ejemplos | Entender cada query |

---

## 🚀 COMIENZA AQUÍ

### Si quieres VER RÁPIDO quién NO tiene turno:

```sql
-- LA QUERY MÁS IMPORTANTE (Copiar tal cual)
SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    p.dni,
    s.nombre AS sucursal
FROM TRABAJADORES t
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador 
                              AND at.es_vigente = 1
WHERE at.id IS NULL
ORDER BY p.apellidos_nombres;
```

**Resultado:** Solo trabajadores SIN turno asignado ✅

---

### Si quieres VER TODO (Con y Sin):

```sql
-- CON ESTADO VISUAL
SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    s.nombre AS sucursal,
    ISNULL(trn.nombre_codigo, '❌ SIN ASIGNAR') AS turno,
    ISNULL(ht.nombre_horario, '-') AS horario,
    CASE 
        WHEN at.id IS NULL THEN '❌ SIN ASIGNAR'
        WHEN at.es_vigente = 1 THEN '✅ ASIGNADO'
        ELSE '⚠️ VENCIDO'
    END AS estado
FROM TRABAJADORES t
LEFT JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
ORDER BY estado, p.apellidos_nombres;
```

**Resultado:** Todos con indicador visual (✅ ó ❌) ✅

---

### Si quieres ESTADÍSTICAS:

```sql
-- EN UN RENGLÓN
SELECT 
    (SELECT COUNT(*) FROM TRABAJADORES) AS total,
    (SELECT COUNT(DISTINCT id_trabajador) 
     FROM ASIGNACION_TURNO WHERE es_vigente = 1) AS con_turno,
    (SELECT COUNT(*) FROM TRABAJADORES) - 
    (SELECT COUNT(DISTINCT id_trabajador) 
     FROM ASIGNACION_TURNO WHERE es_vigente = 1) AS sin_turno;
```

**Resultado:**
```
total | con_turno | sin_turno
------|-----------|----------
50    | 47        | 3
```

---

## 📋 Opciones Disponibles

| Query # | Descripción | Complejidad | Velocidad |
|---------|-------------|-------------|-----------|
| **1** | Todos + Detalles | ⭐⭐⭐ | Lenta |
| **2** | Solo sin turno | ⭐ | ⚡ MÁS RÁPIDA |
| **3** | Estadísticas | ⭐⭐ | ⚡ Rápida |
| **4** | Por sucursal | ⭐⭐ | ⚡ Rápida |
| **5** | Horarios desglosados | ⭐⭐⭐ | Lenta |
| **6** | Duplicados/Errores | ⭐⭐ | ⚡ Rápida |

---

## 🎯 Recomendaciones

### Si necesitas ACCIÓN INMEDIATA
**→ Usa Query #2** (SIN turno)
- Más rápida
- Muestra exactamente quiénes falta asignar
- Ideal para hacer asignaciones

### Si haces AUDITORÍA
**→ Usa Query #1** (Todos)
- Muestra panorama completo
- Incluye detalles de horarios
- Para informes ejecutivos

### Si haces VALIDACIÓN
**→ Usa Query #6** (Duplicados/Errores)
- Detecta problemas de datos
- Identifica asignaciones vencidas
- Control de integridad

### Si necesitas DASHBOARD
**→ Usa Query #3 + #4** (Estadísticas)
- KPIs para directivos
- Desglose por sucursal
- Porcentajes

---

## 🔍 Explicación de Joins

```
LEFT JOIN ASIGNACION_TURNO
    ↓
Mantiene TODOS los trabajadores (incluso sin turno)

WHERE at.id IS NULL
    ↓
Filtra SOLO los que NO tienen asignación

at.es_vigente = 1
    ↓
Considera SOLO asignaciones activas (no vencidas)
```

---

## 💾 Dónde están las queries

### Archivo 1: QUERY-Trabajadores-Turnos-SinAsignacion.sql
```
- 6 queries completas
- Con explicaciones
- Opciones para cada necesidad
```

### Archivo 2: QUERIES-Rapidas-Copiar-Pegar.sql
```
- 8 queries listos para copiar
- Sin comentarios (más limpio)
- Enfoque práctico
```

### Archivo 3: GUIA-Consultas-Trabajadores-Turnos.md
```
- Explicación de cada query
- Ejemplos de resultados
- Cuándo usar cada una
```

---

## 📊 Ejemplo de Resultado

```
¿Quiénes NO tienen turno?

id_trabajador | apellidos_nombres    | dni        | sucursal
──────────────┼──────────────────────┼────────────┼─────────────
2             | María García         | 45678901   | Lima Centro
4             | Ana Rodríguez        | 78901234   | Lima Norte
7             | Patricia Sánchez     | 23456789   | Lima Este

TOTAL: 3 trabajadores sin asignación
```

---

## ⚙️ Si Quieres Modificar las Queries

### Agregar más filtros:
```sql
WHERE at.id IS NULL
AND s.id_sucursal = 1  -- ← Solo Lima Centro
AND t.fecha_ingreso >= '2025-01-01'  -- ← Solo recientes
```

### Agregar más columnas:
```sql
SELECT 
    ...,
    t.numero_empleado,
    FORMAT(t.fecha_ingreso, 'dd/MM/yyyy') AS ingreso,
    t.cargo,
    ...
```

### Cambiar orden:
```sql
ORDER BY 
    s.nombre ASC,
    p.apellidos_nombres ASC
```

---

## 🎓 Conclusión

**Tienes 3 archivos SQL listos:**

1. ✅ Archivo detallado (6 queries)
2. ✅ Archivo rápido (8 queries)
3. ✅ Guía completa (explicaciones)

**Recomendación:**
- Abre `QUERIES-Rapidas-Copiar-Pegar.sql`
- Copia la segunda query (la más importante)
- Pégala en Management Studio
- ¡Ves quiénes no tienen turno!

---

**Las queries están en `/docs/`** 🚀
