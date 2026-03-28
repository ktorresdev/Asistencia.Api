using Asistencia.Data.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public DashboardController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen()
        {
            var role = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();

            int? jefeId = null;
            if (role != "SUPERADMIN")
            {
                var trabIdClaim = User.FindFirst("trabajador_id")?.Value;
                if (!int.TryParse(trabIdClaim, out var tid))
                    return Forbid();
                jefeId = tid;
            }

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek + 1); // lunes
            var finSemana = inicioSemana.AddDays(6);                   // domingo

            // Base: trabajadores activos (sin fecha de baja) filtrados por jefe
            var trabQ = _context.Trabajadores.Where(t => t.FechaBaja == null);
            if (jefeId.HasValue)
                trabQ = trabQ.Where(t => t.JefeInmediatoId == jefeId.Value);

            var totalTrabajadores = await trabQ.CountAsync();
            var trabIds = await trabQ.Select(t => t.Id).ToListAsync();

            // Asistencia del día (desde ASISTENCIA_RESUMEN_DIARIO)
            var asistenciasHoy = await _context.AsistenciaResumenDiarios
                .Where(a => a.FechaAsistencia == hoy && trabIds.Contains(a.TrabajadorId))
                .GroupBy(a => a.EstadoAsistencia)
                .Select(g => new { Estado = g.Key, Cant = g.Count() })
                .ToListAsync();

            var presenteHoy   = asistenciasHoy.Where(x => x.Estado != null && x.Estado.StartsWith("PRESENTE")).Sum(x => x.Cant);
            var tardanzaHoy   = asistenciasHoy.Where(x => x.Estado != null && x.Estado.StartsWith("TARDANZA")).Sum(x => x.Cant);
            var faltaHoy      = asistenciasHoy.Where(x => x.Estado != null && (x.Estado.StartsWith("FALTA") || x.Estado == "AUSENTE")).Sum(x => x.Cant);
            var totalRegistrados = asistenciasHoy.Sum(x => x.Cant);
            var porcentajeAsistencia = totalRegistrados > 0
                ? Math.Round((presenteHoy + tardanzaHoy) * 100.0 / totalRegistrados, 1)
                : 0.0;

            // Ausencias esta semana (PTS con tipo_ausencia != null)
            var ausenciasSemana = await _context.ProgramacionTurnosSemanal
                .Where(p => p.Fecha >= inicioSemana && p.Fecha <= finSemana
                         && p.TipoAusencia != null
                         && trabIds.Contains(p.TrabajadorId))
                .CountAsync();

            // Coberturas pendientes
            var coberturasPendientes = await _context.CoberturasTurno
                .Where(c => c.Estado == "PENDIENTE"
                         && (trabIds.Contains(c.IdTrabajadorCubre) || trabIds.Contains(c.IdTrabajadorAusente)))
                .CountAsync();

            // Trabajadores sin programación esta semana
            var conProgramacion = await _context.ProgramacionTurnosSemanal
                .Where(p => p.Fecha >= inicioSemana && p.Fecha <= finSemana && trabIds.Contains(p.TrabajadorId))
                .Select(p => p.TrabajadorId)
                .Distinct()
                .CountAsync();

            var sinProgramacion = totalTrabajadores - conProgramacion;

            return Ok(new DashboardResumenDto
            {
                TotalTrabajadores    = totalTrabajadores,
                PresenteHoy          = presenteHoy,
                TardanzaHoy          = tardanzaHoy,
                FaltaHoy             = faltaHoy,
                PorcentajeAsistencia = porcentajeAsistencia,
                AusenciasSemana      = ausenciasSemana,
                CoberturasPendientes = coberturasPendientes,
                SinProgramacion      = sinProgramacion < 0 ? 0 : sinProgramacion,
                FechaConsulta        = hoy.ToString("yyyy-MM-dd"),
                InicioSemana         = inicioSemana.ToString("yyyy-MM-dd"),
                FinSemana            = finSemana.ToString("yyyy-MM-dd"),
            });
        }

        private sealed class DashboardResumenDto
        {
            public int    TotalTrabajadores    { get; set; }
            public int    PresenteHoy          { get; set; }
            public int    TardanzaHoy          { get; set; }
            public int    FaltaHoy             { get; set; }
            public double PorcentajeAsistencia { get; set; }
            public int    AusenciasSemana      { get; set; }
            public int    CoberturasPendientes { get; set; }
            public int    SinProgramacion      { get; set; }
            public string FechaConsulta        { get; set; } = string.Empty;
            public string InicioSemana         { get; set; } = string.Empty;
            public string FinSemana            { get; set; } = string.Empty;
        }
    }
}
