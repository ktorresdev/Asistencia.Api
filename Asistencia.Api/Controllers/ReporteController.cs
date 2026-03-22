using Asistencia.Services.Implements;
using Asistencia.Services.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Asistencia.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
    [ApiController]
    public class ReporteController : ControllerBase
    {
        public readonly IReportesService _reportesService;

        public ReporteController(IReportesService reportesService)
        {
            _reportesService = reportesService;
        }

        //[HttpPost("inasistencias")]
        //public async Task<IActionResult> GenerarReporteInasistencias([FromBody] FiltroReporteDto request)
        //{
        //    var respuesta = await _reportesService.GetInasistencias(request);
        //    return Ok(respuesta);
        //}

        //[HttpPost("tardanzas")]
        //public async Task<IActionResult> GenerarReporteTardanzas([FromBody] FiltroReporteDto request)
        //{
        //    var respuesta = await _reportesService.GetTardanzas(request);
        //    return Ok(respuesta);
        //}

        //[HttpPost("horas-extra")]
        //public async Task<IActionResult> GenerarReporteHorasExtra([FromBody] FiltroReporteDto request)
        //{
        //    var respuesta = await _reportesService.GetHorasExtra(request);
        //    return Ok(respuesta);
        //}

        //[HttpPost("resumen")]
        //public async Task<IActionResult> GenerarReporteResumen([FromBody] FiltroReporteDto request)
        //{
        //    var respuesta = await _reportesService.GetResumen(request);
        //    return Ok(respuesta);
        //}

        [HttpGet("inconsistencias")]
        [HttpGet("inasistencias")]
        public async Task<IActionResult> GetInconsistencias([FromQuery] string? area = null)
        {
            try
            {
                var scope = GetAreaScope(area);
                if (!scope.EsValido)
                {
                    return Forbid();
                }

                var result = await _reportesService.GetInconsistenciasAsync(scope.Area);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Loggear el error real aquí
                return StatusCode(500, "Error obteniendo inconsistencias: " + ex.Message);
            }
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] string? dni = null,
            [FromQuery] string? area = null)
        {
            // Validaciones básicas
            if (fechaInicio > fechaFin)
                return BadRequest("La fecha de inicio no puede ser mayor a la fecha fin.");

            try
            {
                var scope = GetAreaScope(area);
                if (!scope.EsValido)
                {
                    return Forbid();
                }

                var result = await _reportesService.GetResumenAsistenciaAsync(fechaInicio, fechaFin, dni, scope.Area);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error generando reporte: " + ex.Message);
            }
        }

        [HttpGet("tardanzas")]
        public async Task<IActionResult> GetTardanzas(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] string? area = null)
        {
            if (fechaInicio > fechaFin)
                return BadRequest("La fecha de inicio no puede ser mayor a la fecha fin.");

            var scope = GetAreaScope(area);
            if (!scope.EsValido)
            {
                return Forbid();
            }

            var resultado = await _reportesService.GetTardanzasAsync(fechaInicio, fechaFin, scope.Area);
            return Ok(resultado);
        }

        [HttpGet("horas-extras")]
        public async Task<IActionResult> GetHorasExtras(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] string? area = null)
        {
            if (fechaInicio > fechaFin)
                return BadRequest("La fecha de inicio no puede ser mayor a la fecha fin.");

            var scope = GetAreaScope(area);
            if (!scope.EsValido)
            {
                return Forbid();
            }

            var resultado = await _reportesService.GetHorasExtrasAsync(fechaInicio, fechaFin, scope.Area);
            return Ok(resultado);
        }

        [HttpGet("trabajadores-por-jefe")]
        public async Task<IActionResult> GetTrabajadoresPorJefe(
            [FromQuery] DateTime fecha,
            [FromQuery] int jefeId)
        {
            if (jefeId <= 0)
            {
                return BadRequest("El parámetro jefeId es obligatorio y debe ser mayor a 0.");
            }

            // Para este reporte el área se resuelve por jefeId dentro del servicio,
            // evitando depender del claim "area" en tokens de dispositivo.
            var resultado = await _reportesService.GetTrabajadoresPorJefeYFechaAsync(fecha, jefeId, null);
            return Ok(resultado);
        }

        private (bool EsValido, string? Area) GetAreaScope(string? areaSolicitada)
        {
            var rol = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();

            if (rol == "SUPERADMIN" || rol == "SUPERVISOR")
            {
                return (true, string.IsNullOrWhiteSpace(areaSolicitada) ? null : areaSolicitada.Trim());
            }

            if (rol != "ADMIN")
            {
                return (false, null);
            }

            var areaAdmin = User.FindFirst("area")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(areaAdmin))
            {
                return (false, null);
            }

            // Para ADMIN siempre se fuerza el área del token, ignorando query string.
            return (true, areaAdmin);
        }
    }
}