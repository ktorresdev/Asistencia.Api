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

        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<ActionResult<AsignacionTurno>> CreateAsync([FromBody] AsignacionTurnoCreateDto createDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var nuevaAsignacion = await _asignacionTurnoService.AddAsync(createDto);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = nuevaAsignacion.Id }, nuevaAsignacion);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] AsignacionTurnoUpdateDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _asignacionTurnoService.UpdateAsync(id, updateDto);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                await _asignacionTurnoService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
