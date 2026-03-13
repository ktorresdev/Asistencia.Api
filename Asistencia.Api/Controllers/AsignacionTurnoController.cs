using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class AsignacionTurnoController : ControllerBase
    {
        private readonly IAsignacionTurnoService _asignacionTurnoService;

        public AsignacionTurnoController(IAsignacionTurnoService asignacionTurnoService)
        {
            _asignacionTurnoService = asignacionTurnoService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<AsignacionTurno>>> GetAllAsync([FromQuery] PaginationDto pagination)
        {
            var asignaciones = await _asignacionTurnoService.GetAllAsync(pagination);
            return Ok(asignaciones);
        }

        [HttpGet("{id:int}", Name = "GetByIdAsync")]
        public async Task<ActionResult<AsignacionTurno>> GetByIdAsync(int id)
        {
            var asignacion = await _asignacionTurnoService.GetByIdAsync(id);
            if (asignacion == null)
            {
                return NotFound($"No se encontró la asignación de turno con ID {id}.");
            }
            return Ok(asignacion);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AsignacionTurno>> CreateAsync([FromBody] AsignacionTurno createDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var nuevaAsignacion = await _asignacionTurnoService.AddAsync(createDto);
            if (nuevaAsignacion == null) return StatusCode(500, "Error al crear la asignación.");
            // Usamos CreatedAtAction para devolver un 201 con la URL del nuevo recurso
            return CreatedAtAction(nameof(GetByIdAsync), new { id = nuevaAsignacion.Id }, nuevaAsignacion);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] AsignacionTurno updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _asignacionTurnoService.UpdateAsync(id, updateDto);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            await _asignacionTurnoService.DeleteAsync(id);
            return NoContent();
        }
    }
}
