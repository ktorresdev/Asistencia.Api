# 📌 REFERENCIA RÁPIDA - Cheat Sheet

## 🎯 La Query que Necesitas (Copy-Paste)

```sql
-- VER QUIÉNES NO TIENEN TURNO
SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    p.dni,
    s.nombre
FROM TRABAJADORES t
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador AND at.es_vigente = 1
WHERE at.id IS NULL
ORDER BY p.apellidos_nombres;
```

---

## 📊 VER TODO (Con y Sin Turno)

```sql
SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    ISNULL(trn.nombre_codigo, '❌ SIN TURNO') AS turno,
    CASE WHEN at.id IS NULL THEN '❌' ELSE '✅' END AS estado
FROM TRABAJADORES t
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
ORDER BY CASE WHEN at.id IS NULL THEN 0 ELSE 1 END DESC, p.apellidos_nombres;
```

---

## 📈 ESTADÍSTICAS EN UN RENGLÓN

```sql
SELECT 
    (SELECT COUNT(*) FROM TRABAJADORES) AS total,
    (SELECT COUNT(DISTINCT id_trabajador) FROM ASIGNACION_TURNO WHERE es_vigente=1) AS con_turno,
    (SELECT COUNT(*) FROM TRABAJADORES) - (SELECT COUNT(DISTINCT id_trabajador) FROM ASIGNACION_TURNO WHERE es_vigente=1) AS sin_turno;
```

**Resultado:**
```
total | con_turno | sin_turno
------|-----------|----------
50    | 47        | 3
```

---

## 🏢 ESTADÍSTICAS POR SUCURSAL

```sql
SELECT 
    s.nombre,
    COUNT(*) total,
    COUNT(CASE WHEN at.id IS NOT NULL THEN 1 END) con_turno,
    COUNT(*) - COUNT(CASE WHEN at.id IS NOT NULL THEN 1 END) sin_turno
FROM TRABAJADORES t
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador AND at.es_vigente=1
GROUP BY s.nombre
ORDER BY sin_turno DESC;
```

---

## 🔍 BUSCAR UN TRABAJADOR ESPECÍFICO

```sql
SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    s.nombre AS sucursal,
    ISNULL(trn.nombre_codigo, 'SIN TURNO') AS turno,
    at.es_vigente
FROM TRABAJADORES t
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
WHERE p.apellidos_nombres LIKE '%Maria%'  -- ← Cambiar nombre
ORDER BY p.apellidos_nombres;
```

---

## 📋 LISTAR CON HORARIOS (UNA FILA POR DÍA)

```sql
SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    ISNULL(trn.nombre_codigo, 'SIN TURNO') turno,
    CASE hd.dia_semana WHEN 1 THEN 'Lunes' WHEN 2 THEN 'Martes' 
                       WHEN 3 THEN 'Miércoles' WHEN 4 THEN 'Jueves'
                       WHEN 5 THEN 'Viernes' WHEN 6 THEN 'Sábado'
                       WHEN 0 THEN 'Domingo' ELSE 'N/A' END dia,
    FORMAT(CAST(hd.hora_entrada AS TIME), 'HH:mm') entrada,
    FORMAT(CAST(hd.hora_salida AS TIME), 'HH:mm') salida
FROM TRABAJADORES t
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador AND at.es_vigente=1
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
LEFT JOIN HORARIOS_DETALLE hd ON ht.id = hd.id_horario_turno
ORDER BY p.apellidos_nombres, hd.dia_semana;
```

---

## ⚠️ DETECTAR PROBLEMAS

```sql
-- Asignaciones vigentes vencidas
SELECT t.id_trabajador, p.apellidos_nombres, at.fecha_fin_vigencia, GETDATE() fecha_hoy
FROM ASIGNACION_TURNO at
INNER JOIN TRABAJADORES t ON at.id_trabajador = t.id_trabajador
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
WHERE at.es_vigente = 1 AND at.fecha_fin_vigencia < GETDATE();

-- Trabajadores con múltiples asignaciones vigentes
SELECT t.id_trabajador, p.apellidos_nombres, COUNT(*) cantidad
FROM ASIGNACION_TURNO at
INNER JOIN TRABAJADORES t ON at.id_trabajador = t.id_trabajador
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
WHERE at.es_vigente = 1
GROUP BY t.id_trabajador, p.apellidos_nombres
HAVING COUNT(*) > 1;
```

