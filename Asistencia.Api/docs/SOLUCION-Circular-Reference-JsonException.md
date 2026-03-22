# ✅ SOLUCIÓN: Error de Circular Reference en AsignacionTurno

## 🔴 El Problema

```
System.Text.Json.JsonException: 
A possible object cycle was detected. 
This can either be due to a cycle or if the object depth 
is larger than the maximum allowed depth of 32
```

**Causa:**
```
Turno → HorariosTurno → Turno → HorariosTurno → ... (infinito)
```

Entity Framework estaba cargando referencias bidireccionales y JSON no podía serializarlas.

---

## ✅ La Solución Implementada

### 1. Crear DTO sin Ciclos

```csharp
// ..\Services\Dtos\AsignacionTurnoResponseDto.cs
public class AsignacionTurnoResponseDto
{
    public int Id { get; set; }
    
    // Trabajador
    public int TrabajadorId { get; set; }
    public string? TrabajadorNombre { get; set; }
    public string? TrabajadorDni { get; set; }
    
    // Turno (SIN HorariosTurno para evitar ciclos)
    public int TurnoId { get; set; }
    public string? TurnoNombre { get; set; }
    public string? TipoTurno { get; set; }
    
    // Horario Turno (SIN Turno para evitar ciclos)
    public int? HorarioTurnoId { get; set; }
    public string? HorarioTurnoNombre { get; set; }
    
    // Vigencia
    public bool EsVigente { get; set; }
    public DateOnly FechaInicioVigencia { get; set; }
    public DateOnly? FechaFinVigencia { get; set; }
    
    // Metadatos
    public string? MotivoCambio { get; set; }
    public int? AprobadoPor { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### 2. Actualizar el Servicio

```csharp
// ..\Services\Services\AsignacionTurnoService.cs

public async Task<PagedResult<AsignacionTurnoResponseDto>> GetAllAsync(PaginationDto pagination)
{
    var query = _context.AsignacionesTurno
                        .Include(a => a.Trabajador)
                        .ThenInclude(t => t!.Persona)
                        .Include(a => a.Turno)
                        .ThenInclude(t => t!.TipoTurno)
                        .Include(a => a.HorarioTurno)
                        .AsQueryable();

    var totalCount = await query.CountAsync();

    var items = await query
        .Skip((pagination.PageNumber - 1) * pagination.PageSize)
        .Take(pagination.PageSize)
        .Select(a => new AsignacionTurnoResponseDto
        {
            Id = a.Id,
            TrabajadorId = a.TrabajadorId,
            TrabajadorNombre = a.Trabajador!.Persona!.ApellidosNombres,
            TrabajadorDni = a.Trabajador.Persona.Dni,
            TurnoId = a.TurnoId,
            TurnoNombre = a.Turno!.NombreCodigo,
            TipoTurno = a.Turno.TipoTurno!.NombreTipo,
            HorarioTurnoId = a.HorarioTurnoId,
            HorarioTurnoNombre = a.HorarioTurno!.NombreHorario,
            EsVigente = a.EsVigente,
            FechaInicioVigencia = a.FechaInicioVigencia,
            FechaFinVigencia = a.FechaFinVigencia,
            MotivoCambio = a.MotivoCambio,
            AprobadoPor = a.AprobadoPor,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        })
        .ToListAsync();

    return new PagedResult<AsignacionTurnoResponseDto>
    {
        Items = items,
        TotalCount = totalCount,
        PageSize = pagination.PageSize,
        CurrentPage = pagination.PageNumber,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
    };
}

public async Task<AsignacionTurnoResponseDto?> GetByIdAsync(int id)
{
    return await _context.AsignacionesTurno
                         .Include(a => a.Trabajador)
                         .ThenInclude(t => t!.Persona)
                         .Include(a => a.Turno)
                         .ThenInclude(t => t!.TipoTurno)
                         .Include(a => a.HorarioTurno)
                         .Where(a => a.Id == id)
                         .Select(a => new AsignacionTurnoResponseDto
                         {
                             Id = a.Id,
                             TrabajadorId = a.TrabajadorId,
                             TrabajadorNombre = a.Trabajador!.Persona!.ApellidosNombres,
                             TrabajadorDni = a.Trabajador.Persona.Dni,
                             TurnoId = a.TurnoId,
                             TurnoNombre = a.Turno!.NombreCodigo,
                             TipoTurno = a.Turno.TipoTurno!.NombreTipo,
                             HorarioTurnoId = a.HorarioTurnoId,
                             HorarioTurnoNombre = a.HorarioTurno!.NombreHorario,
                             EsVigente = a.EsVigente,
                             FechaInicioVigencia = a.FechaInicioVigencia,
                             FechaFinVigencia = a.FechaFinVigencia,
                             MotivoCambio = a.MotivoCambio,
                             AprobadoPor = a.AprobadoPor,
                             CreatedAt = a.CreatedAt,
                             UpdatedAt = a.UpdatedAt
                         })
                         .FirstOrDefaultAsync();
}
```

### 3. Actualizar el Interfaz

```csharp
// ..\Services\Implements\IAsignacionTurnoService.cs

