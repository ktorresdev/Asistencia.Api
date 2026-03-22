# ✅ Revisión: 3 Puntos Críticos del Sistema

## 📋 Estado Actual: Análisis Completo

### ✅ PUNTO 1: Turno Vigente en Wizard

**Estado:** ✅ **YA IMPLEMENTADO**

**Endpoint Existente:**
```
GET /api/trabajadores/{id}/turno-vigente
```

**Ubicación en Código:**
```csharp
Controllers/TrabajadoresController.cs (línea 111-150)
```

**Qué Hace:**
```csharp
[HttpGet("~/api/trabajadores/{id:int}/turno-vigente")]
public async Task<IActionResult> GetTurnoVigente(int id)
{
    var asignacion = await _context.AsignacionesTurno
        .Include(a => a.Turno)
        .Include(a => a.HorarioTurno)
        .FirstOrDefaultAsync(a => a.TrabajadorId == id
            && a.EsVigente
            && a.FechaInicioVigencia <= today
            && (a.FechaFinVigencia == null || a.FechaFinVigencia.Value >= today));
    
    // Retorna: turnoId, asignacionId, horarios, vigencia
}
```

**Response Esperado:**
```json
{
  "trabajadorId": 42,
  "asignacionId": 5,
  "turno": {
    "id": 1,
    "codigo": "TURNO_MAÑANA",
    "tipoTurnoId": 1,
    "esActivo": true
  },
  "vigencia": {
    "inicio": "2026-01-01",
    "fin": "2026-12-31"
  }
}
```

**Acción Requerida:**
```
✅ El endpoint ESTÁ implementado
✅ El frontend DEBE usarlo en el wizard cuando abre en modo EDICIÓN
✅ Paso 3 del wizard: llamar antes de mostrar el selector

Pseudocódigo Frontend:
if (mode === 'edit') {
  const response = await GET /api/trabajadores/{id}/turno-vigente
  formData.turnoId = response.turno.id
  formData.asignacionId = response.asignacionId
}
```

---

### ✅ PUNTO 2: AsignacionTurno - POST vs PUT

**Estado:** ✅ **CORRECTAMENTE IMPLEMENTADO**

**Endpoints Existentes:**

```
POST   /api/Rrhh/AsignacionTurno
       → Crear nueva asignación
       → Usa: AsignacionTurnoCreateDto

PUT    /api/Rrhh/AsignacionTurno/{id}
       → Actualizar asignación existente
       → Usa: AsignacionTurnoUpdateDto
```

**Ubicación en Código:**
```csharp
Controllers/AsignacionTurnoController.cs
```

**Análisis del Código:**

```csharp
[HttpPost]
public async Task<ActionResult<AsignacionTurno>> CreateAsync(
    [FromBody] AsignacionTurnoCreateDto createDto)
{
    // POST: Crear nueva asignación
}

[HttpPut("{id:int}")]
public async Task<IActionResult> UpdateAsync(
    int id, 
    [FromBody] AsignacionTurnoUpdateDto updateDto)
{
    // PUT: Actualizar asignación existente
}
```

**¿Qué Hace el Backend?**

- **POST:** Crea una **nueva asignación** (no hace upsert)
- **PUT:** Actualiza una **asignación existente** (por ID)
- **NO hace upsert** - Son operaciones separadas

**Acción Requerida:**

```javascript
// Frontend CORRECTO:

// 1. CREAR nuevo turno (primera vez)
POST /api/Rrhh/AsignacionTurno
Body: {
  trabajadorId: 42,
  turnoId: 1,
  horarioTurnoId: 3,
  fechaInicioVigencia: "2026-01-01",
  fechaFinVigencia: "2026-12-31"
}

// 2. CAMBIAR turno (ya existe)
PUT /api/Rrhh/AsignacionTurno/{asignacionId}
Body: {
  trabajadorId: 42,
  turnoId: 2,  // ← Nuevo turno
  horarioTurnoId: 5,
  fechaInicioVigencia: "2026-01-01",
  fechaFinVigencia: "2026-12-31"
}
```

**Lógica en Frontend:**

```javascript
// En el wizard:
if (isNewAsignacion) {
  // POST para crear
  await createAsignacion(asignacionData)
} else {
  // PUT para actualizar
  await updateAsignacion(asignacionId, asignacionData)
}
```

**Flujo Correcto:**

```
WIZARD EDITAR TRABAJADOR
    │
    ├─ GET /api/trabajadores/{id}/turno-vigente
    │  └─ Obtiene asignacionId actual
    │
    ├─ Usuario selecciona nuevo turno
    │
    └─ Guardar:
       ├─ Si asignacionId existe → PUT
       └─ Si NO existe → POST
```

---

### ✅ PUNTO 3: Horarios Disponibles

**Estado:** ✅ **AMBOS ENDPOINTS EXISTEN**

**Opción A: Endpoint Específico (Recomendado)**

```
GET /api/Rrhh/ProgramacionSemanal/horarios-disponibles
```

**Ubicación:**
```csharp
Controllers/ProgramacionSemanalController.cs (línea ~20)
```

