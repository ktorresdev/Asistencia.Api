# 📋 Documentación API - Tipo Turnos

## Descripción General
API RESTful para gestionar tipos de turnos. Los tipos de turnos clasifican los turnos (ej: Rotativo, Fijo, etc.)

**Base URL:** `https://127.0.0.1:7209/api/Rrhh/TipoTurno`

**Autenticación:** ✅ Requerida (JWT Bearer Token)

---

## 1️⃣ Listar Todos los Tipos de Turnos

### Endpoint
```
GET /api/Rrhh/TipoTurno
```

### Parámetros Query
| Parámetro | Tipo | Requerido |
|-----------|------|-----------|
| `pageNumber` | integer | ✅ Sí |
| `pageSize` | integer | ✅ Sí |

### Ejemplo
```bash
curl -X GET "https://127.0.0.1:7209/api/Rrhh/TipoTurno?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer {token}"
```

### Respuesta (200 OK)
```json
{
  "items": [
    {
      "id": 1,
      "nombreTipo": "Rotativo"
    },
    {
      "id": 2,
      "nombreTipo": "Fijo"
    }
  ],
  "totalCount": 2,
  "pageSize": 10,
  "currentPage": 1,
  "totalPages": 1
}
```

---

## 2️⃣ Obtener Tipo de Turno por ID

### Endpoint
```
GET /api/Rrhh/TipoTurno/{id}
```

### Ejemplo
```bash
curl -X GET "https://127.0.0.1:7209/api/Rrhh/TipoTurno/1" \
  -H "Authorization: Bearer {token}"
```

### Respuesta (200 OK)
```json
{
  "id": 1,
  "nombreTipo": "Rotativo"
}
```

---

## 3️⃣ Crear Tipo de Turno

### Endpoint
```
POST /api/Rrhh/TipoTurno
```

### Permisos
- ✅ ADMIN
- ✅ SUPERADMIN

### Body del Request
```json
{
  "nombreTipo": "Nocturno"
}
```

### Validaciones
| Campo | Validación |
|-------|-----------|
| `nombreTipo` | ✅ Requerido, máx 50 caracteres, debe ser único |

### Ejemplo
```bash
curl -X POST "https://127.0.0.1:7209/api/Rrhh/TipoTurno" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"nombreTipo": "Nocturno"}'
```

### Respuesta (201 Created)
```json
{
  "nombreTipo": "Nocturno"
}
```

### Errores
- **400** - Nombre duplicado: `"Ya existe un tipo de turno con el nombre 'Nocturno'"`
- **403** - Sin permisos: Usuario no es ADMIN/SUPERADMIN

---

## 4️⃣ Actualizar Tipo de Turno

### Endpoint
```
PUT /api/Rrhh/TipoTurno/{id}
```

### Body del Request
```json
{
  "id": 1,
  "nombreTipo": "Rotativo Actualizado"
}
```

### Ejemplo
```bash
curl -X PUT "https://127.0.0.1:7209/api/Rrhh/TipoTurno/1" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"id": 1, "nombreTipo": "Rotativo Actualizado"}'
```

### Respuesta (204 No Content)
```
(Sin contenido)
```

---

## 5️⃣ Eliminar Tipo de Turno

### Endpoint
```
DELETE /api/Rrhh/TipoTurno/{id}
```

### Ejemplo
```bash
curl -X DELETE "https://127.0.0.1:7209/api/Rrhh/TipoTurno/1" \
  -H "Authorization: Bearer {token}"
```

### Respuesta (204 No Content)
```
(Sin contenido)
```

### Errores
- **400** - Tiene turnos asociados: `"No se puede eliminar el tipo de turno porque tiene turnos asociados"`

---

## 📱 Servicio JavaScript/React

```typescript
class TipoTurnoService {
  private baseUrl = 'https://127.0.0.1:7209/api/Rrhh/TipoTurno';
  private token: string = '';

  setToken(token: string) {
    this.token = token;
  }

  async getAll(pageNumber: number = 1, pageSize: number = 10) {
    const response = await fetch(
      `${this.baseUrl}?pageNumber=${pageNumber}&pageSize=${pageSize}`,
      {
        headers: { 'Authorization': `Bearer ${this.token}` }
      }
    );
    return await response.json();
  }

  async getById(id: number) {
    const response = await fetch(
      `${this.baseUrl}/${id}`,
      {
        headers: { 'Authorization': `Bearer ${this.token}` }
      }
    );
    return await response.json();
  }

  async create(nombreTipo: string) {
    const response = await fetch(
      this.baseUrl,
      {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ nombreTipo })
      }
    );
    if (!response.ok) throw new Error('Error al crear');
    return await response.json();
  }

  async update(id: number, nombreTipo: string) {
    const response = await fetch(
      `${this.baseUrl}/${id}`,
      {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${this.token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ id, nombreTipo })
      }
    );
    if (!response.ok) throw new Error('Error al actualizar');
  }

  async delete(id: number) {
    const response = await fetch(
      `${this.baseUrl}/${id}`,
      {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${this.token}` }
      }
    );
    if (!response.ok) throw new Error('Error al eliminar');
  }
}

export default new TipoTurnoService();
```

---

**Última actualización:** 2026-03-19
