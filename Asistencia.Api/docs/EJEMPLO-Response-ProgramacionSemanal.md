# 📊 Ejemplo de Response: ProgramacionSemanal (Todos los Trabajadores)

## 🎯 Request

```http
GET /api/Rrhh/ProgramacionSemanal?fechaInicio=2026-03-16&fechaFin=2026-03-22
Authorization: Bearer <token>
```

---

## ✅ Response (200 OK)

```json
{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "totalCount": 375,
  "items": [
    {
      "trabajadorId": 1,
      "trabajadorNombre": "HERNANDEZ CALDERON LUIS ANTONIO",
      "dias": [
        {
          "fecha": "2026-03-16",
          "horarioTurnoId": 26,
          "horarioTurnoNombre": "MAÑANA 06-14",
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "trabaja"
        },
        {
          "fecha": "2026-03-17",
          "horarioTurnoId": 26,
          "horarioTurnoNombre": "MAÑANA 06-14",
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "trabaja"
        },
        {
          "fecha": "2026-03-18",
          "horarioTurnoId": 26,
          "horarioTurnoNombre": "MAÑANA 06-14",
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "trabaja"
        },
        {
          "fecha": "2026-03-19",
          "horarioTurnoId": 26,
          "horarioTurnoNombre": "MAÑANA 06-14",
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "trabaja"
        },
        {
          "fecha": "2026-03-20",
          "horarioTurnoId": 26,
          "horarioTurnoNombre": "MAÑANA 06-14",
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "trabaja"
        },
        {
          "fecha": "2026-03-21",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "descanso"
        },
        {
          "fecha": "2026-03-22",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "descanso"
        }
      ]
    },
    {
      "trabajadorId": 2,
      "trabajadorNombre": "GARCIA LOPEZ MARIA",
      "dias": [
        {
          "fecha": "2026-03-16",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-17",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-18",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-19",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-20",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-21",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-22",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "sin-asignar"
        }
      ]
    },
    {
      "trabajadorId": 3,
      "trabajadorNombre": "RODRIGUEZ SANCHEZ CARLOS",
      "dias": [
        {
          "fecha": "2026-03-16",
          "horarioTurnoId": 27,
          "horarioTurnoNombre": "TARDE 14-22",
          "turnoId": 17,
          "turnoNombre": "TURNO_TARDE",
          "estado": "trabaja"
        },
        {
          "fecha": "2026-03-17",
          "horarioTurnoId": 27,
          "horarioTurnoNombre": "TARDE 14-22",
          "turnoId": 17,
          "turnoNombre": "TURNO_TARDE",
          "estado": "trabaja"
        },
        {
          "fecha": "2026-03-18",
          "horarioTurnoId": 27,
          "horarioTurnoNombre": "TARDE 14-22",
          "turnoId": 17,
          "turnoNombre": "TURNO_TARDE",
          "estado": "trabaja"
        },
        {
          "fecha": "2026-03-19",
          "horarioTurnoId": 27,
          "horarioTurnoNombre": "TARDE 14-22",
          "turnoId": 17,
          "turnoNombre": "TURNO_TARDE",
          "estado": "trabaja"
        },
        {
          "fecha": "2026-03-20",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 17,
          "turnoNombre": "TURNO_TARDE",
          "estado": "vacaciones"
        },
        {
          "fecha": "2026-03-21",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 17,
          "turnoNombre": "TURNO_TARDE",
          "estado": "vacaciones"
        },
        {
          "fecha": "2026-03-22",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": 17,
          "turnoNombre": "TURNO_TARDE",
          "estado": "boleta"
        }
      ]
    },
    {
      "trabajadorId": 4,
      "trabajadorNombre": "MARTINEZ GUTIERREZ ANA",
      "dias": [
        {
          "fecha": "2026-03-16",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": null,
          "turnoNombre": null,
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-17",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": null,
          "turnoNombre": null,
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-18",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": null,
          "turnoNombre": null,
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-19",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": null,
          "turnoNombre": null,
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-20",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": null,
          "turnoNombre": null,
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-21",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": null,
          "turnoNombre": null,
          "estado": "sin-asignar"
        },
        {
          "fecha": "2026-03-22",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": null,
          "turnoNombre": null,
          "estado": "sin-asignar"
        }
      ]
    }
  ]
}
```

---

## 📌 Casos Observados en el Response

### Caso 1: Trabajador CON Programación Semanal Completa
**Trabajador ID 1** - HERNANDEZ CALDERON
- ✅ Tiene horarioTurnoId en todos los días
- ✅ Estado "trabaja" para días de trabajo
- ✅ Estado "descanso" para fines de semana
- ✅ turnoId y turnoNombre siempre presentes

### Caso 2: Trabajador SIN Programación (Por Asignar)
**Trabajador ID 2** - GARCIA LOPEZ
- ❌ horarioTurnoId es NULL (no tiene programación)
- ✅ turnoId y turnoNombre presentes (FIJO con base)
- ✅ Estado "sin-asignar" en todos los días
- **Frontend muestra: "Por Programar"**

### Caso 3: Trabajador CON Estados Mixtos
**Trabajador ID 3** - RODRIGUEZ SANCHEZ
- ✅ Días de trabajo: "trabaja"
- ✅ Vacaciones: "vacaciones"
- ✅ Boleta: "boleta"
- ✅ turnoId presente en todos (FIJO)

### Caso 4: Trabajador ROTATIVO SIN Asignación Fija
**Trabajador ID 4** - MARTINEZ GUTIERREZ
- ❌ horarioTurnoId es NULL
- ❌ turnoId es NULL (ROTATIVO sin base)
- ✅ Estado "sin-asignar"
- **Frontend muestra: "Por Programar" (rotativo nuevo)**

---

## 🎯 Interpretación para Frontend

```javascript
// Para cada trabajador en items:
items.forEach(trabajador => {
  trabajador.dias.forEach(dia => {
    
    if (dia.estado === "sin-asignar") {
      // MOSTRAR: "Por Programar" (clickeable para asignar)
      // Color: Rojo o amarillo
      // Acción: Abrir dialog para asignar horario
    } 
    else if (dia.estado === "trabaja") {
      // MOSTRAR: Horario actual (ej: "MAÑANA 06-14")
      // Color: Verde
    } 
    else if (dia.estado === "descanso") {
      // MOSTRAR: "Descanso"
      // Color: Gris
    } 
    else if (dia.estado === "boleta") {
      // MOSTRAR: "Boleta"
      // Color: Azul
    } 
    else if (dia.estado === "vacaciones") {
      // MOSTRAR: "Vacaciones"
      // Color: Naranja
    }
  });
});
```

---

## ✅ Verificación Checklist

- ✅ Retorna 375 trabajadores (totalCount)
- ✅ Cada trabajador tiene 7 días (rango solicitado)
- ✅ Trabajadores sin programación tienen estado "sin-asignar"
- ✅ Estado "sin-asignar" tiene turnoId cuando es FIJO (null para ROTATIVO)
- ✅ horarioTurnoId es null para "sin-asignar"
- ✅ Estados correctos: trabaja, descanso, boleta, vacaciones, sin-asignar

---

**Response actualizado y listo para frontend.** 🎉
