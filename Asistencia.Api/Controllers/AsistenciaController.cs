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
    public class AsistenciaController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public AsistenciaController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen([FromQuery] DateTime fecha)
        {
            var role = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return Forbid();

            var trabajador = await _context.Trabajadores
                .AsNoTracking()
                .Include(t => t.Persona)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (trabajador == null) return Forbid();

            int? sucursalFiltro = null;
            if (role == "TRABAJADOR")
            {
                sucursalFiltro = trabajador.SucursalId;
            }
            else if (role == "ADMIN")
            {
                if (Request.Headers.TryGetValue("X-Sucursal-Activa", out var sucHdr) && int.TryParse(sucHdr.ToString(), out var sucId) && sucId > 0)
                {
                    sucursalFiltro = sucId;
                }
                else
                {
                    sucursalFiltro = trabajador.SucursalId;
                }
            }

            var data = await _context.Database
                .SqlQueryRaw<AsistenciaResumenDto>(@"
                    SELECT
                        ard.id_resumen AS IdResumen,
                        ard.id_trabajador AS IdTrabajador,
                        p.apellidos_nombres AS Nombre,
                        p.dni AS Dni,
                        CONVERT(varchar(10), ard.fecha_asistencia, 23) AS FechaAsistencia,
                        ard.estado_asistencia AS EstadoAsistencia,
                        ard.minutos_tardanza AS MinutosTardanza,
                        ard.minutos_extra AS MinutosExtra
                    FROM dbo.ASISTENCIA_RESUMEN_DIARIO ard
                    INNER JOIN dbo.TRABAJADORES t ON t.id_trabajador = ard.id_trabajador
                    INNER JOIN dbo.PERSONAS p ON p.id_persona = t.id_persona
                    WHERE ard.fecha_asistencia = {0}
                      AND ({1} IS NULL OR t.id_sucursal = {1})
                    ORDER BY p.apellidos_nombres ASC", fecha.Date, sucursalFiltro)
                .ToListAsync();

            return Ok(data);
        }

        private sealed class AsistenciaResumenDto
        {
            public long IdResumen { get; set; }
            public int IdTrabajador { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Dni { get; set; } = string.Empty;
            public string FechaAsistencia { get; set; } = string.Empty;
            public string EstadoAsistencia { get; set; } = string.Empty;
            public int MinutosTardanza { get; set; }
            public int MinutosExtra { get; set; }
        }
    }
}
