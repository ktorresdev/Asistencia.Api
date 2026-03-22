# 📊 DIAGRAMA VISUAL: Lógica de Turnos FIJO vs ROTATIVO

## 🔄 Flujo en ResolveShiftContextAsync()

```
┌─────────────────────────────────────────────────────────────┐
│ Obtener ASIGNACION_TURNO del trabajador                   │
│ Include: Turno → HorariosTurno → HorariosDetalle         │
│ Include: HorarioTurno → HorariosDetalle                  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ✓ Encontrado
                     │
┌────────────────────▼────────────────────────────────────────┐
│ ¿Tiene TURNO?                                             │
├─────────────────────────────────────────────────────────────┤
│ NO  → return { HasAssignedShift = false }                │
│                                                            │
│ SÍ  → Continuar                                          │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ ¿Tiene HorarioTurnoId en ASIGNACION_TURNO?              │
├─────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌──────────────────────┐      ┌────────────────────────┐ │
│  │ SÍ                   │      │ NO                     │ │
│  │ (TURNO FIJO)         │      │ (TURNO ROTATIVO)       │ │
│  └──────────────┬───────┘      └────────┬───────────────┘ │
│                │                        │                 │
│           Usar el                   Buscar en             │
│           HorarioTurno              PROGRAMACION_        │
│           específico                TURNOS_SEMANAL       │
│           de ASIGNACION_TURNO       para HOY             │
│                │                        │                 │
│           ✓ Siempre igual          ¿Encontrado?         │
│                │                    │          │          │
│                │                    ✓          ❌         │
│                │                    │          │          │
│                │               Usar ese   Usar primer    │
│                │               horario    horario activo │
│                │                 del       del Turno    │
│                │                 día     (FALLBACK)     │
│                │                    │          │         │
└─────────────────┼────────────────────┼──────────┼────────┘
                  │                    │          │
       ┌──────────▼────────────┬───────▼──────────▼────────┐
       │ Retornar ShiftContext │                           │
       │ con:                  │                           │
       │ - HorarioTurno        │                           │
       │ - HorariosDetalle     │                           │
       │ - Horarios Entrada/Salida                         │
       │ - Ventanas de tiempo  │                           │
       └───────────────────────┴───────────────────────────┘
```

---

## 📋 Comparación: FIJO vs ROTATIVO

```
┌────────────────────────────────────────────────────────────────┐
│ TURNO FIJO                     │ TURNO ROTATIVO                │
├────────────────────────────────┼───────────────────────────────┤
│ ASIGNACION_TURNO:              │ ASIGNACION_TURNO:             │
│ ├─ TurnoId: 1                  │ ├─ TurnoId: 2                 │
│ ├─ HorarioTurnoId: 5 (✓)       │ ├─ HorarioTurnoId: null (❌) │
│ └─ es_vigente: true            │ └─ es_vigente: true           │
│                                │                               │
│ HORARIO SIEMPRE:               │ HORARIO VARÍA:                │
│ Lun-Vie: 09:00-17:00           │                               │
│ Sab-Dom: Descanso              │ Lun 2026-03-16: 09:00-17:00  │
│                                │ Mar 2026-03-17: 14:00-22:00  │
│                                │ Mié 2026-03-18: 22:00-06:00  │
│                                │                               │
│ FUENTE DE HORARIO:             │ FUENTE DE HORARIO:            │
│ ASIGNACION_TURNO.HorarioTurno │ PROGRAMACION_TURNOS_SEMANAL  │
│                                │ (o fallback a Turno)          │
├────────────────────────────────┼───────────────────────────────┤
│ CAMBIOS:                       │ CAMBIOS:                      │
│ Nunca (permanente)             │ Cada semana (flexible)        │
│                                │                               │
│ SI NECESITAS CAMBIAR:          │ SI NECESITAS CAMBIAR:         │
│ Editar ASIGNACION_TURNO        │ Crear PROGRAMACION_TURNOS_   │
│ (permanente)                   │ SEMANAL para ese día          │
└────────────────────────────────┴───────────────────────────────┘
```

---

## 🔍 Ejemplo de Datos

### Escenario: 2026-03-20 (Viernes)

#### Trabajador A (FIJO)
```
ASIGNACION_TURNO (Trabajador A):
├─ id: 100
├─ id_trabajador: 1
├─ id_turno: 1 (TURNO_MAÑANA)
├─ id_horario_turno: 5 ✅ (ESPECIFICADO)
└─ es_vigente: true

HORARIOS_TURNO (ID 5):
├─ nombre_horario: "Mañana 09:00-17:00"
└─ HorariosDetalle:
   ├─ Lun-Vie: 09:00-17:00
   └─ Sab-Dom: Descanso

EN 2026-03-20 (Viernes):
└─ Usa: HORARIO FIJO 09:00-17:00 (de ASIGNACION_TURNO)
```

#### Trabajador B (ROTATIVO)
```
ASIGNACION_TURNO (Trabajador B):
├─ id: 101
├─ id_trabajador: 2
├─ id_turno: 2 (TURNO_ROTATIVO)
├─ id_horario_turno: null ❌ (NO ESPECIFICADO)
└─ es_vigente: true

PROGRAMACION_TURNOS_SEMANAL (2026-03-20):
├─ id_trabajador: 2
├─ fecha: 2026-03-20
├─ id_horario_turno: 6
└─ es_descanso: false

HORARIOS_TURNO (ID 6):
├─ nombre_horario: "Tarde 14:00-22:00"
└─ HorariosDetalle:
   └─ Lun-Vie: 14:00-22:00

EN 2026-03-20 (Viernes):
└─ Usa: HORARIO DEL DÍA 14:00-22:00 (de PROGRAMACION_TURNOS_SEMANAL)
```

---

## 🎯 Código Decisión

```csharp
// Paso 1: Verificar tipo de turno
if (asignacion?.HorarioTurnoId.HasValue == true)
{
    // FIJO: Tiene horario específico asignado
    // └─ Usar ASIGNACION_TURNO.HorarioTurno
    horarioTurno = asignacion.HorarioTurno;
}
else
{
    // ROTATIVO: Sin horario específico, buscar en programación diaria
    var programacionHoy = await _context.ProgramacionTurnosSemanal
        .FirstOrDefaultAsync(p =>
            p.TrabajadorId == trabajadorId &&
            p.Fecha == DateOnly.FromDateTime(today));

    if (programacionHoy?.HorarioTurno != null)
    {
        // ROTATIVO PROGRAMADO: Existe programación para hoy
        // └─ Usar PROGRAMACION_TURNOS_SEMANAL.HorarioTurno
        horarioTurno = programacionHoy.HorarioTurno;
    }
    else
    {
        // ROTATIVO SIN PROGRAMACIÓN: Fallback
        // └─ Usar primer horario activo del TURNO
        horarioTurno = turno.HorariosTurno?.FirstOrDefault(ht => ht.EsActivo);
    }
}
```

---

## 📊 Matriz de Decisión

| HorarioTurnoId | PROG_HOY | Resultado | Fuente |
|---|---|---|---|
| ✅ Sí | (ignorado) | FIJO | ASIGNACION_TURNO |
| ❌ No | ✅ Sí | ROTATIVO día | PROGRAMACION_TURNOS_SEMANAL |
| ❌ No | ❌ No | ROTATIVO fallback | TURNO.HorariosTurno |

---

**Diagrama de flujo implementado correctamente.** ✅
