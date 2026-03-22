# 🎨 Diagrama: Flujos Correctos de los 3 Puntos

## FLUJO 1: Cargar Turno Vigente en Wizard (EDITAR)

```
┌─────────────────────────────────────────────────────────┐
│ USUARIO ABRE WIZARD - MODO EDITAR                       │
│ Ruta: /wizard/trabajador/42/edit                        │
└────────────────┬────────────────────────────────────────┘
                 │
         ┌───────▼────────┐
         │ ngOnInit()     │
         │ isEditMode=true│
         │ id = 42        │
         └───────┬────────┘
                 │
    ┌────────────▼────────────┐
    │ PASO 1: Cargar Datos    │
    │ GET /Trabajadores/{id}  │
    │ Nombre, DNI, email...   │
    └────────────┬────────────┘
                 │
    ┌────────────▼─────────────────┐
    │ PASO 2: Cargar Descansos      │
    │ GET /Descansos/{id}/{fecha}   │
    │ Información sobre faltas      │
    └────────────┬─────────────────┘
                 │
    ┌────────────▼──────────────────────────┐
    │ PASO 3: Mostrar Formulario TURNO      │
    │ [AQUÍ OCURRE EL PROBLEMA]             │
    │                                       │
    │ ❌ ANTES: Selector vacío              │
    │ ✅ DESPUÉS: Necesita cargar vigente   │
    └────────────┬──────────────────────────┘
                 │
    ┌────────────▼──────────────────────────┐
    │ SOLUCIÓN: loadTurnoVigente()          │
    │                                       │
    │ GET /trabajadores/{id}/turno-vigente │
    │                                       │
    │ Response: {                           │
    │   turnoId: 1,                         │
    │   asignacionId: 5,  ← IMPORTANTE     │
    │   tipoTurnoId: 1,                     │
    │   vigencia: {...}                     │
    │ }                                     │
    └────────────┬──────────────────────────┘
                 │
    ┌────────────▼──────────────────────────┐
    │ PRECARGAR FORMULARIO                  │
    │                                       │
    │ turnoId: 1        ✅ Precargado      │
    │ asignacionId: 5   ✅ Guardado        │
    │ horarioTurnoId: 3 ✅ Por turno       │
    │ vigencia: {...}   ✅ Precargado      │
    │                                       │
    │ Selector visible con valor actual     │
    └────────────┬──────────────────────────┘
                 │
    ┌────────────▼──────────────────────────┐
    │ USUARIO PUEDE:                        │
    │                                       │
    │ □ Cambiar turno                       │
    │ □ Cambiar horario                     │
    │ □ Cambiar vigencia                    │
    │ □ O mantener igual                    │
    └────────────┬──────────────────────────┘
                 │
    ┌────────────▼──────────────────────────┐
    │ USUARIO GUARDA                        │
    │ Click "Guardar Turno"                 │
    └────────────┬──────────────────────────┘
                 │
    ┌────────────▼──────────────────────────────────────┐
    │ PUNTO 2: Decidir entre POST vs PUT               │
    │ (Siguiente flujo)                                 │
    └────────────────────────────────────────────────────┘
```

---

## FLUJO 2: Guardar - POST vs PUT

