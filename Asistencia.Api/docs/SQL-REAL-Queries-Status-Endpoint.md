# 🔍 SQL REAL: Qué Consultas Se Ejecutan en `/status/2`

## 📊 Simulación: Trabajador ID 2, Fecha: 2026-03-20

---

## QUERY 1️⃣: Obtener ASIGNACION_TURNO

```sql
-- Esto se ejecuta PRIMERO en ResolveShiftContextAsync()
SELECT 
    at.id_asignacion,
    at.id_trabajador,
    at.id_turno,
    at.id_horario_turno,           -- ← CLAVE (será NULL para ROTATIVO)
    at.fecha_inicio_vigencia,
    at.fecha_fin_vigencia,
    at.es_vigente,
    
    -- TURNO y sus HORARIOS_TURNO
    t.id_turno,
    t.nombre_codigo,
    ht.id_horario_turno,
    ht.nombre_horario,
    hd.id_detalle,
    hd.dia_semana,
    hd.hora_inicio,
    hd.hora_fin,
    
    -- HORARIO_TURNO de ASIGNACION (si tiene)
    ht2.id_horario_turno AS horario_asignacion_id,
    ht2.nombre_horario AS horario_asignacion_nombre,
    hd2.dia_semana AS horario_asignacion_dia,
    hd2.hora_inicio AS horario_asignacion_inicio,
    hd2.hora_fin AS horario_asignacion_fin

FROM ASIGNACIONES_TURNO at
INNER JOIN TURNOS t ON at.id_turno = t.id_turno
LEFT JOIN HORARIOS_TURNO ht ON t.id_turno = ht.id_turno
LEFT JOIN HORARIOS_DETALLE hd ON ht.id_horario_turno = hd.id_horario_turno
LEFT JOIN HORARIOS_TURNO ht2 ON at.id_horario_turno = ht2.id_horario_turno
LEFT JOIN HORARIOS_DETALLE hd2 ON ht2.id_horario_turno = hd2.id_horario_turno

WHERE at.id_trabajador = 2                      -- ← Trabajador ID 2
  AND at.fecha_inicio_vigencia <= '2026-03-20' -- ← Hoy
  AND (at.fecha_fin_vigencia IS NULL OR at.fecha_fin_vigencia >= '2026-03-20')
  AND at.es_vigente = 1
```

### RESULTADO de QUERY 1:

```
id_asignacion: 101
id_trabajador: 2
id_turno: 2 (ROTATIVO)
id_horario_turno: NULL                     ← ¡¡CLAVE!! Es NULL = ROTATIVO
fecha_inicio_vigencia: 2025-01-01
fecha_fin_vigencia: NULL
es_vigente: 1

TURNO.HorariosTurno (todas las opciones):
├─ Row 1:
│  ├─ horario_id: 5
│  ├─ horario_nombre: "Mañana 09:00-17:00"
│  ├─ dia_semana: "Lun-Vie"
│  ├─ hora_inicio: 09:00
│  └─ hora_fin: 17:00
│
├─ Row 2:
│  ├─ horario_id: 6
│  ├─ horario_nombre: "Tarde 14:00-22:00"
│  ├─ dia_semana: "Lun-Vie"
│  ├─ hora_inicio: 14:00
│  └─ hora_fin: 22:00
│
└─ Row 3:
   ├─ horario_id: 7
   ├─ horario_nombre: "Noche 22:00-06:00"
   ├─ dia_semana: "Lun-Vie"
   ├─ hora_inicio: 22:00
   └─ hora_fin: 06:00

horario_asignacion_id: NULL               ← Confirma que NO tiene horario específico
horario_asignacion_nombre: NULL
horario_asignacion_dia: NULL
```

---

## EVALUACIÓN en C#:

```csharp
// Después de QUERY 1
var asignacion = ... // resultado anterior

if (asignacion?.HorarioTurnoId.HasValue == true)
{
    // ❌ NO entra aquí (HorarioTurnoId es NULL)
}
else
{
    // ✅ ENTRA AQUÍ (es ROTATIVO)
    // → Proceder a QUERY 2
}
```

