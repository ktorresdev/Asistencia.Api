# 📚 Documentación Completa de API - Asistencia

## 🎯 Tabla de Contenidos

1. [Autenticación](#autenticación)
2. [Turnos](#turnos)
3. [Tipos de Turnos](#tipos-de-turnos)
4. [Descansos](#descansos)
5. [Códigos de Respuesta](#códigos-de-respuesta)
6. [Ejemplos de Integración](#ejemplos-de-integración)

---

## 🔐 Autenticación

Todos los endpoints requieren un token JWT en el header `Authorization`.

### Login
```http
POST https://127.0.0.1:7209/api/Auth/login
Content-Type: application/json

{
  "username": "superAdmin",
  "password": "password123"
}
```

**Respuesta 200:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 7200
}
```

### Usar el Token
```
Authorization: Bearer {accessToken}
```

---

## 📋 Turnos

### 1. GET /api/Rrhh/Turnos (Listar)
**Autenticación:** ✅ Requerida  
**Permisos:** Todos

```http
GET https://127.0.0.1:7209/api/Rrhh/Turnos?pageNumber=1&pageSize=10
Authorization: Bearer {token}
```

**Respuesta 200:**
```json
{
  "items": [
    {
      "id": 1,
      "tipoTurnoId": 1,
      "nombreCodigo": "TURNO_MAÑANA",
      "toleranciaIngreso": 15,
      "toleranciaSalida": 10,
      "esActivo": true,
      "tipoTurno": { "id": 1, "nombreTipo": "Rotativo" }
    }
  ],
  "totalCount": 1,
  "pageSize": 10,
  "currentPage": 1,
  "totalPages": 1
}
```

---

### 2. GET /api/Rrhh/Turnos/{id} (Detalle)
**Autenticación:** ✅ Requerida  
**Permisos:** Todos

```http
GET https://127.0.0.1:7209/api/Rrhh/Turnos/1
Authorization: Bearer {token}
```

**Respuesta 200:**
```json
{
  "id": 1,
  "tipoTurnoId": 1,
  "nombreCodigo": "TURNO_MAÑANA",
  "toleranciaIngreso": 15,
  "toleranciaSalida": 10,
  "esActivo": true,
  "tipoTurno": { "id": 1, "nombreTipo": "Rotativo" }
}
```

---

### 3. POST /api/Rrhh/Turnos (Crear)
**Autenticación:** ✅ Requerida  
**Permisos:** ADMIN, SUPERADMIN

```http
POST https://127.0.0.1:7209/api/Rrhh/Turnos
Authorization: Bearer {token}
Content-Type: application/json

{
  "tipoTurnoId": 1,
  "nombreCodigo": "TURNO_NOCHE",
  "toleranciaIngreso": 15,
  "toleranciaSalida": 10,
  "esActivo": true
}
```

**Validaciones:**
- `tipoTurnoId`: Requerido, debe existir
- `nombreCodigo`: Requerido, máx 20 caracteres, único
- `toleranciaIngreso/Salida`: Opcional
- `esActivo`: Opcional (default: true)

**Respuesta 201:**
```
Location: https://127.0.0.1:7209/api/Rrhh/Turnos/3
```

---

### 4. PUT /api/Rrhh/Turnos/{id} (Actualizar)
**Autenticación:** ✅ Requerida  
**Permisos:** ADMIN, SUPERADMIN

```http
PUT https://127.0.0.1:7209/api/Rrhh/Turnos/1
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": 1,
  "tipoTurnoId": 1,
  "nombreCodigo": "TURNO_MAÑANA_V2",
  "toleranciaIngreso": 20,
  "toleranciaSalida": 15,
  "esActivo": true
}
```

**Respuesta 204:** (Sin contenido)

---

### 5. DELETE /api/Rrhh/Turnos/{id} (Eliminar)
**Autenticación:** ✅ Requerida  
**Permisos:** ADMIN, SUPERADMIN

```http
DELETE https://127.0.0.1:7209/api/Rrhh/Turnos/1
Authorization: Bearer {token}
```

**Validaciones:**
- No puede tener asignaciones
- No puede tener horarios

**Respuesta 204:** (Sin contenido)

---

## 🏷️ Tipos de Turnos

### 1. GET /api/Rrhh/TipoTurno (Listar)
```http
GET https://127.0.0.1:7209/api/Rrhh/TipoTurno?pageNumber=1&pageSize=10
Authorization: Bearer {token}
```

---

### 2. GET /api/Rrhh/TipoTurno/{id} (Detalle)
```http
GET https://127.0.0.1:7209/api/Rrhh/TipoTurno/1
Authorization: Bearer {token}
```

---

### 3. POST /api/Rrhh/TipoTurno (Crear)
**Permisos:** ADMIN, SUPERADMIN

```http
POST https://127.0.0.1:7209/api/Rrhh/TipoTurno
Authorization: Bearer {token}
Content-Type: application/json

{
  "nombreTipo": "Nocturno"
}
```

**Validación:** `nombreTipo` debe ser único

---

### 4. PUT /api/Rrhh/TipoTurno/{id} (Actualizar)
**Permisos:** ADMIN, SUPERADMIN

```http
PUT https://127.0.0.1:7209/api/Rrhh/TipoTurno/1
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": 1,
  "nombreTipo": "Nocturno Actualizado"
}
```

---

### 5. DELETE /api/Rrhh/TipoTurno/{id} (Eliminar)
**Permisos:** ADMIN, SUPERADMIN

```http
DELETE https://127.0.0.1:7209/api/Rrhh/TipoTurno/1
Authorization: Bearer {token}
```

---

## 😴 Descansos

### GET /api/Descansos/{idTrabajador}/{fecha}
**Formato de fecha:** YYYY-MM-DD

```http
GET https://127.0.0.1:7209/api/Descansos/42/2026-03-20
Authorization: Bearer {token}
```

**Respuesta 200:**
```json
{
  "idTrabajador": 42,
  "fechaLunes": "2026-03-16",
  "dias": [
    {
      "fecha": "2026-03-16",
      "esDescanso": false,
      "esDiaBoleta": false
    },
    {
      "fecha": "2026-03-17",
      "esDescanso": true,
      "esDiaBoleta": false
    }
  ]
}
```

---

## 📊 Códigos de Respuesta

| Código | Significado | Ejemplo |
|--------|------------|---------|
| **200** | OK | Recurso obtenido exitosamente |
| **201** | Created | Recurso creado exitosamente |
| **204** | No Content | Operación exitosa (sin contenido) |
| **400** | Bad Request | Validación fallida / Datos inválidos |
| **401** | Unauthorized | Token inválido / Expirado |
| **403** | Forbidden | Sin permisos para la acción |
| **404** | Not Found | Recurso no existe |
| **500** | Server Error | Error interno del servidor |

---

## 📱 Ejemplos de Integración

### JavaScript/Fetch API
```javascript
// Helper para requests
const apiCall = async (endpoint, method = 'GET', body = null) => {
  const token = localStorage.getItem('token');
  const options = {
    method,
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  };
  
  if (body) options.body = JSON.stringify(body);
  
  const response = await fetch(`https://127.0.0.1:7209${endpoint}`, options);
  
  if (response.status === 401) {
    // Token expirado, refrescar
    window.location.href = '/login';
  }
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
  
  if (response.status === 204) return null;
  return await response.json();
};

// Uso
const turnos = await apiCall('/api/Rrhh/Turnos?pageNumber=1&pageSize=10');
const nuevoTurno = await apiCall('/api/Rrhh/Turnos', 'POST', {
  tipoTurnoId: 1,
  nombreCodigo: 'TURNO_NOCHE',
  toleranciaIngreso: 15,
  toleranciaSalida: 10,
  esActivo: true
});
```

### React Hook
```javascript
import { useState, useEffect } from 'react';

const useTurnos = () => {
  const [turnos, setTurnos] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    cargarTurnos();
  }, []);

  const cargarTurnos = async () => {
    setLoading(true);
    try {
      const data = await apiCall('/api/Rrhh/Turnos?pageNumber=1&pageSize=10');
      setTurnos(data.items);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return { turnos, loading, error, cargarTurnos };
};
```

---

## 🛠️ Importar en Postman

1. Abre Postman
2. Click en "Import"
3. Selecciona el archivo `Postman-Collection-Asistencia-API.json`
4. En el Environment, configura la variable `{{token}}` con tu JWT

---

## 📖 Documentación Adicional

- **API de Turnos:** Ver `api-turnos-documentacion.md`
- **API de Tipos de Turnos:** Ver `api-tipo-turnos-documentacion.md`
- **Guía de Integración:** Ver `guia-integracion-frontend.md`

---

**Versión:** 1.0  
**Última actualización:** 2026-03-19  
**Base URL:** https://127.0.0.1:7209
