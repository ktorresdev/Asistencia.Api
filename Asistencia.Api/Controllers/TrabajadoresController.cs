using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Data.Entities.UserEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.AspNetCore.Authorization;
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

        public TrabajadoresController(ITrabajadorService trabajadorService)
        {
            _trabajadorService = trabajadorService;
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
    }
}
