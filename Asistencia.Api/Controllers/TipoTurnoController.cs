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
    public class TipoTurnoController : ControllerBase
    {
        private readonly ITipoTurnoService _tipoTurnoService;

        public TipoTurnoController(ITipoTurnoService tipoTurnoService)
        {
            _tipoTurnoService = tipoTurnoService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<TipoTurno>>> GetAllAsync([FromQuery] PaginationDto pagination)
        {
            var tipoTurnos = await _tipoTurnoService.GetAllAsync(pagination);
            return Ok(tipoTurnos);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TipoTurno>> GetTipoTurnoById(int id)
        {
            var tipoTurno = await _tipoTurnoService.GetByIdAsync(id);
            if (tipoTurno == null)
                return NotFound($"No se encontró tipo de turno con ID {id}.");
            return Ok(tipoTurno);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<ActionResult<TipoTurno>> CreateTipoTurno([FromBody] TipoTurnoCreateDto request)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            try
            {
                var tipoTurnoId = await _tipoTurnoService.AddAsync(request);
                return CreatedAtAction(nameof(GetTipoTurnoById), new { id = tipoTurnoId }, request);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> UpdateTipoTurno(int id, [FromBody] TipoTurnoUpdateDto request)
        {
            if (id != request.Id)
                return BadRequest("El ID del tipo de turno en la URL no coincide con el del cuerpo de la solicitud.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _tipoTurnoService.UpdateAsync(id, request);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> DeleteTipoTurno(int id)
        {
            try
            {
                await _tipoTurnoService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
