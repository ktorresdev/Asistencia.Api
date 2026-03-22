# 🔄 Arquitectura: Dos Modelos de Asignación de Turnos

## 📊 Comparativa

| Aspecto | TRABAJADOR FIJO | TRABAJADOR ROTATIVO |
|--------|-----------------|-------------------|
| **Tabla Principal** | `ASIGNACION_TURNO` | `PROGRAMACION_TURNOS_SEMANAL` |
| **Duración** | Largo plazo (meses/años) | Semanal (flexible) |
| **Horario Base** | ✅ Sí (turno fijo) | ❌ No (cada semana diferente) |
| **Cambios Puntuales** | ✅ Sí (ausencia, cambio de turno) | ✅ Sí (modificable cada semana) |
| **Cobertura** | ✅ Via CoberturaTurno | ✅ Recalcula cada semana |
| **Frecuencia de Cambio** | Raro (cada varios meses) | Frecuente (cada semana) |
| **Caso de Uso** | Administrativos, supervisores | Personal operacional, turneros |

---

## 🏢 MODELO 1: TRABAJADOR FIJO

### Estructura

```
ASIGNACION_TURNO (Tabla)
├── id_asignacion
├── id_trabajador
├── id_turno
├── id_horario_turno ← Horario fijo (ej: 9am-5pm)
├── fecha_inicio_vigencia (ej: 01-01-2026)
├── fecha_fin_vigencia (ej: 31-12-2026)
└── es_vigente = true
```

### Endpoint para Crear

```
POST /api/Rrhh/AsignacionTurno

{
  "trabajadorId": 5,
  "turnoId": 1,
  "horarioTurnoId": 3,
  "fechaInicioVigencia": "2026-01-01",
  "fechaFinVigencia": "2026-12-31",
  "esVigente": true
}
```

### Resultado en BD

```
Trabajador 5 tiene asignado:
- Turno: TURNO_MAÑANA
- Horario: 9am-5pm
- Período: TODO 2026
- Descanso fijo: Cada lunes
```

### Modificaciones (2 Opciones)

#### **Opción A: Ausencia Puntual**
```
Se usa: PROGRAMACION_DESCANSOS
Función: Marca días específicos como descanso/boleta
Ejemplo: Trabajador 5 descansa el 15 de marzo
```

#### **Opción B: Cambio de Turno con Compañero**
```
Se usa: COBERTURA_TURNO
Función: Trabaja con turno del compañero por un día
Ejemplo: Trabajador 5 intercambia turno con Trabajador 8 el 20 de marzo
```

### Flujo para Trabajador FIJO

```
┌─────────────────────────────┐
│ ASIGNACION_TURNO            │
│ (Base: Turno 9am-5pm)       │
└────────────┬────────────────┘
             │
             ├──→ ¿Ausencia? → PROGRAMACION_DESCANSOS
             │
             ├──→ ¿Cambio? → COBERTURA_TURNO
             │
             └──→ MOSTRAR en calendario
```

---

## 🔄 MODELO 2: TRABAJADOR ROTATIVO

### Estructura

```
PROGRAMACION_TURNOS_SEMANAL (Tabla)
├── id
├── id_trabajador
├── fecha (día específico)
├── id_horario_turno ← Puede cambiar cada día
├── es_descanso (true/false)
├── es_dia_boleta (true/false)
├── es_vacaciones (true/false)
└── created_at
```

### Endpoint para Crear

```
POST /api/Rrhh/ProgramacionSemanal

{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "programaciones": [
    {
      "trabajadorId": 10,
      "fecha": "2026-03-16",
      "idHorarioTurno": 1,
      "esDescanso": true
    },
    {
      "trabajadorId": 10,
      "fecha": "2026-03-17",
      "idHorarioTurno": 2,
      "esDescanso": false
    },
    {
      "trabajadorId": 10,
      "fecha": "2026-03-18",
      "idHorarioTurno": 3,
      "esDescanso": false
    }
  ]
}
```

### Resultado en BD

```
Semana 16-22 Marzo 2026 para Trabajador 10:
- Lunes (16): Descanso
- Martes (17): Turno Tarde (5pm-9pm)
- Miércoles (18): Turno Noche (9pm-5am)
- Jueves (19): Turno Mañana (9am-5pm)
- Viernes (20): Turno Mañana (9am-5pm)
- Sábado (21): Boleta
- Domingo (22): Boleta
```

### Modificaciones (1 Opción)

