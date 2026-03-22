# 📋 Documentación: Asignación de Semana de Descansos

## 🎯 Resumen General

El sistema tiene **dos endpoints** para gestionar las semanas de descanso de trabajadores:

1. **POST /api/Descansos/semana** - Carga/asigna la semana de descansos
2. **GET /api/Descansos/{idTrabajador}/{semana}** - Obtiene los descansos de una semana

---

## 📍 Endpoint 1: Cargar Semana de Descansos

### **Ruta**
```
POST /api/Descansos/semana
```

### **Autenticación**
```
Authorization: Bearer <token>
Roles permitidos: ADMIN, SUPERADMIN, SUPERVISOR
```

### **Body Request**

```json
{
  "fechaLunes": "2026-03-16",
  "trabajadores": [
    {
      "idTrabajador": 1,
      "diaDescanso": 0,
      "diasBoleta": [0, 6]
    },
    {
      "idTrabajador": 2,
      "diaDescanso": 1,
      "diasBoleta": []
    },
    {
      "idTrabajador": 3,
      "diaDescanso": 2,
      "diasBoleta": [3]
    }
  ]
}
```

### **Explicación del Body**

| Campo | Tipo | Descripción | Ejemplo |
|-------|------|-------------|---------|
| **fechaLunes** | `DateTime` | Fecha de un día cualquiera en la semana (se ajusta automáticamente al lunes) | `2026-03-16` |
| **trabajadores** | `Array` | Lista de trabajadores con sus descansos | - |
| **idTrabajador** | `int` | ID del trabajador | `1`, `2`, `3` |
| **diaDescanso** | `int` | Día de descanso semanal (0-6, donde 0=Lunes, 6=Domingo) | `0`, `1`, `2` |
| **diasBoleta** | `Array<int>` | Días de boleta/vacaciones en la semana (0-6) | `[0, 6]`, `[]` |

### **Mapping de Días**

```
0 = Lunes (Lunch Monday)
1 = Martes (Tuesday)
2 = Miércoles (Wednesday)
3 = Jueves (Thursday)
4 = Viernes (Friday)
5 = Sábado (Saturday)
6 = Domingo (Sunday)
```

### **Response Success (200 OK)**

```json
{
  "ok": true,
  "mensaje": "Semana cargada.",
  "fechaLunes": "2026-03-16",
  "fechaFin": "2026-03-22"
}
```

### **Response Error (400 Bad Request)**

```json
{
  "message": "Debe enviar al menos un trabajador."
}
```

```json
{
  "message": "DiaDescanso debe estar entre 0 y 6."
}
```

```json
{
  "message": "DiasBoleta solo admite valores entre 0 y 6."
}
```

---

## 🗄️ ¿Qué se graba en la Base de Datos?

### **Tabla: PROGRAMACION_DESCANSOS**

```sql
CREATE TABLE PROGRAMACION_DESCANSOS (
    id_programacion INT PRIMARY KEY IDENTITY(1,1),
    id_trabajador INT NOT NULL,
    fecha DATE NOT NULL,
    es_descanso BIT DEFAULT 0,
    es_dia_boleta BIT DEFAULT 0,
    created_by INT,
    created_at DATETIME2 DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2
);
```

### **Proceso de Grabación**

El endpoint **NO graba directamente**, sino que **ejecuta un Stored Procedure**:

```sql
EXEC dbo.SP_CARGAR_SEMANA_DESCANSOS @FechaLunes, @DatosXML
```

**Parámetros:**
- `@FechaLunes` - Fecha del lunes de la semana
- `@DatosXML` - XML con datos de trabajadores y descansos

### **Formato XML que se envía**

```xml
<semana>
  <t id="1" desc="0" bol="0,6" />
  <t id="2" desc="1" bol="" />
  <t id="3" desc="2" bol="3" />
</semana>
```

### **Registros generados para Trabajador ID=1**

Si `fechaLunes = 2026-03-16` y el trabajador 1 tiene:
- `diaDescanso = 0` (Lunes es su descanso)
- `diasBoleta = [0, 6]` (Lunes y Domingo son boleta)

Se grabarán 7 registros (uno por cada día de la semana):

| id_trabajador | fecha | es_descanso | es_dia_boleta |
|---|---|---|---|
| 1 | 2026-03-16 (Lunes) | 1 | 1 |
| 1 | 2026-03-17 (Martes) | 0 | 0 |
| 1 | 2026-03-18 (Miércoles) | 0 | 0 |
| 1 | 2026-03-19 (Jueves) | 0 | 0 |
| 1 | 2026-03-20 (Viernes) | 0 | 0 |
| 1 | 2026-03-21 (Sábado) | 0 | 0 |
| 1 | 2026-03-22 (Domingo) | 0 | 1 |

---

## 📍 Endpoint 2: Obtener Descansos de Semana

### **Ruta**
```
GET /api/Descansos/{idTrabajador}/{semana}
```

