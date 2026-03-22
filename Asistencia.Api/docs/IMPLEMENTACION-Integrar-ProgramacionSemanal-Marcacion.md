# 🚀 GUÍA: Integrar PROGRAMACION_TURNOS_SEMANAL con Marcación

## 🎯 El Objetivo

Que la marcación de asistencia respete:
1. **Horario base** (ASIGNACION_TURNO) - permanente
2. **Cambios diarios** (PROGRAMACION_TURNOS_SEMANAL) - temporal

---

## 📊 Lógica

```
¿Puede marcar hoy?

├─ ¿Tiene PROGRAMACION_TURNOS_SEMANAL para HOY?
│  ├─ ✅ SÍ, es DESCANSO/VACACIONES → ❌ NO PUEDE
│  ├─ ✅ SÍ, tiene HORARIO → USA ESE HORARIO
│  └─ ❌ NO → Continúa
│
└─ Usa ASIGNACION_TURNO (horario base)
   ├─ ✅ Tiene turno asignado → USA ESE
   └─ ❌ NO → ERROR "Sin turno asignado"
```

---

## 💻 Código a Actualizar

**Archivo:** `..\Services\Services\MarcacionAsistenciaService.cs`

**Método:** `ResolveShiftContextAsync()` - Línea 72

**Cambio:** Agregue esto al INICIO del método:

```csharp
private async Task<ShiftContext> ResolveShiftContextAsync(int trabajadorId, DateTime now, bool includeTodayFallback, bool includeDefaultFallback)
{
    var today = now.Date;

    // ✅ NUEVO: Buscar PROGRAMACION_TURNOS_SEMANAL para HOY
    var programacionHoy = await _context.ProgramacionTurnosSemanal
        .Include(p => p.HorarioTurno!)
            .ThenInclude(ht => ht.HorariosDetalle)
        .FirstOrDefaultAsync(p =>
            p.TrabajadorId == trabajadorId &&
            p.Fecha == DateOnly.FromDateTime(today));

    // Si existe y es descanso o vacaciones, NO puede marcar
    if (programacionHoy != null)
    {
        if (programacionHoy.EsDescanso || programacionHoy.EsVacaciones)
        {
            return new ShiftContext
            {
                HasAssignedShift = false,
                HasActiveSchedule = false
            };
        }

        // Si tiene horario definido en la programación semanal, usarlo
        if (programacionHoy.HorarioTurno?.EsActivo == true && 
            programacionHoy.HorarioTurno.HorariosDetalle?.Any() == true)
        {
            var horarioTurno = programacionHoy.HorarioTurno;
            
            // Buscar el detalle del día actual
            var detalleHoy = horarioTurno.HorariosDetalle
                .FirstOrDefault(hd => IsDiaSemanaMatch(hd.DiaSemana, today));

            if (detalleHoy != null)
            {
                var scheduledRange = BuildScheduledRange(detalleHoy, today);
                return new ShiftContext
                {
                    HasAssignedShift = true,
                    HasActiveSchedule = true,
                    ScheduleDetail = detalleHoy,
                    ScheduledStart = scheduledRange.scheduledStart,
                    ScheduledEnd = scheduledRange.scheduledEnd,
                    WindowStart = scheduledRange.scheduledStart.Subtract(EarlyWindowTolerance),
                    WindowEnd = scheduledRange.scheduledEnd.Add(LateWindowTolerance)
                };
            }
        }
    }

    // ✅ RESTO: Si NO hay PROGRAMACION_TURNOS_SEMANAL, usar ASIGNACION_TURNO (código actual)
    var asignacion = await _context.AsignacionesTurno
        .Include(a => a.Turno)
            .ThenInclude(t => t!.HorariosTurno!)
                .ThenInclude(ht => ht.HorariosDetalle)
        .FirstOrDefaultAsync(a =>
            a.TrabajadorId == trabajadorId &&
            a.FechaInicioVigencia <= DateOnly.FromDateTime(now.Date) &&
            (a.FechaFinVigencia == null || a.FechaFinVigencia.Value >= DateOnly.FromDateTime(now.Date)) &&
            a.EsVigente == true);

    // ... resto del código original sigue igual
}
```

