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
        public async Task<ActionResult<PagedResult<SucursalCentro>>> GetAllSucursales([FromQuery] PaginationDto pagination, [FromQuery] string? search = null)
        {
            var sucursales = await _sucursalService.GetAllAsync(pagination, search);
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
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<ActionResult<SucursalCentro>> CreateSucursal([FromBody] SucursalCentroCreateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var sucursal = new SucursalCentro
                {
                    NombreSucursal = request.NombreSucursal,
                    Direccion = request.Direccion,
                    LatitudCentro = request.LatitudCentro,
                    LongitudCentro = request.LongitudCentro,
                    PerimetroM = request.PerimetroM,
                    EsActivo = request.EsActivo
                };

                await _sucursalService.AddAsync(sucursal);
                return CreatedAtAction(nameof(GetSucursalById), new { id = sucursal.Id }, sucursal);
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
        public async Task<IActionResult> UpdateSucursal(int id, [FromBody] SucursalCentroUpdateDto request)
        {
            if (id != request.Id) 
                return BadRequest("El ID de la sucursal en la URL no coincide con el del cuerpo de la solicitud.");

            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            try
            {
                var sucursal = new SucursalCentro
                {
                    Id = request.Id,
                    NombreSucursal = request.NombreSucursal,
                    Direccion = request.Direccion,
                    LatitudCentro = request.LatitudCentro,
                    LongitudCentro = request.LongitudCentro,
                    PerimetroM = request.PerimetroM,
                    EsActivo = request.EsActivo
                };

                await _sucursalService.UpdateAsync(id, sucursal);
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