using Asistencia.Data.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CoberturasController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public CoberturasController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> Registrar([FromBody] CrearCoberturaRequest request)
        {
            if (!request.EsSoloAsignacion && request.IdTrabajadorAusente == null)
                return BadRequest(new { message = "Se requiere el trabajador ausente para un reemplazo." });

            if (request.IdTrabajadorCubre.HasValue && request.IdTrabajadorAusente.HasValue
                && request.IdTrabajadorCubre == request.IdTrabajadorAusente)
                return BadRequest(new { message = "El trabajador que cubre no puede ser el mismo ausente." });

            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    EXEC dbo.SP_REGISTRAR_COBERTURA_TURNO
                        @fecha = {0},
                        @id_trabajador_cubre = {1},
                        @id_trabajador_ausente = {2},
                        @id_horario_turno_original = {3},
                        @tipo_cobertura = {4},
                        @fecha_swap_devolucion = {5},
                        @aprobado_por = {6}",
                        request.Fecha.Date,
                        request.IdTrabajadorCubre,
                        request.IdTrabajadorAusente,
                        request.IdHorarioTurnoOriginal,
                        request.TipoCobertura,
                        request.FechaSwapDevolucion,
                        request.AprobadoPor);

                return StatusCode(StatusCodes.Status201Created, new { message = "Cobertura registrada correctamente." });
            }
            catch (SqlException ex)
            {
                // Errores de negocio lanzados con RAISERROR/THROW desde el SP (severidad < 17)
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? fecha, [FromQuery] string? estado, [FromQuery] int? idTrabajador)
        {
            var role = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();

            int? jefeId = null;
            if (role == "TRABAJADOR")
            {
                var userId = GetUserId();
                if (userId == null) return Forbid();
                var trabId = await _context.Trabajadores
                    .Where(t => t.UserId == userId.Value)
                    .Select(t => (int?)t.Id)
                    .FirstOrDefaultAsync();
                if (!trabId.HasValue) return Forbid();

                var query2 = @"
                    SELECT
                        c.id_cobertura AS IdCobertura,
                        CONVERT(varchar(10), c.fecha, 23) AS Fecha,
                        c.id_trabajador_cubre AS IdTrabajadorCubre,
                        c.id_trabajador_ausente AS IdTrabajadorAusente,
                        c.id_horario_turno_original AS IdHorarioTurnoOriginal,
                        c.tipo_cobertura AS TipoCobertura,
                        c.estado AS Estado,
                        CONVERT(varchar(10), c.fecha_swap_devolucion, 23) AS FechaSwapDevolucion,
                        c.aprobado_por AS AprobadoPor
                    FROM dbo.COBERTURA_TURNOS c
                    WHERE ({0} IS NULL OR c.fecha = {0})
                      AND ({1} IS NULL OR c.estado = {1})
                      AND ({2} IS NULL OR c.id_trabajador_cubre = {2} OR c.id_trabajador_ausente = {2})
                      AND (c.id_trabajador_cubre = {3} OR c.id_trabajador_ausente = {3})
                    ORDER BY c.fecha DESC";
                var dataTrab = await _context.Database
                    .SqlQueryRaw<CoberturaDto>(query2, fecha, estado, idTrabajador, trabId.Value)
                    .ToListAsync();
                return Ok(dataTrab);
            }

            if (role != "SUPERADMIN")
            {
                var trabClaim = User.FindFirst("trabajador_id")?.Value;
                if (int.TryParse(trabClaim, out var tid)) jefeId = tid;
            }

            var baseQuery = @"
                SELECT
                    c.id_cobertura AS IdCobertura,
                    CONVERT(varchar(10), c.fecha, 23) AS Fecha,
                    c.id_trabajador_cubre AS IdTrabajadorCubre,
                    c.id_trabajador_ausente AS IdTrabajadorAusente,
                    c.id_horario_turno_original AS IdHorarioTurnoOriginal,
                    c.tipo_cobertura AS TipoCobertura,
                    c.estado AS Estado,
                    CONVERT(varchar(10), c.fecha_swap_devolucion, 23) AS FechaSwapDevolucion,
                    c.aprobado_por AS AprobadoPor
                FROM dbo.COBERTURA_TURNOS c
                WHERE ({0} IS NULL OR c.fecha = {0})
                  AND ({1} IS NULL OR c.estado = {1})
                  AND ({2} IS NULL OR c.id_trabajador_cubre = {2} OR c.id_trabajador_ausente = {2})
                  AND ({3} IS NULL
                       OR EXISTS (SELECT 1 FROM dbo.TRABAJADORES t WHERE t.id_trabajador = c.id_trabajador_ausente AND t.id_jefe_inmediato = {3})
                       OR EXISTS (SELECT 1 FROM dbo.TRABAJADORES t WHERE t.id_trabajador = c.id_trabajador_cubre  AND t.id_jefe_inmediato = {3}))
                ORDER BY c.fecha DESC";

            var data = await _context.Database
                .SqlQueryRaw<CoberturaDto>(baseQuery, fecha, estado, idTrabajador, jefeId)
                .ToListAsync();

            return Ok(data);
        }

        [HttpPut("{id:int}/aprobar")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var aprobador = await GetTrabajadorIdFromUserAsync();

            var rows = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.COBERTURA_TURNOS
                SET estado = 'APROBADO',
                    aprobado_por = {1},
                    updated_at = SYSUTCDATETIME()
                WHERE id_cobertura = {0}
                  AND estado = 'PENDIENTE'", id, aprobador);

            if (rows == 0) return NotFound(new { message = "Cobertura no encontrada o no está pendiente." });
            return NoContent();
        }

        [HttpPut("{id:int}/rechazar")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> Rechazar(int id)
        {
            var rows = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.COBERTURA_TURNOS
                SET estado = 'RECHAZADO',
                    updated_at = SYSUTCDATETIME()
                WHERE id_cobertura = {0}
                  AND estado = 'PENDIENTE'", id);

            if (rows == 0) return NotFound(new { message = "Cobertura no encontrada o no está pendiente." });
            return NoContent();
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        private async Task<int?> GetTrabajadorIdFromUserAsync()
        {
            var userId = GetUserId();
            if (userId == null) return null;

            return await _context.Trabajadores
                .Where(t => t.UserId == userId.Value)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();
        }

        public sealed class CrearCoberturaRequest
        {
            public DateTime Fecha { get; set; }
            public int? IdTrabajadorCubre { get; set; }
            public int? IdTrabajadorAusente { get; set; }
            public int? IdHorarioTurnoOriginal { get; set; }
            public string TipoCobertura { get; set; } = "COBERTURA";
            public string? MotivoFalta { get; set; }
            public DateTime? FechaSwapDevolucion { get; set; }
            public int? AprobadoPor { get; set; }
            public bool EsSoloAsignacion { get; set; } = false;
        }

        private sealed class CoberturaDto
        {
            public int IdCobertura { get; set; }
            public string Fecha { get; set; } = string.Empty;
            public int? IdTrabajadorCubre { get; set; }
            public int IdTrabajadorAusente { get; set; }
            public int? IdHorarioTurnoOriginal { get; set; }
            public string TipoCobertura { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public string? FechaSwapDevolucion { get; set; }
            public int? AprobadoPor { get; set; }
        }
    }
}
