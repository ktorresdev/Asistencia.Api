# 🎯 Resumen Rápido - Endpoints de API

## 📋 Todos los Endpoints Disponibles

### 🔐 Autenticación
```
POST /api/Auth/login
```

### 📊 Turnos
```
GET    /api/Rrhh/Turnos                   ← Listar (Paginado)
GET    /api/Rrhh/Turnos/{id}              ← Obtener por ID
POST   /api/Rrhh/Turnos                   ← Crear (ADMIN/SUPERADMIN)
PUT    /api/Rrhh/Turnos/{id}              ← Actualizar (ADMIN/SUPERADMIN)
DELETE /api/Rrhh/Turnos/{id}              ← Eliminar (ADMIN/SUPERADMIN)
```

### 🏷️ Tipos de Turnos
```
GET    /api/Rrhh/TipoTurno                ← Listar (Paginado)
GET    /api/Rrhh/TipoTurno/{id}           ← Obtener por ID
POST   /api/Rrhh/TipoTurno                ← Crear (ADMIN/SUPERADMIN)
PUT    /api/Rrhh/TipoTurno/{id}           ← Actualizar (ADMIN/SUPERADMIN)
DELETE /api/Rrhh/TipoTurno/{id}           ← Eliminar (ADMIN/SUPERADMIN)
```

### 😴 Descansos
```
GET    /api/Descansos/{idTrabajador}/{fecha}  ← Obtener descansos (Todos)
```

---

## 🚀 Quick Start

### 1. Loguéarse
```bash
curl -X POST "https://127.0.0.1:7209/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"superAdmin","password":"password123"}'
```
✅ Respuesta: `{ "accessToken": "...", "refreshToken": "..." }`

### 2. Guardar el Token
```javascript
const token = "eyJhbGciOiJIUzI1NiIs...";
localStorage.setItem('token', token);
```

### 3. Listar Turnos
```bash
curl -X GET "https://127.0.0.1:7209/api/Rrhh/Turnos?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer {token}"
```

### 4. Crear Turno
```bash
curl -X POST "https://127.0.0.1:7209/api/Rrhh/Turnos" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "tipoTurnoId": 1,
    "nombreCodigo": "TURNO_NOCHE",
    "toleranciaIngreso": 15,
    "toleranciaSalida": 10,
    "esActivo": true
  }'
```

### 5. Actualizar Turno
```bash
curl -X PUT "https://127.0.0.1:7209/api/Rrhh/Turnos/1" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 1,
    "tipoTurnoId": 1,
    "nombreCodigo": "TURNO_NOCHE_V2",
    "toleranciaIngreso": 20,
    "toleranciaSalida": 15,
    "esActivo": true
  }'
```

### 6. Eliminar Turno
```bash
curl -X DELETE "https://127.0.0.1:7209/api/Rrhh/Turnos/1" \
  -H "Authorization: Bearer {token}"
```

---

## 📱 Servicio Reutilizable (JavaScript)

```javascript
class ApiService {
  constructor(baseUrl = 'https://127.0.0.1:7209') {
    this.baseUrl = baseUrl;
    this.token = localStorage.getItem('token');
  }

  setToken(token) {
    this.token = token;
    localStorage.setItem('token', token);
  }

  async request(endpoint, method = 'GET', body = null) {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method,
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json'
      },
      body: body ? JSON.stringify(body) : null
    });

    if (response.status === 401) throw new Error('Token expirado');
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    if (response.status === 204) return null;
    
    return await response.json();
  }

  // TURNOS
  getTurnos(page = 1, size = 10) {
    return this.request(`/api/Rrhh/Turnos?pageNumber=${page}&pageSize=${size}`);
  }

  getTurno(id) {
    return this.request(`/api/Rrhh/Turnos/${id}`);
  }

  createTurno(data) {
    return this.request('/api/Rrhh/Turnos', 'POST', data);
  }

  updateTurno(id, data) {
    return this.request(`/api/Rrhh/Turnos/${id}`, 'PUT', { id, ...data });
  }

  deleteTurno(id) {
    return this.request(`/api/Rrhh/Turnos/${id}`, 'DELETE');
  }

  // TIPOS DE TURNOS
  getTiposTurnos(page = 1, size = 10) {
    return this.request(`/api/Rrhh/TipoTurno?pageNumber=${page}&pageSize=${size}`);
  }

  getTipoTurno(id) {
    return this.request(`/api/Rrhh/TipoTurno/${id}`);
  }

  createTipoTurno(nombreTipo) {
    return this.request('/api/Rrhh/TipoTurno', 'POST', { nombreTipo });
  }

  updateTipoTurno(id, nombreTipo) {
    return this.request(`/api/Rrhh/TipoTurno/${id}`, 'PUT', { id, nombreTipo });
  }

  deleteTipoTurno(id) {
    return this.request(`/api/Rrhh/TipoTurno/${id}`, 'DELETE');
  }

  // DESCANSOS
  getDescansos(idTrabajador, fecha) {
    return this.request(`/api/Descansos/${idTrabajador}/${fecha}`);
  }
}

// Uso
const api = new ApiService();
const turnos = await api.getTurnos();
const nuevoTurno = await api.createTurno({
  tipoTurnoId: 1,
  nombreCodigo: 'TURNO_NOCHE',
  toleranciaIngreso: 15,
  toleranciaSalida: 10,
  esActivo: true
});
```

