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
    }
}
