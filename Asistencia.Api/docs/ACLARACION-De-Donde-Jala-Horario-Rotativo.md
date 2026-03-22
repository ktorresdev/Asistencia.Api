# ❓ ACLARACIÓN: ¿De Dónde Jala el Horario en ROTATIVO?

## 🎯 La Pregunta

"Por más que le haya asignado el turno tarde (14-22), ¿por qué se modificó? ¿De dónde lo jala?"

---

## ✅ La Respuesta Corta

El horario **NO se modifica** en ASIGNACION_TURNO. Se obtiene de **dos lugares diferentes** dependiendo del tipo:

```
FIJO → ASIGNACION_TURNO.HorarioTurno (permanente)
ROTATIVO → PROGRAMACION_TURNOS_SEMANAL (cada día)
```

---

## 📊 EXPLICACIÓN VISUAL

### ASIGNACION_TURNO (Una sola vez, permanente)

```sql
-- Esto se guarda UNA SOLA VEZ y NO CAMBIA
SELECT id_trabajador, id_horario_turno FROM ASIGNACIONES_TURNO
WHERE id_trabajador = 2;

RESULTADO:
├─ id_trabajador: 2
├─ id_turno: 2 (ROTATIVO)
└─ id_horario_turno: NULL  ← AQUÍ dice NULL = "Buscar cada día"
```

**¿Cambia?** ❌ NO. Una vez que lo asignas, se queda así.

---

### PROGRAMACION_TURNOS_SEMANAL (Cambia cada día)

```sql
-- Esto CAMBIA cada día según lo que programes
SELECT id_trabajador, fecha, id_horario_turno FROM PROGRAMACION_TURNOS_SEMANAL
WHERE id_trabajador = 2
ORDER BY fecha;

RESULTADO:
├─ 2026-03-20: id_horario_turno = 6 (TARDE 14:00-22:00)
├─ 2026-03-21: id_horario_turno = 7 (NOCHE 22:00-06:00)
├─ 2026-03-22: (sin registro) → fallback a primer horario
└─ 2026-03-23: id_horario_turno = 5 (MAÑANA 09:00-17:00)
```

**¿Cambia?** ✅ SÍ. Cada semana puedes programar horarios diferentes.

---

## 🎯 FLUJO: ¿De Dónde Jala el Horario?

```
GET /api/Rrhh/MarcacionAsistencia/status/2 (Hoy: 2026-03-20)
│
├─ PASO 1: ¿Qué dice ASIGNACION_TURNO?
│  ├─ id_horario_turno = NULL
│  └─ "Es ROTATIVO, busca en PROGRAMACION_TURNOS_SEMANAL"
│
└─ PASO 2: Busca en PROGRAMACION_TURNOS_SEMANAL
   ├─ WHERE id_trabajador = 2 AND fecha = '2026-03-20'
   └─ ENCUENTRA: id_horario_turno = 6
      └─ Obtiene: HORARIO_TURNO ID 6 = "Tarde 14:00-22:00"
      
RESULTADO: Retorna "14:00 - 22:00"
```

---

## 💡 LA CLAVE

**ASIGNACION_TURNO.id_horario_turno = NULL significa:**

> "Este trabajador NO tiene un horario FIJO. Busca su horario en PROGRAMACION_TURNOS_SEMANAL cada día."

**PROGRAMACION_TURNOS_SEMANAL es el lugar donde cambias el horario cada semana.**

---

## 📋 EJEMPLO REAL

### Día 1: Asignación

```
POST /api/AsignacionTurno (Una sola vez)
{
  "trabajadorId": 2,
  "turnoId": 2,           // ROTATIVO
  "horarioTurnoId": null, // NULL = "No tiene horario fijo"
  "fechaInicio": "2025-01-01"
}

RESULTADO EN BD:
ASIGNACIONES_TURNO:
├─ id: 101
├─ id_trabajador: 2
├─ id_turno: 2
└─ id_horario_turno: NULL  ← GRABADO. NO CAMBIA NUNCA.
```

---

### Día 2: Programación Semanal (Semana 1)

```
POST /api/ProgramacionTurnoSemanal (Cada semana)
[
  {
    "trabajadorId": 2,
    "fecha": "2026-03-20",
    "idHorarioTurno": 6  ← TARDE
  },
  {
    "trabajadorId": 2,
    "fecha": "2026-03-21",
    "idHorarioTurno": 7  ← NOCHE
  }
]

RESULTADO EN BD:
PROGRAMACION_TURNOS_SEMANAL:
├─ 2026-03-20: id_horario_turno = 6 (TARDE)
└─ 2026-03-21: id_horario_turno = 7 (NOCHE)
```

---

### Día 3: Consultar Status HOY

```
GET /api/Rrhh/MarcacionAsistencia/status/2 (Hoy: 2026-03-20)

LÓGICA:
1. Busca ASIGNACION_TURNO → id_horario_turno = NULL
2. Dice "Es ROTATIVO"
3. Busca PROGRAMACION_TURNOS_SEMANAL (2026-03-20)
4. ENCUENTRA: id_horario_turno = 6
5. Obtiene detalles: "Tarde 14:00-22:00"

RESPONSE:
{
  "horarioProgramado": "14:00 - 22:00"  ← DE PROGRAMACION_TURNOS_SEMANAL
}
```