---

## ⚡ Validaciones Importantes

### Crear/Actualizar Turno
```javascript
{
  tipoTurnoId: 1,              // ✅ Requerido, debe existir
  nombreCodigo: "TURNO_NOCHE", // ✅ Requerido, máx 20 chars, único
  toleranciaIngreso: 15,       // ❌ Opcional (minutos)
  toleranciaSalida: 10,        // ❌ Opcional (minutos)
  esActivo: true               // ❌ Opcional (default: true)
}
```

### Crear/Actualizar Tipo de Turno
```javascript
{
  nombreTipo: "Nocturno"       // ✅ Requerido, máx 50 chars, único
}
```

### Obtener Descansos
```
Fecha debe estar en formato: YYYY-MM-DD
Ejemplo: /api/Descansos/42/2026-03-20
```

---

## 🎨 Ejemplo Completo - React Component

```jsx
import { useState, useEffect } from 'react';
import ApiService from './services/api';

export default function TurnosManager() {
  const api = new ApiService();
  const [turnos, setTurnos] = useState([]);
  const [form, setForm] = useState({
    tipoTurnoId: '',
    nombreCodigo: '',
    toleranciaIngreso: 0,
    toleranciaSalida: 0,
    esActivo: true
  });

  useEffect(() => {
    cargar();
  }, []);

  const cargar = async () => {
    try {
      const data = await api.getTurnos(1, 10);
      setTurnos(data.items);
    } catch (error) {
      alert('Error: ' + error.message);
    }
  };

  const guardar = async (e) => {
    e.preventDefault();
    try {
      await api.createTurno(form);
      alert('Turno creado');
      setForm({
        tipoTurnoId: '',
        nombreCodigo: '',
        toleranciaIngreso: 0,
        toleranciaSalida: 0,
        esActivo: true
      });
      cargar();
    } catch (error) {
      alert('Error: ' + error.message);
    }
  };

  return (
    <div>
      <h1>Gestionar Turnos</h1>
      
      <form onSubmit={guardar}>
        <input
          type="number"
          placeholder="ID Tipo Turno"
          value={form.tipoTurnoId}
          onChange={(e) => setForm({...form, tipoTurnoId: e.target.value})}
          required
        />
        <input
          type="text"
          placeholder="Código"
          value={form.nombreCodigo}
          onChange={(e) => setForm({...form, nombreCodigo: e.target.value})}
          required
          maxLength="20"
        />
        <input
          type="number"
          placeholder="Tolerancia Ingreso"
          value={form.toleranciaIngreso}
          onChange={(e) => setForm({...form, toleranciaIngreso: e.target.value})}
        />
        <button type="submit">Crear</button>
      </form>

      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>Código</th>
            <th>Activo</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          {turnos.map(turno => (
            <tr key={turno.id}>
              <td>{turno.id}</td>
              <td>{turno.nombreCodigo}</td>
              <td>{turno.esActivo ? 'Sí' : 'No'}</td>
              <td>
                <button onClick={() => alert(JSON.stringify(turno))}>Ver</button>
                <button onClick={() => api.deleteTurno(turno.id).then(() => cargar())}>Eliminar</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

---

## 📋 Checklist de Integración

- ✅ Obtener token JWT en login
- ✅ Guardar token en localStorage
- ✅ Incluir token en header `Authorization: Bearer {token}`
- ✅ Manejar errores 401 (token expirado)
- ✅ Validar datos antes de enviar
- ✅ Mostrar mensajes de error al usuario
- ✅ Refrescar lista después de crear/actualizar/eliminar
- ✅ Validar permisos (ADMIN/SUPERADMIN para POST/PUT/DELETE)

---

## 📚 Documentación Completa

Ver archivos en la carpeta `docs/`:
- `README-API.md` - Documentación completa
- `api-turnos-documentacion.md` - Detalles de endpoints de Turnos
- `api-tipo-turnos-documentacion.md` - Detalles de endpoints de Tipo Turnos
- `guia-integracion-frontend.md` - Ejemplos de integración avanzados
- `Postman-Collection-Asistencia-API.json` - Colección Postman

---

**Última actualización:** 2026-03-19
