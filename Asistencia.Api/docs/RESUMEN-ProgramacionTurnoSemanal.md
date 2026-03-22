# ✅ Resumen: Endpoint Asignar Programación Semanal

## 🎯 ¿Qué es?

Endpoint que permite **grabar la programación día a día** de los turnos de trabajadores para una semana completa.

**Tabla:** `PROGRAMACION_TURNOS_SEMANAL`
**Acción:** UPSERT (elimina existentes, graba nuevos)

---

## 📌 Endpoint

```
POST /api/Rrhh/ProgramacionSemanal
```

---

## 📤 Body Mínimo

```json
{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "programaciones": [
    {
      "trabajadorId": 1,
      "fecha": "2026-03-16",
      "idHorarioTurno": 5
    }
  ]
}
```

---

## 📥 Response

```json
{
  "ok": true,
  "mensaje": "Programación semanal grabada",
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "registrosGrabados": 7
}
```

---

## 🗄️ Base de Datos

**Registros generados:**

```
Para 1 trabajador, 7 días:
  Lunes    → 1 registro
  Martes   → 1 registro
  Miércoles → 1 registro
  Jueves   → 1 registro
  Viernes  → 1 registro
  Sábado   → 1 registro
  Domingo  → 1 registro
  
  Total: 7 registros
```

Si cargas 3 trabajadores × 7 días = **21 registros**

---

## 📋 Campos Opcionales

| Campo | Defecto | Significado |
|-------|---------|-------------|
| **esDescanso** | false | Día de descanso semanal |
| **esDiaBoleta** | false | Día de boleta/día libre |
| **esVacaciones** | false | Día de vacaciones |

---

## ✨ Características

✅ **UPSERT automático** - Carga la misma semana 2 veces = actualiza
✅ **Validación** - Verifica que trabajadores y horarios existan
✅ **Flexible** - Cada día puede tener un horario diferente
✅ **Masivo** - Carga múltiples trabajadores en 1 request
✅ **Seguro** - Requiere autenticación (ADMIN/SUPERADMIN)

---

## 🔄 Flujo

```
1. Envías JSON con programación semanal
         ↓
2. Sistema valida datos
         ↓
3. Elimina registros previos (rango de fechas)
         ↓
4. Graba los nuevos registros
         ↓
5. Retorna cantidad de registros grabados
```

---

## 💡 Diferencia vs AsignacionTurno

**AsignacionTurno:** Turno fijo para un período largo
```
Ej: Trabajador siempre turno Mañana (9am-5pm)
    desde 01-01-2026 hasta 31-12-2026
```

**ProgramacionSemanal:** Programación flexible día a día
```
Ej: Lunes Turno A, Martes Turno B, Miércoles Turno A...
    diferente cada semana
```

---

## 📊 Tabla de Resumen

| Aspecto | Detalle |
|---------|---------|
| **URL** | `POST /api/Rrhh/ProgramacionSemanal` |
| **Body** | fechaInicio, fechaFin, programaciones[] |
| **Tabla BD** | `PROGRAMACION_TURNOS_SEMANAL` |
| **Registros** | 1 por día, por trabajador |
| **Validación** | Trabajadores y horarios deben existir |
| **Autorización** | ADMIN, SUPERADMIN |
| **Respuesta** | JSON con count de registros |
| **Comportamiento** | Reemplaza registros existentes |

---

## 🚀 Ejemplo de Uso Real

### Escenario: Programar una semana completa para 2 turneros

**Semana del 16 al 22 de Marzo 2026**

**Trabajador 1 (Juan):**
- L: Turno Mañana, descansa
- M-V: Turno Mañana, trabaja
- S-D: Boleta/Descanso

**Trabajador 2 (Maria):**
- L: Turno Noche, trabaja  
- M: Descansa
- M-V: Turno Noche, trabaja
- S-D: Trabaja

```bash
POST /api/Rrhh/ProgramacionSemanal

{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "programaciones": [
    // Juan
    { "trabajadorId": 1, "fecha": "2026-03-16", "idHorarioTurno": 5, "esDescanso": true },
    { "trabajadorId": 1, "fecha": "2026-03-17", "idHorarioTurno": 5 },
    { "trabajadorId": 1, "fecha": "2026-03-18", "idHorarioTurno": 5 },
    { "trabajadorId": 1, "fecha": "2026-03-19", "idHorarioTurno": 5 },
    { "trabajadorId": 1, "fecha": "2026-03-20", "idHorarioTurno": 5 },
    { "trabajadorId": 1, "fecha": "2026-03-21", "idHorarioTurno": 5, "esDiaBoleta": true },
    { "trabajadorId": 1, "fecha": "2026-03-22", "idHorarioTurno": 5, "esDiaBoleta": true },
    
    // Maria
    { "trabajadorId": 2, "fecha": "2026-03-16", "idHorarioTurno": 3 },
    { "trabajadorId": 2, "fecha": "2026-03-17", "idHorarioTurno": 3, "esDescanso": true },
    { "trabajadorId": 2, "fecha": "2026-03-18", "idHorarioTurno": 3 },
    { "trabajadorId": 2, "fecha": "2026-03-19", "idHorarioTurno": 3 },
    { "trabajadorId": 2, "fecha": "2026-03-20", "idHorarioTurno": 3 },
    { "trabajadorId": 2, "fecha": "2026-03-21", "idHorarioTurno": 3 },
    { "trabajadorId": 2, "fecha": "2026-03-22", "idHorarioTurno": 3 }
  ]
}
```

**Response:**
```json
{
  "ok": true,
  "mensaje": "Programación semanal grabada",
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "registrosGrabados": 14
}
```

**BD Resultado:**
```
14 registros insertados en PROGRAMACION_TURNOS_SEMANAL
- 7 para Trabajador 1 (Juan)
- 7 para Trabajador 2 (Maria)
```

---

## 🎓 Conclusión

Este endpoint es la **pieza central** de la gestión semanal de turnos. Permite:

1. ✅ Crear programación inicial
2. ✅ Modificar programación existente
3. ✅ Cambiar horarios día a día
4. ✅ Marcar descansos, boletas, vacaciones
5. ✅ Masificar cambios para múltiples trabajadores

**Es flexible, potente y fácil de usar.** 🚀