---

### Día 4: Cambiar Programación (Semana 2)

```
PUT /api/ProgramacionTurnoSemanal/2026-03-20
{
  "idHorarioTurno": 5  ← CAMBIO A MAÑANA
}

RESULTADO EN BD (ACTUALIZADO):
PROGRAMACION_TURNOS_SEMANAL:
├─ 2026-03-20: id_horario_turno = 5 (MAÑANA) ← CAMBIÓ

ASIGNACION_TURNO:
├─ id_horario_turno: NULL  ← NO CAMBIÓ (sigue NULL)
```

---

### Día 5: Consultar Status de NUEVO

```
GET /api/Rrhh/MarcacionAsistencia/status/2 (Hoy: 2026-03-20)

LÓGICA (IDÉNTICA):
1. Busca ASIGNACION_TURNO → id_horario_turno = NULL
2. Dice "Es ROTATIVO"
3. Busca PROGRAMACION_TURNOS_SEMANAL (2026-03-20)
4. ENCUENTRA: id_horario_turno = 5  ← CAMBIÓ desde Día 4
5. Obtiene detalles: "Mañana 09:00-17:00"

RESPONSE:
{
  "horarioProgramado": "09:00 - 17:00"  ← AHORA ES DIFERENTE
}
```

---

## 🔄 RESUMEN: DOS TABLAS DIFERENTES

```
┌──────────────────────────────────┬──────────────────────────────────┐
│ ASIGNACION_TURNO                 │ PROGRAMACION_TURNOS_SEMANAL      │
│ (Una sola vez, permanente)       │ (Cada semana, flexible)          │
├──────────────────────────────────┼──────────────────────────────────┤
│ ¿Cuándo se actualiza?            │ ¿Cuándo se actualiza?            │
│ → Una sola vez (al asignar)      │ → Cada semana                    │
│                                  │                                  │
│ ¿Qué contiene?                   │ ¿Qué contiene?                   │
│ → Información del turno base     │ → Horario específico de cada día │
│ → id_turno, id_horario_turno     │ → id_horario_turno, fecha        │
│                                  │                                  │
│ ¿Para qué sirve?                 │ ¿Para qué sirve?                 │
│ → Decir "¿Es FIJO o ROTATIVO?"   │ → Decir "¿Qué horario ESTE DÍA?"│
│                                  │                                  │
│ FIJO: id_horario_turno = 5       │ (No aplica para FIJO)            │
│ ROTATIVO: id_horario_turno = NULL│ ROTATIVO: id_horario_turno puede │
│           → "Busca cada día"     │                cambiar cada día  │
└──────────────────────────────────┴──────────────────────────────────┘
```

---

## 🎯 La Diferencia Clave

### FIJO (id_horario_turno = 5)

```
ASIGNACION_TURNO:
├─ id_horario_turno: 5  ← ESPECIFICADO

PROGRAMACION_TURNOS_SEMANAL:
└─ (No se consulta, se ignora)

Resultado: SIEMPRE id_horario_turno = 5
```

### ROTATIVO (id_horario_turno = NULL)

```
ASIGNACION_TURNO:
├─ id_horario_turno: NULL  ← NO especificado

PROGRAMACION_TURNOS_SEMANAL:
├─ 2026-03-20: id_horario_turno = 6
├─ 2026-03-21: id_horario_turno = 7
├─ 2026-03-22: (sin registro)
└─ ...

Resultado: CAMBIA según lo que hay en PROGRAMACION_TURNOS_SEMANAL
```

---

## ❌ LO QUE NO PASA

❌ **NO se modifica ASIGNACION_TURNO**
```
// Esto NO sucede:
UPDATE ASIGNACIONES_TURNO 
SET id_horario_turno = 6 
WHERE id_trabajador = 2;
```

❌ **NO se crea un nuevo registro en ASIGNACION_TURNO**
```
// Esto NO sucede:
INSERT INTO ASIGNACIONES_TURNO (id_trabajador, id_turno, id_horario_turno, ...)
VALUES (2, 2, 6, ...);
```

---

## ✅ LO QUE SÍ PASA

✅ **Se crea un registro EN PROGRAMACION_TURNOS_SEMANAL**
```
-- Esto SÍ sucede:
INSERT INTO PROGRAMACION_TURNOS_SEMANAL (id_trabajador, fecha, id_horario_turno)
VALUES (2, '2026-03-20', 6);
```

✅ **Cada día se CONSULTA PROGRAMACION_TURNOS_SEMANAL**
```
-- Cada vez que consultas /status/2:
SELECT id_horario_turno 
FROM PROGRAMACION_TURNOS_SEMANAL
WHERE id_trabajador = 2 AND fecha = '2026-03-20';
-- Retorna: 6 (o NULL si no hay registro)
```

---

## 📌 CONCLUSIÓN

**¿De dónde jala el horario?**

Para **FIJO:** De ASIGNACION_TURNO.id_horario_turno (una sola vez)

Para **ROTATIVO:** De PROGRAMACION_TURNOS_SEMANAL.id_horario_turno (cada día)

**ASIGNACION_TURNO NUNCA CAMBIA.** Solo PROGRAMACION_TURNOS_SEMANAL cambia.

---

**Espero que esto aclare de dónde jala el horario en rotativo.** ✅
