using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class HorarioTurnoController : ControllerBase
    {
        private readonly IHorarioTurnoService _horarioTurnoService;
        private readonly MarcacionAsistenciaDbContext _context;

        public HorarioTurnoController(IHorarioTurnoService horarioTurnoService, MarcacionAsistenciaDbContext context)
        {
            _horarioTurnoService = horarioTurnoService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var horarios = await _context.HorariosTurno
                .Include(h => h.HorariosDetalle)
                .OrderBy(h => h.NombreHorario)
                .Select(h => new
                {
                    id = h.Id,
                    turnoId = h.TurnoId,
                    nombreHorario = h.NombreHorario,
                    esActivo = h.EsActivo,
                    horariosDetalle = h.HorariosDetalle.OrderBy(d => d.DiaSemana).Select(d => new
                    {
                        id = d.Id,
                        horarioTurnoId = d.HorarioTurnoId,
                        diaSemana = d.DiaSemana,
                        horaInicio = d.HoraInicio,
                        horaFin = d.HoraFin,
                        horaInicioRefrigerio = d.HoraInicioRefrigerio,
                        horaFinRefrigerio = d.HoraFinRefrigerio,
                        tiempoRefrigerioMinutos = d.TiempoRefrigerioMinutos,
                        salidaDiaSiguiente = d.SalidaDiaSiguiente
                    })
                })
                .ToListAsync();
            return Ok(horarios);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTurnoById(int id)
        {
            var horario = await _context.HorariosTurno
                .Include(h => h.HorariosDetalle)
                .Where(h => h.Id == id)
                .Select(h => new
                {
                    id = h.Id,
                    turnoId = h.TurnoId,
                    nombreHorario = h.NombreHorario,
                    esActivo = h.EsActivo,
                    horariosDetalle = h.HorariosDetalle.OrderBy(d => d.DiaSemana).Select(d => new
                    {
                        id = d.Id,
                        horarioTurnoId = d.HorarioTurnoId,
                        diaSemana = d.DiaSemana,
                        horaInicio = d.HoraInicio,
                        horaFin = d.HoraFin,
                        horaInicioRefrigerio = d.HoraInicioRefrigerio,
                        horaFinRefrigerio = d.HoraFinRefrigerio,
                        tiempoRefrigerioMinutos = d.TiempoRefrigerioMinutos,
                        salidaDiaSiguiente = d.SalidaDiaSiguiente
                    })
                })
                .FirstOrDefaultAsync();

            if (horario == null) return NotFound(new { message = $"No se encontró HorarioTurno con ID {id}." });
            return Ok(horario);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> CreateTurno([FromBody] HorarioTurnoRequest turno)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _horarioTurnoService.AddAsync(turno);
                return CreatedAtAction(nameof(GetTurnoById), new { id = created.Id }, new { id = created.Id, turnoId = created.TurnoId, nombreHorario = created.NombreHorario, esActivo = created.EsActivo, horariosDetalle = Array.Empty<object>() });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> UpdateTurno(int id, [FromBody] HorarioTurnoUpdateDto request)
        {
            if (id != request.Id) return BadRequest("El ID del horario en la URL no coincide con el del cuerpo.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _horarioTurnoService.UpdateAsync(id, new HorarioTurno
                {
                    Id = request.Id,
                    TurnoId = request.TurnoId,
                    NombreHorario = request.NombreHorario,
                    EsActivo = request.EsActivo
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            return NoContent();
        }

        // ── DETALLES ─────────────────────────────────────────────────────────────

        [HttpPost("{id:int}/detalles")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> CreateDetalle(int id, [FromBody] HorarioDetalleRequest request)
        {
            if (!await _context.HorariosTurno.AnyAsync(h => h.Id == id))
                return NotFound(new { message = $"HorarioTurno {id} no encontrado." });

            var detalle = new HorarioDetalle
            {
                HorarioTurnoId = id,
                DiaSemana = request.DiaSemana,
                HoraInicio = request.HoraInicio,
                HoraFin = request.HoraFin,
                HoraInicioRefrigerio = request.HoraInicioRefrigerio,
                HoraFinRefrigerio = request.HoraFinRefrigerio,
                TiempoRefrigerioMinutos = request.TiempoRefrigerioMinutos,
                SalidaDiaSiguiente = request.SalidaDiaSiguiente
            };

            _context.HorariosDetalle.Add(detalle);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                id = detalle.Id,
                horarioTurnoId = detalle.HorarioTurnoId,
                diaSemana = detalle.DiaSemana,
                horaInicio = detalle.HoraInicio,
                horaFin = detalle.HoraFin,
                horaInicioRefrigerio = detalle.HoraInicioRefrigerio,
                horaFinRefrigerio = detalle.HoraFinRefrigerio,
                tiempoRefrigerioMinutos = detalle.TiempoRefrigerioMinutos,
                salidaDiaSiguiente = detalle.SalidaDiaSiguiente
            });
        }

        [HttpPut("{id:int}/detalles/{detalleId:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> UpdateDetalle(int id, int detalleId, [FromBody] HorarioDetalleRequest request)
        {
            var detalle = await _context.HorariosDetalle
                .FirstOrDefaultAsync(d => d.Id == detalleId && d.HorarioTurnoId == id);
            if (detalle == null) return NotFound(new { message = $"Detalle {detalleId} no encontrado en horario {id}." });

            detalle.DiaSemana = request.DiaSemana;
            detalle.HoraInicio = request.HoraInicio;
            detalle.HoraFin = request.HoraFin;
            detalle.HoraInicioRefrigerio = request.HoraInicioRefrigerio;
            detalle.HoraFinRefrigerio = request.HoraFinRefrigerio;
            detalle.TiempoRefrigerioMinutos = request.TiempoRefrigerioMinutos;
            detalle.SalidaDiaSiguiente = request.SalidaDiaSiguiente;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}/detalles/{detalleId:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> DeleteDetalle(int id, int detalleId)
        {
            var detalle = await _context.HorariosDetalle
                .FirstOrDefaultAsync(d => d.Id == detalleId && d.HorarioTurnoId == id);
            if (detalle == null) return NotFound(new { message = $"Detalle {detalleId} no encontrado en horario {id}." });

            _context.HorariosDetalle.Remove(detalle);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        public sealed class HorarioDetalleRequest
        {
            public required string DiaSemana { get; set; }
            public TimeSpan HoraInicio { get; set; }
            public TimeSpan HoraFin { get; set; }
            public TimeSpan? HoraInicioRefrigerio { get; set; }
            public TimeSpan? HoraFinRefrigerio { get; set; }
            public int TiempoRefrigerioMinutos { get; set; } = 60;
            public bool SalidaDiaSiguiente { get; set; }
        }
    }
}
