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
            if (turno == null) return NotFound($"No se encontro turno.");
            return Ok(turno);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Turno>> CreateTurno([FromBody] Turno turno)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _turnoService.AddAsync(turno);

            return CreatedAtAction(nameof(GetTurnoById), new { id = turno.Id }, turno);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTurno(int id, [FromBody] Turno turno)
        {
            if (id != turno.Id) return BadRequest("El ID de la turno en la URL no coincide con el del cuerpo de la solicitud.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _turnoService.UpdateAsync(id, turno);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return NoContent();
        }
    }
}