### **Parámetros**

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| **idTrabajador** | `int` | ID del trabajador | 
| **semana** | `DateTime` | Cualquier fecha de la semana (se ajusta al lunes) |

### **Ejemplo Request**

```
GET /api/Descansos/42/2026-03-20
Authorization: Bearer <token>
```

### **Response (200 OK)**

```json
{
  "idTrabajador": 42,
  "fechaLunes": "2026-03-16",
  "dias": [
    {
      "fecha": "2026-03-16",
      "esDescanso": true,
      "esDiaBoleta": true
    },
    {
      "fecha": "2026-03-17",
      "esDescanso": false,
      "esDiaBoleta": false
    },
    {
      "fecha": "2026-03-18",
      "esDescanso": false,
      "esDiaBoleta": false
    },
    {
      "fecha": "2026-03-19",
      "esDescanso": false,
      "esDiaBoleta": false
    },
    {
      "fecha": "2026-03-20",
      "esDescanso": false,
      "esDiaBoleta": false
    },
    {
      "fecha": "2026-03-21",
      "esDescanso": false,
      "esDiaBoleta": false
    },
    {
      "fecha": "2026-03-22",
      "esDescanso": false,
      "esDiaBoleta": true
    }
  ]
}
```

---

## 🔄 Flujo Completo

### **1️⃣ Cargar semana**
```bash
POST /api/Descansos/semana
Body: {
  "fechaLunes": "2026-03-16",
  "trabajadores": [
    { "idTrabajador": 42, "diaDescanso": 0, "diasBoleta": [0, 6] }
  ]
}
```

↓

### **2️⃣ Base de datos se actualiza**
Se ejecuta SP que inserta/actualiza en PROGRAMACION_DESCANSOS

↓

### **3️⃣ Obtener descansos**
```bash
GET /api/Descansos/42/2026-03-20
```

↓

### **4️⃣ Sistema retorna los 7 días de la semana**
Con flags de descanso y boleta para cada día

---

## 💡 Notas Importantes

1. **Cálculo automático del lunes**: El endpoint ajusta automáticamente cualquier fecha al lunes de esa semana
2. **Un registro por día**: Se graban 7 registros por trabajador (uno por cada día de la semana)
3. **Upsert (Insert or Update)**: El SP_CARGAR_SEMANA_DESCANSOS probablemente hace upsert (si existe actualiza, si no inserta)
4. **Stored Procedure**: La lógica real está en `SP_CARGAR_SEMANA_DESCANSOS` en SQL Server
5. **Validación de roles**: Solo ADMIN, SUPERADMIN o SUPERVISOR pueden cargar semanas

---

## 📝 Ejemplo Completo de Uso

### **Paso 1: Cargar descansos para 3 trabajadores**

```bash
curl -X POST "https://127.0.0.1:7209/api/Descansos/semana" \
  -H "Authorization: Bearer eyJhbGci..." \
  -H "Content-Type: application/json" \
  -d '{
    "fechaLunes": "2026-03-20",
    "trabajadores": [
      {
        "idTrabajador": 1,
        "diaDescanso": 0,
        "diasBoleta": [0, 6]
      },
      {
        "idTrabajador": 2,
        "diaDescanso": 1,
        "diasBoleta": []
      },
      {
        "idTrabajador": 3,
        "diaDescanso": 2,
        "diasBoleta": [3]
      }
    ]
  }'
```

### **Paso 2: Obtener descansos del trabajador 1**

```bash
curl -X GET "https://127.0.0.1:7209/api/Descansos/1/2026-03-20" \
  -H "Authorization: Bearer eyJhbGci..."
```

### **Respuesta:**

```json
{
  "idTrabajador": 1,
  "fechaLunes": "2026-03-16",
  "dias": [
    { "fecha": "2026-03-16", "esDescanso": true, "esDiaBoleta": true },
    { "fecha": "2026-03-17", "esDescanso": false, "esDiaBoleta": false },
    { "fecha": "2026-03-18", "esDescanso": false, "esDiaBoleta": false },
    { "fecha": "2026-03-19", "esDescanso": false, "esDiaBoleta": false },
    { "fecha": "2026-03-20", "esDescanso": false, "esDiaBoleta": false },
    { "fecha": "2026-03-21", "esDescanso": false, "esDiaBoleta": false },
    { "fecha": "2026-03-22", "esDescanso": false, "esDiaBoleta": true }
  ]
}
```

---

## 🚀 Resumen Rápido

| Aspecto | Detalle |
|--------|---------|
| **Endpoint Cargar** | `POST /api/Descansos/semana` |
| **Endpoint Obtener** | `GET /api/Descansos/{idTrabajador}/{semana}` |
| **Tabla BD** | `PROGRAMACION_DESCANSOS` |
| **SP Usado** | `SP_CARGAR_SEMANA_DESCANSOS` |
| **Registros por semana** | 7 (uno por día) |
| **Roles requeridos** | ADMIN, SUPERADMIN, SUPERVISOR |
| **Validación** | Días 0-6, al menos 1 trabajador |