---

## ✅ Ejemplo de Flujo

### Escenario 1: Descanso en PROGRAMACION_TURNOS_SEMANAL
```
Hoy: 2026-03-20
Trabajador: Juan

PROGRAMACION_TURNOS_SEMANAL para Juan, 2026-03-20:
├─ es_descanso = true

Resultado:
├─ Return HasAssignedShift = false
├─ Message: "No puede marcar, tiene descanso programado"
└─ ❌ NO PUEDE MARCAR
```

### Escenario 2: Horario DIFERENTE en PROGRAMACION_TURNOS_SEMANAL
```
Hoy: 2026-03-21
Trabajador: María

ASIGNACION_TURNO:
├─ Turno: TURNO_MAÑANA (09:00-17:00)

PROGRAMACION_TURNOS_SEMANAL para María, 2026-03-21:
├─ id_horario_turno: 27 (TURNO_TARDE 14:00-22:00)

Resultado:
├─ USA horario 27 (TARDE)
├─ No usa ASIGNACION_TURNO
└─ ✅ PUEDE MARCAR TARDE (14:00-22:00)
```

### Escenario 3: Sin PROGRAMACION_TURNOS_SEMANAL
```
Hoy: 2026-03-22
Trabajador: Carlos

ASIGNACION_TURNO:
├─ Turno: TURNO_MAÑANA (09:00-17:00)

PROGRAMACION_TURNOS_SEMANAL para Carlos:
├─ No existe registro para 2026-03-22

Resultado:
├─ USA ASIGNACION_TURNO (horario base)
├─ TURNO_MAÑANA (09:00-17:00)
└─ ✅ PUEDE MARCAR MAÑANA
```

---

## 🧪 Testing

### Caso 1: Marcar con Descanso
```bash
POST /api/MarcacionAsistencia
{
  "idTrabajador": 1,
  "latitud": 10.123,
  "longitud": -74.456,
  "foto": "base64..."
}

# Si hay PROGRAMACION_TURNOS_SEMANAL con es_descanso=true
Esperado: 400 "No tiene un turno asignado para hoy" o "En descanso"
```

### Caso 2: Marcar con Horario Override
```bash
POST /api/MarcacionAsistencia
{
  "idTrabajador": 2,
  "latitud": 10.123,
  "longitud": -74.456,
  "foto": "base64..."
}

# Si hay PROGRAMACION_TURNOS_SEMANAL con horario diferente
Esperado: 200 "Marcación registrada" con horario de PROGRAMACION_TURNOS_SEMANAL
```

### Caso 3: Marcar sin PROGRAMACION_TURNOS_SEMANAL
```bash
POST /api/MarcacionAsistencia
{
  "idTrabajador": 3,
  "latitud": 10.123,
  "longitud": -74.456,
  "foto": "base64..."
}

# Si NO hay PROGRAMACION_TURNOS_SEMANAL para hoy
Esperado: 200 "Marcación registrada" con horario de ASIGNACION_TURNO
```

---

## 🎯 Ventajas

| Antes | Ahora |
|-------|-------|
| Solo horario FIJO (ASIGNACION_TURNO) | Horario FIJO + CAMBIOS DIARIOS |
| No respetar descansos programados | ✅ Respeta descansos |
| No cambiar turno por un día | ✅ Puede cambiar turno por día |
| No justificar vacaciones | ✅ Marca como vacaciones |

---

## ⚠️ Considera También

1. **AsistenciaResumenDiario**: Actualizar para considerar PROGRAMACION_TURNOS_SEMANAL
2. **Validación de Geofence**: Mantener igual (no cambia)
3. **Cálculo de Tardanza**: Usar horario correcto (de PROGRAMACION o ASIGNACION)

---

## 🔗 Archivos Relacionados

- `MarcacionAsistenciaService.cs` - Lógica principal
- `MarcacionAsistencia.cs` - Entidad
- `ProgramacionTurnoSemanal.cs` - Entidad para cambios diarios
- `AsignacionTurno.cs` - Entidad para horario base

---

**¿Implemento estos cambios?**
