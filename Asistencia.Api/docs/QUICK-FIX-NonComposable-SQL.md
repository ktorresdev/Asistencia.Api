# ⚡ QUICK FIX: NonComposable SQL Error

## 🔴 Error
```
'FromSql' or 'SqlQuery' was called with non-composable SQL
```

## ✅ Causa
Usar `.FirstOrDefaultAsync()` directamente en un `SqlQueryRaw` con procedimiento almacenado.

## 🔧 Solución

**Cambiar de:**
```csharp
var result = await _context.Database
    .SqlQueryRaw<T>(@"EXEC SP...")
    .FirstOrDefaultAsync();  // ❌
```

**A:**
```csharp
var results = await _context.Database
    .SqlQueryRaw<T>(@"EXEC SP...")
    .ToListAsync();  // ✅

var result = results.FirstOrDefault();  // ✅
```

## 📝 Cambio
**Archivo:** `Controllers\CoberturasController.cs`  
**Línea:** 31-48  
**Cambio:** Agregar `.ToListAsync()` antes de filtrar

---

## ✅ Status
✅ Resuelto  
✅ Compila correctamente  
🚀 Listo para usar

---

**Detalles completos en: `docs\SOLUCION-FromSql-NonComposable-Error.md`**
