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
        public async Task<IActionResult> GetResumen([FromQuery] DateTime fecha, [FromQuery] int? idArea)
        {
            var role = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();

            int? jefeId = null;
            if (role != "SUPERADMIN")
            {
                var trabIdClaim = User.FindFirst("trabajador_id")?.Value;
                if (!int.TryParse(trabIdClaim, out var tid)) return Forbid();
                jefeId = tid;
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
                        ard.minutos_extra AS MinutosExtra,
                        ard.id_asignacion AS IdAsignacion,
                        tu.nombre_codigo AS Turno,
                        CONVERT(varchar(5), ard.hora_entrada_teorica, 108) AS EntradaTeorica,
                        CONVERT(varchar(5), ard.hora_salida_teorica, 108) AS SalidaTeorica
                    FROM dbo.ASISTENCIA_RESUMEN_DIARIO ard
                    INNER JOIN dbo.TRABAJADORES t ON t.id_trabajador = ard.id_trabajador
                    INNER JOIN dbo.PERSONAS p ON p.id_persona = t.id_persona
                    LEFT JOIN dbo.ASIGNACIONES_TURNO ast ON ast.id_asignacion = ard.id_asignacion
                    LEFT JOIN dbo.TURNOS tu ON tu.id_turno = ast.id_turno
                    WHERE ard.fecha_asistencia = {0}
                      AND ({1} IS NULL OR t.id_jefe_inmediato = {1})
                      AND ({2} IS NULL OR t.id_area = {2})
                    ORDER BY p.apellidos_nombres ASC, ard.hora_entrada_teorica ASC", fecha.Date, jefeId, idArea)
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
            // Doble turno: identifican a qué turno corresponde esta fila del día.
            public int? IdAsignacion { get; set; }
            public string? Turno { get; set; }
            public string? EntradaTeorica { get; set; }
            public string? SalidaTeorica { get; set; }
        }
    }
}
