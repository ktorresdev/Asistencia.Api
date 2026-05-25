using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class JustificacionesController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public JustificacionesController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        [HttpGet("tipos")]
        public async Task<IActionResult> GetTipos()
        {
            var tipos = await _context.TipoJustificaciones
                .Where(t => t.EsActivo)
                .OrderBy(t => t.Id)
                .Select(t => new { t.Id, t.NombreTipo, t.RequiereAdjunto })
                .ToListAsync();

            return Ok(tipos);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? trabajadorId,
            [FromQuery] DateOnly? fechaDesde,
            [FromQuery] DateOnly? fechaHasta,
            [FromQuery] int? idEstado)
        {
            var role = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();

            int? miTrabajadorId = await GetTrabajadorIdFromUserAsync();

            if (role == "TRABAJADOR")
            {
                if (miTrabajadorId == null) return Forbid();
                trabajadorId = miTrabajadorId.Value;
            }

            var query = _context.Justificaciones
                .Include(j => j.TipoJustificacion)
                .Include(j => j.Estado)
                .Include(j => j.Trabajador).ThenInclude(t => t.Persona)
                .AsQueryable();

            if (trabajadorId.HasValue)
                query = query.Where(j => j.TrabajadorId == trabajadorId.Value);

            if (fechaDesde.HasValue)
                query = query.Where(j => DateOnly.FromDateTime(j.FechaJustificada) >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(j => DateOnly.FromDateTime(j.FechaJustificada) <= fechaHasta.Value);

            if (idEstado.HasValue)
                query = query.Where(j => j.IdEstado == idEstado.Value);

            if (role == "SUPERVISOR" && miTrabajadorId.HasValue)
            {
                var jefeId = miTrabajadorId.Value;
                query = query.Where(j => j.Trabajador.JefeInmediatoId == jefeId);
            }

            var result = await query
                .OrderByDescending(j => j.FechaJustificada)
                .Select(j => new JustificacionDto
                {
                    Id = j.Id,
                    TrabajadorId = j.TrabajadorId,
                    NombreTrabajador = j.Trabajador.Persona.ApellidosNombres,
                    TipoJustificacionId = j.TipoJustificacionId,
                    NombreTipo = j.TipoJustificacion.NombreTipo,
                    RequiereAdjunto = j.TipoJustificacion.RequiereAdjunto,
                    FechaJustificada = j.FechaJustificada.ToString("yyyy-MM-dd"),
                    Motivo = j.Motivo,
                    DocumentoAdjuntoUrl = j.DocumentoAdjuntoUrl,
                    IdEstado = j.IdEstado,
                    NombreEstado = j.Estado.NombreEstado,
                    ColorEstado = j.Estado.ColorHex,
                    FechaAutorizacion = j.FechaAutorizacion,
                    UsuarioAutoriza = j.UsuarioAutoriza
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearJustificacionRequest request)
        {
            var role = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();
            int? miTrabajadorId = await GetTrabajadorIdFromUserAsync();

            int targetTrabajadorId;
            if (role == "TRABAJADOR")
            {
                if (miTrabajadorId == null) return Forbid();
                targetTrabajadorId = miTrabajadorId.Value;
            }
            else
            {
                if (request.TrabajadorId == null)
                    return BadRequest(new { message = "Se requiere el id_trabajador." });
                targetTrabajadorId = request.TrabajadorId.Value;
            }

            var tipo = await _context.TipoJustificaciones.FindAsync(request.TipoJustificacionId);
            if (tipo == null || !tipo.EsActivo)
                return BadRequest(new { message = "Tipo de justificación no válido." });

            if (tipo.RequiereAdjunto && string.IsNullOrWhiteSpace(request.DocumentoAdjuntoUrl))
                return BadRequest(new { message = $"El tipo '{tipo.NombreTipo}' requiere documento adjunto." });

            var estadoPendiente = await _context.MaestroEstados
                .Where(e => e.GrupoEstado == "JUSTIFICACION" && e.NombreEstado == "PENDIENTE")
                .Select(e => e.Id)
                .FirstOrDefaultAsync();

            if (estadoPendiente == 0)
                estadoPendiente = 1;

            var ya = await _context.Justificaciones.AnyAsync(j =>
                j.TrabajadorId == targetTrabajadorId &&
                j.FechaJustificada.Date == request.FechaJustificada.Date &&
                j.IdEstado != 3);

            if (ya)
                return Conflict(new { message = "Ya existe una justificación activa para esa fecha." });

            var justificacion = new Justificacion
            {
                TrabajadorId = targetTrabajadorId,
                TipoJustificacionId = request.TipoJustificacionId,
                FechaJustificada = request.FechaJustificada.Date,
                Motivo = request.Motivo,
                DocumentoAdjuntoUrl = request.DocumentoAdjuntoUrl,
                IdEstado = estadoPendiente
            };

            _context.Justificaciones.Add(justificacion);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new { justificacion.Id, message = "Justificación registrada." });
        }

        [HttpPut("{id:int}/aprobar")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var justificacion = await _context.Justificaciones
                .Include(j => j.TipoJustificacion)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (justificacion == null)
                return NotFound(new { message = "Justificación no encontrada." });

            var estadoActual = await _context.MaestroEstados
                .Where(e => e.Id == justificacion.IdEstado)
                .Select(e => e.NombreEstado)
                .FirstOrDefaultAsync();

            if (estadoActual != "PENDIENTE")
                return BadRequest(new { message = "Solo se pueden aprobar justificaciones en estado PENDIENTE." });

            if (justificacion.TipoJustificacion.RequiereAdjunto && string.IsNullOrWhiteSpace(justificacion.DocumentoAdjuntoUrl))
                return BadRequest(new { message = "Esta justificación requiere documento adjunto antes de ser aprobada." });

            var estadoAprobado = await _context.MaestroEstados
                .Where(e => e.GrupoEstado == "JUSTIFICACION" && e.NombreEstado == "APROBADO")
                .Select(e => e.Id)
                .FirstOrDefaultAsync();

            if (estadoAprobado == 0) estadoAprobado = 2;

            var aprobadorUsername = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("username")?.Value
                ?? "sistema";
            var aprobadorTrabId = await GetTrabajadorIdFromUserAsync();

            justificacion.IdEstado = estadoAprobado;
            justificacion.FechaAutorizacion = DateTime.UtcNow;
            justificacion.UsuarioAutoriza = aprobadorUsername;
            justificacion.IdAutoriza = aprobadorTrabId;

            var fechaSolo = DateOnly.FromDateTime(justificacion.FechaJustificada);
            var resumen = await _context.AsistenciaResumenDiarios
                .FirstOrDefaultAsync(r =>
                    r.TrabajadorId == justificacion.TrabajadorId &&
                    r.FechaAsistencia == fechaSolo);

            string? efectoAplicado = null;
            if (resumen != null)
            {
                var estadoResumen = resumen.EstadoAsistencia?.ToUpperInvariant();
                if (estadoResumen == "TARDANZA")
                {
                    resumen.MinutosTardanza = 0;
                    resumen.EstadoAsistencia = "PRESENTE";
                    efectoAplicado = "Tardanza eliminada, estado cambiado a PRESENTE";
                }
                else if (estadoResumen is "FALTA" or "AUSENTE")
                {
                    resumen.EstadoAsistencia = "JUSTIFICADO";
                    efectoAplicado = "Estado cambiado a JUSTIFICADO";
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Justificación aprobada.",
                efectoEnAsistencia = efectoAplicado ?? "Sin registro de asistencia para esa fecha."
            });
        }

        [HttpPut("{id:int}/rechazar")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarRequest? request)
        {
            var justificacion = await _context.Justificaciones.FindAsync(id);
            if (justificacion == null)
                return NotFound(new { message = "Justificación no encontrada." });

            var estadoActual = await _context.MaestroEstados
                .Where(e => e.Id == justificacion.IdEstado)
                .Select(e => e.NombreEstado)
                .FirstOrDefaultAsync();

            if (estadoActual != "PENDIENTE")
                return BadRequest(new { message = "Solo se pueden rechazar justificaciones en estado PENDIENTE." });

            var estadoRechazado = await _context.MaestroEstados
                .Where(e => e.GrupoEstado == "JUSTIFICACION" && e.NombreEstado == "RECHAZADO")
                .Select(e => e.Id)
                .FirstOrDefaultAsync();

            if (estadoRechazado == 0) estadoRechazado = 3;

            justificacion.IdEstado = estadoRechazado;

            if (!string.IsNullOrWhiteSpace(request?.MotivoRechazo))
                justificacion.Motivo = (justificacion.Motivo ?? "") + $" | RECHAZADO: {request.MotivoRechazo}";

            await _context.SaveChangesAsync();
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

        public sealed class CrearJustificacionRequest
        {
            public int? TrabajadorId { get; set; }
            public int TipoJustificacionId { get; set; }
            public DateTime FechaJustificada { get; set; }
            public string? Motivo { get; set; }
            public string? DocumentoAdjuntoUrl { get; set; }
        }

        public sealed class RechazarRequest
        {
            public string? MotivoRechazo { get; set; }
        }

        private sealed class JustificacionDto
        {
            public int Id { get; set; }
            public int TrabajadorId { get; set; }
            public string NombreTrabajador { get; set; } = string.Empty;
            public int TipoJustificacionId { get; set; }
            public string NombreTipo { get; set; } = string.Empty;
            public bool RequiereAdjunto { get; set; }
            public string FechaJustificada { get; set; } = string.Empty;
            public string? Motivo { get; set; }
            public string? DocumentoAdjuntoUrl { get; set; }
            public int IdEstado { get; set; }
            public string NombreEstado { get; set; } = string.Empty;
            public string? ColorEstado { get; set; }
            public DateTime? FechaAutorizacion { get; set; }
            public string? UsuarioAutoriza { get; set; }
        }
    }
}
