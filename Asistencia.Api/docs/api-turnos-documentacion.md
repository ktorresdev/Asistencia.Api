# 📋 Documentación API - Turnos

## Descripción General
API RESTful para gestionar turnos del sistema de asistencia. Los turnos son configuraciones de horarios que se asignan a los trabajadores.

**Base URL:** `https://127.0.0.1:7209/api/Rrhh/Turnos`

**Autenticación:** ✅ Requerida (JWT Bearer Token)

---

## 1️⃣ Listar Todos los Turnos

### Endpoint
```
GET /api/Rrhh/Turnos
```

### Parámetros Query
| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `pageNumber` | integer | ✅ Sí | Número de página (ej: 1, 2, 3) |
| `pageSize` | integer | ✅ Sí | Cantidad de registros por página (ej: 10, 20) |

### Headers Requeridos
```
Authorization: Bearer {token_jwt}
Content-Type: application/json
```

### Ejemplo de Request
```bash
curl -X GET "https://127.0.0.1:7209/api/Rrhh/Turnos?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

### Respuesta Exitosa (200 OK)
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
      "tipoTurno": {
        "id": 1,
        "nombreTipo": "Rotativo"
      },
      "horariosTurno": []
    },
    {
      "id": 2,
      "tipoTurnoId": 1,
      "nombreCodigo": "TURNO_TARDE",
      "toleranciaIngreso": 15,
      "toleranciaSalida": 10,
      "esActivo": true,
      "tipoTurno": {
        "id": 1,
        "nombreTipo": "Rotativo"
      },
      "horariosTurno": []
    }
  ],
  "totalCount": 2,
  "pageSize": 10,
  "currentPage": 1,
  "totalPages": 1
}
```

### Códigos de Respuesta
| Código | Significado |
|--------|------------|
| 200 | ✅ OK - Listado obtenido exitosamente |
| 401 | ❌ Unauthorized - Token inválido o expirado |
| 403 | ❌ Forbidden - Sin permisos de acceso |

---

## 2️⃣ Obtener Un Turno por ID

### Endpoint
```
GET /api/Rrhh/Turnos/{id}
```

### Parámetros Path
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | integer | ID del turno a obtener |

### Headers Requeridos
```
Authorization: Bearer {token_jwt}
Content-Type: application/json
```

### Ejemplo de Request
```bash
curl -X GET "https://127.0.0.1:7209/api/Rrhh/Turnos/1" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

### Respuesta Exitosa (200 OK)
```json
{
  "id": 1,
  "tipoTurnoId": 1,
  "nombreCodigo": "TURNO_MAÑANA",
  "toleranciaIngreso": 15,
  "toleranciaSalida": 10,
  "esActivo": true,
  "tipoTurno": {
    "id": 1,
    "nombreTipo": "Rotativo"
  },
  "horariosTurno": []
}
```

### Códigos de Respuesta
| Código | Significado |
|--------|------------|
| 200 | ✅ OK - Turno encontrado |
| 401 | ❌ Unauthorized - Token inválido |
| 404 | ❌ Not Found - Turno no existe |

---

## 3️⃣ Crear Un Turno

### Endpoint
```
POST /api/Rrhh/Turnos
```

### Permisos Requeridos
- ✅ **ADMIN**
- ✅ **SUPERADMIN**

### Headers Requeridos
```
Authorization: Bearer {token_jwt}
Content-Type: application/json
```

### Body del Request
```json
{
  "tipoTurnoId": 1,
  "nombreCodigo": "TURNO_NOCHE",
  "toleranciaIngreso": 15,
  "toleranciaSalida": 10,
  "esActivo": true
}
```

### Validaciones
| Campo | Tipo | Validación |
|-------|------|-----------|
| `tipoTurnoId` | integer | ✅ Requerido, debe existir en BD |
| `nombreCodigo` | string | ✅ Requerido, máx 20 caracteres, debe ser único |
| `toleranciaIngreso` | integer? | ❌ Opcional, minutos de tolerancia |
| `toleranciaSalida` | integer? | ❌ Opcional, minutos de tolerancia |
| `esActivo` | boolean | ❌ Opcional (default: true) |

### Ejemplo de Request
```bash
curl -X POST "https://127.0.0.1:7209/api/Rrhh/Turnos" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "tipoTurnoId": 1,
    "nombreCodigo": "TURNO_NOCHE",
    "toleranciaIngreso": 15,
    "toleranciaSalida": 10,
    "esActivo": true
  }'
