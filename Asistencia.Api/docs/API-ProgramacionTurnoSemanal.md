# 📋 Endpoint: Asignar Programación Semanal de Turnos

## 🎯 Descripción General

Este endpoint permite **grabar la programación semanal de turnos** para múltiples trabajadores en la tabla `PROGRAMACION_TURNOS_SEMANAL`.

**Tabla destino:** `PROGRAMACION_TURNOS_SEMANAL`
**Acción:** INSERT/UPSERT (elimina registros existentes en el rango y graba los nuevos)

---

## 📍 Endpoint

### **Ruta**
```
POST /api/Rrhh/ProgramacionSemanal
```

### **Autenticación**
```
Authorization: Bearer <token>
Roles permitidos: ADMIN, SUPERADMIN
```

---

## 📤 Body Request

```json
{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "programaciones": [
    {
      "trabajadorId": 1,
      "fecha": "2026-03-16",
      "idHorarioTurno": 5,
      "esDescanso": true,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 1,
      "fecha": "2026-03-17",
      "idHorarioTurno": 5,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 1,
      "fecha": "2026-03-18",
      "idHorarioTurno": 5,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 2,
      "fecha": "2026-03-16",
      "idHorarioTurno": 3,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    }
  ]
}
```

### **Explicación de Campos**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| **fechaInicio** | `DateOnly` | ✅ | Fecha inicio del rango (YYYY-MM-DD) |
| **fechaFin** | `DateOnly` | ✅ | Fecha fin del rango (YYYY-MM-DD) |
| **programaciones** | `Array` | ✅ | Lista de asignaciones diarias |
| **trabajadorId** | `int` | ✅ | ID del trabajador |
| **fecha** | `DateOnly` | ✅ | Fecha del día asignado |
| **idHorarioTurno** | `int` | ✅ | ID del horario/turno para ese día |
| **esDescanso** | `bool` | ❌ | Indica si es día de descanso (default: false) |
| **esDiaBoleta** | `bool` | ❌ | Indica si es día de boleta/vacaciones (default: false) |
| **esVacaciones** | `bool` | ❌ | Indica si es día de vacaciones (default: false) |

---

## 📥 Response

### **Success (200 OK)**

```json
{
  "ok": true,
  "mensaje": "Programación semanal grabada",
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "registrosGrabados": 7
}
```

### **Error 400 - Fechas Inválidas**

```json
{
  "message": "FechaInicio no puede ser mayor a FechaFin"
}
```

### **Error 400 - Sin Programaciones**

```json
{
  "message": "Debe enviar al menos una programación"
}
```

### **Error 404 - Trabajador no existe**

```json
{
  "message": "Uno o más trabajadores no existen"
}
```

### **Error 404 - Horario no existe**

```json
{
  "message": "Uno o más horarios no existen"
}
```

---

## 🗄️ ¿Qué se graba en Base de Datos?

### **Tabla: PROGRAMACION_TURNOS_SEMANAL**

```sql
CREATE TABLE PROGRAMACION_TURNOS_SEMANAL (
    id INT PRIMARY KEY IDENTITY(1,1),
    id_trabajador INT NOT NULL,
    fecha DATE NOT NULL,
    id_horario_turno INT NOT NULL,
    es_descanso BIT DEFAULT 0,
    es_dia_boleta BIT DEFAULT 0,
    es_vacaciones BIT DEFAULT 0,
    created_by INT,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2
);
```

### **Proceso de Grabación**

1. **Validación:** Verifica que trabajadores y horarios existan
2. **Eliminación:** Borra registros existentes en el rango de fechas para esos trabajadores
3. **Inserción:** Graba los nuevos registros
4. **Commit:** Guarda todos los cambios en BD

### **Ejemplo de Registros Grabados**

Para la solicitud anterior con `fechaInicio: 2026-03-16` y `fechaFin: 2026-03-22`:

```sql
SELECT * FROM PROGRAMACION_TURNOS_SEMANAL WHERE id_trabajador IN (1, 2)
  AND fecha BETWEEN '2026-03-16' AND '2026-03-22';
```

**Resultado:**

| id | id_trabajador | fecha | id_horario_turno | es_descanso | es_dia_boleta | es_vacaciones | created_at |
|----|---|---|---|---|---|---|---|
| 1 | 1 | 2026-03-16 | 5 | 1 | 0 | 0 | 2026-03-14 10:30:00 |
| 2 | 1 | 2026-03-17 | 5 | 0 | 0 | 0 | 2026-03-14 10:30:00 |
| 3 | 1 | 2026-03-18 | 5 | 0 | 0 | 0 | 2026-03-14 10:30:00 |
| 4 | 2 | 2026-03-16 | 3 | 0 | 0 | 0 | 2026-03-14 10:30:00 |

---

## 💡 Comportamiento UPSERT

El endpoint **actualiza** automáticamente si cargas la misma semana dos veces:

```
Primera carga (2026-03-16 a 2026-03-22):
  → 7 registros insertados para Trabajador 1
  → 7 registros insertados para Trabajador 2

Segunda carga (mismas fechas, diferentes horarios):
  → Elimina los 14 registros anteriores
  → Inserta 14 nuevos registros
  → Total: 14 registros (sin duplicados)
```

---

