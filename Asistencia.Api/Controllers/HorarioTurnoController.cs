using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Implements;
using Asistencia.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class HorarioTurnoController : ControllerBase
    {
        private readonly IHorarioTurnoService _horarioTurnoService;

        public HorarioTurnoController(IHorarioTurnoService horarioTurnoService)
        {
            _horarioTurnoService = horarioTurnoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HorarioTurno>>> GetAllAsync()
        {
            var turnos = await _horarioTurnoService.GetAllAsync();
            return Ok(turnos);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<HorarioTurno>> GetTurnoById(int id)
        {
            var turno = await _horarioTurnoService.GetByIdAsync(id);
            if (turno == null) return NotFound($"No se encontro turno.");
            return Ok(turno);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<HorarioTurno>> CreateTurno([FromBody] HorarioTurnoRequest turno)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _horarioTurnoService.AddAsync(turno);

            return CreatedAtAction(nameof(GetTurnoById), new { id = turno.TurnoId }, turno);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTurno(int id, [FromBody] HorarioTurno turno)
        {
            if (id != turno.Id) return BadRequest("El ID de la turno en la URL no coincide con el del cuerpo de la solicitud.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _horarioTurnoService.UpdateAsync(id, turno);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return NoContent();
        }
    }
}
