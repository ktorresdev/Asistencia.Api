# ⚡ QUICK REFERENCE: Crear Cobertura de Turno

## 🎯 Endpoint

```
POST /api/Coberturas
Authorization: Bearer <token>
```

---

## 📤 Body (JSON)

```json
{
  "fecha": "2026-03-20",
  "idTrabajadorCubre": 5,
  "idTrabajadorAusente": 2,
  "idHorarioTurnoOriginal": 26,
  "tipoCobertura": "INTERCAMBIO",
  "fechaSwapDevolucion": null,
  "aprobadoPor": null
}
```

---

## 📥 Response (201)

```json
{
  "idCobertura": 42,
  "fecha": "2026-03-20",
  "idTrabajadorCubre": 5,
  "idTrabajadorAusente": 2,
  "idHorarioTurnoOriginal": 26,
  "tipoCobertura": "INTERCAMBIO",
  "estado": "PENDIENTE",
  "fechaSwapDevolucion": null,
  "aprobadoPor": null
}
```

---

## ✅ Validaciones Principales

- ✅ IdTrabajadorCubre ≠ IdTrabajadorAusente
- ✅ Roles: ADMIN, SUPERADMIN, SUPERVISOR
- ✅ Fecha formato: YYYY-MM-DD

---

## 🔗 Ubicación

`Controllers\CoberturasController.cs` - Línea 22-56

---

**Detalles completos en: `docs\ENDPOINT-Crear-Cobertura-Turno.md`**
