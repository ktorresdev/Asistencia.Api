# 📊 EXPLICACIÓN DETALLADA: Cómo Funciona `/api/Rrhh/MarcacionAsistencia/status/2`

## 🎯 La Consulta

```bash
GET https://127.0.0.1:7209/api/Rrhh/MarcacionAsistencia/status/2
Authorization: Bearer <token>
```

**Parámetro:** `trabajadorId = 2`

---

## 🔍 Paso a Paso: Qué Sucede en el Backend

### PASO 1: Obtener ASIGNACION_TURNO

```csharp
var asignacion = await _context.AsignacionesTurno
    .Include(a => a.Turno)
        .ThenInclude(t => t!.HorariosTurno!)
            .ThenInclude(ht => ht.HorariosDetalle)
    .Include(a => a.HorarioTurno)
        .ThenInclude(ht => ht!.HorariosDetalle)
    .FirstOrDefaultAsync(a =>
        a.TrabajadorId == 2 &&  // ← Trabajador ID 2
        a.FechaInicioVigencia <= DateOnly.FromDateTime(now.Date) &&
        (a.FechaFinVigencia == null || a.FechaFinVigencia >= DateOnly.FromDateTime(now.Date)) &&
        a.EsVigente == true);
```

**Query SQL generado:**
```sql
SELECT at.*, t.*, ht.*, hd.*, ht2.*, hd2.*
FROM ASIGNACIONES_TURNO at
INNER JOIN TURNOS t ON at.id_turno = t.id_turno
LEFT JOIN HORARIOS_TURNO ht ON t.id_turno = ht.id_turno
LEFT JOIN HORARIOS_DETALLE hd ON ht.id_horario_turno = hd.id_horario_turno
LEFT JOIN HORARIOS_TURNO ht2 ON at.id_horario_turno = ht2.id_horario_turno
LEFT JOIN HORARIOS_DETALLE hd2 ON ht2.id_horario_turno = hd2.id_horario_turno
WHERE at.id_trabajador = 2
  AND at.fecha_inicio_vigencia <= '2026-03-20'
  AND (at.fecha_fin_vigencia IS NULL OR at.fecha_fin_vigencia >= '2026-03-20')
  AND at.es_vigente = 1
```

---

## 📋 EJEMPLO REAL: Trabajador 2 (ROTATIVO)

### Datos en BD

```
ASIGNACIONES_TURNO:
├─ id: 101
├─ id_trabajador: 2
├─ id_turno: 2 (ROTATIVO)
├─ id_horario_turno: NULL  ← ¡CLAVE! NO tiene horario específico = ROTATIVO
├─ fecha_inicio_vigencia: 2025-01-01
├─ fecha_fin_vigencia: NULL (indefinida)
└─ es_vigente: 1

TURNOS (ID 2):
├─ nombre_codigo: "ROTATIVO"
└─ HORARIOS_TURNO asociados:
   ├─ ID 5: "Mañana 09:00-17:00"
   ├─ ID 6: "Tarde 14:00-22:00"
   └─ ID 7: "Noche 22:00-06:00"
```

---

## 🎯 PASO 2: Determinar si es FIJO o ROTATIVO

```csharp
// En el código:
if (asignacion?.HorarioTurnoId.HasValue == true)
{
    // FIJO: Tiene valor específico
    horarioTurno = asignacion.HorarioTurno;
}
else
{
    // ROTATIVO: Es NULL
    // → Buscar en PROGRAMACION_TURNOS_SEMANAL
}
```

**Para Trabajador 2:**
- `asignacion.HorarioTurnoId` = `NULL`
- ✅ **Es ROTATIVO**

---

## 🔄 PASO 3: Buscar en PROGRAMACION_TURNOS_SEMANAL

```csharp
var programacionHoy = await _context.ProgramacionTurnosSemanal
    .Include(p => p.HorarioTurno!)
        .ThenInclude(ht => ht.HorariosDetalle)
    .FirstOrDefaultAsync(p =>
        p.TrabajadorId == 2 &&  // ← Trabajador ID 2
        p.Fecha == DateOnly.FromDateTime(today));  // ← Hoy (2026-03-20)
```

**Query SQL:**
```sql
SELECT pts.*, ht.*, hd.*
FROM PROGRAMACION_TURNOS_SEMANAL pts
LEFT JOIN HORARIOS_TURNO ht ON pts.id_horario_turno = ht.id_horario_turno
LEFT JOIN HORARIOS_DETALLE hd ON ht.id_horario_turno = hd.id_horario_turno
WHERE pts.id_trabajador = 2
  AND pts.fecha = '2026-03-20'
```

---

## 📊 ESCENARIOS POSIBLES

