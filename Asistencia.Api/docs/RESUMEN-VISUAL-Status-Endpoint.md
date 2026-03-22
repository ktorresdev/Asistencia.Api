# 🎯 RESUMEN VISUAL: /api/Rrhh/MarcacionAsistencia/status/2

## 📊 LA CONSULTA

```bash
GET /api/Rrhh/MarcacionAsistencia/status/2
```

**Parámetro:** Trabajador ID = 2  
**Hoy:** 2026-03-20 (Viernes)

---

## 🔍 3 QUERIES QUE SE EJECUTAN

```
┌───────────────────────────────────────────────────────────┐
│ QUERY 1: ¿Tiene turno asignado?                          │
├───────────────────────────────────────────────────────────┤
│ ASIGNACIONES_TURNO WHERE id_trabajador = 2               │
│                                                            │
│ RESULTADO:                                                │
│ ├─ id_horario_turno: NULL  ← ROTATIVO (flexible)         │
│ ├─ id_turno: 2 (ROTATIVO)                                │
│ └─ TURNOS.HorariosTurno: [ID 5, 6, 7]                   │
└───────────────────────────────────────────────────────────┘
                         ↓
        ┌─────────────────────────────────┐
        │ ¿Es FIJO (con valor) o         │
        │  ROTATIVO (NULL)?              │
        │                                 │
        │ → ROTATIVO (NULL)              │
        └────────┬────────────────────────┘
                 ↓
┌───────────────────────────────────────────────────────────┐
│ QUERY 2: ¿Qué horario hoy?                               │
├───────────────────────────────────────────────────────────┤
│ PROGRAMACION_TURNOS_SEMANAL                              │
│ WHERE id_trabajador = 2 AND fecha = '2026-03-20'        │
│                                                            │
│ RESULTADO (3 POSIBILIDADES):                             │
│ ┌─ OPCIÓN A: Encontrado                                  │
│ │  ├─ id_horario_turno: 6                                │
│ │  ├─ horario: "Tarde 14:00-22:00"                       │
│ │  └─ es_descanso: 0                                     │
│ │                                                         │
│ ├─ OPCIÓN B: No encontrado                               │
│ │  └─ Usar FALLBACK: Primer horario del turno (ID 5)   │
│ │                                                         │
│ └─ OPCIÓN C: Descanso/Vacaciones                         │
│    ├─ es_descanso: 1 ← NO PUEDE MARCAR                  │
│    └─ return "ERROR_NO_TURNO"                            │
└───────────────────────────────────────────────────────────┘
                         ↓
        ┌─────────────────────────────────┐
        │ Ya tenemos el horario correcto  │
        │ para HOY                        │
        └────────┬────────────────────────┘
                 ↓
┌───────────────────────────────────────────────────────────┐
│ QUERY 3: ¿Qué marcaciones tiene hoy?                     │
├───────────────────────────────────────────────────────────┤
│ MARCACIONES_ASISTENCIA                                   │
│ WHERE id_trabajador = 2                                  │
│   AND fecha_hora >= '2026-03-20 14:00:00'  ← Del horario│
│   AND fecha_hora <= '2026-03-20 22:00:00'  ← Del horario│
│                                                            │
│ RESULTADO (si es 15:45):                                 │
│ ├─ ENTRADA: 2026-03-20 14:15:30                          │
│ ├─ SALIDA: (ninguna aún)                                 │
│ └─ Tiempo trabajado: 1h 30m                              │
└───────────────────────────────────────────────────────────┘
                         ↓
                    ┌────▼─────────────────┐
                    │ ARMAR RESPONSE JSON  │
                    └────┬─────────────────┘
                         ↓
```

---

## 📋 RESPONSE JSON (OPCIÓN A: Encontrado en PROGRAMACION)

```json
{
  "success": true,
  "trabajadorId": 2,
  
  "horarioProgramado": "14:00 - 22:00",  ← DEL DÍA (QUERY 2)
  
  "marcacionEntrada": "2026-03-20T14:15:30",  ← De QUERY 3
  "marcacionSalida": null,                     ← No ha salido
  "tiempoTrabajadoMinutos": 90.5,
  "tiempoTrabajadoFormato": "1h 30m",
  
  "puedeMarcarEntrada": false,      ← Ya marcó entrada
  "puedeMarcarSalida": true,        ← Puede marcar salida
  "salidaPendiente": true           ← Falta marcar salida
}
```

---

## 📊 COMPARACIÓN: FIJO vs ROTATIVO

```
┌──────────────────────┬──────────────────────┐
│ TRABAJADOR 1 (FIJO)  │ TRABAJADOR 2 (ROTATIVO)
├──────────────────────┼──────────────────────┤
│ id_horario_turno: 5  │ id_horario_turno: NULL
│ (valor específico)   │ (buscar cada día)
├──────────────────────┼──────────────────────┤
│ Query 1: Solo        │ Query 1: Más 2 queries
│ (obtiene todo)       │ (Queries 2 y 3)
├──────────────────────┼──────────────────────┤
│ 2026-03-20:          │ 2026-03-20:
│ Mañana 09:00-17:00   │ Tarde 14:00-22:00
│                      │ (de PROGRAMACION)
│ 2026-03-21:          │
│ Mañana 09:00-17:00   │ 2026-03-21:
│ (igual)              │ Noche 22:00-06:00
│                      │ (diferente)
│ 2026-03-22:          │
│ Mañana 09:00-17:00   │ 2026-03-22:
│ (igual)              │ Sin programación
│                      │ → Fallback Mañana
└──────────────────────┴──────────────────────┘
```

---

## 🎯 LA MAGIA

**¿Por qué el horario puede cambiar en ROTATIVO?**

```
Porque cada día QUERY 2 busca en PROGRAMACION_TURNOS_SEMANAL
y puede retornar un horario diferente.

2026-03-20 → id_horario: 6 (Tarde 14:00-22:00)
2026-03-21 → id_horario: 7 (Noche 22:00-06:00)
2026-03-22 → (sin programación) → Fallback

Todo automático según lo que esté programado.
```

---

## 📝 CÓDIGO CLAVE

```csharp
// La decisión está aquí:
if (asignacion?.HorarioTurnoId.HasValue == true)
{
    // FIJO: Un solo horario siempre
    horarioTurno = asignacion.HorarioTurno;
}
else
{
    // ROTATIVO: Buscar el del día
    var programacionHoy = await _context.ProgramacionTurnosSemanal...
    
    if (programacionHoy?.HorarioTurno != null)
        horarioTurno = programacionHoy.HorarioTurno;  // ← Del día
    else
        horarioTurno = turno.HorariosTurno?.FirstOrDefault();  // ← Fallback
}
```

---

## ✅ RESUMEN

**Cuando llamas a `/status/2`:**

1. ✅ QUERY 1: Obtiene ASIGNACION_TURNO (ve que id_horario_turno = NULL)
2. ✅ QUERY 2: Busca en PROGRAMACION_TURNOS_SEMANAL para hoy
3. ✅ QUERY 3: Obtiene MARCACIONES dentro del horario obtenido
4. ✅ RESPONSE: JSON con el horario correcto + marcaciones

**Resultado:** El endpoint SIEMPRE retorna el horario correcto para ese día.

---

**Así es cómo los turnos rotativos cambian su horario cada día.** ✅

Documentación completa en: `docs\EXPLICACION-DETALLADA-Status-Endpoint.md` y `docs\SQL-REAL-Queries-Status-Endpoint.md`