## 🔄 Diferencia con AsignacionTurno

| Aspecto | ASIGNACION_TURNO | PROGRAMACION_TURNOS_SEMANAL |
|--------|-------------------|-----|
| **Alcance** | Largo plazo (meses/años) | Corto plazo (semanal) |
| **Vigencia** | FechaInicioVigencia → FechaFinVigencia | Fecha específica |
| **Uso** | Asigna turno base al trabajador | Programa día a día |
| **Flexibilidad** | Rigida (todo el período igual) | Flexible (cada día diferente) |
| **Típico** | Trabajador siempre turno mañana | Trabaja L-M-X, descansa J-V-S-D |

---

## 📝 Ejemplos Prácticos

### **Ejemplo 1: Semana completa para 1 trabajador**

```bash
POST /api/Rrhh/ProgramacionSemanal
Authorization: Bearer <token>
Content-Type: application/json

{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "programaciones": [
    { "trabajadorId": 1, "fecha": "2026-03-16", "idHorarioTurno": 5, "esDescanso": true },
    { "trabajadorId": 1, "fecha": "2026-03-17", "idHorarioTurno": 5, "esDescanso": false },
    { "trabajadorId": 1, "fecha": "2026-03-18", "idHorarioTurno": 5, "esDescanso": false },
    { "trabajadorId": 1, "fecha": "2026-03-19", "idHorarioTurno": 5, "esDescanso": false },
    { "trabajadorId": 1, "fecha": "2026-03-20", "idHorarioTurno": 5, "esDescanso": false },
    { "trabajadorId": 1, "fecha": "2026-03-21", "idHorarioTurno": 5, "esDescanso": false },
    { "trabajadorId": 1, "fecha": "2026-03-22", "idHorarioTurno": 5, "esDescanso": false }
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
  "registrosGrabados": 7
}
```

### **Ejemplo 2: 3 trabajadores con diferentes horarios**

```bash
POST /api/Rrhh/ProgramacionSemanal
Authorization: Bearer <token>
Content-Type: application/json

{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "programaciones": [
    { "trabajadorId": 1, "fecha": "2026-03-16", "idHorarioTurno": 5, "esDescanso": true, "esDiaBoleta": false },
    { "trabajadorId": 1, "fecha": "2026-03-17", "idHorarioTurno": 5 },
    { "trabajadorId": 1, "fecha": "2026-03-18", "idHorarioTurno": 5 },
    { "trabajadorId": 1, "fecha": "2026-03-19", "idHorarioTurno": 5 },
    { "trabajadorId": 1, "fecha": "2026-03-20", "idHorarioTurno": 5 },
    { "trabajadorId": 1, "fecha": "2026-03-21", "idHorarioTurno": 5 },
    { "trabajadorId": 1, "fecha": "2026-03-22", "idHorarioTurno": 5, "esDiaBoleta": true },
    
    { "trabajadorId": 2, "fecha": "2026-03-16", "idHorarioTurno": 3 },
    { "trabajadorId": 2, "fecha": "2026-03-17", "idHorarioTurno": 3, "esDescanso": true },
    { "trabajadorId": 2, "fecha": "2026-03-18", "idHorarioTurno": 3 },
    { "trabajadorId": 2, "fecha": "2026-03-19", "idHorarioTurno": 3 },
    { "trabajadorId": 2, "fecha": "2026-03-20", "idHorarioTurno": 3 },
    { "trabajadorId": 2, "fecha": "2026-03-21", "idHorarioTurno": 3 },
    { "trabajadorId": 2, "fecha": "2026-03-22", "idHorarioTurno": 3 },
    
    { "trabajadorId": 3, "fecha": "2026-03-16", "idHorarioTurno": 1, "esVacaciones": true },
    { "trabajadorId": 3, "fecha": "2026-03-17", "idHorarioTurno": 1, "esVacaciones": true },
    { "trabajadorId": 3, "fecha": "2026-03-18", "idHorarioTurno": 1 },
    { "trabajadorId": 3, "fecha": "2026-03-19", "idHorarioTurno": 1 },
    { "trabajadorId": 3, "fecha": "2026-03-20", "idHorarioTurno": 1, "esDescanso": true },
    { "trabajadorId": 3, "fecha": "2026-03-21", "idHorarioTurno": 1 },
    { "trabajadorId": 3, "fecha": "2026-03-22", "idHorarioTurno": 1 }
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
  "registrosGrabados": 21
}
```

---

## ⚠️ Validaciones Realizadas

✅ Fechas válidas (inicio ≤ fin)
✅ Al menos 1 programación
✅ Trabajadores existen
✅ Horarios existen
✅ Datos de entrada válidos (ModelState)

---

## 📊 Resumen

| Aspecto | Detalle |
|--------|---------|
| **Endpoint** | `POST /api/Rrhh/ProgramacionSemanal` |
| **Tabla** | `PROGRAMACION_TURNOS_SEMANAL` |
| **Registros por semana/trabajador** | Flexible (1-7 por día) |
| **Validación** | Trabajadores y horarios deben existir |
| **Comportamiento** | UPSERT (elimina existentes, graba nuevos) |
| **Roles** | ADMIN, SUPERADMIN |
| **Respuesta** | JSON con cantidad de registros grabados |