```
┌──────────────────────────────────────────────────────┐
│ USUARIO HACE CLIC EN "GUARDAR TURNO"                │
└─────────────────┬──────────────────────────────────────┘
                  │
        ┌─────────▼─────────┐
        │ Validar formulario │
        │ ¿Todos los datos? │
        └─────────┬─────────┘
                  │
              ┌───▼────────────────────────┐
              │ ¿currentAsignacionId       │
              │   existe?                  │
              └───┬──────────────────┬─────┘
                  │                  │
         NO ┌─────▼──────┐    SI ┌────▼──────┐
           │   POST      │       │    PUT    │
           │   (CREATE)  │       │ (UPDATE)  │
           └─────┬──────┘       └────┬──────┘
                 │                   │
                 │    ┌──────────────┘
                 │    │
        ┌────────▼────▼─────────────────────────┐
        │ Construir payload                     │
        │                                       │
        │ {                                     │
        │   trabajadorId: 42,                   │
        │   turnoId: 2,      (posiblemente      │
        │   horarioTurnoId: 5,   nuevo)        │
        │   fechaInicioVigencia: "2026-01-01", │
        │   fechaFinVigencia: null              │
        │ }                                     │
        └────────┬────────────────────────────┘
                 │
        ┌────────▼────────────────────────────┐
        │                                     │
        ├─POST→ /api/Rrhh/AsignacionTurno    │
        │                                     │
        │ Response: {                         │
        │   id: 10,  ← NUEVO ID               │
        │   ...                               │
        │ }                                   │
        │                                     │
        │ currentAsignacionId = 10            │
        │                                     │
        └─────────┬──────────────────────────┘
                  │
        ┌────────▼────────────────────────────┐
        │ ├─PUT→ /api/Rrhh/AsignacionTurno/5 │
        │                                     │
        │ Response: 204 No Content             │
        │                                     │
        │ (Actualiza el ID 5)                 │
        │                                     │
        └─────────┬──────────────────────────┘
                  │
        ┌─────────▼──────────────────────────┐
        │ ✅ ÉXITO                            │
        │                                    │
        │ 'Turno asignado/actualizado'       │
        │                                    │
        │ Avanzar al siguiente paso (4)       │
        └────────────────────────────────────┘
```

---

## FLUJO 3: Cargar Horarios - Dos Opciones

```
┌──────────────────────────────────────────────┐
│ USUARIO SELECCIONA TURNO EN PASO 3           │
│ onTurnoChange(event)                         │
└────────────────┬─────────────────────────────┘
                 │
    ┌────────────▼────────────────────┐
    │ ¿Tipo de Trabajador?            │
    └────────────┬────────────────────┘
                 │
        ┌────────┴────────┐
        │                 │
    FIJO│                 │ROTATIVO
        │                 │
        │                 │
    ┌───▼──────────────┐  ┌──────────────────────────┐
    │ OPCIÓN A:        │  │ OPCIÓN B (RECOMENDADA):  │
    │ General          │  │ Específica                │
    │                  │  │                          │
    │ GET /HorarioTurno│  │ GET /ProgramacionSemanal/│
    │                  │  │ horarios-disponibles     │
    │ Response:        │  │                          │
    │ [{              │  │ Response:                │
    │   id: 1,        │  │ {                        │
    │   nombre: "M",  │  │   total: 3,              │
    │   turnoId: 1,   │  │   horarios: [            │
    │   esActivo: true│  │     {                    │
    │ }]              │  │       id: 1,             │
    │                  │  │       nombre: "M",      │
    │ En cliente:      │  │       turnoId: 1,       │
    │ filtrar por      │  │       turnoNombre: "...",
    │ turnoId          │  │       esActivo: true    │
    │                  │  │     }                    │
    │ ✅ Funciona     │  │   ]                      │
    │ ❌ No óptimo    │  │ }                        │
    │                  │  │                          │
    │                  │  │ ✅ Ya filtrado          │
    │                  │  │ ✅ Óptimo               │
    └────────┬─────────┘  └───────┬──────────────────┘
             │                    │
        ┌────▼─────────────────────▼──────┐
        │ Actualizar horariosFiltrados    │
        │                                 │
        │ [Mostrar selector con opciones] │
        │                                 │
        │ <select>                        │
        │   <option>-- Selecciona --      │
        │   <option value="1">Mañana 9-5  │
        │   <option value="2">Tarde 5-9pm │
        │   <option value="3">Noche 9pm   │
        │ </select>                       │
        └────────┬──────────────────────┘
                 │
        ┌────────▼──────────────────────────┐
        │ Usuario selecciona horario        │
        │                                   │
        │ Formulario completado:            │
        │ ✅ Turno: 1                       │
        │ ✅ Horario: 2                     │
        │ ✅ Vigencia: 2026-01-01 al ...    │
        │                                   │
        │ Botón "Guardar" habilitado        │
        └────────┬──────────────────────────┘
                 │
        ┌────────▼──────────────────────────┐
        │ (Vuelve a FLUJO 2: POST vs PUT)   │
        └───────────────────────────────────┘
```

