﻿using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
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
    public class TurnosController : ControllerBase
    {
        private readonly ITurnoService _turnoService;

        public TurnosController(ITurnoService turnoService) {
            _turnoService = turnoService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<Turno>>> GetAllAsync([FromQuery] PaginationDto pagination)
        {
            var turnos = await _turnoService.GetAllAsync(pagination);
            return Ok(turnos);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Turno>> GetTurnoById(int id)
        {
            var turno = await _turnoService.GetByIdAsync(id);
            if (turno == null) 
                return NotFound($"No se encontró turno con ID {id}.");
            return Ok(turno);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<ActionResult<Turno>> CreateTurno([FromBody] TurnoCreateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var turnoId = await _turnoService.AddAsync(request);
                return CreatedAtAction(nameof(GetTurnoById), new { id = turnoId }, request);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> UpdateTurno(int id, [FromBody] TurnoUpdateDto request)
        {
            if (id != request.Id) 
                return BadRequest("El ID del turno en la URL no coincide con el del cuerpo de la solicitud.");

            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            try
            {
                await _turnoService.UpdateAsync(id, request);
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
        public async Task<IActionResult> DeleteTurno(int id)
        {
            try
            {
                await _turnoService.DeleteAsync(id);
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
