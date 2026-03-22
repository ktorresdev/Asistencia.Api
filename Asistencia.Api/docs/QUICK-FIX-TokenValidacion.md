# ⚡ SOLUCIÓN RÁPIDA: TokenValidacion Column Error

## 🔴 El Error
```
Invalid column name 'TokenValidacion'
```

## ✅ La Causa
La entidad `MarcacionAsistencia` tiene una propiedad `TokenValidacion` que NO está mapeada en DbContext.

## 🔧 El Fix

**Archivo:** `..\Data\DbContexts\MarcacionAsistenciaDbContext.cs`  
**Línea:** 275

**Agregar:**
```csharp
entity.Ignore(e => e.TokenValidacion);
```

---

## 📝 Dónde Agregar

```csharp
modelBuilder.Entity<MarcacionAsistencia>(entity =>
{
    // ... otras propiedades ...
    entity.Property(e => e.UbicacionValida).HasColumnName("ubicacion_valida");
    
    // ✅ AGREGAR ESTA LÍNEA:
    entity.Ignore(e => e.TokenValidacion);

    entity.HasOne(d => d.Trabajador).WithMany()...
});
```

---

## ✅ Status

✅ Implementado  
✅ Compilación OK  
🚀 Endpoint funciona

---

**Detalles completos en: `docs\SOLUCION-TokenValidacion-Column-Error.md`**
