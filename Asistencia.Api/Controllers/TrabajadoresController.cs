using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Data.Entities.UserEntites;
using Asistencia.Data.DbContexts;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class TrabajadoresController : ControllerBase
    {
        private readonly ITrabajadorService _trabajadorService;
        private readonly MarcacionAsistenciaDbContext _context;

        public TrabajadoresController(ITrabajadorService trabajadorService, MarcacionAsistenciaDbContext context)
        {
            _trabajadorService = trabajadorService;
            _context = context;
        }

        // GET: api/Rrhh/Trabajadores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Trabajador>>> GetAllTrabajadores([FromQuery] PaginationDto pagination)
        {
            var trabajadores = await _trabajadorService.GetAllAsync(pagination);
            return Ok(trabajadores);
        }

        // GET: api/Rrhh/Trabajadores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Trabajador>> GetTrabajadorById(int id)
        {
            var trabajador = await _trabajadorService.GetByIdAsync(id);

            if (trabajador == null)
            {
                return NotFound($"No se encontró el trabajador con ID {id}.");
            }

            return Ok(trabajador);
        }

        // POST: api/Rrhh/Trabajadores
        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<ActionResult<Trabajador>> CreateTrabajador([FromBody] TrabajadorDto trabajador)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _trabajadorService.AddAsync(trabajador);
            var nuevoTrabajador = trabajador; // O asigna el objeto correcto si AddAsync retorna el Trabajador creado

            return CreatedAtAction(nameof(GetTrabajadorById), new { id = trabajador.PersonaId }, trabajador);
        }

        // PUT: api/Rrhh/Trabajadores/5
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> UpdateTrabajador(int id, [FromBody] TrabajadorDto trabajador)
        {
            //if (id != trabajador.PersonaId)
            //{
            //    return BadRequest("El ID del trabajador en la URL no coincide con el del cuerpo de la solicitud.");
            //}

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _trabajadorService.UpdateAsync(id, trabajador);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return NoContent();
        }

        // DELETE: api/Rrhh/Trabajadores/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> DeleteTrabajador(int id)
        {
            try
            {
                await _trabajadorService.DeleteAsync(id);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return NoContent();
        }

        [HttpGet("~/api/trabajadores/{id:int}/turno-vigente")]
        public async Task<IActionResult> GetTurnoVigente(int id)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var asignacion = await _context.AsignacionesTurno
                .AsNoTracking()
                .Include(a => a.Turno)
                    .ThenInclude(t => t!.HorariosTurno)
                        .ThenInclude(ht => ht.HorariosDetalle)
                .Include(a => a.HorarioTurno)
                    .ThenInclude(h => h.HorariosDetalle)
                .FirstOrDefaultAsync(a => a.TrabajadorId == id
                    && a.EsVigente
                    && a.FechaInicioVigencia <= today
                    && (a.FechaFinVigencia == null || a.FechaFinVigencia.Value >= today));

            if (asignacion == null)
            {
                return NotFound(new { message = "El trabajador no tiene turno vigente." });
            }

            var horario = asignacion.HorarioTurno ?? asignacion.Turno?.HorariosTurno?.FirstOrDefault(h => h.EsActivo)
                ?? asignacion.Turno?.HorariosTurno?.FirstOrDefault();

            var response = new
            {
                trabajadorId = id,
                asignacionId = asignacion.Id,
                turno = new
                {
                    id = asignacion.Turno?.Id,
                    codigo = asignacion.Turno?.NombreCodigo,
                    tipoTurnoId = asignacion.Turno?.TipoTurnoId,
                    esActivo = asignacion.Turno?.EsActivo
                },
                vigencia = new
                {
                    inicio = asignacion.FechaInicioVigencia,
                    fin = asignacion.FechaFinVigencia,
                    esVigente = asignacion.EsVigente
                },
                horario = horario == null ? null : new
                {
                    idHorarioTurno = horario.Id,
                    nombreHorario = horario.NombreHorario,
                    detalles = horario.HorariosDetalle
                        .OrderBy(d => d.DiaSemana)
                        .Select(d => new
                        {
                            diaSemana = d.DiaSemana,
                            horaInicio = d.HoraInicio.ToString(@"hh\:mm"),
                            horaFin = d.HoraFin.ToString(@"hh\:mm"),
                            salidaDiaSiguiente = d.SalidaDiaSiguiente
                        })
                }
            };

            return Ok(response);
        }

        [HttpPost("~/api/trabajadores/{id:int}/asignar-turno")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> AsignarTurnoTrabajador(int id, [FromBody] AsignarTurnoTrabajadorRequest request)
        {
            if (request.FechaFinVigencia.HasValue && request.FechaFinVigencia.Value < request.FechaInicioVigencia)
            {
                return BadRequest(new { message = "La fecha fin no puede ser menor que la fecha inicio." });
            }

            var trabajadorExiste = await _context.Trabajadores.AnyAsync(t => t.Id == id);
            if (!trabajadorExiste)
            {
                return NotFound(new { message = $"No existe trabajador con ID {id}." });
            }

            var turno = await _context.Turnos.Include(t => t.TipoTurno).FirstOrDefaultAsync(t => t.Id == request.TurnoId);
            if (turno == null)
            {
                return NotFound(new { message = $"No existe turno con ID {request.TurnoId}." });
            }

            // Si el turno es rotativo, preferir exigir HorarioTurnoId
            var nombreTipo = turno.TipoTurno?.NombreTipo ?? string.Empty;
            var esRotativo = nombreTipo.ToUpperInvariant().Contains("ROT");
            if (esRotativo && !request.HorarioTurnoId.HasValue)
            {
                return BadRequest(new { message = "Para turnos rotativos se requiere HorarioTurnoId." });
            }

            // Si se proporcionó HorarioTurnoId, validar que exista y pertenezca al turno
            if (request.HorarioTurnoId.HasValue)
            {
                var horarioValido = await _context.HorariosTurno
                    .AnyAsync(h => h.Id == request.HorarioTurnoId.Value && h.TurnoId == request.TurnoId);
                if (!horarioValido)
                {
                    return NotFound(new { message = $"HorarioTurno con ID {request.HorarioTurnoId} no encontrado o no pertenece al turno {request.TurnoId}." });
                }
            }

            // Validar solapamiento de vigencias para el trabajador
            var newStart = request.FechaInicioVigencia;
            var newEnd = request.FechaFinVigencia ?? DateOnly.MaxValue;
            var existeSolapamiento = await _context.AsignacionesTurno
                .AnyAsync(a => a.TrabajadorId == id &&
                               newStart <= (a.FechaFinVigencia ?? DateOnly.MaxValue) &&
                               newEnd >= a.FechaInicioVigencia);

            if (existeSolapamiento)
            {
                return Conflict(new { message = "La vigencia de la nueva asignación solapa con otra existente para el trabajador." });
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            var asignacionesVigentes = await _context.AsignacionesTurno
                .Where(a => a.TrabajadorId == id && a.EsVigente)
                .ToListAsync();

            foreach (var vigente in asignacionesVigentes)
            {
                vigente.EsVigente = false;
                vigente.FechaFinVigencia = request.FechaInicioVigencia.AddDays(-1);
                vigente.UpdatedAt = DateTime.UtcNow;
            }

            var nuevaAsignacion = new AsignacionTurno
            {
                TrabajadorId = id,
                TurnoId = request.TurnoId,
                HorarioTurnoId = request.HorarioTurnoId,
                FechaInicioVigencia = request.FechaInicioVigencia,
                FechaFinVigencia = request.FechaFinVigencia,
                EsVigente = true,
                MotivoCambio = request.MotivoCambio,
                AprobadoPor = request.AprobadoPorTrabajadorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AsignacionesTurno.Add(nuevaAsignacion);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                asignacionId = nuevaAsignacion.Id,
                trabajadorId = nuevaAsignacion.TrabajadorId,
                turnoId = nuevaAsignacion.TurnoId,
                fechaInicioVigencia = nuevaAsignacion.FechaInicioVigencia,
                fechaFinVigencia = nuevaAsignacion.FechaFinVigencia,
                esVigente = nuevaAsignacion.EsVigente
            });
        }

        public sealed class AsignarTurnoTrabajadorRequest
        {
            public int TurnoId { get; set; }
            public int? HorarioTurnoId { get; set; }
            public DateOnly FechaInicioVigencia { get; set; }
            public DateOnly? FechaFinVigencia { get; set; }
            public string? MotivoCambio { get; set; }
            public int? AprobadoPorTrabajadorId { get; set; }
        }
    }
}
