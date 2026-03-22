using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class ProgramacionSemanalController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public ProgramacionSemanalController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        [HttpGet("horarios-disponibles")]
        public async Task<IActionResult> GetHorariosDisponibles()
        {
            var horarios = await _context.HorariosTurno
                .Include(h => h.Turno)
                .Where(h => h.EsActivo)
                .Select(h => new
                {
                    id = h.Id,
                    nombre = h.NombreHorario,
                    turnoId = h.TurnoId,
                    turnoNombre = h.Turno.NombreCodigo,
                    esActivo = h.EsActivo
                })
                .OrderBy(h => h.nombre)
                .ToListAsync();

            return Ok(new
            {
                mensaje = "Horarios disponibles",
                total = horarios.Count,
                horarios = horarios
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetProgramacionSemanal([FromQuery] DateOnly fechaInicio, [FromQuery] DateOnly fechaFin)
        {
            if (fechaInicio > fechaFin) return BadRequest("fechaInicio no puede ser mayor a fechaFin");

            // PASO 1: Obtener TODOS los trabajadores activos (no solo los que tienen programación)
            var todosTrabajadores = await _context.Trabajadores
                .Include(t => t.Persona)
                .Include(t => t.AsignacionesTurno.Where(a => a.EsVigente)) // Turno vigente del trabajador
                .ThenInclude(a => a.Turno)
                .OrderBy(t => t.Persona!.ApellidosNombres)
                .ToListAsync();

            // PASO 2: Obtener programaciones semanales ya grabadas (LEFT JOIN)
            var programacionesSemanal = await _context.ProgramacionTurnosSemanal
                .Where(p => p.Fecha >= fechaInicio && p.Fecha <= fechaFin)
                .Include(p => p.HorarioTurno)
                .ThenInclude(ht => ht.Turno)
                .OrderBy(p => p.Fecha)
                .ToListAsync();

            var response = new ProgramacionSemanalResponseDto
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                TotalCount = todosTrabajadores.Count
            };

            // PASO 3: Para CADA trabajador, generar TODOS los días (con o sin programación)
            foreach (var trab in todosTrabajadores)
            {
                var item = new ProgramacionPorTrabajadorDto 
                { 
                    TrabajadorId = trab.Id, 
                    TrabajadorNombre = trab.Persona?.ApellidosNombres 
                };

                // Obtener turno vigente del trabajador (para cuando no hay PROGRAMACION_TURNOS_SEMANAL)
                var turnoVigente = trab.AsignacionesTurno?.FirstOrDefault();
                var turnoIdDefault = turnoVigente?.Turno?.Id;
                var turnoNombreDefault = turnoVigente?.Turno?.NombreCodigo;

                // GENERAR UNA FILA POR CADA DÍA DEL RANGO
                for (var d = fechaInicio; d <= fechaFin; d = d.AddDays(1))
                {
                    var progSemanal = programacionesSemanal
                        .FirstOrDefault(p => p.TrabajadorId == trab.Id && p.Fecha == d);

                    if (progSemanal != null)
                    {
                        // ✅ Hay programación semanal para este día
                        item.Dias.Add(new ProgramacionDiaDto
                        {
                            Fecha = d,
                            HorarioTurnoId = progSemanal.IdHorarioTurno,
                            HorarioTurnoNombre = progSemanal.HorarioTurno?.NombreHorario,
                            TurnoId = progSemanal.HorarioTurno?.TurnoId,
                            TurnoNombre = progSemanal.HorarioTurno?.Turno?.NombreCodigo,
                            Estado = progSemanal.EsDescanso ? "descanso" : 
                                     progSemanal.EsDiaBoleta ? "boleta" :
                                     progSemanal.EsVacaciones ? "vacaciones" : "trabaja"
                        });
                    }
                    else
                    {
                        // ❌ Sin programación → mostrar "sin-asignar" pero con turnoId del trabajador
                        item.Dias.Add(new ProgramacionDiaDto 
                        { 
                            Fecha = d, 
                            HorarioTurnoId = null,
                            HorarioTurnoNombre = null,
                            TurnoId = turnoIdDefault,        // ← Turno base del trabajador (FIJO)
                            TurnoNombre = turnoNombreDefault, // ← Para rotativos, podría ser null
                            Estado = "sin-asignar" 
                        });
                    }
                }

                response.Items.Add(item);
            }

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> AsignarSemanal([FromBody] ProgramacionTurnoSemanalBulkCreateDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.FechaInicio > request.FechaFin)
                return BadRequest("FechaInicio no puede ser mayor a FechaFin");

            if (request.Programaciones == null || request.Programaciones.Count == 0)
                return BadRequest("Debe enviar al menos una programación");

            try
            {
                // Validar que todos los trabajadores y horarios existan
                var trabIds = request.Programaciones.Select(p => p.TrabajadorId).Distinct().ToList();
                var horarioIds = request.Programaciones.Select(p => p.IdHorarioTurno).Distinct().ToList();

                var trabajadoresExisten = await _context.Trabajadores
                    .Where(t => trabIds.Contains(t.Id))
                    .CountAsync();

                if (trabajadoresExisten != trabIds.Count)
                    return NotFound("Uno o más trabajadores no existen");

                var horariosExisten = await _context.HorariosTurno
                    .Where(h => horarioIds.Contains(h.Id))
                    .CountAsync();

                if (horariosExisten != horarioIds.Count)
                {
                    var horariosValidos = await _context.HorariosTurno
                        .Select(h => new { h.Id, h.NombreHorario })
                        .ToListAsync();

                    return NotFound(new 
                    { 
                        message = "Uno o más horarios no existen",
                        horariosEnviados = horarioIds,
                        horariosDisponibles = horariosValidos.Select(h => new { h.Id, h.NombreHorario })
                    });
                }

                // Eliminar programaciones existentes para el rango de fechas (upsert)
                var existentes = await _context.ProgramacionTurnosSemanal
                    .Where(p => p.Fecha >= request.FechaInicio && p.Fecha <= request.FechaFin
                        && trabIds.Contains(p.TrabajadorId))
                    .ToListAsync();

                _context.ProgramacionTurnosSemanal.RemoveRange(existentes);

                // Insertar nuevas programaciones
                var programaciones = request.Programaciones.Select(p => new ProgramacionTurnoSemanal
                {
                    TrabajadorId = p.TrabajadorId,
                    Fecha = p.Fecha,
                    IdHorarioTurno = p.IdHorarioTurno,
                    EsDescanso = p.EsDescanso,
                    EsDiaBoleta = p.EsDiaBoleta,
                    EsVacaciones = p.EsVacaciones,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _context.ProgramacionTurnosSemanal.AddRangeAsync(programaciones);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    ok = true,
                    mensaje = $"Programación semanal grabada",
                    fechaInicio = request.FechaInicio.ToString("yyyy-MM-dd"),
                    fechaFin = request.FechaFin.ToString("yyyy-MM-dd"),
                    registrosGrabados = programaciones.Count
                });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { message = "Error al grabar la programación", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error inesperado", error = ex.Message });
            }
        }

        [HttpPost("publicar")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> Publicar([FromBody] PublicarProgramacionDto request)
        {
            if (request.FechaInicio > request.FechaFin) return BadRequest("fechaInicio no puede ser mayor a fechaFin");

            // Implementación simple: marcar que la programación está publicada -> por ahora devolvemos resumen
            return Ok(new { publicadas = 0, errores = Array.Empty<string>() });
        }

        [HttpPost("copiar")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> Copiar([FromBody] CopiarProgramacionDto request)
        {
            // Implementación simple: copiar lógicamente no persistente -> devolver resumen
            return Ok(new { copiados = 0, errores = Array.Empty<string>() });
        }
    }
}
