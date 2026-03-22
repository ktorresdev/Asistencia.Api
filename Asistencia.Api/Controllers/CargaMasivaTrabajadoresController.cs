using Asistencia.Services.Implements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Asistencia.Api.Controllers
{
    public class ImportarTrabajadoresArchivoRequest
    {
        [Required]
        public IFormFile Archivo { get; set; } = null!;
    }

    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class CargaMasivaTrabajadoresController : ControllerBase
    {
        private readonly ICargaMasivaTrabajadoresService _cargaMasivaTrabajadoresService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<CargaMasivaTrabajadoresController> _logger;

        public CargaMasivaTrabajadoresController(
            ICargaMasivaTrabajadoresService cargaMasivaTrabajadoresService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<CargaMasivaTrabajadoresController> logger)
        {
            _cargaMasivaTrabajadoresService = cargaMasivaTrabajadoresService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [HttpPost("importar")]
        [HttpPost("importar-csv")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> ImportarArchivo([FromForm] ImportarTrabajadoresArchivoRequest request, CancellationToken cancellationToken)
        {
            var archivo = request.Archivo;

            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest(new { message = "Debes adjuntar un archivo CSV o XLSX con contenido." });
            }

            var extension = Path.GetExtension(archivo.FileName);
            var esCsv = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
            var esXlsx = extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
            if (!esCsv && !esXlsx)
            {
                return BadRequest(new { message = "Solo se permite archivo con extensión .csv o .xlsx" });
            }

            try
            {
                await using var stream = archivo.OpenReadStream();
                var resultado = esXlsx
                    ? await _cargaMasivaTrabajadoresService.ProcesarXlsxAsync(stream, archivo.FileName, cancellationToken)
                    : await _cargaMasivaTrabajadoresService.ProcesarCsvAsync(stream, archivo.FileName, cancellationToken);

                var carpetaLogs = Path.Combine(_webHostEnvironment.ContentRootPath, "Logs", "Importaciones");
                Directory.CreateDirectory(carpetaLogs);

                var nombreLog = $"importacion-trabajadores-{resultado.ImportacionId}.json";
                var rutaCompletaLog = Path.Combine(carpetaLogs, nombreLog);

                var opcionesJson = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(resultado, opcionesJson);
                await System.IO.File.WriteAllTextAsync(rutaCompletaLog, json, cancellationToken);

                resultado.RutaLogJson = Path.GetRelativePath(_webHostEnvironment.ContentRootPath, rutaCompletaLog).Replace("\\", "/");

                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante la carga masiva de trabajadores.");
                return StatusCode(500, new { message = "Error interno durante la importación masiva." });
            }
        }
    }
}
