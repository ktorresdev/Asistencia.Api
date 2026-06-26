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
    public class AreasController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public AreasController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        // GET: api/Rrhh/Areas?soloActivas=true
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool soloActivas = false)
        {
            var query = _context.Areas.AsNoTracking().AsQueryable();

            if (soloActivas)
                query = query.Where(a => a.EsActivo);

            var areas = await query
                .OrderBy(a => a.NombreArea)
                .Select(a => new
                {
                    id = a.Id,
                    nombreArea = a.NombreArea,
                    descripcion = a.Descripcion,
                    esActivo = a.EsActivo
                })
                .ToListAsync();

            return Ok(areas);
        }

        // GET: api/Rrhh/Areas/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var area = await _context.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (area == null)
                return NotFound(new { message = $"No existe area con ID {id}." });

            return Ok(new
            {
                id = area.Id,
                nombreArea = area.NombreArea,
                descripcion = area.Descripcion,
                esActivo = area.EsActivo
            });
        }

        // POST: api/Rrhh/Areas
        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> Create([FromBody] AreaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreArea))
                return BadRequest(new { message = "El nombre del area es obligatorio." });

            var nombre = request.NombreArea.Trim();

            if (await _context.Areas.AnyAsync(a => a.NombreArea == nombre))
                return Conflict(new { message = "Ya existe un area con ese nombre." });

            var area = new Area
            {
                NombreArea = nombre,
                Descripcion = request.Descripcion,
                EsActivo = request.EsActivo
            };

            _context.Areas.Add(area);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = area.Id }, new
            {
                id = area.Id,
                nombreArea = area.NombreArea,
                descripcion = area.Descripcion,
                esActivo = area.EsActivo
            });
        }

        // PUT: api/Rrhh/Areas/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> Update(int id, [FromBody] AreaRequest request)
        {
            var area = await _context.Areas.FindAsync(id);
            if (area == null)
                return NotFound(new { message = $"No existe area con ID {id}." });

            if (string.IsNullOrWhiteSpace(request.NombreArea))
                return BadRequest(new { message = "El nombre del area es obligatorio." });

            var nombre = request.NombreArea.Trim();

            if (await _context.Areas.AnyAsync(a => a.NombreArea == nombre && a.Id != id))
                return Conflict(new { message = "Ya existe otra area con ese nombre." });

            area.NombreArea = nombre;
            area.Descripcion = request.Descripcion;
            area.EsActivo = request.EsActivo;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Rrhh/Areas/5  (baja logica)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> Delete(int id)
        {
            var area = await _context.Areas.FindAsync(id);
            if (area == null)
                return NotFound(new { message = $"No existe area con ID {id}." });

            area.EsActivo = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        public sealed class AreaRequest
        {
            public string NombreArea { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public bool EsActivo { get; set; } = true;
        }
    }
}