---

## 🎯 VER DETALLES COMPLETOS (1 TRABAJADOR)

```sql
DECLARE @id_trabajador INT = 42;  -- ← CAMBIAR EL ID

SELECT 
    t.id_trabajador,
    p.apellidos_nombres,
    p.dni,
    s.nombre AS sucursal,
    trn.nombre_codigo AS turno,
    ht.nombre_horario AS horario,
    at.es_vigente,
    FORMAT(at.fecha_inicio_vigencia, 'dd/MM/yyyy') AS desde,
    ISNULL(FORMAT(at.fecha_fin_vigencia, 'dd/MM/yyyy'), 'Indefinido') AS hasta,
    CASE 
        WHEN at.id IS NULL THEN '❌ SIN TURNO'
        WHEN GETDATE() > at.fecha_fin_vigencia THEN '⚠️ VENCIDA'
        ELSE '✅ VIGENTE'
    END AS estado
FROM TRABAJADORES t
INNER JOIN PERSONAS p ON t.id_persona = p.id_persona
LEFT JOIN SUCURSAL s ON t.id_sucursal = s.id_sucursal
LEFT JOIN ASIGNACION_TURNO at ON t.id_trabajador = at.id_trabajador
LEFT JOIN TURNOS trn ON at.id_turno = trn.id
LEFT JOIN HORARIOS_TURNO ht ON at.id_horario_turno = ht.id
WHERE t.id_trabajador = @id_trabajador;
```

---

## 📌 TABLA REFERENCIA RÁPIDA

```
TABLA             │ CLAVE PRIMARIA │ RELACIÓN IMPORTANTE
──────────────────┼────────────────┼──────────────────────
TRABAJADORES      │ id_trabajador  │ → PERSONAS (id_persona)
PERSONAS          │ id_persona     │ Información básica
ASIGNACION_TURNO  │ id             │ → TRABAJADORES (clave)
TURNOS            │ id             │ Tipos de turno
HORARIOS_TURNO    │ id             │ Configuración horaria
HORARIOS_DETALLE  │ id             │ Detalle por día
SUCURSAL          │ id_sucursal    │ Ubicaciones
```

---

## 🔑 COLUMNAS IMPORTANTES

```
TRABAJADORES:
  - id_trabajador ← Clave principal
  - id_persona
  - id_sucursal
  - fecha_ingreso

ASIGNACION_TURNO:
  - id ← Clave principal
  - id_trabajador ← CRUCIAL (une con TRABAJADORES)
  - id_turno
  - id_horario_turno
  - es_vigente ← Filtra asignaciones activas
  - fecha_inicio_vigencia
  - fecha_fin_vigencia

PERSONAS:
  - id_persona
  - apellidos_nombres ← Para reportes
  - dni

HORARIOS_DETALLE:
  - dia_semana (0=Dom, 1=Lun, ..., 6=Sáb)
  - hora_entrada
  - hora_salida
```

---

## ⚡ TIPS DE RENDIMIENTO

```sql
-- ✅ RÁPIDO: Filtra antes de JOIN
SELECT ... WHERE at.id IS NULL  -- Filtra primero

-- ❌ LENTO: Complica JOINs innecesarios
SELECT ... JOIN HORARIOS_DETALLE WHERE ...  -- Innecesario si solo quieres sin turno

-- ✅ RÁPIDO: Usa índices
-- (asegúrate de indexar: id_trabajador en ASIGNACION_TURNO)

-- ✅ RÁPIDO: Especifica columnas
SELECT t.id_trabajador, p.apellidos_nombres  -- No SELECT *
```

---

## 📚 DOCUMENTOS COMPLETOS

Si quieres más detalle, consulta:
- `QUERY-Trabajadores-Turnos-SinAsignacion.sql` - 6 queries completas
- `QUERIES-Rapidas-Copiar-Pegar.sql` - 8 queries prácticas
- `GUIA-Consultas-Trabajadores-Turnos.md` - Explicaciones
- `DIAGRAMA-Joins-Visualizacion.md` - Visualizaciones

---

**Bookmark esta página - La usarás mucho.** 📌
