using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class MarcacionAsistenciaController : ControllerBase
    {
        private readonly IMarcacionAsistenciaService _marcacionAsistenciaService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MarcacionAsistenciaController> _logger;
        private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };
        private const long MaxImageBytes = 3 * 1024 * 1024;

        public MarcacionAsistenciaController(IMarcacionAsistenciaService marcacionAsistenciaService, IWebHostEnvironment environment, IConfiguration configuration, ILogger<MarcacionAsistenciaController> logger)
        {
            _marcacionAsistenciaService = marcacionAsistenciaService;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<MarcacionAsistencia>>> GetAll([FromQuery] PaginationDto pagination)
        {
            var marcaciones = await _marcacionAsistenciaService.GetAllAsync(pagination);
            return Ok(marcaciones);
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] MarcacionRequest marcacionRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return await ProcessMarcacionAsync(marcacionRequest);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PostForm([FromForm] MarcacionFormRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string? fotoUrl = null;
            if (request.Foto != null)
            {
                if (request.Foto.Length <= 0)
                {
                    return BadRequest(new { success = false, code = "ERROR_IMAGEN_INVALIDA", message = "La imagen enviada está vacía." });
                }

                if (request.Foto.Length > MaxImageBytes)
                {
                    return BadRequest(new { success = false, code = "ERROR_IMAGEN_PESO", message = "La imagen supera el tamaño máximo permitido de 3 MB." });
                }

                if (!AllowedImageContentTypes.Contains(request.Foto.ContentType))
                {
                    return BadRequest(new { success = false, code = "ERROR_IMAGEN_TIPO", message = "Formato de imagen no permitido. Use JPG, PNG o WEBP." });
                }

                try
                {
                    fotoUrl = await SaveImageAsync(request.Foto);
                }
                catch (Exception ex)
                {
                    // Log detallado para diagnosticar (ruta, permisos, recurso de red).
                    _logger.LogError(ex,
                        "Fallo al guardar imagen de marcación. Trabajador={IdTrabajador} BasePath='{BasePath}' WebRoot='{WebRoot}'",
                        request.IdTrabajador,
                        _configuration["ImageStorage:BasePath"],
                        _environment.WebRootPath);
                    // No bloquear el registro de asistencia por un fallo de almacenamiento de imagen:
                    // se registra la marcación sin foto y queda el error en el log para corregir el destino.
                    fotoUrl = null;
                }
            }

            var marcacionRequest = new MarcacionRequest
            {
                IdTrabajador = request.IdTrabajador,
                Latitud = request.Latitud,
                Longitud = request.Longitud,
                FotoUrl = fotoUrl,
                EsMockLocation = request.EsMockLocation
            };

            return await ProcessMarcacionAsync(marcacionRequest);
        }

        private async Task<IActionResult> ProcessMarcacionAsync(MarcacionRequest marcacionRequest)
        {

            var response = await _marcacionAsistenciaService.AddMarcacionAsync(marcacionRequest);

            if (!response.Success)
            {
                // Map response.Code to HTTP status
                return response.Code switch
                {
                    "ERROR_NO_TURNO" => NotFound(new { success = false, code = response.Code, message = response.Message, detail = response.Detail }),
                    "ERROR_TRABAJADOR_NO_ENCONTRADO" => NotFound(new { success = false, code = response.Code, message = response.Message, detail = response.Detail }),
                    "ERROR_SIN_HORARIO" => NotFound(new { success = false, code = response.Code, message = response.Message, detail = response.Detail }),
                    "ERROR_GPS_FALSO" => StatusCode(StatusCodes.Status403Forbidden, new { success = false, code = response.Code, message = response.Message, detail = response.Detail }),
                    "ERROR_FUERA_ZONA" => StatusCode(StatusCodes.Status403Forbidden, new { success = false, code = response.Code, message = response.Message, detail = response.Detail }),
                    "ERROR_SALIDA_REGISTRADA" => Conflict(new { success = false, code = response.Code, message = response.Message, detail = response.Detail }),
                    "ERROR_DUPLICADO_RECIENTE" => Conflict(new { success = false, code = response.Code, message = response.Message, detail = response.Detail }),
                    _ => BadRequest(new { success = false, code = response.Code ?? "ERROR_UNKNOWN", message = response.Message, detail = response.Detail })
                };
            }

            // Success -> return 201 Created
            return StatusCode(StatusCodes.Status201Created, new { success = true, code = response.Code, message = response.Message, data = response.Data });
        }

        private async Task<string> SaveImageAsync(IFormFile foto)
        {
            var utcNow = DateTime.UtcNow;
            var anio = utcNow.ToString("yyyy");
            var mes = utcNow.ToString("MM");

            // Prefijo de URL con que se sirven las imágenes (configurable; default /uploads/marcaciones)
            var requestPath = _configuration["ImageStorage:RequestPath"];
            if (string.IsNullOrWhiteSpace(requestPath)) requestPath = "/uploads/marcaciones";
            requestPath = "/" + requestPath.Trim('/');

            // Carpeta física base configurable (ej. recurso de red \\10.1.2.4\asistencias\imagenes).
            // Vacío = wwwroot (comportamiento actual). Se puede cambiar desde appsettings sin recompilar.
            var basePath = _configuration["ImageStorage:BasePath"];

            string physicalFolder;
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                // Las imágenes se guardan en <BasePath>\yyyy\MM y se sirven en <RequestPath>/yyyy/MM
                // (el mapeo de archivos estáticos hacia BasePath se configura en Program.cs).
                physicalFolder = Path.Combine(basePath, anio, mes);
            }
            else
            {
                var webRootPath = _environment.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRootPath))
                {
                    webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }
                var relativeFolder = requestPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                physicalFolder = Path.Combine(webRootPath, relativeFolder, anio, mes);
            }

            Directory.CreateDirectory(physicalFolder);

            var extension = Path.GetExtension(foto.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = foto.ContentType switch
                {
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    _ => ".jpg"
                };
            }

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var physicalPath = Path.Combine(physicalFolder, fileName);

            _logger.LogInformation("Guardando imagen de marcación en '{PhysicalPath}' (BasePath='{BasePath}')",
                physicalPath, basePath);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await foto.CopyToAsync(stream);
            }

            var relativeUrl = $"{requestPath}/{anio}/{mes}/{fileName}";
            return $"{Request.Scheme}://{Request.Host}{relativeUrl}";
        }

        public class MarcacionFormRequest
        {
            public int IdTrabajador { get; set; }
            public double Latitud { get; set; }
            public double Longitud { get; set; }
            public IFormFile? Foto { get; set; }
            public bool? EsMockLocation { get; set; }
        }

        // ✅ Consultar si puede marcar y obtener horario actual
        [HttpGet("status/{trabajadorId}")]
        public async Task<IActionResult> GetMarcacionStatus(int trabajadorId)
        {
            try
            {
                var timeWorked = await _marcacionAsistenciaService.CalculateTimeWorkedAsync(trabajadorId);

                var isEntryRegistered = timeWorked.EntryRegisteredAt.HasValue;
                var isExitRegistered = timeWorked.ExitRegisteredAt.HasValue && 
                                      timeWorked.EntryRegisteredAt.HasValue && 
                                      timeWorked.ExitRegisteredAt.Value > timeWorked.EntryRegisteredAt.Value;

                return Ok(new
                {
                    success = true,
                    trabajadorId = trabajadorId,

                    // Información de horario
                    horarioProgramado = timeWorked.ScheduledTime,

                    // Información de marcaciones
                    marcacionEntrada = timeWorked.EntryRegisteredAt,
                    marcacionSalida = timeWorked.ExitRegisteredAt,
                    tiempoTrabajadoMinutos = timeWorked.TimeWorkedMinutes,
                    tiempoTrabajadoFormato = timeWorked.TimeWorkedFormatted,

                    // Estados
                    estado = timeWorked.StatusMessage,

                    // Permisos de marcación
                    puedeMarcarEntrada = !isEntryRegistered,      // ✅ Puede entrar si NO ha entrado
                    puedeMarcarSalida = isEntryRegistered && !isExitRegistered, // ✅ Puede salir si entró pero no salió
                    salidaPendiente = isEntryRegistered && !isExitRegistered     // ⚠️ Tiene salida pendiente
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    code = "ERROR_TRABAJADOR_NO_ENCONTRADO",
                    message = "No se encontró el trabajador o no tiene turno asignado.",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    code = "ERROR_INTERNO",
                    message = "Error al consultar el estado de marcación.",
                    detail = ex.Message
                });
            }
        }
    }
}