---

## QUERY 2️⃣: Buscar PROGRAMACION_TURNOS_SEMANAL

```sql
-- Solo se ejecuta si NO tiene HorarioTurnoId (ROTATIVO)
SELECT TOP 1
    pts.id,
    pts.id_trabajador,
    pts.fecha,
    pts.id_horario_turno,           -- ← Horario para ESTE día
    pts.es_descanso,
    pts.es_dia_boleta,
    pts.es_vacaciones,
    
    -- HORARIO_TURNO del día
    ht.id_horario_turno,
    ht.nombre_horario,
    hd.id_detalle,
    hd.dia_semana,
    hd.hora_inicio,
    hd.hora_fin

FROM PROGRAMACION_TURNOS_SEMANAL pts
LEFT JOIN HORARIOS_TURNO ht ON pts.id_horario_turno = ht.id_horario_turno
LEFT JOIN HORARIOS_DETALLE hd ON ht.id_horario_turno = hd.id_horario_turno

WHERE pts.id_trabajador = 2           -- ← Trabajador ID 2
  AND pts.fecha = '2026-03-20'        -- ← Hoy específicamente
```

---

## 📋 ESCENARIOS DE RESULTADO QUERY 2:

### ESCENARIO A: Sí hay programación para HOY

```
RESULTADO:
├─ id: 501
├─ id_trabajador: 2
├─ fecha: 2026-03-20
├─ id_horario_turno: 6            ← Horario del día
├─ es_descanso: 0
├─ es_dia_boleta: 0
├─ es_vacaciones: 0
└─ HORARIO_TURNO:
   ├─ id_horario_turno: 6
   ├─ nombre_horario: "Tarde 14:00-22:00"
   ├─ HorariosDetalle:
   │  ├─ dia_semana: "Lun-Vie"
   │  ├─ hora_inicio: 14:00:00
   │  └─ hora_fin: 22:00:00
```

**En C#:**
```csharp
if (programacionHoy?.HorarioTurno != null)
{
    horarioTurno = programacionHoy.HorarioTurno;  // ← ID 6 (TARDE)
}
```

**RESPONSE:**
```json
{
  "horarioProgramado": "14:00 - 22:00"  // ← DEL DÍA
}
```

---

### ESCENARIO B: NO hay programación para HOY

```
RESULTADO:
├─ NULL (No hay registro)
```

**En C#:**
```csharp
else
{
    // Fallback: Usar primer horario activo del turno
    horarioTurno = turno.HorariosTurno?.FirstOrDefault(ht => ht.EsActivo == true);
    // De QUERY 1, esto sería ID 5 (Mañana, el primero)
}
```

**RESPONSE:**
```json
{
  "horarioProgramado": "09:00 - 17:00"  // ← FALLBACK
}
```

---

### ESCENARIO C: Hay DESCANSO programado

```
RESULTADO:
├─ id: 501
├─ id_trabajador: 2
├─ fecha: 2026-03-20
├─ id_horario_turno: 6
├─ es_descanso: 1              ← ¡¡DESCANSO!!
├─ es_dia_boleta: 0
├─ es_vacaciones: 0
```

**En C#:**
```csharp
// En AddMarcacionAsync() o GetMarcacionStatus()
if (programacionHoy != null && 
    (programacionHoy.EsDescanso || programacionHoy.EsVacaciones))
{
    return new ShiftContext
    {
        HasAssignedShift = false  // ← NO PUEDE MARCAR
    };
}
```

**RESPONSE:**
```json
{
  "success": false,
  "code": "ERROR_NO_TURNO",
  "message": "No tiene un turno asignado para la fecha actual."
}
```

---

## 🔄 QUERY 3️⃣: Obtener MARCACIONES del día