```

### Respuesta Exitosa (201 Created)
```json
{
  "tipoTurnoId": 1,
  "nombreCodigo": "TURNO_NOCHE",
  "toleranciaIngreso": 15,
  "toleranciaSalida": 10,
  "esActivo": true
}
```

**Header Location:**
```
Location: https://127.0.0.1:7209/api/Rrhh/Turnos/3
```

### Errores Posibles

**400 Bad Request - Validación falla**
```json
{
  "message": "Ya existe un turno con el código 'TURNO_NOCHE'."
}
```

**404 Not Found - TipoTurno no existe**
```json
{
  "message": "TipoTurno con ID 999 no encontrado."
}
```

**403 Forbidden - Sin permisos**
```json
{
  "message": "Solo ADMIN o SUPERADMIN pueden crear turnos"
}
```

### Códigos de Respuesta
| Código | Significado |
|--------|------------|
| 201 | ✅ Created - Turno creado exitosamente |
| 400 | ❌ Bad Request - Validación falló |
| 401 | ❌ Unauthorized - Token inválido |
| 403 | ❌ Forbidden - Sin permisos (no es ADMIN) |
| 404 | ❌ Not Found - TipoTurno no existe |

---

## 4️⃣ Actualizar Un Turno

### Endpoint
```
PUT /api/Rrhh/Turnos/{id}
```

### Permisos Requeridos
- ✅ **ADMIN**
- ✅ **SUPERADMIN**

### Parámetros Path
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | integer | ID del turno a actualizar |

### Headers Requeridos
```
Authorization: Bearer {token_jwt}
Content-Type: application/json
```

### Body del Request
```json
{
  "id": 1,
  "tipoTurnoId": 1,
  "nombreCodigo": "TURNO_MAÑANA_ACTUALIZADO",
  "toleranciaIngreso": 20,
  "toleranciaSalida": 15,
  "esActivo": true
}
```

### Validaciones
| Campo | Validación |
|-------|-----------|
| `id` | ✅ Requerido, debe coincidir con el ID de la URL |
| `tipoTurnoId` | ✅ Requerido, debe existir en BD |
| `nombreCodigo` | ✅ Requerido, máx 20 caracteres, debe ser único |
| `toleranciaIngreso` | ❌ Opcional |
| `toleranciaSalida` | ❌ Opcional |
| `esActivo` | ❌ Opcional |

### Ejemplo de Request
```bash
curl -X PUT "https://127.0.0.1:7209/api/Rrhh/Turnos/1" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "id": 1,
    "tipoTurnoId": 1,
    "nombreCodigo": "TURNO_MAÑANA_ACTUALIZADO",
    "toleranciaIngreso": 20,
    "toleranciaSalida": 15,
    "esActivo": true
  }'
```

### Respuesta Exitosa (204 No Content)
```
Status: 204 No Content
Body: (vacío)
```

### Errores Posibles

**400 Bad Request - ID no coincide**
```json
{
  "message": "El ID del turno en la URL no coincide con el del cuerpo de la solicitud."
}
```

**400 Bad Request - Nombre duplicado**
```json
{
  "message": "Ya existe otro turno con el código 'TURNO_MAÑANA_ACTUALIZADO'."
}
```

**404 Not Found - Turno no existe**
```json
{
  "message": "Turno con ID 999 no encontrado."
}
```

### Códigos de Respuesta
| Código | Significado |
|--------|------------|
| 204 | ✅ No Content - Turno actualizado exitosamente |
| 400 | ❌ Bad Request - Validación falló |
| 401 | ❌ Unauthorized - Token inválido |
| 403 | ❌ Forbidden - Sin permisos |
| 404 | ❌ Not Found - Turno no existe |

---

## 5️⃣ Eliminar Un Turno

### Endpoint
```
DELETE /api/Rrhh/Turnos/{id}
```

### Permisos Requeridos
- ✅ **ADMIN**
- ✅ **SUPERADMIN**

### Parámetros Path
| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | integer | ID del turno a eliminar |

### Headers Requeridos
```
Authorization: Bearer {token_jwt}
Content-Type: application/json
```

### Ejemplo de Request
```bash
curl -X DELETE "https://127.0.0.1:7209/api/Rrhh/Turnos/1" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

