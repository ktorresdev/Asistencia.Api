using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class JustificacionesController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        public JustificacionesController(
            MarcacionAsistenciaDbContext context,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        // GET /api/Rrhh/Justificaciones/tipos
        [HttpGet("tipos")]
        public async Task<IActionResult> GetTipos()
        {
            var tipos = await _context.TipoJustificaciones
                .Where(t => t.EsActivo)
                .OrderBy(t => t.NombreTipo)
                .Select(t => new { t.Id, t.NombreTipo, t.RequiereAdjunto })
                .ToListAsync();

            return Ok(tipos);
        }

        // POST /api/Rrhh/Justificaciones
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Crear([FromForm] CrearJustificacionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var maxArchivos = _configuration.GetValue<int>("Justificaciones:MaxArchivosAdjuntos", 5);
            var maxMb      = _configuration.GetValue<long>("Justificaciones:MaxTamanoArchivoMb", 5);
            var maxBytes   = maxMb * 1024 * 1024;

            // Validar que el trabajador solicitado coincide con el usuario si es TRABAJADOR
            var role = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();
            if (role == "TRABAJADOR")
            {
                var myTrabId = await GetTrabajadorIdAsync();
                if (myTrabId == null) return Forbid();
                if (request.TrabajadorId != myTrabId.Value)
                    return Forbid();
            }

            // Validar tipo de justificación
            var tipo = await _context.TipoJustificaciones
                .FirstOrDefaultAsync(t => t.Id == request.TipoJustificacionId && t.EsActivo);
            if (tipo == null)
                return BadRequest(new { message = "Tipo de justificación inválido o inactivo." });

            var archivos = request.Archivos ?? new List<IFormFile>();

            if (tipo.RequiereAdjunto && archivos.Count == 0)
                return BadRequest(new
                {
                    message = $"El tipo '{tipo.NombreTipo}' requiere al menos un documento adjunto."
                });

            if (archivos.Count > maxArchivos)
                return BadRequest(new
                {
                    message = $"Se permiten máximo {maxArchivos} archivos adjuntos por justificación."
                });

            foreach (var archivo in archivos)
            {
                if (archivo.Length <= 0)
                    return BadRequest(new { message = $"El archivo '{archivo.FileName}' está vacío." });

                if (archivo.Length > maxBytes)
                    return BadRequest(new
                    {
                        message = $"El archivo '{archivo.FileName}' supera el tamaño máximo de {maxMb} MB."
                    });

                if (!AllowedContentTypes.Contains(archivo.ContentType))
                    return BadRequest(new
                    {
                        message = $"Tipo de archivo no permitido ({archivo.ContentType}). Use PDF, Word, JPG, PNG o WEBP."
                    });
            }

            // Obtener estado PENDIENTE de justificaciones
            var estadoPendienteId = await _context.MaestroEstados
                .Where(e => e.GrupoEstado == "JUSTIFICACION" && e.NombreEstado == "PENDIENTE")
                .Select(e => e.Id)
                .FirstOrDefaultAsync();
            if (estadoPendienteId == 0) estadoPendienteId = 1; // fallback al default del modelo

            var justificacion = new Justificacion
            {
                TrabajadorId       = request.TrabajadorId,
                TipoJustificacionId = request.TipoJustificacionId,
                FechaJustificada   = request.FechaJustificada.Date,
                Motivo             = request.Motivo,
                IdEstado           = estadoPendienteId
            };

            _context.Justificaciones.Add(justificacion);
            await _context.SaveChangesAsync();

            // Guardar archivos adjuntos
            foreach (var archivo in archivos)
            {
                var url = await SaveFileAsync(archivo);
                _context.JustificacionDocumentos.Add(new JustificacionDocumento
                {
                    JustificacionId = justificacion.Id,
                    DocumentoUrl    = url,
                    NombreArchivo   = Path.GetFileName(archivo.FileName)
                });
            }

            if (archivos.Count > 0)
                await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                message = "Justificación registrada correctamente.",
                id      = justificacion.Id
            });
        }

        // GET /api/Rrhh/Justificaciones
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? fecha,
            [FromQuery] string?   estado,
            [FromQuery] int?      idTrabajador)
        {
            var role = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();

            var query = _context.Justificaciones
                .Include(j => j.TipoJustificacion)
                .Include(j => j.Estado)
                .Include(j => j.Documentos)
                .Include(j => j.Trabajador).ThenInclude(t => t.Persona)
                .AsQueryable();

            if (fecha.HasValue)
                query = query.Where(j => j.FechaJustificada.Date == fecha.Value.Date);

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(j => j.Estado.NombreEstado == estado.ToUpperInvariant());

            if (idTrabajador.HasValue)
                query = query.Where(j => j.TrabajadorId == idTrabajador.Value);

            if (role == "TRABAJADOR")
            {
                var myTrabId = await GetTrabajadorIdAsync();
                if (myTrabId == null) return Forbid();
                query = query.Where(j => j.TrabajadorId == myTrabId.Value);
            }
            else if (role != "SUPERADMIN")
            {
                var jefeTrabId = GetTrabajadorIdFromClaim();
                if (jefeTrabId.HasValue)
                {
                    var subordinados = await _context.Trabajadores
                        .Where(t => t.JefeInmediatoId == jefeTrabId.Value)
                        .Select(t => t.Id)
                        .ToListAsync();
                    query = query.Where(j => subordinados.Contains(j.TrabajadorId));
                }
            }

            var result = await query
                .OrderByDescending(j => j.FechaJustificada)
                .Select(j => new
                {
                    j.Id,
                    j.TrabajadorId,
                    NombreTrabajador    = j.Trabajador.Persona.ApellidosNombres,
                    j.TipoJustificacionId,
                    TipoJustificacion   = j.TipoJustificacion.NombreTipo,
                    RequiereAdjunto     = j.TipoJustificacion.RequiereAdjunto,
                    FechaJustificada    = j.FechaJustificada.ToString("yyyy-MM-dd"),
                    j.Motivo,
                    Estado              = j.Estado.NombreEstado,
                    j.FechaAutorizacion,
                    j.UsuarioAutoriza,
                    Documentos          = j.Documentos.Select(d => new
                    {
                        d.Id,
                        Url            = d.DocumentoUrl,
                        d.NombreArchivo
                    })
                })
                .ToListAsync();

            return Ok(result);
        }

        // GET /api/Rrhh/Justificaciones/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var j = await _context.Justificaciones
                .Include(j => j.TipoJustificacion)
                .Include(j => j.Estado)
                .Include(j => j.Documentos)
                .Include(j => j.Trabajador).ThenInclude(t => t.Persona)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (j == null)
                return NotFound(new { message = "Justificación no encontrada." });

            var role = (User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Trim().ToUpperInvariant();
            if (role == "TRABAJADOR")
            {
                var myTrabId = await GetTrabajadorIdAsync();
                if (myTrabId == null || j.TrabajadorId != myTrabId.Value) return Forbid();
            }

            return Ok(new
            {
                j.Id,
                j.TrabajadorId,
                NombreTrabajador    = j.Trabajador.Persona.ApellidosNombres,
                j.TipoJustificacionId,
                TipoJustificacion   = j.TipoJustificacion.NombreTipo,
                RequiereAdjunto     = j.TipoJustificacion.RequiereAdjunto,
                FechaJustificada    = j.FechaJustificada.ToString("yyyy-MM-dd"),
                j.Motivo,
                Estado              = j.Estado.NombreEstado,
                j.FechaAutorizacion,
                j.UsuarioAutoriza,
                Documentos          = j.Documentos.Select(d => new
                {
                    d.Id,
                    Url            = d.DocumentoUrl,
                    d.NombreArchivo
                })
            });
        }

        // PUT /api/Rrhh/Justificaciones/{id}/aprobar
        [HttpPut("{id:int}/aprobar")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var justificacion = await _context.Justificaciones
                .Include(j => j.Estado)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (justificacion == null)
                return NotFound(new { message = "Justificación no encontrada." });

            if (justificacion.Estado.NombreEstado != "PENDIENTE")
                return BadRequest(new { message = "Solo se pueden aprobar justificaciones en estado PENDIENTE." });

            var estadoAprobado = await _context.MaestroEstados
                .FirstOrDefaultAsync(e => e.GrupoEstado == "JUSTIFICACION" && e.NombreEstado == "APROBADO");

            if (estadoAprobado == null)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Estado 'APROBADO' no encontrado en MAESTRO_ESTADOS para el grupo JUSTIFICACION." });

            justificacion.IdEstado         = estadoAprobado.Id;
            justificacion.FechaAutorizacion = DateTime.Now;
            justificacion.UsuarioAutoriza  = User.Identity?.Name ?? "Sistema";
            justificacion.IdAutoriza       = GetTrabajadorIdFromClaim();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT /api/Rrhh/Justificaciones/{id}/rechazar
        [HttpPut("{id:int}/rechazar")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> Rechazar(int id)
        {
            var justificacion = await _context.Justificaciones
                .Include(j => j.Estado)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (justificacion == null)
                return NotFound(new { message = "Justificación no encontrada." });

            if (justificacion.Estado.NombreEstado != "PENDIENTE")
                return BadRequest(new { message = "Solo se pueden rechazar justificaciones en estado PENDIENTE." });

            var estadoRechazado = await _context.MaestroEstados
                .FirstOrDefaultAsync(e => e.GrupoEstado == "JUSTIFICACION" && e.NombreEstado == "RECHAZADO");

            if (estadoRechazado == null)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Estado 'RECHAZADO' no encontrado en MAESTRO_ESTADOS para el grupo JUSTIFICACION." });

            justificacion.IdEstado         = estadoRechazado.Id;
            justificacion.FechaAutorizacion = DateTime.Now;
            justificacion.UsuarioAutoriza  = User.Identity?.Name ?? "Sistema";
            justificacion.IdAutoriza       = GetTrabajadorIdFromClaim();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private async Task<int?> GetTrabajadorIdAsync()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId)) return null;

            return await _context.Trabajadores
                .Where(t => t.UserId == userId)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();
        }

        private int? GetTrabajadorIdFromClaim()
        {
            var claim = User.FindFirst("trabajador_id")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        private async Task<string> SaveFileAsync(IFormFile archivo)
        {
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var utcNow         = DateTime.UtcNow;
            var relativeFolder = Path.Combine("uploads", "justificaciones",
                                              utcNow.ToString("yyyy"), utcNow.ToString("MM"));
            var physicalFolder = Path.Combine(webRootPath, relativeFolder);
            Directory.CreateDirectory(physicalFolder);

            var extension = Path.GetExtension(archivo.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = archivo.ContentType switch
                {
                    "application/pdf"   => ".pdf",
                    "application/msword" => ".doc",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                    "image/png"         => ".png",
                    "image/webp"        => ".webp",
                    _                   => ".jpg"
                };
            }

            var fileName     = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var physicalPath = Path.Combine(physicalFolder, fileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
                await archivo.CopyToAsync(stream);

            var relativeUrl = "/" + Path.Combine(relativeFolder, fileName).Replace("\\", "/");
            return $"{Request.Scheme}://{Request.Host}{relativeUrl}";
        }

        // ─── Request / DTOs ──────────────────────────────────────────────────

        public sealed class CrearJustificacionRequest
        {
            [Required]
            public int TrabajadorId { get; set; }

            [Required]
            public int TipoJustificacionId { get; set; }

            [Required]
            public DateTime FechaJustificada { get; set; }

            [MaxLength(1000)]
            public string? Motivo { get; set; }

            public List<IFormFile>? Archivos { get; set; }
        }
    }
}
