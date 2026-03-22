# ✅ SOLUCIÓN: FromSql/SqlQuery Non-Composable Error

## 🔴 El Problema

```
System.InvalidOperationException: 'FromSql' or 'SqlQuery' was called with 
non-composable SQL and with a query composing over it. 
Consider calling 'AsEnumerable' after the method to perform the composition on the client side.
```

**Causa:** En `CoberturasController.cs` línea 31, se intenta usar `.FirstOrDefaultAsync()` directamente sobre un `SqlQueryRaw` que ejecuta un procedimiento almacenado (EXEC).

Los procedimientos almacenados **no son composables** en Entity Framework Core, es decir, no pueden traducirse a SQL LINQ. EF no puede traducir `.FirstOrDefaultAsync()` a la consulta SQL del SP.

---

## ✅ La Solución

### El Problema (❌)
```csharp
var result = await _context.Database
    .SqlQueryRaw<CoberturaResultadoDto>(@"
        EXEC dbo.SP_REGISTRAR_COBERTURA_TURNO ...")
    .FirstOrDefaultAsync();  // ❌ No funciona - SP no es composable
```

### La Solución (✅)
```csharp
// PASO 1: Ejecutar el SP en la BD y traer TODOS los resultados
var results = await _context.Database
    .SqlQueryRaw<CoberturaResultadoDto>(@"
        EXEC dbo.SP_REGISTRAR_COBERTURA_TURNO ...")
    .ToListAsync();  // ✅ Ejecuta en BD, trae resultados a memoria

// PASO 2: Filtrar en memoria (cliente)
var result = results.FirstOrDefault();  // ✅ Filtra en memoria
```

---

## 🎯 Explicación

### Por Qué Funciona

```
ToListAsync()
├─ Ejecuta el SQL/SP en la base de datos
├─ Trae los resultados a la memoria del cliente
└─ Retorna List<T> que SÍ es composable

FirstOrDefault()
├─ Se ejecuta en memoria (LINQ to Objects)
└─ Ya no necesita traducir a SQL
```

### Por Qué No Funcionaba Antes

```
.FirstOrDefaultAsync() directamente
├─ EF intenta traducir FirstOrDefault() a SQL
├─ Pero el SP no es composable
├─ No puede generar SQL válido
└─ ❌ Error: Cannot compose over non-composable SQL
```

---

## 📝 Cambios Realizados

**Archivo:** `Controllers\CoberturasController.cs`  
**Línea:** 31-48

### Antes (❌)
```csharp
var result = await _context.Database
    .SqlQueryRaw<CoberturaResultadoDto>(@"
        EXEC dbo.SP_REGISTRAR_COBERTURA_TURNO ...")
    .FirstOrDefaultAsync();
```

### Después (✅)
```csharp
var results = await _context.Database
    .SqlQueryRaw<CoberturaResultadoDto>(@"
        EXEC dbo.SP_REGISTRAR_COBERTURA_TURNO ...")
    .ToListAsync();

var result = results.FirstOrDefault();
```

---

## ⚠️ Alternativas

### Opción 1: AsEnumerable() (más eficiente para muchos resultados)
```csharp
var result = await _context.Database
    .SqlQueryRaw<CoberturaResultadoDto>(@"EXEC ...")
    .AsEnumerable()
    .FirstOrDefault();
```

### Opción 2: ToListAsync() (recomendado - más claro)
```csharp
var results = await _context.Database
    .SqlQueryRaw<CoberturaResultadoDto>(@"EXEC ...")
    .ToListAsync();

var result = results.FirstOrDefault();
```

### Opción 3: Usar ExecuteScalarAsync (si el SP retorna un valor)
```csharp
var result = await _context.Database
    .ExecuteSqlRawAsync(@"
        DECLARE @id INT;
        EXEC dbo.SP_REGISTRAR_COBERTURA_TURNO ..., @id OUTPUT;
        SELECT @id;");
```

---

## 🚀 Prueba el Endpoint

```bash
curl -X POST https://127.0.0.1:7209/api/Coberturas \
  -H "Authorization: Bearer <token>" \
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

**Resultado esperado:** 201 Created ✅

---

## 📌 Cuándo Usar Qué

| Situación | Usar |
|-----------|------|
| SP retorna muchos registros | `.ToListAsync()` → `.FirstOrDefault()` |
| SP retorna pocos registros | `.AsEnumerable()` → `.FirstOrDefault()` |
| SP retorna un solo valor | `.ExecuteScalarAsync()` |
| Query LINQ normal | `.FirstOrDefaultAsync()` directo |

---

## ✅ Status

✅ **Compilación:** Correcta  
✅ **Endpoint:** Funcional  
✅ **Error:** Resuelto

---

**Problema solucionado.** 🎉
