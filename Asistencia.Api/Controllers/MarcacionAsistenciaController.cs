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
        private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };
        private const long MaxImageBytes = 3 * 1024 * 1024;

        public MarcacionAsistenciaController(IMarcacionAsistenciaService marcacionAsistenciaService, IWebHostEnvironment environment)
        {
            _marcacionAsistenciaService = marcacionAsistenciaService;
            _environment = environment;
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

                fotoUrl = await SaveImageAsync(request.Foto);
            }

            var marcacionRequest = new MarcacionRequest
            {
                IdTrabajador = request.IdTrabajador,
                Latitud = request.Latitud,
                Longitud = request.Longitud,
                FotoUrl = fotoUrl
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
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var utcNow = DateTime.UtcNow;
            var relativeFolder = Path.Combine("uploads", "marcaciones", utcNow.ToString("yyyy"), utcNow.ToString("MM"));
            var physicalFolder = Path.Combine(webRootPath, relativeFolder);
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

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await foto.CopyToAsync(stream);
            }

            var relativeUrl = "/" + Path.Combine(relativeFolder, fileName).Replace("\\", "/");
            return $"{Request.Scheme}://{Request.Host}{relativeUrl}";
        }

        public class MarcacionFormRequest
        {
            public int IdTrabajador { get; set; }
            public double Latitud { get; set; }
            public double Longitud { get; set; }
            public IFormFile? Foto { get; set; }
        }

        // Dentro de AttendanceController.cs
        [HttpGet("status/{trabajadorId}")]
        public async Task<IActionResult> GetAttendanceStatus(int trabajadorId)
        {
            // Llamada al servicio de estado
            var status = await _marcacionAsistenciaService.CalculateTimeWorkedAsync(trabajadorId);

            // Lógica para los botones (basada en el DTO devuelto)
            var isEntryRegistered = status.EntryRegisteredAt.HasValue;
            var isExitRegistered = status.ExitRegisteredAt.HasValue && status.EntryRegisteredAt.HasValue && status.ExitRegisteredAt.Value > status.EntryRegisteredAt.Value;

            return Ok(new
            {
                // Datos para Cards Informativas
                TimeData = status,
                // Datos para los Botones
                PermiteMarcarEntrada = !isEntryRegistered, //|| isExitRegistered, // Permite entrada si no ha marcado O si ya cerró la jornada anterior
                PermiteMarcarSalida = isEntryRegistered && !isExitRegistered, // Permite salida solo si entró y no ha salido

                // Estado visual del app
                SalidaPendiente = isEntryRegistered && !isExitRegistered
            });
        }
    }
}
