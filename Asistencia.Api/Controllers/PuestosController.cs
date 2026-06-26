using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class PuestosController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public PuestosController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        // GET: api/Rrhh/Puestos?idArea=3&soloActivos=true
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? idArea = null, [FromQuery] bool soloActivos = false)
        {
            var query = _context.Puestos.AsNoTracking().Include(p => p.Area).AsQueryable();

            if (idArea.HasValue)
                query = query.Where(p => p.IdArea == idArea.Value);

            if (soloActivos)
                query = query.Where(p => p.EsActivo);

            var puestos = await query
                .OrderBy(p => p.NombrePuesto)
                .Select(p => new
                {
                    id = p.Id,
                    nombrePuesto = p.NombrePuesto,
                    idArea = p.IdArea,
                    nombreArea = p.Area != null ? p.Area.NombreArea : null,
                    esActivo = p.EsActivo
                })
                .ToListAsync();

            return Ok(puestos);
        }

        // GET: api/Rrhh/Puestos/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var puesto = await _context.Puestos.AsNoTracking()
                .Include(p => p.Area)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (puesto == null)
                return NotFound(new { message = $"No existe puesto con ID {id}." });

            return Ok(new
            {
                id = puesto.Id,
                nombrePuesto = puesto.NombrePuesto,
                idArea = puesto.IdArea,
                nombreArea = puesto.Area?.NombreArea,
                esActivo = puesto.EsActivo
            });
        }

        // POST: api/Rrhh/Puestos
        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> Create([FromBody] PuestoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombrePuesto))
                return BadRequest(new { message = "El nombre del puesto es obligatorio." });

            var nombre = request.NombrePuesto.Trim();

            if (await _context.Puestos.AnyAsync(p => p.NombrePuesto == nombre))
                return Conflict(new { message = "Ya existe un puesto con ese nombre." });

            if (request.IdArea.HasValue && !await _context.Areas.AnyAsync(a => a.Id == request.IdArea.Value))
                return NotFound(new { message = $"No existe area con ID {request.IdArea.Value}." });

            var puesto = new Puesto
            {
                NombrePuesto = nombre,
                IdArea = request.IdArea,
                EsActivo = request.EsActivo
            };

            _context.Puestos.Add(puesto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = puesto.Id }, new
            {
                id = puesto.Id,
                nombrePuesto = puesto.NombrePuesto,
                idArea = puesto.IdArea,
                esActivo = puesto.EsActivo
            });
        }

        // PUT: api/Rrhh/Puestos/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> Update(int id, [FromBody] PuestoRequest request)
        {
            var puesto = await _context.Puestos.FindAsync(id);
            if (puesto == null)
                return NotFound(new { message = $"No existe puesto con ID {id}." });

            if (string.IsNullOrWhiteSpace(request.NombrePuesto))
                return BadRequest(new { message = "El nombre del puesto es obligatorio." });

            var nombre = request.NombrePuesto.Trim();

            if (await _context.Puestos.AnyAsync(p => p.NombrePuesto == nombre && p.Id != id))
                return Conflict(new { message = "Ya existe otro puesto con ese nombre." });

            if (request.IdArea.HasValue && !await _context.Areas.AnyAsync(a => a.Id == request.IdArea.Value))
                return NotFound(new { message = $"No existe area con ID {request.IdArea.Value}." });

            puesto.NombrePuesto = nombre;
            puesto.IdArea = request.IdArea;
            puesto.EsActivo = request.EsActivo;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Rrhh/Puestos/5  (baja logica)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> Delete(int id)
        {
            var puesto = await _context.Puestos.FindAsync(id);
            if (puesto == null)
                return NotFound(new { message = $"No existe puesto con ID {id}." });

            puesto.EsActivo = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        public sealed class PuestoRequest
        {
            public string NombrePuesto { get; set; } = string.Empty;
            public int? IdArea { get; set; }
            public bool EsActivo { get; set; } = true;
        }
    }
}
