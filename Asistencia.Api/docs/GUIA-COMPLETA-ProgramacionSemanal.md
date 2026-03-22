# 📋 Guía Completa: Asignar Programación Semanal de Turnos

## 🎯 Objetivo

Programar los **turnos día a día** para los trabajadores. Cada día puede tener un horario diferente, descansos, boletas o vacaciones.

---

## 🔄 Flujo Paso a Paso

### **PASO 1: Obtener Token de Autenticación**

**Endpoint:**
```
POST /api/Auth/login
```

**Body:**
```json
{
  "username": "SUPERADMIN",
  "password": "tuPassword"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "expiresIn": 7200
}
```

✅ **Copia el `accessToken`**

---

### **PASO 2: Obtener Horarios Disponibles**

**Endpoint:**
```
GET /api/Rrhh/ProgramacionSemanal/horarios-disponibles
Authorization: Bearer <TU_TOKEN>
```

**Response:**
```json
{
  "mensaje": "Horarios disponibles",
  "total": 4,
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
    },
    {
      "id": 4,
      "nombre": "Turno Mixto",
      "turnoId": 4,
      "turnoNombre": "TURNO_MIXTO",
      "esActivo": true
    }
  ]
}
```

✅ **Anota los IDs de los horarios que necesitas**

---

### **PASO 3: Obtener Trabajadores**

**Endpoint:**
```
GET /api/Rrhh/Trabajadores?pageNumber=1&pageSize=100
Authorization: Bearer <TU_TOKEN>
```

**Response (simplificado):**
```json
{
  "items": [
    {
      "id": 1,
      "persona": {
        "apellidosNombres": "Juan Pérez"
      }
    },
    {
      "id": 9,
      "persona": {
        "apellidosNombres": "Maria García"
      }
    },
    {
      "id": 42,
      "persona": {
        "apellidosNombres": "Carlos López"
      }
    }
  ],
  "totalCount": 50
}
```

✅ **Anota los IDs de los trabajadores**

---

### **PASO 4: Asignar Programación Semanal**

**Endpoint:**
```
POST /api/Rrhh/ProgramacionSemanal
Authorization: Bearer <TU_TOKEN>
Content-Type: application/json
```

**Body (usando los IDs del PASO 2 y 3):**

```json
{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "programaciones": [
    {
      "trabajadorId": 1,
      "fecha": "2026-03-16",
      "idHorarioTurno": 1,
      "esDescanso": true,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 1,
      "fecha": "2026-03-17",
      "idHorarioTurno": 1,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 1,
      "fecha": "2026-03-18",
      "idHorarioTurno": 1,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 1,
      "fecha": "2026-03-19",
      "idHorarioTurno": 1,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 1,
      "fecha": "2026-03-20",
      "idHorarioTurno": 1,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 1,
      "fecha": "2026-03-21",
      "idHorarioTurno": 1,
      "esDescanso": false,
      "esDiaBoleta": true,
      "esVacaciones": false
    },
    {
      "trabajadorId": 1,
      "fecha": "2026-03-22",
      "idHorarioTurno": 1,
      "esDescanso": false,
      "esDiaBoleta": true,
      "esVacaciones": false
    },
    {
      "trabajadorId": 9,
      "fecha": "2026-03-16",
      "idHorarioTurno": 2,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 9,
      "fecha": "2026-03-17",
      "idHorarioTurno": 2,
      "esDescanso": true,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 9,
      "fecha": "2026-03-18",
      "idHorarioTurno": 2,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 9,
      "fecha": "2026-03-19",
      "idHorarioTurno": 2,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 9,
      "fecha": "2026-03-20",
      "idHorarioTurno": 2,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 9,
      "fecha": "2026-03-21",
      "idHorarioTurno": 2,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    },
    {
      "trabajadorId": 9,
      "fecha": "2026-03-22",
      "idHorarioTurno": 2,
      "esDescanso": false,
      "esDiaBoleta": false,
      "esVacaciones": false
    }
  ]
}
```

**Response Success:**
```json
{
  "ok": true,
  "mensaje": "Programación semanal grabada",
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "registrosGrabados": 14
}
```

✅ **¡Programación grabada exitosamente!**

---

### **PASO 5: Obtener Programación Grabada**

**Endpoint:**
```
GET /api/Rrhh/ProgramacionSemanal?fechaInicio=2026-03-16&fechaFin=2026-03-22
Authorization: Bearer <TU_TOKEN>
```

**Response:**
```json
{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "items": [
    {
      "trabajadorId": 1,
      "trabajadorNombre": "Juan Pérez",
      "dias": [
        {
          "fecha": "2026-03-16",
          "horarioTurnoId": 1,
          "horarioTurnoNombre": "Turno Mañana 9-5",
          "turnoId": 1,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "descanso"
        },
        {
          "fecha": "2026-03-17",
          "horarioTurnoId": 1,
          "horarioTurnoNombre": "Turno Mañana 9-5",
          "turnoId": 1,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "programado"
        },
        ...más días...
      ]
    }
  ]
}
```

✅ **¡Puedes ver toda la programación!**

---

## ⚠️ **Error Común: "Uno o más horarios no existen"**

### Causa:
```json
{
  "idHorarioTurno": 0  // ← ID inválido o no existe
}
```

### Solución:

1. **Ejecuta PASO 2** para obtener los IDs correctos
2. **Verifica que uses los IDs correctos**
3. **En el PASO 4, usa los IDs válidos** (NO 0)

### Respuesta de error mejorada:

```json
{
  "message": "Uno o más horarios no existen",
  "horariosEnviados": [0],
  "horariosDisponibles": [
    {
      "id": 1,
      "nombreHorario": "Turno Mañana 9-5"
    },
    {
      "id": 2,
      "nombreHorario": "Turno Tarde 5-9pm"
    }
  ]
}
```

✅ **Ahora sabes exactamente qué IDs existen**

---

## 📝 Campos Explicados

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| **fechaInicio** | DateOnly | ✅ | YYYY-MM-DD (inicio de rango) |
| **fechaFin** | DateOnly | ✅ | YYYY-MM-DD (fin de rango) |
| **trabajadorId** | int | ✅ | ID del trabajador (obten en PASO 3) |
| **fecha** | DateOnly | ✅ | YYYY-MM-DD (día a programar) |
| **idHorarioTurno** | int | ✅ | ID del horario (obten en PASO 2) |
| **esDescanso** | bool | ❌ | true = día de descanso (default: false) |
| **esDiaBoleta** | bool | ❌ | true = día de boleta/libre (default: false) |
| **esVacaciones** | bool | ❌ | true = día de vacaciones (default: false) |

---

## 🎯 Resumen Rápido

```
1. Login → Obtén token
2. GET horarios-disponibles → Obtén IDs válidos
3. GET Trabajadores → Obtén IDs de trabajadores
4. POST ProgramacionSemanal → Asigna programación
5. GET ProgramacionSemanal → Verifica grabado
```

---

## ✅ Checklist Antes de POST

- ☑️ Tengo un token válido en Authorization header
- ☑️ `fechaInicio` y `fechaFin` son válidas (YYYY-MM-DD)
- ☑️ `trabajadorId` existe (verificado en PASO 3)
- ☑️ `idHorarioTurno` existe (verificado en PASO 2)
- ☑️ No estoy usando `idHorarioTurno: 0`
- ☑️ Las `fechas` están dentro del rango `fechaInicio` - `fechaFin`

---

## 🚀 ¡Listo para usarlo!

Sigue los 5 pasos y funcionará sin problemas.
