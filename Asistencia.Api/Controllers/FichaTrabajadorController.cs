using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asistencia.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class FichaTrabajadorController : ControllerBase
    {
        private readonly IFichaTrabajadorService _service;

        public FichaTrabajadorController(IFichaTrabajadorService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Crear(CreateTrabajadorDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _service.postCrearTrabajador(dto);
                return CreatedAtAction(nameof(Crear), new { id = result.IdTrabajador }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocurrió un error interno en el servidor.", details = ex.Message }); // Retorna 500 Internal Server Error
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTrabajadorDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _service.UpdateTrabajadorAsync(id, dto);
                return Ok(result); // Retorna 200 OK con el objeto actualizado
            }
            catch (InvalidOperationException ex) // Para DNI o correo duplicado
            {
                return Conflict(new { message = ex.Message }); // Retorna 409 Conflict
            }
            catch (KeyNotFoundException ex) // Para IDs no encontrados (trabajador, sucursal, jefe)
            {
                return NotFound(new { message = ex.Message }); // Retorna 404 Not Found
            }
            catch (Exception ex) // Para cualquier otro error inesperado
            {
                return StatusCode(500, new { message = "Ocurrió un error interno en el servidor.", details = ex.Message });
            }
        }
    }
}