### ESCENARIO A: Hay PROGRAMACION_TURNOS_SEMANAL para HOY

```
PROGRAMACION_TURNOS_SEMANAL (2026-03-20):
├─ id_trabajador: 2
├─ fecha: 2026-03-20
├─ id_horario_turno: 6 ← TARDE
├─ es_descanso: 0
├─ es_dia_boleta: 0
└─ es_vacaciones: 0

HORARIOS_TURNO (ID 6):
├─ nombre_horario: "Tarde 14:00-22:00"
└─ HorariosDetalle:
   └─ DiaSemana: "Lun-Vie"
      ├─ hora_inicio: 14:00
      └─ hora_fin: 22:00
```

**¿Qué sucede?**
```csharp
if (programacionHoy?.HorarioTurno != null)
{
    horarioTurno = programacionHoy.HorarioTurno;  // ← Usa ID 6 (TARDE)
}
```

**Response:**
```json
{
  "success": true,
  "trabajadorId": 2,
  "horarioProgramado": "14:00 - 22:00",  // ← DEL DÍA (ROTATIVO)
  "puedeMarcarEntrada": true,
  "puedeMarcarSalida": false
}
```

---

### ESCENARIO B: NO hay PROGRAMACION_TURNOS_SEMANAL para HOY

```
PROGRAMACION_TURNOS_SEMANAL:
└─ (No existe registro para Trabajador 2, 2026-03-20)
```

**¿Qué sucede?**
```csharp
else
{
    // Si no hay programación diaria, usar el primer horario activo del turno
    horarioTurno = turno.HorariosTurno?.FirstOrDefault(ht => ht.EsActivo == true);
    // ← Usa ID 5 (Mañana, el primero activo)
}
```

**Response:**
```json
{
  "success": true,
  "trabajadorId": 2,
  "horarioProgramado": "09:00 - 17:00",  // ← FALLBACK (primer horario)
  "puedeMarcarEntrada": true,
  "puedeMarcarSalida": false
}
```

---

### ESCENARIO C: DESCANSO en PROGRAMACION_TURNOS_SEMANAL

```
PROGRAMACION_TURNOS_SEMANAL (2026-03-20):
├─ id_trabajador: 2
├─ fecha: 2026-03-20
├─ id_horario_turno: 6
├─ es_descanso: 1  ← ¡DESCANSO!
├─ es_dia_boleta: 0
└─ es_vacaciones: 0
```

**¿Qué sucede en MarcacionAsistenciaService.AddMarcacionAsync()?**
```csharp
var shiftContext = await ResolveShiftContextAsync(2, now, ...);

if (!shiftContext.HasAssignedShift)
{
    return new MarcacionResponse 
    { 
        Success = false, 
        Code = "ERROR_NO_TURNO", 
        Message = "No tiene un turno asignado para la fecha actual." 
    };
}
```

**Response al intentar marcar:**
```json
{
  "success": false,
  "code": "ERROR_NO_TURNO",
  "message": "No tiene un turno asignado para la fecha actual.",
  "detail": "Es descanso programado"
}
```

**Response al consultar status:**
```json
{
  "success": false,
  "code": "ERROR_INTERNO",
  "message": "No se puede consultar el estado en descanso"
}
```

---

## 🔄 Flujo Completo Visualizado

```
GET /api/Rrhh/MarcacionAsistencia/status/2
│
├─ PASO 1: Obtener ASIGNACION_TURNO (trabajadorId=2)
│  └─ Encontrado: id_horario_turno = NULL (ROTATIVO)
│
├─ PASO 2: ¿Es FIJO o ROTATIVO?
│  ├─ if (HorarioTurnoId != NULL) → FIJO
│  └─ else → ROTATIVO ✅ (nuestro caso)
│
├─ PASO 3: Buscar PROGRAMACION_TURNOS_SEMANAL
│  └─ SELECT ... WHERE id_trabajador=2 AND fecha='2026-03-20'
│
├─ RESULTADO de búsqueda:
│  ├─ Encontrado (ESCENARIO A) → Usar horario de ese día
│  ├─ No encontrado (ESCENARIO B) → Usar primer horario del turno
│  └─ Encontrado con descanso (ESCENARIO C) → NO PUEDE MARCAR
│
└─ RESPONSE: JSON con horario correcto del día
```

---

## 💻 Código Completo en Contexto