```sql
-- Después de determinar el horario, obtener marcaciones
SELECT 
    m.id_marcacion,
    m.id_trabajador,
    m.fecha_hora,
    m.tipo_marcacion,
    m.latitud,
    m.longitud,
    m.foto_url,
    m.ubicacion_valida

FROM MARCACIONES_ASISTENCIA m

WHERE m.id_trabajador = 2                    -- ← Trabajador ID 2
  AND m.fecha_hora >= '2026-03-20 14:00:00'  -- ← Inicio del horario del día
  AND m.fecha_hora <= '2026-03-20 22:00:00'  -- ← Fin del horario del día

ORDER BY m.fecha_hora ASC
```

### RESULTADO QUERY 3:

```
Si es 09:45 y aún no ha marcado:
└─ (No hay registros)

Si es 15:30 y ya marcó entrada:
├─ id_marcacion: 1001
├─ fecha_hora: 2026-03-20 14:15:30
├─ tipo_marcacion: "ENTRADA"
└─ (No hay salida aún)

Si es 22:15 y ya salió:
├─ Registro 1: ENTRADA a las 14:15:30
├─ Registro 2: SALIDA a las 22:10:45
```

---

## 📊 RESUMEN DE QUERIES EJECUTADAS

```
GET /api/Rrhh/MarcacionAsistencia/status/2
│
├─ QUERY 1: ASIGNACIONES_TURNO (incluye TURNOS y HORARIOS_TURNO)
│  └─ Resultado: id_horario_turno = NULL (ROTATIVO)
│
├─ QUERY 2: PROGRAMACION_TURNOS_SEMANAL (si es ROTATIVO)
│  ├─ Encontrado: Retorna HORARIO_TURNO para ese día
│  └─ No encontrado: Usa fallback de QUERY 1
│
└─ QUERY 3: MARCACIONES_ASISTENCIA dentro de la ventana de horario
   └─ Calcula tiempo trabajado
```

---

## ✅ FLUJO FINAL VISUALIZADO

```
┌─────────────────────────────────────────────────┐
│ GET /api/.../status/2 (Hoy: 2026-03-20 09:45) │
└──────────────┬──────────────────────────────────┘
               │
        ┌──────▼──────┐
        │ QUERY 1:    │
        │ ASIGNACION_ │
        │ TURNO       │
        │ id_hora..=  │
        │ NULL        │
        └──────┬──────┘
               │
        ¿ROTATIVO?
               │
        ┌──────▼──────┐
        │ YES → QUERY │
        │ 2:          │
        │ PROGRAMA-   │
        │ CION        │
        │ (2026-03-20)│
        └──────┬──────┘
               │
        ¿Encontrado?
        │
    ┌───┴────┐
    │        │
   SÍ       NO
    │        │
Hora   Usar Fallback
del    (Primer
día    horario)
(ID 6)  (ID 5)
    │        │
    └───┬────┘
        │
   ┌────▼─────┐
   │ QUERY 3:  │
   │ MARCA-    │
   │ CIONES    │
   │ hoy       │
   └────┬─────┘
        │
   ┌────▼─────────────────────┐
   │ RESPONSE:                 │
   │ {                         │
   │  horarioProgramado: ..    │
   │  marcacionEntrada: ..     │
   │  marcacionSalida: ..      │
   │  puedeMarcar: ..          │
   │  tiempoTrabajado: ..      │
   │ }                         │
   └──────────────────────────┘
```

---

## 💡 POR QUÉ FUNCIONA ASI

1. **QUERY 1** determina el **tipo** de turno (FIJO o ROTATIVO)
2. **QUERY 2** (solo para ROTATIVO) obtiene el **horario específico del día**
3. **QUERY 3** obtiene las **marcaciones dentro de ese horario**

Esto permite:
✅ FIJO: Horario constante todos los días
✅ ROTATIVO: Horario flexible, cambiar cada día
✅ Marcación: Se valida contra el horario correcto del día

---

**Ahora sabes exactamente qué sucede cuando llamas a `/status/2`.** ✅
