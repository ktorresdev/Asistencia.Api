using Asistencia.Data.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            if (request.IdTrabajadorCubre == request.IdTrabajadorAusente)
            {
                return BadRequest(new { message = "El trabajador que cubre no puede ser el mismo ausente." });
            }

            var results = await _context.Database
                .SqlQueryRaw<CoberturaResultadoDto>(@"
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
                        request.AprobadoPor)
                .ToListAsync();  // ← Ejecuta en BD y trae los resultados

            var result = results.FirstOrDefault();  // Luego filtra en memoria

            if (result == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo registrar la cobertura." });
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? fecha, [FromQuery] string? estado, [FromQuery] int? idTrabajador)
        {
            var userId = GetUserId();
            if (userId == null) return Forbid();

            var role = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();
            var trabajadorId = await _context.Trabajadores
                .Where(t => t.UserId == userId.Value)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();

            var query = @"
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
                  AND ({2} IS NULL OR c.id_trabajador_cubre = {2} OR c.id_trabajador_ausente = {2})";

            if (role == "TRABAJADOR" && trabajadorId.HasValue)
            {
                query += " AND (c.id_trabajador_cubre = {3} OR c.id_trabajador_ausente = {3})";
                var dataTrab = await _context.Database
                    .SqlQueryRaw<CoberturaDto>(query + " ORDER BY c.fecha DESC", fecha, estado, idTrabajador, trabajadorId.Value)
                    .ToListAsync();
                return Ok(dataTrab);
            }

            var data = await _context.Database
                .SqlQueryRaw<CoberturaDto>(query + " ORDER BY c.fecha DESC", fecha, estado, idTrabajador)
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
            public int IdTrabajadorCubre { get; set; }
            public int IdTrabajadorAusente { get; set; }
            public int IdHorarioTurnoOriginal { get; set; }
            public string TipoCobertura { get; set; } = "COBERTURA";
            public DateTime? FechaSwapDevolucion { get; set; }
            public int? AprobadoPor { get; set; }
        }

        private sealed class CoberturaResultadoDto
        {
            public int IdCobertura { get; set; }
            public string TipoCobertura { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
        }

        private sealed class CoberturaDto
        {
            public int IdCobertura { get; set; }
            public string Fecha { get; set; } = string.Empty;
            public int IdTrabajadorCubre { get; set; }
            public int IdTrabajadorAusente { get; set; }
            public int IdHorarioTurnoOriginal { get; set; }
            public string TipoCobertura { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public string? FechaSwapDevolucion { get; set; }
            public int? AprobadoPor { get; set; }
        }
    }
}
