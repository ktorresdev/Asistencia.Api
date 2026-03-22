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
    public class NotificacionesController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public NotificacionesController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNoLeidas()
        {
            var userId = GetUserId();
            if (userId == null) return Forbid();

            var data = await _context.Database
                .SqlQueryRaw<NotificacionDto>(@"
                    SELECT
                        n.id_notificacion AS IdNotificacion,
                        n.tipo AS Tipo,
                        n.titulo AS Titulo,
                        n.mensaje AS Mensaje,
                        n.leida AS Leida,
                        CONVERT(varchar(19), n.created_at, 120) AS CreatedAt
                    FROM dbo.NOTIFICACIONES n
                    WHERE n.id_user = {0}
                      AND n.leida = 0
                    ORDER BY n.created_at DESC", userId.Value)
                .ToListAsync();

            return Ok(data);
        }

        [HttpPut("{id:long}/leer")]
        public async Task<IActionResult> MarcarLeida(long id)
        {
            var userId = GetUserId();
            if (userId == null) return Forbid();

            var rows = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.NOTIFICACIONES
                SET leida = 1,
                    leida_at = SYSUTCDATETIME()
                WHERE id_notificacion = {0}
                  AND id_user = {1}", id, userId.Value);

            if (rows == 0) return NotFound(new { message = "Notificación no encontrada." });
            return NoContent();
        }

        [HttpPut("leer-todas")]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var userId = GetUserId();
            if (userId == null) return Forbid();

            await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.NOTIFICACIONES
                SET leida = 1,
                    leida_at = SYSUTCDATETIME()
                WHERE id_user = {0}
                  AND leida = 0", userId.Value);

            return NoContent();
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        private sealed class NotificacionDto
        {
            public long IdNotificacion { get; set; }
            public string Tipo { get; set; } = string.Empty;
            public string Titulo { get; set; } = string.Empty;
            public string Mensaje { get; set; } = string.Empty;
            public bool Leida { get; set; }
            public string CreatedAt { get; set; } = string.Empty;
        }
    }
}