```
Simplemente RECARGA la programación para la siguiente semana
No necesita sistemas adicionales (ausencia, cobertura)
```

### Flujo para Trabajador ROTATIVO

```
┌──────────────────────────────────┐
│ PROGRAMACION_TURNOS_SEMANAL      │
│ (Semana actual: flexible)        │
└────────────┬─────────────────────┘
             │
             ├──→ ¿Cambios? → Recarga la siguiente semana
             │
             └──→ MOSTRAR en calendario (7 días)
```

---

## 🎯 ¿Cuándo Usar Cada Uno?

### ✅ Usa ASIGNACION_TURNO (FIJO) Si:

```
□ El trabajador tiene un horario base que NO cambia
□ Los cambios son EXCEPCIONALES y puntuales
□ Necesitas historial a largo plazo
□ El trabajador es administrativo o supervisor
□ Cambios cada varios meses

Ejemplo:
Juan es administrativo
- Base: 9am-5pm todos los días hábiles
- Cambios: Ocasionalmente ausente un día
```

### ✅ Usa PROGRAMACION_TURNOS_SEMANAL (ROTATIVO) Si:

```
□ El trabajador tiene horarios DIFERENTES cada semana
□ Necesitas máxima flexibilidad
□ El trabajador es operacional/turnero
□ Los cambios son FRECUENTES (cada semana)
□ Requiere descansos/boletas variables

Ejemplo:
María es turnera
- Lunes: Turno Mañana
- Martes: Descanso
- Miércoles: Turno Noche
- Próxima semana: completamente diferente
```

---

## 🔗 Integración: Cómo Se Complementan

### **Escenario Real**

```
Empresa con 100 trabajadores:

70 FIJOS (Administrativos, supervisores)
   ├── AsignacionTurno: Base 9am-5pm
   ├── Ausencias: ProgramacionDescansos
   └── Cambios: CoberturaTurno

30 ROTATIVOS (Personal operacional)
   ├── ProgramacionTurnoSemanal: Flexible
   ├── Cambios: Recarga la programación
   └── Ausencias: Incluidas en próxima carga
```

### **Datos que Se Consultan Juntos**

Cuando quieres ver el calendario de un trabajador:

```
GET /api/Rrhh/ProgramacionSemanal?fechaInicio=2026-03-16&fechaFin=2026-03-22

El sistema retorna:
1. Si es FIJO: Toma base de ASIGNACION_TURNO + PROGRAMACION_DESCANSOS
2. Si es ROTATIVO: Toma de PROGRAMACION_TURNOS_SEMANAL
3. Ambos: Aplica COBERTURA_TURNO (intercambios)
```

---

## 📋 Checklist de Implementación

### Para TRABAJADOR FIJO

- ✅ Crear AsignacionTurno (base a largo plazo)
- ✅ Usar ProgramacionDescansos (ausencias puntuales)
- ✅ Usar CoberturaTurno (cambios con compañeros)
- ✅ Mostrar en calendario (combinado)

### Para TRABAJADOR ROTATIVO

- ✅ Crear ProgramacionTurnoSemanal (cada semana)
- ✅ Actualizar cada semana (nuevo POST)
- ✅ Incluir descansos/boletas en la carga
- ✅ Mostrar en calendario (7 días)

---

## 🚀 Endpoints Necesarios

### Trabajador FIJO

```
POST   /api/Rrhh/AsignacionTurno            → Crear asignación base
GET    /api/Rrhh/AsignacionTurno            → Listar asignaciones
PUT    /api/Rrhh/AsignacionTurno/{id}       → Actualizar
DELETE /api/Rrhh/AsignacionTurno/{id}       → Eliminar

POST   /api/Descansos/semana                → Marcar ausencias
GET    /api/Descansos/{id}/{fecha}          → Ver ausencias

POST   /api/Rrhh/CoberturaTurno             → Cambio de turno
```

### Trabajador ROTATIVO

```
POST   /api/Rrhh/ProgramacionSemanal        → Cargar semana
GET    /api/Rrhh/ProgramacionSemanal        → Ver semana
GET    /api/Rrhh/ProgramacionSemanal/horarios-disponibles → Horarios
```

---

## 💡 Conclusión

Tu arquitectura es **perfecta**:

1. **FIJO** = Base estable + excepciones puntuales
2. **ROTATIVO** = Totalmente flexible cada semana

Ambos sistemas trabajan **independientemente** pero se pueden **consultar juntos** en vistas consolidadas.

**No necesitas cambios.** El sistema está bien diseñado. 🎯