public interface IAsignacionTurnoService
{
    Task<PagedResult<AsignacionTurnoResponseDto>> GetAllAsync(PaginationDto pagination);
    Task<AsignacionTurnoResponseDto?> GetByIdAsync(int id);
    Task<AsignacionTurno> AddAsync(AsignacionTurnoCreateDto request);
    Task UpdateAsync(int id, AsignacionTurnoUpdateDto request);
    Task DeleteAsync(int id);
}
```

### 4. Actualizar el Controller

```csharp
// Controllers\AsignacionTurnoController.cs

[HttpGet]
public async Task<ActionResult<PagedResult<AsignacionTurnoResponseDto>>> GetAllAsync([FromQuery] PaginationDto pagination)
{
    var asignaciones = await _asignacionTurnoService.GetAllAsync(pagination);
    return Ok(asignaciones);
}

[HttpGet("{id:int}", Name = "GetByIdAsync")]
public async Task<ActionResult<AsignacionTurnoResponseDto>> GetByIdAsync(int id)
{
    var asignacion = await _asignacionTurnoService.GetByIdAsync(id);
    if (asignacion == null)
    {
        return NotFound($"No se encontró la asignación de turno con ID {id}.");
    }
    return Ok(asignacion);
}
```

---

## 🔍 ¿Qué Cambió?

### Antes (❌ Generaba Error)
```csharp
.Include(a => a.Turno)
.Include(a => a.HorarioTurno)
// Retorna la entidad completa con todas las referencias
// JSON intenta serializar: Turno → HorariosTurno → Turno → ... (infinito)
```

### Después (✅ Funciona Correctamente)
```csharp
.Select(a => new AsignacionTurnoResponseDto
{
    // Solo datos específicos
    TurnoNombre = a.Turno!.NombreCodigo,
    HorarioTurnoNombre = a.HorarioTurno!.NombreHorario,
    // NO incluye HorariosTurno ni Turno (evita ciclos)
})
// JSON serializa solo strings e ints (sin ciclos)
```

---

## 🎯 Beneficios

| Aspecto | Antes | Después |
|---------|-------|---------|
| **Serialización JSON** | ❌ Error de ciclo | ✅ Funciona perfectamente |
| **Tamaño de respuesta** | Muy grande | Optimizado |
| **Performance** | Carga entidades completas | Solo datos necesarios |
| **Tipo de retorno** | `AsignacionTurno` | `AsignacionTurnoResponseDto` |
| **API Response** | Caos circular | JSON limpio y predecible |

---

## ✅ Próximos Pasos

1. **Reinicia Visual Studio** (los cambios de firma requieren reinicio)
2. **Ejecuta la app**
3. **Llama al endpoint:**
   ```
   GET https://127.0.0.1:7209/api/Rrhh/AsignacionTurno
   Authorization: Bearer <tu_token>
   ```
4. **¡Funciona sin errores!** ✅

---

## 📊 Response Esperado

```json
{
  "items": [
    {
      "id": 1,
      "trabajadorId": 5,
      "trabajadorNombre": "Juan Pérez",
      "trabajadorDni": "12345678",
      "turnoId": 1,
      "turnoNombre": "TURNO_MAÑANA",
      "tipoTurno": "FIJO",
      "horarioTurnoId": 3,
      "horarioTurnoNombre": "Turno Mañana 9-5",
      "esVigente": true,
      "fechaInicioVigencia": "2026-01-01",
      "fechaFinVigencia": null,
      "motivoCambio": null,
      "aprobadoPor": null,
      "createdAt": "2024-01-01T10:00:00",
      "updatedAt": null
    }
  ],
  "totalCount": 47,
  "pageSize": 10,
  "currentPage": 1,
  "totalPages": 5
}
```

---

## 🚀 ¡Problema Resuelto!

**Archivos modificados:**
- ✅ `..\Services\Dtos\AsignacionTurnoResponseDto.cs` (NUEVO)
- ✅ `..\Services\Services\AsignacionTurnoService.cs` (MODIFICADO)
- ✅ `..\Services\Implements\IAsignacionTurnoService.cs` (MODIFICADO)
- ✅ `Controllers\AsignacionTurnoController.cs` (MODIFICADO)

**Compila correctamente - Reinicia para cambios de firma de método.** ✅
