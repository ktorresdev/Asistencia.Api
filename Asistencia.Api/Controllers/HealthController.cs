using Asistencia.Data.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Asistencia.Api.Controllers
{
    /// <summary>
    /// Estado de salud del API y de la conexión a la base de datos.
    /// Útil para monitoreo y para diagnosticar caídas de BD (el "API Error").
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public HealthController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            bool dbOk;
            string? error = null;
            try
            {
                dbOk = await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                dbOk = false;
                error = ex.Message;
            }

            var payload = new
            {
                status = dbOk ? "healthy" : "unhealthy",
                api = "ok",
                database = dbOk ? "ok" : "unreachable",
                error,
                utc = DateTime.UtcNow
            };

            return dbOk ? Ok(payload) : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
        }
    }
}
