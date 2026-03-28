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
                var result = await _reportesService.GetInconsistenciasAsync(area);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error obteniendo inconsistencias: " + ex.Message);
            }
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] string? dni = null)
        {
            if (fechaInicio > fechaFin)
                return BadRequest("La fecha de inicio no puede ser mayor a la fecha fin.");

            try
            {
                var jefeId = GetJefeId();
                var result = await _reportesService.GetResumenAsistenciaAsync(fechaInicio, fechaFin, dni, jefeId);
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
            [FromQuery] DateTime fechaFin)
        {
            if (fechaInicio > fechaFin)
                return BadRequest("La fecha de inicio no puede ser mayor a la fecha fin.");

            var jefeId = GetJefeId();
            var resultado = await _reportesService.GetTardanzasAsync(fechaInicio, fechaFin, jefeId);
            return Ok(resultado);
        }

        [HttpGet("horas-extras")]
        public async Task<IActionResult> GetHorasExtras(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            if (fechaInicio > fechaFin)
                return BadRequest("La fecha de inicio no puede ser mayor a la fecha fin.");

            var jefeId = GetJefeId();
            var resultado = await _reportesService.GetHorasExtrasAsync(fechaInicio, fechaFin, jefeId);
            return Ok(resultado);
        }

        [HttpGet("trabajadores-por-jefe")]
        public async Task<IActionResult> GetTrabajadoresPorJefe(
            [FromQuery] string fecha,
            [FromQuery] int jefeId)
        {
            if (jefeId <= 0)
                return BadRequest("El parámetro jefeId es obligatorio y debe ser mayor a 0.");

            if (!TryParseFecha(fecha, out var fechaParsed))
                return BadRequest("El parámetro 'fecha' no tiene un formato válido. Use dd/MM/yyyy o yyyy-MM-dd.");

            var resultado = await _reportesService.GetTrabajadoresPorJefeYFechaAsync(fechaParsed, jefeId, null);
            return Ok(resultado);
        }

        [HttpGet("trabajadores-por-sucursal")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> GetTrabajadoresPorSucursal(
            [FromQuery] string fecha,
            [FromQuery] int sucursalId)
        {
            if (sucursalId <= 0)
                return BadRequest("El parámetro sucursalId es obligatorio y debe ser mayor a 0.");

            if (!TryParseFecha(fecha, out var fechaParsed))
                return BadRequest("El parámetro 'fecha' no tiene un formato válido. Use dd/MM/yyyy o yyyy-MM-dd.");

            var resultado = await _reportesService.GetTrabajadoresPorSucursalAsync(fechaParsed, sucursalId);
            return Ok(resultado);
        }

        private static bool TryParseFecha(string? valor, out DateTime resultado)
        {
            resultado = default;
            if (string.IsNullOrWhiteSpace(valor)) return false;
            return DateTime.TryParseExact(valor, new[] { "dd/MM/yyyy", "yyyy-MM-dd", "MM/dd/yyyy" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out resultado);
        }

        /// <summary>
        /// Devuelve el trabajador_id del usuario logueado.
        /// ADMIN/SUPERVISOR → su propio trabajador_id (filtra solo sus subordinados).
        /// SUPERADMIN → null (sin filtro, ve todos).
        /// </summary>
        private int? GetJefeId()
        {
            var rol = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();
            if (rol == "SUPERADMIN") return null;

            var claim = User.FindFirst("trabajador_id")?.Value;
            if (int.TryParse(claim, out var id)) return id;
            return null;
        }
    }
}