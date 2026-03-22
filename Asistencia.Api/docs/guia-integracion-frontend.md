# 🚀 Guía Rápida - Integración API Frontend

Esta guía te ayuda a integrar los endpoints de la API en tu aplicación Frontend.

---

## 📌 Configuración Inicial

### 1. Guardar el Token JWT Después de Login
```javascript
// Después de hacer login exitoso
const loginResponse = await fetch('https://127.0.0.1:7209/api/Auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    username: 'superAdmin',
    password: 'password123'
  })
});

const data = await loginResponse.json();
// Guardar el token
localStorage.setItem('token', data.accessToken);
localStorage.setItem('refreshToken', data.refreshToken);
```

### 2. Crear una Función Helper para Requests
```javascript
const apiCall = async (endpoint, options = {}) => {
  const token = localStorage.getItem('token');
  
  const response = await fetch(`https://127.0.0.1:7209${endpoint}`, {
    ...options,
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
      ...options.headers
    }
  });

  if (response.status === 401) {
    // Token expirado, refrescar
    await refreshToken();
    return apiCall(endpoint, options);
  }

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || `HTTP ${response.status}`);
  }

  if (response.status === 204) return null;
  return await response.json();
};
```

---

## 📋 Ejemplos por Funcionalidad

### A. CRUD de Turnos

#### Obtener lista de turnos
```javascript
const obtenerTurnos = async () => {
  try {
    const data = await apiCall('/api/Rrhh/Turnos?pageNumber=1&pageSize=10');
    console.log('Turnos:', data.items);
    return data;
  } catch (error) {
    console.error('Error:', error.message);
  }
};
```

#### Crear turno
```javascript
const crearTurno = async () => {
  try {
    const response = await apiCall('/api/Rrhh/Turnos', {
      method: 'POST',
      body: JSON.stringify({
        tipoTurnoId: 1,
        nombreCodigo: 'TURNO_NOCHE',
        toleranciaIngreso: 15,
        toleranciaSalida: 10,
        esActivo: true
      })
    });
    console.log('Turno creado:', response);
    return response;
  } catch (error) {
    alert(`Error: ${error.message}`);
  }
};
```

#### Actualizar turno
```javascript
const actualizarTurno = async (id, datosTurno) => {
  try {
    await apiCall(`/api/Rrhh/Turnos/${id}`, {
      method: 'PUT',
      body: JSON.stringify({
        id,
        ...datosTurno
      })
    });
    alert('Turno actualizado');
  } catch (error) {
    alert(`Error: ${error.message}`);
  }
};
```

#### Eliminar turno
```javascript
const eliminarTurno = async (id) => {
  if (!confirm('¿Estás seguro de eliminar este turno?')) return;
  
  try {
    await apiCall(`/api/Rrhh/Turnos/${id}`, { method: 'DELETE' });
    alert('Turno eliminado');
  } catch (error) {
    alert(`Error: ${error.message}`);
  }
};
```

---

### B. CRUD de Tipos de Turnos

#### Obtener lista
```javascript
const obtenerTipoTurnos = async () => {
  try {
    const data = await apiCall('/api/Rrhh/TipoTurno?pageNumber=1&pageSize=10');
    return data.items;
  } catch (error) {
    console.error('Error:', error.message);
  }
};
```

#### Crear
```javascript
const crearTipoTurno = async (nombreTipo) => {
  try {
    const response = await apiCall('/api/Rrhh/TipoTurno', {
      method: 'POST',
      body: JSON.stringify({ nombreTipo })
    });
    alert('Tipo de turno creado');
    return response;
  } catch (error) {
    alert(`Error: ${error.message}`);
  }
};
```

#### Actualizar
```javascript
const actualizarTipoTurno = async (id, nombreTipo) => {
  try {
    await apiCall(`/api/Rrhh/TipoTurno/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ id, nombreTipo })
    });
    alert('Tipo de turno actualizado');
  } catch (error) {
    alert(`Error: ${error.message}`);
  }
};
```

#### Eliminar
```javascript
const eliminarTipoTurno = async (id) => {
  if (!confirm('¿Eliminar este tipo de turno?')) return;
  
  try {
    await apiCall(`/api/Rrhh/TipoTurno/${id}`, { method: 'DELETE' });
    alert('Tipo de turno eliminado');
  } catch (error) {
    alert(`Error: ${error.message}`);
  }
};
```

---

### C. Obtener Descansos de un Trabajador

```javascript
const obtenerDescansos = async (idTrabajador, fecha) => {
  try {
    // fecha en formato: 2026-03-20
    const data = await apiCall(`/api/Descansos/${idTrabajador}/${fecha}`);
    return data;
  } catch (error) {
    console.error('Error:', error.message);
  }
};

// Uso:
const descansos = await obtenerDescansos(42, '2026-03-20');
console.log('Descansos:', descansos.dias);
```

---

## 🎨 Ejemplo Completo - Componente React

### Gestión de Turnos
```jsx
import React, { useState, useEffect } from 'react';

const TurnosManager = () => {
  const [turnos, setTurnos] = useState([]);
  const [tiposTurno, setTiposTurno] = useState([]);
  const [loading, setLoading] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [formData, setFormData] = useState({
    tipoTurnoId: '',
    nombreCodigo: '',
    toleranciaIngreso: 0,
    toleranciaSalida: 0,
    esActivo: true
  });

  // Cargar datos iniciales
  useEffect(() => {
    cargarTurnos();
    cargarTiposTurno();
  }, []);

  const cargarTurnos = async () => {
    setLoading(true);
    try {
      const data = await apiCall('/api/Rrhh/Turnos?pageNumber=1&pageSize=10');
      setTurnos(data.items);
    } catch (error) {
      console.error('Error:', error);
    } finally {
      setLoading(false);
    }
  };

  const cargarTiposTurno = async () => {
    try {
      const data = await apiCall('/api/Rrhh/TipoTurno?pageNumber=1&pageSize=10');
      setTiposTurno(data.items);
    } catch (error) {
      console.error('Error:', error);
    }
  };

  const handleGuardar = async (e) => {
    e.preventDefault();
    
    try {
      if (editingId) {
        await apiCall(`/api/Rrhh/Turnos/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify({ id: editingId, ...formData })
        });
        alert('Turno actualizado');
      } else {
        await apiCall('/api/Rrhh/Turnos', {
          method: 'POST',
          body: JSON.stringify(formData)
        });
        alert('Turno creado');
      }
      
      limpiarFormulario();
      cargarTurnos();
    } catch (error) {
      alert(`Error: ${error.message}`);
    }
  };

  const handleEditar = (turno) => {
    setEditingId(turno.id);
    setFormData({
      tipoTurnoId: turno.tipoTurnoId,
      nombreCodigo: turno.nombreCodigo,
      toleranciaIngreso: turno.toleranciaIngreso,
      toleranciaSalida: turno.toleranciaSalida,
      esActivo: turno.esActivo
    });
  };

  const handleEliminar = async (id) => {
    if (!confirm('¿Eliminar este turno?')) return;
    
    try {
      await apiCall(`/api/Rrhh/Turnos/${id}`, { method: 'DELETE' });
      alert('Turno eliminado');
      cargarTurnos();
    } catch (error) {
      alert(`Error: ${error.message}`);
    }
  };

  const limpiarFormulario = () => {
    setEditingId(null);
    setFormData({
      tipoTurnoId: '',
      nombreCodigo: '',
      toleranciaIngreso: 0,
      toleranciaSalida: 0,
      esActivo: true
    });
  };

  return (
    <div className="container">
      <h1>Gestión de Turnos</h1>

      {/* Formulario */}
      <form onSubmit={handleGuardar}>
        <select 
          value={formData.tipoTurnoId}
          onChange={(e) => setFormData({...formData, tipoTurnoId: e.target.value})}
          required
        >
          <option value="">Seleccionar tipo de turno</option>
          {tiposTurno.map(tipo => (
            <option key={tipo.id} value={tipo.id}>
              {tipo.nombreTipo}
            </option>
          ))}
        </select>

        <input
          type="text"
          placeholder="Código del turno"
          value={formData.nombreCodigo}
          onChange={(e) => setFormData({...formData, nombreCodigo: e.target.value})}
          required
          maxLength={20}
        />

        <input
          type="number"
          placeholder="Tolerancia ingreso (min)"
          value={formData.toleranciaIngreso}
          onChange={(e) => setFormData({...formData, toleranciaIngreso: parseInt(e.target.value)})}
        />

        <input
          type="number"
          placeholder="Tolerancia salida (min)"
          value={formData.toleranciaSalida}
          onChange={(e) => setFormData({...formData, toleranciaSalida: parseInt(e.target.value)})}
        />

        <label>
          <input
            type="checkbox"
            checked={formData.esActivo}
            onChange={(e) => setFormData({...formData, esActivo: e.target.checked})}
          />
          Activo
        </label>

        <button type="submit">
          {editingId ? 'Actualizar' : 'Crear'} Turno
        </button>
        {editingId && <button type="button" onClick={limpiarFormulario}>Cancelar</button>}
      </form>

      {/* Tabla */}
      {loading ? (
        <p>Cargando...</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Código</th>
              <th>Tipo</th>
              <th>Tol. Ingreso</th>
              <th>Tol. Salida</th>
              <th>Activo</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {turnos.map(turno => (
              <tr key={turno.id}>
                <td>{turno.id}</td>
                <td>{turno.nombreCodigo}</td>
                <td>{turno.tipoTurno?.nombreTipo}</td>
                <td>{turno.toleranciaIngreso || '-'}</td>
                <td>{turno.toleranciaSalida || '-'}</td>
                <td>{turno.esActivo ? 'Sí' : 'No'}</td>
                <td>
                  <button onClick={() => handleEditar(turno)}>Editar</button>
                  <button onClick={() => handleEliminar(turno.id)}>Eliminar</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default TurnosManager;
```

---

## 🔐 Refrescar Token Expirado

```javascript
const refreshToken = async () => {
  try {
    const token = localStorage.getItem('refreshToken');
    const response = await fetch('https://127.0.0.1:7209/api/Auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: token })
    });

    const data = await response.json();
    localStorage.setItem('token', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
  } catch (error) {
    // Redirigir a login
    window.location.href = '/login';
  }
};
```

---

## ⚠️ Manejo de Errores

```javascript
const handleError = (error, context) => {
  const messages = {
    'duplicado': 'Este registro ya existe',
    'no encontrado': 'El registro no existe',
    'sin permisos': 'No tienes permisos para esta acción',
    'token expirado': 'Sesión expirada, por favor inicia sesión de nuevo'
  };

  const message = messages[error.message.toLowerCase()] || error.message;
  console.error(`[${context}]`, message);
  
  return message;
};
```

---

## 📱 Métodos HTTP Resumen

| Método | URL | Uso |
|--------|-----|-----|
| **GET** | `/api/Rrhh/Turnos` | Listar |
| **GET** | `/api/Rrhh/Turnos/{id}` | Detalle |
| **POST** | `/api/Rrhh/Turnos` | Crear |
| **PUT** | `/api/Rrhh/Turnos/{id}` | Actualizar |
| **DELETE** | `/api/Rrhh/Turnos/{id}` | Eliminar |

---

**Última actualización:** 2026-03-19
