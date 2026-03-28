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

            // ADMIN (jefe local) solo ve sus trabajadores directos.
            // Si envía sucursalId de una sede en comisión → ve todos los de esa sede.
            // SUPERADMIN ve todo.
            int? jefeId = null;
            bool soloSede = false;
            int? sucursalFiltro = null;

            if (User.IsInRole("ADMIN") && !User.IsInRole("SUPERADMIN"))
            {
                var claim = User.FindFirst("trabajador_id")?.Value;
                if (!int.TryParse(claim, out var tid))
                    return Ok(new ProgramacionSemanalResponseDto { FechaInicio = fechaInicio, FechaFin = fechaFin });

                jefeId = tid;

                // Leer sucursalId opcional desde query param
                var sucursalParam = HttpContext.Request.Query["sucursalId"].FirstOrDefault();
                if (int.TryParse(sucursalParam, out var sid) && sid > 0)
                {
                    sucursalFiltro = sid;

                    var esSedePrincipal = await _context.Trabajadores
                        .AnyAsync(t => t.Id == tid && t.SucursalId == sid);

                    if (!esSedePrincipal)
                    {
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var tieneComision = await _context.TrabajadorSucursales
                            .AnyAsync(ts =>
                                ts.TrabajadorId == tid &&
                                ts.SucursalId == sid &&
                                ts.FechaInicio <= today &&
                                (ts.FechaFin == null || ts.FechaFin.Value >= today));

                        if (tieneComision)
                        {
                            soloSede = true;
                            jefeId = null;
                        }
                    }
                }
            }

            // PASO 1: Obtener TODOS los trabajadores activos (no solo los que tienen programación)
            var todosTrabajadores = await _context.Trabajadores
                .Include(t => t.Persona)
                .Include(t => t.AsignacionesTurno.Where(a => a.EsVigente))
                    .ThenInclude(a => a.Turno)
                        .ThenInclude(t => t.TipoTurno)
                .Include(t => t.AsignacionesTurno.Where(a => a.EsVigente))
                    .ThenInclude(a => a.HorarioTurno)
                        .ThenInclude(ht => ht.HorariosDetalle)
                .OrderBy(t => t.Persona!.ApellidosNombres)
                .Where(t =>
                    (soloSede
                        ? t.SucursalId == sucursalFiltro
                        : (!jefeId.HasValue || t.JefeInmediatoId == jefeId.Value))
                    && (!sucursalFiltro.HasValue || soloSede || t.SucursalId == sucursalFiltro))
                .ToListAsync();

            // PASO 1b: Cargar TODOS los horarios activos con sus detalles
            // Necesario para FIJ con múltiples horarios por turno (ej. L-V + SABADO separados)
            var horariosPorTurno = (await _context.HorariosTurno
                .Include(ht => ht.HorariosDetalle)
                .Where(ht => ht.EsActivo)
                .ToListAsync())
                .GroupBy(ht => ht.TurnoId)
                .ToDictionary(g => g.Key, g => g.ToList());

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
                // Obtener turno vigente del trabajador (para cuando no hay PROGRAMACION_TURNOS_SEMANAL)
                var turnoVigente = trab.AsignacionesTurno?.FirstOrDefault();
                var turnoIdDefault = turnoVigente?.Turno?.Id;
                var turnoNombreDefault = turnoVigente?.Turno?.NombreCodigo;
                var tipoTurnoNombreDefault = turnoVigente?.Turno?.TipoTurno?.NombreTipo;
                var horarioTurnoIdDefault = turnoVigente?.HorarioTurnoId;
                var esRotativo = tipoTurnoNombreDefault?.ToUpperInvariant().Contains("ROT") ?? false;
                // Solo auto-completar como "trabaja" si tiene asignación vigente y es FIJ
                var esFijoConAsignacion = turnoVigente != null && !esRotativo;

                var item = new ProgramacionPorTrabajadorDto
                {
                    TrabajadorId = trab.Id,
                    TrabajadorNombre = trab.Persona?.ApellidosNombres,
                    TipoTurnoNombre = tipoTurnoNombreDefault,
                    TurnoId = turnoIdDefault,
                    SucursalId = trab.SucursalId
                };

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
                            Estado = progSemanal.TipoAusencia != null ? progSemanal.TipoAusencia.ToLower() :
                                     progSemanal.EsDescanso ? "descanso" :
                                     progSemanal.EsDiaBoleta ? "boleta" :
                                     progSemanal.EsVacaciones ? "vacaciones" : "trabaja",
                            TipoAusencia = progSemanal.TipoAusencia
                        });
                    }
                    else
                    {
                        // Sin programación explícita:
                        // - ROT → "sin-asignar" (requiere programación diaria)
                        // - FIJ con HorarioDetalle → "trabaja" o "descanso" según el patrón semanal
                        // - Sin asignación → "sin-asignar"
                        string estadoAuto;
                        int? horarioDelDia = null;
                        if (!esFijoConAsignacion)
                        {
                            estadoAuto = "sin-asignar";
                        }
                        else
                        {
                            // Buscar entre TODOS los horarios del turno cuál cubre este día
                            // (ej. turno con HORARIO L-V + HORARIO SABADO separados)
                            List<HorarioTurno> horariosDelTurno;
                            if (turnoVigente != null && horariosPorTurno.TryGetValue(turnoVigente.TurnoId, out var hts))
                                horariosDelTurno = hts;
                            else if (turnoVigente?.HorarioTurno != null)
                                horariosDelTurno = new List<HorarioTurno> { turnoVigente.HorarioTurno };
                            else
                                horariosDelTurno = new List<HorarioTurno>();
                            var horarioParaDia = horariosDelTurno
                                .FirstOrDefault(ht => ht.HorariosDetalle?.Any(hd => IsDiaSemanaMatch(hd.DiaSemana, d)) == true);
                            estadoAuto = horarioParaDia != null ? "trabaja" : "descanso";
                            horarioDelDia = horarioParaDia?.Id;
                        }

                        item.Dias.Add(new ProgramacionDiaDto
                        {
                            Fecha = d,
                            HorarioTurnoId = estadoAuto == "trabaja" ? horarioDelDia : null,
                            HorarioTurnoNombre = null,
                            TurnoId = turnoIdDefault,
                            TurnoNombre = turnoNombreDefault,
                            Estado = estadoAuto
                        });
                    }
                }

                response.Items.Add(item);
            }

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
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
                var horarioIds = request.Programaciones
                    .Where(p => p.IdHorarioTurno.HasValue)
                    .Select(p => p.IdHorarioTurno!.Value).Distinct().ToList();

                var trabajadoresExisten = await _context.Trabajadores
                    .Where(t => trabIds.Contains(t.Id))
                    .CountAsync();

                if (trabajadoresExisten != trabIds.Count)
                    return NotFound("Uno o más trabajadores no existen");

                var horariosExisten = horarioIds.Count == 0 ? 0 : await _context.HorariosTurno
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
                        && trabIds.Contains(p.TrabajadorId)
                        && p.TipoAusencia == null)   // no borrar ausencias registradas
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
                    TipoAusencia = string.IsNullOrWhiteSpace(p.TipoAusencia) ? null : p.TipoAusencia.ToUpper(),
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

        // ── AUSENCIAS: listar y eliminar ──────────────────────────
        [HttpGet("ausencias")]
        public async Task<IActionResult> GetAusencias(
            [FromQuery] DateOnly? fechaInicio,
            [FromQuery] DateOnly? fechaFin,
            [FromQuery] int? trabajadorId,
            [FromQuery] string? tipo)
        {
            var query = _context.ProgramacionTurnosSemanal
                .Include(p => p.Trabajador).ThenInclude(t => t.Persona)
                .Where(p => p.TipoAusencia != null)
                .AsQueryable();

            if (fechaInicio.HasValue) query = query.Where(p => p.Fecha >= fechaInicio.Value);
            if (fechaFin.HasValue)    query = query.Where(p => p.Fecha <= fechaFin.Value);
            if (trabajadorId.HasValue) query = query.Where(p => p.TrabajadorId == trabajadorId.Value);
            if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(p => p.TipoAusencia == tipo.ToUpper());

            // ADMIN: solo ve ausencias de sus trabajadores directos
            if (User.IsInRole("ADMIN") && !User.IsInRole("SUPERADMIN"))
            {
                var jefeClaimAus = User.FindFirst("trabajador_id")?.Value;
                if (int.TryParse(jefeClaimAus, out var jefeIdAus))
                    query = query.Where(p => p.Trabajador.JefeInmediatoId == jefeIdAus);
            }

            var result = await query
                .OrderByDescending(p => p.Fecha)
                .Select(p => new
                {
                    id             = p.Id,
                    trabajadorId   = p.TrabajadorId,
                    trabajadorNombre = p.Trabajador.Persona!.ApellidosNombres,
                    dni            = p.Trabajador.Persona!.Dni,
                    fecha          = p.Fecha,
                    tipoAusencia   = p.TipoAusencia
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpDelete("ausencias/{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> DeleteAusencia(int id)
        {
            var pts = await _context.ProgramacionTurnosSemanal
                .FirstOrDefaultAsync(p => p.Id == id && p.TipoAusencia != null);

            if (pts == null) return NotFound(new { message = "Ausencia no encontrada" });

            _context.ProgramacionTurnosSemanal.Remove(pts);
            await _context.SaveChangesAsync();
            return NoContent();
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

        // ── Helpers: match DiaSemana (soporta número, nombre, rango, lista) ──────────

        private static bool IsDiaSemanaMatch(string diaSemanaRaw, DateOnly fecha)
        {
            if (string.IsNullOrWhiteSpace(diaSemanaRaw)) return false;
            // ISO: Lunes=1 ... Domingo=7
            var isoDay = fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;
            var parts = diaSemanaRaw
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p));

            foreach (var part in parts)
            {
                if (part.Contains('-'))
                {
                    var range = part.Split('-', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim()).ToArray();
                    if (range.Length != 2) continue;
                    var start = ParseDayTokenToIso(range[0]);
                    var end = ParseDayTokenToIso(range[1]);
                    if (start == null || end == null) continue;
                    if (start <= end) { if (isoDay >= start && isoDay <= end) return true; }
                    else { if (isoDay >= start || isoDay <= end) return true; } // wrap: Sáb-Mar
                }
                else
                {
                    var val = ParseDayTokenToIso(part);
                    if (val != null && val == isoDay) return true;
                }
            }
            return false;
        }

        private static int? ParseDayTokenToIso(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            token = token.Trim();
            if (int.TryParse(token, out var n))
            {
                if (n == 0) return 7; // domingo
                if (n >= 1 && n <= 7) return n;
            }
            return token.ToLowerInvariant() switch
            {
                "mon" or "monday" or "lun" or "lunes" => 1,
                "tue" or "tues" or "tuesday" or "mar" or "martes" => 2,
                "wed" or "wednesday" or "mie" or "miercoles" or "miércoles" => 3,
                "thu" or "thur" or "thurs" or "thursday" or "jue" or "jueves" => 4,
                "fri" or "friday" or "vie" or "viernes" => 5,
                "sat" or "saturday" or "sab" or "sabado" or "sábado" => 6,
                "sun" or "sunday" or "dom" or "domingo" => 7,
                _ => null
            };
        }
    }
}
