# 🎯 ENDPOINT: Consultar Estado de Marcación del Trabajador

## 📍 URL

```
GET /api/Rrhh/MarcacionAsistencia/status/{trabajadorId}
```

### Ejemplo
```
GET https://127.0.0.1:7209/api/Rrhh/MarcacionAsistencia/status/5
Authorization: Bearer <token>
```

---

## 🔐 Autenticación

✅ **Requerida**
- Header: `Authorization: Bearer <token>`

---

## 📤 Response (200 OK)

```json
{
  "success": true,
  "trabajadorId": 5,
  
  "horarioProgramado": "09:00 - 17:00",
  
  "marcacionEntrada": "2026-03-20T09:15:32.123",
  "marcacionSalida": null,
  "tiempoTrabajadoMinutos": 45.5,
  "tiempoTrabajadoFormato": "45 minutos 30 segundos",
  
  "estado": "Trabajador activo",
  
  "puedeMarcarEntrada": false,
  "puedeMarcarSalida": true,
  "salidaPendiente": true
}
```

---

## 📊 Campos de Respuesta

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `success` | boolean | ✅ Indica si fue exitoso |
| `trabajadorId` | int | ID del trabajador |
| `horarioProgramado` | string | Horario asignado (ej: "09:00 - 17:00") |
| `marcacionEntrada` | DateTime? | Fecha/hora de entrada (null si no marcó) |
| `marcacionSalida` | DateTime? | Fecha/hora de salida (null si no salió) |
| `tiempoTrabajadoMinutos` | double | Minutos trabajados hoy |
| `tiempoTrabajadoFormato` | string | Formato legible (ej: "5 horas 30 minutos") |
| `estado` | string | Mensaje de estado actual |
| `puedeMarcarEntrada` | boolean | ✅ ¿Puede marcar entrada ahora? |
| `puedeMarcarSalida` | boolean | ✅ ¿Puede marcar salida ahora? |
| `salidaPendiente` | boolean | ⚠️ ¿Tiene salida pendiente? |

---

## 🎯 Casos de Uso

### Caso 1: Sin Marcar Entrada
```json
{
  "puedeMarcarEntrada": true,
  "puedeMarcarSalida": false,
  "salidaPendiente": false,
  "marcacionEntrada": null,
  "marcacionSalida": null
}
```
**Acción:** Mostrar botón "Marcar Entrada" ✅

### Caso 2: Entrada Marcada, Salida Pendiente
```json
{
  "puedeMarcarEntrada": false,
  "puedeMarcarSalida": true,
  "salidaPendiente": true,
  "marcacionEntrada": "2026-03-20T09:15:32",
  "marcacionSalida": null
}
```
**Acción:** Mostrar botón "Marcar Salida" ✅ + Mostrar alerta "Tienes salida pendiente"

### Caso 3: Ambas Marcadas
```json
{
  "puedeMarcarEntrada": false,
  "puedeMarcarSalida": false,
  "salidaPendiente": false,
  "marcacionEntrada": "2026-03-20T09:15:32",
  "marcacionSalida": "2026-03-20T17:45:10"
}
```
**Acción:** Mostrar "Jornada finalizada" + "Tiempo trabajado: 8 horas 30 minutos"

---

## ❌ Error (404 Not Found)

Si el trabajador no existe o no tiene turno asignado:

```json
{
  "success": false,
  "code": "ERROR_TRABAJADOR_NO_ENCONTRADO",
  "message": "No se encontré el trabajador o no tiene turno asignado.",
  "detail": "..."
}
```

---

## 💻 Ejemplos de Uso

### JavaScript/Fetch
```javascript
const trabajadorId = 5;
const token = localStorage.getItem('token');

const response = await fetch(
  `https://127.0.0.1:7209/api/Rrhh/MarcacionAsistencia/status/${trabajadorId}`,
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  }
);

const data = await response.json();

if (data.success) {
  // Mostrar estado
  console.log('Puede marcar entrada:', data.puedeMarcarEntrada);
  console.log('Puede marcar salida:', data.puedeMarcarSalida);
  console.log('Horario:', data.horarioProgramado);
  console.log('Tiempo trabajado:', data.tiempoTrabajadoFormato);
  
  // Control de botones
  document.getElementById('btnEntrada').disabled = !data.puedeMarcarEntrada;
  document.getElementById('btnSalida').disabled = !data.puedeMarcarSalida;
}
```

### TypeScript/Angular
```typescript
export class MarcacionComponent {
  constructor(private http: HttpClient) {}

