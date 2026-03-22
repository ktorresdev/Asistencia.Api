using Asistencia.Data.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DescansosController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public DescansosController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        [HttpPost("semana")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> CargarSemana([FromBody] CargarSemanaDescansosRequest request)
        {
            if (request.Trabajadores == null || request.Trabajadores.Count == 0)
            {
                return BadRequest(new { message = "Debe enviar al menos un trabajador." });
            }

            var fechaLunes = GetMonday(request.FechaLunes.Date);

            if (request.Trabajadores.Any(t => t.DiaDescanso < 0 || t.DiaDescanso > 6))
            {
                return BadRequest(new { message = "DiaDescanso debe estar entre 0 y 6." });
            }

            if (request.Trabajadores.Any(t => t.DiasBoleta.Any(d => d < 0 || d > 6)))
            {
                return BadRequest(new { message = "DiasBoleta solo admite valores entre 0 y 6." });
            }

            var semanaXml = new XElement("semana",
                request.Trabajadores.Select(t =>
                    new XElement("t",
                        new XAttribute("id", t.IdTrabajador),
                        new XAttribute("desc", t.DiaDescanso),
                        new XAttribute("bol", string.Join(",", t.DiasBoleta.Distinct().OrderBy(d => d))))));

            var pFechaLunes = new SqlParameter("@FechaLunes", fechaLunes);
            var pDatosXml = new SqlParameter("@DatosXML", System.Data.SqlDbType.Xml)
            {
                Value = semanaXml.ToString(SaveOptions.DisableFormatting)
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.SP_CARGAR_SEMANA_DESCANSOS @FechaLunes, @DatosXML",
                pFechaLunes,
                pDatosXml);

            return Ok(new
            {
                ok = true,
                mensaje = "Semana cargada.",
                fechaLunes = fechaLunes.ToString("yyyy-MM-dd"),
                fechaFin = fechaLunes.AddDays(6).ToString("yyyy-MM-dd")
            });
        }

        [HttpGet("{idTrabajador:int}/{semana:datetime}")]
        public async Task<IActionResult> GetSemana(int idTrabajador, DateTime semana)
        {
            var lunes = GetMonday(semana.Date);
            var domingo = lunes.AddDays(6);

            var dias = await _context.Database
                .SqlQueryRaw<DescansoDiaDto>(@"
                    SELECT
                        CONVERT(varchar(10), pd.fecha, 23) AS Fecha,
                        pd.es_descanso AS EsDescanso,
                        pd.es_dia_boleta AS EsDiaBoleta
                    FROM dbo.PROGRAMACION_DESCANSOS pd
                    WHERE pd.id_trabajador = {0}
                      AND pd.fecha BETWEEN {1} AND {2}
                    ORDER BY pd.fecha ASC", idTrabajador, lunes, domingo)
                .ToListAsync();

            return Ok(new
            {
                idTrabajador,
                fechaLunes = lunes.ToString("yyyy-MM-dd"),
                dias
            });
        }

        private static DateTime GetMonday(DateTime date)
        {
            var diff = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-diff);
        }

        public sealed class CargarSemanaDescansosRequest
        {
            public DateTime FechaLunes { get; set; }
            public List<TrabajadorDescansoSemanaDto> Trabajadores { get; set; } = new();
        }

        public sealed class TrabajadorDescansoSemanaDto
        {
            public int IdTrabajador { get; set; }
            public int DiaDescanso { get; set; }
            public List<int> DiasBoleta { get; set; } = new();
        }

        private sealed class DescansoDiaDto
        {
            public string Fecha { get; set; } = string.Empty;
            public bool EsDescanso { get; set; }
            public bool EsDiaBoleta { get; set; }
        }
    }
}