```csharp
public async Task<TimeWorkedDto> CalculateTimeWorkedAsync(int trabajadorId)
{
    var today = DateTime.Today;  // 2026-03-20
    var now = DateTime.Now;      // 2026-03-20 09:45:30

    // PASO 1 & 2 & 3: Todo en ResolveShiftContextAsync
    var shiftContext = await ResolveShiftContextAsync(
        trabajadorId: 2,
        now: now,
        includeTodayFallback: true,
        includeDefaultFallback: true);

    // Aquí ya tenemos el horario correcto del día
    var scheduledStart = shiftContext.ScheduledStart ?? today.Add(DefaultStartTime);
    var scheduledEnd = shiftContext.ScheduledEnd ?? today.Add(DefaultEndTime);

    // Obtener marcaciones
    var calculatedWindowMarks = await _context.MarcacionesAsistencia
        .Where(m => m.TrabajadorId == 2 && m.FechaHora >= scheduledStart && m.FechaHora <= scheduledEnd)
        .OrderBy(m => m.FechaHora)
        .ToListAsync();

    // Calcular tiempo trabajado
    var lastEntry = calculatedWindowMarks.FirstOrDefault(m => m.TipoMarcacion == "ENTRADA");
    var lastExit = lastEntry == null ? null 
        : calculatedWindowMarks.LastOrDefault(m => m.TipoMarcacion == "SALIDA" && m.FechaHora > lastEntry.FechaHora);

    TimeSpan timeWorked = lastEntry == null ? TimeSpan.Zero
        : lastExit == null ? (now - lastEntry.FechaHora)
        : (lastExit.FechaHora - lastEntry.FechaHora);

    // RESPONSE
    return new TimeWorkedDto
    {
        ScheduledTime = $"({scheduledStart:HH:mm} - {scheduledEnd:HH:mm})",  // ← Horario del día
        TimeWorkedMinutes = timeWorked.TotalMinutes,
        TimeWorkedFormatted = $"{Math.Floor(timeWorked.TotalHours)}h {timeWorked.Minutes}m",
        EntryRegisteredAt = lastEntry?.FechaHora,
        ExitRegisteredAt = lastExit?.FechaHora,
        StatusMessage = "Cálculo realizado correctamente."
    };
}
```

---

## 📊 Comparación: FIJO vs ROTATIVO

### Trabajador 1 (FIJO)

```
Request: GET /api/Rrhh/MarcacionAsistencia/status/1

ASIGNACIONES_TURNO:
├─ id_horario_turno: 5 ✅ (tiene valor específico)

Lógica:
└─ if (HorarioTurnoId.HasValue) → Usar ID 5 siempre

Resultado:
├─ 2026-03-20: Horario 09:00-17:00
├─ 2026-03-21: Horario 09:00-17:00 (igual)
├─ 2026-03-22: Horario 09:00-17:00 (igual)
└─ ✅ SIEMPRE el mismo horario
```

### Trabajador 2 (ROTATIVO)

```
Request: GET /api/Rrhh/MarcacionAsistencia/status/2

ASIGNACIONES_TURNO:
├─ id_horario_turno: NULL ❌ (no tiene valor)

Lógica:
└─ else → Buscar en PROGRAMACION_TURNOS_SEMANAL

Resultado:
├─ 2026-03-20: PROGRAMACION → Horario 14:00-22:00
├─ 2026-03-21: PROGRAMACION → Horario 22:00-06:00
├─ 2026-03-22: SIN PROGRAMACIÓN → Horario 09:00-17:00 (fallback)
└─ ✅ CAMBIA según la programación semanal
```

---

## 🎯 Resumen Técnico

| Aspecto | FIJO | ROTATIVO |
|---------|------|----------|
| **HorarioTurnoId en ASIGNACION** | ✅ Tiene valor | ❌ NULL |
| **Condición en código** | `if (HasValue)` | `else` |
| **Fuente del horario** | ASIGNACION_TURNO | PROGRAMACION_TURNOS_SEMANAL |
| **¿Cambia cada día?** | ❌ No, siempre igual | ✅ Sí, cada día |
| **Query secundaria** | Ninguna | Búsqueda en PROGRAMACION_TURNOS_SEMANAL |

---

## ✅ Conclusión

Cuando llamas a `/api/Rrhh/MarcacionAsistencia/status/2`:

1. **El código verifica** si `ASIGNACION_TURNO.id_horario_turno` tiene valor
2. **Si es NULL** (ROTATIVO) → Busca en `PROGRAMACION_TURNOS_SEMANAL` para ese día
3. **Si encuentra programación** → Usa ese horario
4. **Si NO encuentra** → Usa primer horario del turno como fallback
5. **El endpoint retorna** el horario correcto para ese día específico

Por eso los **turnos rotativos pueden cambiar su horario**: cada día se consulta `PROGRAMACION_TURNOS_SEMANAL` y puede tener un horario diferente.

---

**La mejora implementada permite que los turnos rotativos sean flexibles y cambien según lo programado.** ✅