  consultarEstado(trabajadorId: number) {
    this.http.get(`/api/Rrhh/MarcacionAsistencia/status/${trabajadorId}`)
      .subscribe(
        (response: any) => {
          if (response.success) {
            this.horarioProgramado = response.horarioProgramado;
            this.puedeEntrada = response.puedeMarcarEntrada;
            this.puedeSalida = response.puedeMarcarSalida;
            this.tiempoTrabajado = response.tiempoTrabajadoFormato;
            
            // Actualizar UI
            this.actualizarBotones();
          }
        },
        (error) => {
          console.error('Error al consultar estado:', error);
        }
      );
  }
  
  actualizarBotones() {
    // Si puede marcar entrada, mostrar botón
    // Si puede marcar salida, mostrar botón y alerta
  }
}
```

### HTML + JavaScript Vanilla
```html
<div id="estado-marcacion">
  <div id="horario">
    Horario: <span id="horarioProgramado">--:-- a --:--</span>
  </div>
  
  <div id="tiempo-trabajado">
    Tiempo trabajado: <span id="tiempoFormato">0 minutos</span>
  </div>
  
  <div id="marcaciones">
    <p>Entrada: <span id="entrada">--</span></p>
    <p>Salida: <span id="salida">--</span></p>
  </div>
  
  <div id="botones">
    <button id="btnEntrada" onclick="marcarEntrada()">Marcar Entrada</button>
    <button id="btnSalida" onclick="marcarSalida()" disabled>Marcar Salida</button>
  </div>
  
  <div id="alerta" style="display:none;" class="alert alert-warning">
    ⚠️ Tienes salida pendiente
  </div>
</div>

<script>
function consultarEstado(trabajadorId) {
  fetch(`/api/Rrhh/MarcacionAsistencia/status/${trabajadorId}`, {
    headers: {
      'Authorization': `Bearer ${localStorage.getItem('token')}`
    }
  })
  .then(r => r.json())
  .then(data => {
    if (data.success) {
      // Llenar datos
      document.getElementById('horarioProgramado').textContent = data.horarioProgramado;
      document.getElementById('tiempoFormato').textContent = data.tiempoTrabajadoFormato;
      document.getElementById('entrada').textContent = data.marcacionEntrada ? 
        new Date(data.marcacionEntrada).toLocaleTimeString() : '--';
      document.getElementById('salida').textContent = data.marcacionSalida ? 
        new Date(data.marcacionSalida).toLocaleTimeString() : '--';
      
      // Control de botones
      document.getElementById('btnEntrada').disabled = !data.puedeMarcarEntrada;
      document.getElementById('btnSalida').disabled = !data.puedeMarcarSalida;
      
      // Mostrar alerta si hay salida pendiente
      document.getElementById('alerta').style.display = 
        data.salidaPendiente ? 'block' : 'none';
    }
  });
}

// Llamar al cargar la página
consultarEstado(5); // ID del trabajador

// Actualizar cada 30 segundos
setInterval(() => consultarEstado(5), 30000);
</script>
```

---

## 🔄 Actualización en Tiempo Real

Para mantener la UI actualizada, se recomienda:

```javascript
// Actualizar estado cada 30 segundos
setInterval(() => {
  consultarEstado(trabajadorId);
}, 30000);

// O cuando el usuario hace clic en un botón
async function marcarEntrada() {
  // ... realizar marcación ...
  
  // Luego actualizar estado
  consultarEstado(trabajadorId);
}
```

---

## 📌 Estados Posibles

```
┌─ Sin marcar entrada
│  ├─ puedeMarcarEntrada: true
│  └─ puedeMarcarSalida: false
│
├─ Entrada marcada, salida pendiente
│  ├─ puedeMarcarEntrada: false
│  ├─ puedeMarcarSalida: true
│  └─ salidaPendiente: true
│
└─ Jornada completada
   ├─ puedeMarcarEntrada: false
   └─ puedeMarcarSalida: false
```

---

## ✅ Resumen

| Aspecto | Detalle |
|---------|---------|
| **Endpoint** | `GET /api/Rrhh/MarcacionAsistencia/status/{trabajadorId}` |
| **Autenticación** | ✅ Bearer Token |
| **Propósito** | Consultar si puede marcar y estado actual |
| **Response** | JSON con estado, permisos y tiempos |
| **Uso** | Actualizar UI, habilitar/deshabilitar botones |

---

**Endpoint implementado y listo para usar.** ✅