**Response:**
```json
{
  "mensaje": "Horarios disponibles",
  "total": 3,
  "horarios": [
    {
      "id": 1,
      "nombre": "Turno Mañana 9-5",
      "turnoId": 1,
      "turnoNombre": "TURNO_MAÑANA",
      "esActivo": true
    },
    {
      "id": 2,
      "nombre": "Turno Tarde 5-9pm",
      "turnoId": 2,
      "turnoNombre": "TURNO_TARDE",
      "esActivo": true
    },
    {
      "id": 3,
      "nombre": "Turno Noche 9pm-5am",
      "turnoId": 3,
      "turnoNombre": "TURNO_NOCHE",
      "esActivo": true
    }
  ]
}
```

**Opción B: Catálogo General**

```
GET /api/Rrhh/HorarioTurno
```

**Retorna:** Todos los horarios sin filtrar

**Acción Requerida:**

Para **FIJOS (AsignacionTurno):**
```javascript
// Opción 1: Usar catálogo general + filtrar en cliente
const allHorarios = await GET /api/Rrhh/HorarioTurno
const filtered = allHorarios.filter(h => h.turnoId === selectedTurnoId)

// Opción 2: Filtrar manualmente por turnoId del turno asignado
```

Para **ROTATIVOS (ProgramacionSemanal):**
```javascript
// Usar endpoint específico (más eficiente)
const horarios = await GET /api/Rrhh/ProgramacionSemanal/horarios-disponibles
// Ya viene filtrado y en formato correcto
```

---

## 📊 Tabla Resumen

| Punto | Estado | Acción | Prioridad |
|-------|--------|--------|-----------|
| **1. Turno Vigente** | ✅ Implementado | Frontend: Usar en wizard edit | 🔴 ALTA |
| **2. POST vs PUT** | ✅ Correcto | Frontend: Lógica condicional | 🔴 ALTA |
| **3. Horarios** | ✅ Ambos existen | Frontend: Elegir endpoint | 🟡 MEDIA |

---

## 🔧 Checklist para Implementar en Frontend

### Para FIJO (AsignacionTurno)

```javascript
□ En wizard EDITAR:
  □ Llamar GET /api/trabajadores/{id}/turno-vigente
  □ Precargar turnoId en paso 3
  □ Precargar asignacionId para PUT

□ Al guardar:
  □ SI asignacionId → PUT /api/Rrhh/AsignacionTurno/{id}
  □ SI NO asignacionId → POST /api/Rrhh/AsignacionTurno

□ Obtener horarios:
  □ GET /api/Rrhh/HorarioTurno
  □ Filtrar por turnoId en cliente
```

### Para ROTATIVO (ProgramacionSemanal)

```javascript
□ En formulario:
  □ Llamar GET /api/Rrhh/ProgramacionSemanal/horarios-disponibles
  □ Mostrar selector de horarios

□ Al guardar:
  □ POST /api/Rrhh/ProgramacionSemanal
  □ Con array de programaciones diarias

□ Obtener horarios:
  □ Usar endpoint específico (ya filtrados)
```

---

## 📝 Ejemplos de Implementación

### Wizard FIJO - Modo EDICIÓN

```javascript
// Step 1: Cargar trabajador
const trabajador = await GET /api/Rrhh/Trabajadores/{id}

// Step 2: Cargar turno vigente
const turnoVigente = await GET /api/trabajadores/{id}/turno-vigente
formData.turnoId = turnoVigente.turno.id
formData.asignacionId = turnoVigente.asignacionId

// Step 3: Mostrar formulario con valores precargados
displayForm({
  trabajadorId: trabajador.id,
  turnoId: turnoVigente.turno.id,  // ← Precar gado
  asignacionId: turnoVigente.asignacionId
})

// Al guardar:
if (formData.asignacionId) {
  // Actualizar
  await PUT /api/Rrhh/AsignacionTurno/{asignacionId}
} else {
  // Crear
  await POST /api/Rrhh/AsignacionTurno
}
```

### Formulario ROTATIVO - Cargar Semana

```javascript
// Obtener horarios disponibles
const horarios = await GET /api/Rrhh/ProgramacionSemanal/horarios-disponibles

// Mostrar selector con opciones
showSelect(horarios)

// Al cargar semana:
const payload = {
  fechaInicio: "2026-03-16",
  fechaFin: "2026-03-22",
  programaciones: [
    { trabajadorId: 1, fecha: "2026-03-16", idHorarioTurno: 1 },
    { trabajadorId: 1, fecha: "2026-03-17", idHorarioTurno: 2 },
    ...
  ]
}
await POST /api/Rrhh/ProgramacionSemanal
```

---

## 🚀 Conclusión

✅ **Backend está correctamente implementado**

**Próximos pasos en Frontend:**
1. Integrar GET turno-vigente en wizard
2. Implementar lógica POST vs PUT en guardar
3. Elegir endpoints para horarios (específico para rotativo, general para fijo)

**No necesitas cambios en backend.** 🎯
