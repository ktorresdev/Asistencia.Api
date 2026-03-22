# ⚡ QUICK REFERENCE: Endpoint Status Marcación

## 🎯 Endpoint

```
GET /api/Rrhh/MarcacionAsistencia/status/{trabajadorId}
Authorization: Bearer <token>
```

---

## 📤 Response

```json
{
  "success": true,
  "trabajadorId": 5,
  "horarioProgramado": "09:00 - 17:00",
  "marcacionEntrada": "2026-03-20T09:15:32",
  "marcacionSalida": null,
  "tiempoTrabajadoFormato": "45 minutos",
  "puedeMarcarEntrada": false,
  "puedeMarcarSalida": true,
  "salidaPendiente": true
}
```

---

## 🎯 Lógica de Botones

```javascript
// En tu frontend:
const status = await fetch(`/api/Rrhh/MarcacionAsistencia/status/5`)
  .then(r => r.json());

// Habilitar/deshabilitar botones
btnEntrada.disabled = !status.puedeMarcarEntrada;
btnSalida.disabled = !status.puedeMarcarSalida;

// Mostrar alerta si hay salida pendiente
if (status.salidaPendiente) {
  mostrarAlerta('Tienes salida pendiente');
}
```

---

## 📊 Estados

| Estado | puedeEntrada | puedeSalida | Acción |
|--------|--------------|-------------|--------|
| **Sin marcar** | ✅ true | ❌ false | Mostrar "Marcar Entrada" |
| **Entrada marcada** | ❌ false | ✅ true | Mostrar "Marcar Salida" |
| **Completo** | ❌ false | ❌ false | "Jornada finalizada" |

---

## 📍 Ubicación

**Archivo:** `Controllers\MarcacionAsistenciaController.cs`  
**Método:** `GetMarcacionStatus(int trabajadorId)` (línea 161)  
**Cambios:** Implementado con mejor respuesta

---

## ✅ Status

✅ Implementado  
✅ Compilación OK  
🚀 Listo para usar

---

**Detalles completos en: `docs\ENDPOINT-Status-Marcacion.md`**