### Respuesta Exitosa (204 No Content)
```
Status: 204 No Content
Body: (vacío)
```

### Errores Posibles

**400 Bad Request - Tiene asignaciones**
```json
{
  "message": "No se puede eliminar el turno porque tiene asignaciones asociadas. Primero elimina las asignaciones."
}
```

**400 Bad Request - Tiene horarios**
```json
{
  "message": "No se puede eliminar el turno porque tiene horarios asociados. Primero elimina los horarios."
}
```

**404 Not Found - Turno no existe**
```json
{
  "message": "Turno con ID 999 no encontrado."
}
```

### Códigos de Respuesta
| Código | Significado |
|--------|------------|
| 204 | ✅ No Content - Turno eliminado exitosamente |
| 400 | ❌ Bad Request - No se puede eliminar (tiene relaciones) |
| 401 | ❌ Unauthorized - Token inválido |
| 403 | ❌ Forbidden - Sin permisos |
| 404 | ❌ Not Found - Turno no existe |

---

## 🔒 Manejo de Errores Globales

### 401 Unauthorized
```json
{
  "message": "Unauthorized"
}
```
**Causa:** Token JWT inválido, expirado o no incluido.

### 403 Forbidden
```json
{
  "message": "Access Denied"
}
```
**Causa:** Usuario no tiene rol ADMIN o SUPERADMIN.

### 500 Internal Server Error
```json
{
  "message": "Error al procesar la solicitud"
}
```
**Causa:** Error interno del servidor.

---

## 📱 Ejemplo de Implementación en Frontend (JavaScript/React)

### 1. Obtener Token JWT
```javascript
const loginResponse = await fetch('https://127.0.0.1:7209/api/Auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    username: 'superAdmin',
    password: 'password123'
  })
});

const { accessToken } = await loginResponse.json();
```

### 2. Listar Turnos
```javascript
const getTurnos = async (pageNumber = 1, pageSize = 10) => {
  const response = await fetch(
    `https://127.0.0.1:7209/api/Rrhh/Turnos?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
      }
    }
  );
  return await response.json();
};
```

### 3. Obtener Turno por ID
```javascript
const getTurnoById = async (id) => {
  const response = await fetch(
    `https://127.0.0.1:7209/api/Rrhh/Turnos/${id}`,
    {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
      }
    }
  );
  return await response.json();
};
```

### 4. Crear Turno
```javascript
const createTurno = async (turnoData) => {
  const response = await fetch(
    'https://127.0.0.1:7209/api/Rrhh/Turnos',
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(turnoData)
    }
  );
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
  
  return await response.json();
};

// Uso:
try {
  const nuevoTurno = await createTurno({
    tipoTurnoId: 1,
    nombreCodigo: 'TURNO_NOCHE',
    toleranciaIngreso: 15,
    toleranciaSalida: 10,
    esActivo: true
  });
  console.log('Turno creado:', nuevoTurno);
} catch (error) {
  console.error('Error:', error.message);
}
```

### 5. Actualizar Turno
```javascript
const updateTurno = async (id, turnoData) => {
  const response = await fetch(
    `https://127.0.0.1:7209/api/Rrhh/Turnos/${id}`,
    {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        id,
        ...turnoData
      })
    }
  );
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
};

