using Asistencia.Data.Entities;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
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
    public class SucursalesController : ControllerBase
    {
        private readonly ISucursalCentroService _sucursalService;

        public SucursalesController(ISucursalCentroService sucursalService)
        {
            _sucursalService = sucursalService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<SucursalCentro>>> GetAllSucursales([FromQuery] PaginationDto pagination)
        {
            var sucursales = await _sucursalService.GetAllAsync(pagination);
            return Ok(sucursales);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SucursalCentro>> GetSucursalById(int id)
        {
            var sucursal = await _sucursalService.GetByIdAsync(id);
            if (sucursal == null) return NotFound($"No se encontró la sucursal con ID {id}.");
            return Ok(sucursal);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SucursalCentro>> CreateSucursal([FromBody] SucursalCentro sucursal)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _sucursalService.AddAsync(sucursal);

            // Como AddAsync es void, no retorna la nueva sucursal. Usamos el objeto recibido.
            return CreatedAtAction(nameof(GetSucursalById), new { id = sucursal.Id }, sucursal);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSucursal(int id, [FromBody] SucursalCentro sucursal)
        {
            if (id != sucursal.Id) return BadRequest("El ID de la sucursal en la URL no coincide con el del cuerpo de la solicitud.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _sucursalService.UpdateAsync(id, sucursal);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSucursal(int id)
        {
            try
            {
                await _sucursalService.DeleteAsync(id);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return NoContent();
        }
    }
}