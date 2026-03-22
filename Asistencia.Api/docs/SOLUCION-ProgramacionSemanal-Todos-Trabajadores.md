# ✅ SOLUCIÓN: Mostrar TODOS los Trabajadores en ProgramacionSemanal

## 🔴 El Problema

El endpoint `GET /api/Rrhh/ProgramacionSemanal` **solo retornaba trabajadores que tenían registros en `PROGRAMACION_TURNOS_SEMANAL`**.

Los trabajadores **sin programación NO aparecían** en la respuesta, imposibilitando que el frontend mostrara "Por programar".

### Antes (❌)
```json
{
  "items": [
    {
      "trabajadorId": 1,
      "trabajadorNombre": "HERNANDEZ CALDERON...",
      "dias": [...]
    }
    // ❌ Trabajador 2 que NO tiene programación NO aparece
  ]
}
```

---

## ✅ La Solución

### Cambios Realizados

#### **1. Modificar ProgramacionSemanalController.cs**

**Antes:**
```csharp
// Solo traía trabajadores que tenían PROGRAMACION_TURNOS_SEMANAL
var trabajadoresEnProgramacion = programacionesSemanal
    .Select(p => p.Trabajador)
    .Distinct()
    .ToList();

if (!trabajadoresEnProgramacion.Any())  // Solo si no hay programación
{
    trabajadoresEnProgramacion = await _context.Trabajadores
        .Include(t => t.Persona)
        .ToListAsync();
}
```

**Después:**
```csharp
// SIEMPRE traer todos los trabajadores activos
var todosTrabajadores = await _context.Trabajadores
    .Include(t => t.Persona)
    .Include(t => t.AsignacionesTurno.Where(a => a.EsVigente)) // Turno vigente
    .ThenInclude(a => a.Turno)
    .OrderBy(t => t.Persona!.ApellidosNombres)
    .ToListAsync();

// LEFT JOIN con PROGRAMACION_TURNOS_SEMANAL
var programacionesSemanal = await _context.ProgramacionTurnosSemanal
    .Where(p => p.Fecha >= fechaInicio && p.Fecha <= fechaFin)
    .Include(p => p.HorarioTurno)
    .ThenInclude(ht => ht.Turno)
    .ToListAsync();

// Para CADA trabajador, generar TODOS los días (con o sin programación)
foreach (var trab in todosTrabajadores)
{
    var turnoVigente = trab.AsignacionesTurno?.FirstOrDefault();
    
    for (var d = fechaInicio; d <= fechaFin; d = d.AddDays(1))
    {
        var progSemanal = programacionesSemanal
            .FirstOrDefault(p => p.TrabajadorId == trab.Id && p.Fecha == d);

        if (progSemanal != null)
        {
            // ✅ Hay programación
        }
        else
        {
            // ❌ Sin programación → mostrar "sin-asignar" con turnoId del trabajador
            item.Dias.Add(new ProgramacionDiaDto 
            { 
                Fecha = d, 
                TurnoId = turnoVigente?.Turno?.Id,
                Estado = "sin-asignar" 
            });
        }
    }
}
```

#### **2. Agregar propiedad de navegación a Trabajador.cs**

```csharp
// En: ..\Data\Entities\MarcacionAsistenciaEntites\Trabajador.cs

public virtual ICollection<AsignacionTurno> AsignacionesTurno { get; set; } 
    = new List<AsignacionTurno>();
```

#### **3. Agregar TotalCount al DTO**

```csharp
// En: Services\Dtos\ProgramacionSemanalDtos.cs

public class ProgramacionSemanalResponseDto
{
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int TotalCount { get; set; }  // ← NUEVO
    public List<ProgramacionPorTrabajadorDto> Items { get; set; } 
        = new List<ProgramacionPorTrabajadorDto>();
}
```

---

## 📊 Response Ahora Retorna

```json
{
  "fechaInicio": "2026-03-16",
  "fechaFin": "2026-03-22",
  "totalCount": 375,
  "items": [
    {
      "trabajadorId": 1,
      "trabajadorNombre": "HERNANDEZ CALDERON",
      "dias": [
        {
          "fecha": "2026-03-16",
          "horarioTurnoId": 26,
          "horarioTurnoNombre": "MAÑANA 06-14",
          "turnoId": 16,
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "trabaja"
        },
        ...
      ]
    },
    {
      "trabajadorId": 2,
      "trabajadorNombre": "GARCIA LOPEZ",
      "dias": [
        {
          "fecha": "2026-03-16",
          "horarioTurnoId": null,      // ← Sin programación
          "horarioTurnoNombre": null,  // ← Sin programación
          "turnoId": 16,               // ← Pero incluye turno del trabajador
          "turnoNombre": "TURNO_MAÑANA",
          "estado": "sin-asignar"      // ← Para programar
        },
        ...
      ]
    },
    ...
    {
      "trabajadorId": 375,
      "trabajadorNombre": "ULTIMO TRABAJADOR",
      "dias": [
        {
          "fecha": "2026-03-16",
          "horarioTurnoId": null,
          "horarioTurnoNombre": null,
          "turnoId": null,  // Rotativo sin asignación fija
          "turnoNombre": null,
          "estado": "sin-asignar"
        },
        ...
      ]
    }
  ]
}
```

---

## 🎯 Cambios Implementados

| Archivo | Cambio | Estado |
|---------|--------|--------|
| `Controllers\ProgramacionSemanalController.cs` | ✏️ Método `GetProgramacionSemanal` | ✅ Modificado |
| `..\Data\Entities\MarcacionAsistenciaEntites\Trabajador.cs` | ✨ Propiedad `AsignacionesTurno` | ✅ Agregado |
| `Services\Dtos\ProgramacionSemanalDtos.cs` | ✨ Campo `TotalCount` | ✅ Agregado |

---

## 🚀 Próximos Pasos

1. **Reinicia Visual Studio** (cambio de propiedad de entidad)
2. **Ejecuta la app**
3. **Prueba el endpoint:**
```sh
GET https://127.0.0.1:7209/api/Rrhh/ProgramacionSemanal?fechaInicio=2026-03-16&fechaFin=2026-03-22
Authorization: Bearer <tu_token>
```

4. **Verifica que:**
   - ✅ Aparecen TODOS los 375 trabajadores
   - ✅ Los sin programación tienen `estado: "sin-asignar"`
   - ✅ Todos tienen sus días del rango (7 días)
   - ✅ El frontend puede mostrar "Por programar"

---

## 💡 Cómo Funciona Ahora

```
PASO 1: Obtener TODOS los trabajadores activos
   ↓
PASO 2: Obtener PROGRAMACION_TURNOS_SEMANAL (LEFT JOIN)
   ↓
PASO 3: Para CADA trabajador × CADA día del rango:
   ├─ SI hay programación → usa esos datos
   └─ SI NO hay → estado = "sin-asignar" (turnoId puede venir del FIJO)
   ↓
RESULTADO: Todos aparecen en la respuesta
```

---

## ✨ Compilación

✅ **Compila correctamente** - Reinicia para cambios de propiedad

---

**La solución está completa. Todos los trabajadores ahora aparecen en ProgramacionSemanal.** 🎉
