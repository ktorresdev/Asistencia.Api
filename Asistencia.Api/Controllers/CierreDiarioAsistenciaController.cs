using Asistencia.Api.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class CierreDiarioAsistenciaController : ControllerBase
    {
        private readonly ICierreDiarioAsistenciaExecutor _executor;
        private readonly ILogger<CierreDiarioAsistenciaController> _logger;

        public CierreDiarioAsistenciaController(
            ICierreDiarioAsistenciaExecutor executor,
            ILogger<CierreDiarioAsistenciaController> logger)
        {
            _executor = executor;
            _logger = logger;
        }

        [HttpPost("ejecutar/{fechaProceso}")]
        public async Task<IActionResult> Ejecutar([FromRoute] string fechaProceso, CancellationToken cancellationToken)
        {
            if (!DateTime.TryParseExact(fechaProceso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            {
                return BadRequest(new { message = "Debe enviar una fecha valida en formato yyyy-MM-dd en la ruta." });
            }

            try
            {
                fecha = fecha.Date;
                await _executor.ExecuteStoredProcedureAsync(fecha, cancellationToken);

                return Ok(new
                {
                    message = "Cierre diario ejecutado correctamente.",
                    fechaProceso = fecha.ToString("yyyy-MM-dd")
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout,
                    new { message = "La ejecucion del cierre diario fue cancelada." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando cierre diario manual para fecha {FechaProceso:yyyy-MM-dd}.", fecha);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error ejecutando el cierre diario de asistencia." });
            }
        }
    }
}