---

## COMPARATIVA: Antes vs Después

```
ANTES (❌ INCORRECTO)
═════════════════════════════════════════════════════════════

PASO 3: Cargar Turno
    │
    ├─ GET /Trabajadores/{id}
    │
    └─ Mostrar formulario
       turnoId: NULL    ← PROBLEMA 1
       asignacionId: null

USUARIO MODIFICA Y GUARDA
    │
    ├─ POST /AsignacionTurno  ← PROBLEMA 2
    │  (Siempre POST, nunca PUT)
    │
    └─ ❌ Crea duplicado
       ❌ No actualiza vigente


DESPUÉS (✅ CORRECTO)
═════════════════════════════════════════════════════════════

PASO 3: Cargar Turno
    │
    ├─ GET /Trabajadores/{id}
    │
    ├─ GET /trabajadores/{id}/turno-vigente
    │
    └─ Mostrar formulario
       turnoId: 1       ✅ Precargado
       asignacionId: 5  ✅ Guardado

USUARIO MODIFICA Y GUARDA
    │
    ├─ ¿Existe asignacionId?
    │
    ├─ SÍ → PUT /AsignacionTurno/5    ✅ Actualiza
    │
    └─ NO → POST /AsignacionTurno     ✅ Crea nuevo
       + Guarda ID retornado


HORARIOS - ANTES vs DESPUÉS
═════════════════════════════════════════════════════════════

ANTES (❌ INCORRECTO)
├─ GET /HorarioTurno (toda la lista)
├─ Filtrar en cliente por turnoId
└─ ❌ Ineficiente (descarga todos)

DESPUÉS (✅ CORRECTO)
├─ GET /HorarioTurno (catálogo general para FIJOS)
│  Filtrar en cliente por turnoId
│  ✅ Funciona bien
│
└─ GET /ProgramacionSemanal/horarios-disponibles
   (Para ROTATIVOS)
   ✅ Ya filtrado
   ✅ Óptimo
```

---

## 📊 Estados del Wizard

```
STATE 1: CREAR TRABAJADOR (NUEVO)
┌─────────────────────────────────────┐
│ Paso 1: Datos Personales            │
│ Paso 2: Datos Laborales             │
│ Paso 3: Asignar Turno               │
│         asignacionId: null  ← POST  │
└─────────────────────────────────────┘

STATE 2: EDITAR TRABAJADOR (EXISTENTE)
┌─────────────────────────────────────┐
│ Paso 1: Datos Personales (precargado)
│ Paso 2: Datos Laborales (precargado) │
│ Paso 3: Modificar Turno             │
│         asignacionId: 5  ← PUT      │
│         (Cargado via GET vigente)   │
└─────────────────────────────────────┘
```

---

## 🎯 Checklist Final

```
PUNTO 1: getTurnoVigente
├─ □ Detectar isEditMode en ngOnInit
├─ □ Llamar getTurnoVigente(id) si isEditMode
├─ □ Precargar turnoId
├─ □ Guardar asignacionId para PUT
└─ □ Mostrar valores en selector

PUNTO 2: POST vs PUT
├─ □ Si asignacionId → PUT
├─ □ Si NO asignacionId → POST
├─ □ Guardar ID retornado del POST
└─ □ Manejar errores de ambos

PUNTO 3: Horarios
├─ □ FIJO: GET /HorarioTurno + filtrar cliente
├─ □ ROTATIVO: GET .../horarios-disponibles
├─ □ Mostrar solo esActivo: true
└─ □ Actualizar dinámicamente al cambiar turno
```

---

**Implementa estos 3 flujos y todo funcionará correctamente.** ✅
