# 📋 ENDPOINT: Crear Cobertura de Turno

## 🎯 Detalles del Endpoint

### URL
```
POST /api/Coberturas
```

### Autenticación
✅ **Requerida**
- Header: `Authorization: Bearer <token>`
- Roles permitidos: `ADMIN`, `SUPERADMIN`, `SUPERVISOR`

---

## 📤 Request Body

### Estructura
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

### Campos

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `fecha` | `DateOnly` (YYYY-MM-DD) | ✅ Sí | Fecha de la cobertura |
| `idTrabajadorCubre` | `int` | ✅ Sí | ID del trabajador que **cubre** |
| `idTrabajadorAusente` | `int` | ✅ Sí | ID del trabajador que **se ausenta** |
| `idHorarioTurnoOriginal` | `int` | ✅ Sí | ID del horario del turno original |
| `tipoCobertura` | `string` | ✅ Sí | Tipo: `"INTERCAMBIO"`, `"SWAP"`, etc. |
| `fechaSwapDevolucion` | `DateOnly?` | ❌ No | Fecha de devolución (para SWAP) |
| `aprobadoPor` | `int?` | ❌ No | ID del usuario que aprueba |

---

## 📥 Response

### Success (201 Created)
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

### Error (400 Bad Request)
```json
{
  "message": "El trabajador que cubre no puede ser el mismo ausente."
}
```

### Error (401 Unauthorized)
```json
{
  "message": "No autorizado"
}
```

### Error (500 Internal Server Error)
```json
{
  "message": "No se pudo registrar la cobertura."
}
```

---

## 🔍 Validaciones

✅ **Backend valida:**
- `IdTrabajadorCubre ≠ IdTrabajadorAusente` (no puede cubrirse a sí mismo)
- Trabajadores existan en BD
- Horario turno exista
- Fecha sea válida

---

## 📍 Ubicación del Código

| Archivo | Línea | Descripción |
|---------|-------|-------------|
| `Controllers\CoberturasController.cs` | 22-56 | Método POST Registrar |
| `Controllers\CoberturasController.cs` | Clase | Ruta: `/api/Coberturas` |

---

## 🚀 Ejemplo de Uso

### cURL
```bash
curl -X POST https://127.0.0.1:7209/api/Coberturas \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "fecha": "2026-03-20",
    "idTrabajadorCubre": 5,
    "idTrabajadorAusente": 2,
    "idHorarioTurnoOriginal": 26,
    "tipoCobertura": "INTERCAMBIO",
    "fechaSwapDevolucion": null,
    "aprobadoPor": null
  }'
```

### JavaScript/Fetch
```javascript
const response = await fetch('https://127.0.0.1:7209/api/Coberturas', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    fecha: "2026-03-20",
    idTrabajadorCubre: 5,
    idTrabajadorAusente: 2,
    idHorarioTurnoOriginal: 26,
    tipoCobertura: "INTERCAMBIO",
    fechaSwapDevolucion: null,
    aprobadoPor: null
  })
});

const result = await response.json();
console.log(result);
```

### TypeScript/Angular
```typescript
this.http.post('/api/Coberturas', {
  fecha: new Date('2026-03-20').toISOString().split('T')[0],
  idTrabajadorCubre: 5,
  idTrabajadorAusente: 2,
  idHorarioTurnoOriginal: 26,
  tipoCobertura: "INTERCAMBIO",
  fechaSwapDevolucion: null,
  aprobadoPor: null
}, {
  headers: new HttpHeaders({
    'Authorization': `Bearer ${this.token}`
  })
}).subscribe(
  (response: any) => {
    console.log('Cobertura creada:', response);
  },
  (error) => {
    console.error('Error:', error);
  }
);
```

---

## 📌 Otros Endpoints Disponibles

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| **POST** | `/api/Coberturas` | Crear cobertura |
| **GET** | `/api/Coberturas` | Listar coberturas (con filtros) |
| **GET** | `/api/Coberturas?fecha=2026-03-20&estado=PENDIENTE&idTrabajador=5` | Con filtros |

---

## ⚠️ Importante

1. **Fecha**: Debe estar en formato `YYYY-MM-DD`
2. **Tipos de Cobertura**: Confirma con backend cuáles son válidos
3. **Aprobación**: Puede crearse con `aprobadoPor: null` y aprobarse después
4. **Validación**: El trabajador que cubre no puede ser el mismo ausente

---

**Listo para implementar en frontend.** ✅
