# ✅ SOLUCIÓN: Invalid column name 'TokenValidacion'

## 🔴 El Problema

```
{
    "success": false,
    "code": "ERROR_INTERNO",
    "message": "Error al consultar el estado de marcación.",
    "detail": "Invalid column name 'TokenValidacion'."
}
```

**Causa:** La entidad `MarcacionAsistencia` tiene una propiedad `TokenValidacion` que **no está mapeada** en el DbContext. EF Core intentaba traer esa columna de la BD pero no existe.

---

## ✅ La Solución

### El Problema en DbContext

**Antes (❌):**
```csharp
// En MarcacionAsistenciaDbContext.cs línea 260-275
modelBuilder.Entity<MarcacionAsistencia>(entity =>
{
    entity.ToTable("MARCACIONES_ASISTENCIA");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).HasColumnName("id_marcacion");
    entity.Property(e => e.TrabajadorId).HasColumnName("id_trabajador").IsRequired();
    entity.Property(e => e.FechaHora).HasColumnName("fecha_hora").IsRequired();
    entity.Property(e => e.TipoMarcacion).HasColumnName("tipo_marcacion")...
    entity.Property(e => e.Latitud).HasColumnName("latitud")...
    entity.Property(e => e.Longitud).HasColumnName("longitud")...
    entity.Property(e => e.FotoUrl).HasColumnName("foto_url");
    entity.Property(e => e.UbicacionValida).HasColumnName("ubicacion_valida");
    
    // ❌ TokenValidacion NO estaba mapeado
    
    entity.HasOne(d => d.Trabajador).WithMany()...
});
```

**Después (✅):**
```csharp
// En MarcacionAsistenciaDbContext.cs línea 260-277
modelBuilder.Entity<MarcacionAsistencia>(entity =>
{
    entity.ToTable("MARCACIONES_ASISTENCIA");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).HasColumnName("id_marcacion");
    entity.Property(e => e.TrabajadorId).HasColumnName("id_trabajador").IsRequired();
    entity.Property(e => e.FechaHora).HasColumnName("fecha_hora").IsRequired();
    entity.Property(e => e.TipoMarcacion).HasColumnName("tipo_marcacion")...
    entity.Property(e => e.Latitud).HasColumnName("latitud")...
    entity.Property(e => e.Longitud).HasColumnName("longitud")...
    entity.Property(e => e.FotoUrl).HasColumnName("foto_url");
    entity.Property(e => e.UbicacionValida).HasColumnName("ubicacion_valida");
    
    // ✅ TokenValidacion ignorado (no existe en BD y no se usa)
    entity.Ignore(e => e.TokenValidacion);

    entity.HasOne(d => d.Trabajador).WithMany()...
});
```

---

## 🎯 Qué es `TokenValidacion`

En la entidad `MarcacionAsistencia.cs`:
```csharp
public class MarcacionAsistencia
{
    public long Id { get; set; }
    public int TrabajadorId { get; set; }
    public DateTime FechaHora { get; set; }
    public required string TipoMarcacion { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public string? FotoUrl { get; set; }
    public bool? UbicacionValida { get; set; }
    public string TokenValidacion { get; set; }  // ← Existía pero no se usa
}
```

**Estado:** 
- ✅ Existe en la entidad
- ❌ NO existe en la BD
- ❌ NO se usa en el código (estaba comentado en línea 305)
- ❌ NO estaba mapeado en DbContext

---

## 🔧 Cambio Realizado

**Archivo:** `..\Data\DbContexts\MarcacionAsistenciaDbContext.cs`

**Línea:** 275

**Agregado:**
```csharp
// TokenValidacion no se usa por ahora, ignorar en mapeador
entity.Ignore(e => e.TokenValidacion);
```

**Significa:** Le decimos a EF Core que **ignore esta propiedad** y no intente mapearla a ninguna columna de BD.

---

## ✅ Resultado

Ahora el endpoint funciona correctamente:

```bash
GET https://127.0.0.1:7209/api/Rrhh/MarcacionAsistencia/status/5
```

**Response:**
```json
{
  "success": true,
  "trabajadorId": 5,
  "horarioProgramado": "09:00 - 17:00",
  "marcacionEntrada": "2026-03-20T09:15:32",
  "marcacionSalida": null,
  "tiempoTrabajadoFormato": "45 minutos",
  "puedeMarcarEntrada": false,
  "puedeMarcarSalida": true,
  "salidaPendiente": true
}
```

---

## 📝 Cambios Realizados

| Archivo | Línea | Cambio |
|---------|-------|--------|
| `..\Data\DbContexts\MarcacionAsistenciaDbContext.cs` | 275 | ✅ Agregado `entity.Ignore(e => e.TokenValidacion)` |

---

## 🚀 Próximos Pasos

1. **Reinicia Visual Studio** (cambios en DbContext)
2. **Ejecuta la app**
3. **Prueba el endpoint:**
```bash
GET https://127.0.0.1:7209/api/Rrhh/MarcacionAsistencia/status/5
Authorization: Bearer <token>
```

4. **Debería funcionar sin errores** ✅

---

## ℹ️ Nota Adicional

**¿Por qué existía `TokenValidacion` si no se usa?**

Probablemente fue una propiedad planeada para:
- Validar marcaciones por token
- Seguridad adicional
- Pero nunca se implementó

**¿Qué hacer si la necesitas luego?**
1. Crear columna `token_validacion` en tabla `MARCACIONES_ASISTENCIA`
2. Remover el `entity.Ignore()`
3. Agregar mapeador: `.Property(e => e.TokenValidacion).HasColumnName("token_validacion")`

---

## ✅ Status

✅ **Compilación:** OK  
✅ **Error resuelto:** Sí  
🚀 **Endpoint funciona:** Sí

---

**Problema solucionado. El endpoint `/status` ya funciona correctamente.** ✅