// Uso:
try {
  await updateTurno(1, {
    tipoTurnoId: 1,
    nombreCodigo: 'TURNO_MAÑANA_ACTUALIZADO',
    toleranciaIngreso: 20,
    toleranciaSalida: 15,
    esActivo: true
  });
  console.log('Turno actualizado exitosamente');
} catch (error) {
  console.error('Error:', error.message);
}
```

### 6. Eliminar Turno
```javascript
const deleteTurno = async (id) => {
  const response = await fetch(
    `https://127.0.0.1:7209/api/Rrhh/Turnos/${id}`,
    {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
      }
    }
  );
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
};

// Uso:
try {
  await deleteTurno(1);
  console.log('Turno eliminado exitosamente');
} catch (error) {
  console.error('Error:', error.message);
}
```

---

## 🛠️ Servicio Reutilizable (TypeScript/React)

```typescript
// api/turnosService.ts
class TurnosService {
  private baseUrl = 'https://127.0.0.1:7209/api/Rrhh/Turnos';
  private token: string = '';

  setToken(token: string) {
    this.token = token;
  }

  private getHeaders() {
    return {
      'Authorization': `Bearer ${this.token}`,
      'Content-Type': 'application/json'
    };
  }

  async getAll(pageNumber: number = 1, pageSize: number = 10) {
    const response = await fetch(
      `${this.baseUrl}?pageNumber=${pageNumber}&pageSize=${pageSize}`,
      { method: 'GET', headers: this.getHeaders() }
    );
    if (!response.ok) throw new Error(`Error: ${response.status}`);
    return await response.json();
  }

  async getById(id: number) {
    const response = await fetch(
      `${this.baseUrl}/${id}`,
      { method: 'GET', headers: this.getHeaders() }
    );
    if (!response.ok) throw new Error(`Error: ${response.status}`);
    return await response.json();
  }

  async create(data: any) {
    const response = await fetch(
      this.baseUrl,
      {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify(data)
      }
    );
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message);
    }
    return await response.json();
  }

  async update(id: number, data: any) {
    const response = await fetch(
      `${this.baseUrl}/${id}`,
      {
        method: 'PUT',
        headers: this.getHeaders(),
        body: JSON.stringify({ id, ...data })
      }
    );
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message);
    }
  }

  async delete(id: number) {
    const response = await fetch(
      `${this.baseUrl}/${id}`,
      { method: 'DELETE', headers: this.getHeaders() }
    );
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message);
    }
  }
}

export default new TurnosService();
```

### Uso en React:
```typescript
import turnosService from './api/turnosService';

function TurnosPage() {
  const [turnos, setTurnos] = useState([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    cargarTurnos();
  }, []);

  const cargarTurnos = async () => {
    setLoading(true);
    try {
      const data = await turnosService.getAll(1, 10);
      setTurnos(data.items);
    } catch (error) {
      console.error('Error:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      {loading ? <p>Cargando...</p> : (
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Código</th>
              <th>Tipo Turno</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {turnos.map(turno => (
              <tr key={turno.id}>
                <td>{turno.id}</td>
                <td>{turno.nombreCodigo}</td>
                <td>{turno.tipoTurno?.nombreTipo}</td>
                <td>
                  <button onClick={() => handleEdit(turno)}>Editar</button>
                  <button onClick={() => handleDelete(turno.id)}>Eliminar</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
```

---

## 📝 Resumen Rápido

| Método | Endpoint | Descripción | Permisos |
|--------|----------|------------|----------|
| **GET** | `/Turnos` | Listar turnos | Cualquier usuario autenticado |
| **GET** | `/Turnos/{id}` | Obtener turno por ID | Cualquier usuario autenticado |
| **POST** | `/Turnos` | Crear turno | ADMIN, SUPERADMIN |
| **PUT** | `/Turnos/{id}` | Actualizar turno | ADMIN, SUPERADMIN |
| **DELETE** | `/Turnos/{id}` | Eliminar turno | ADMIN, SUPERADMIN |

---

**Última actualización:** 2026-03-19
**Versión API:** 1.0
