using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class PersonasController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public PersonasController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> Create([FromBody] PersonaUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Dni) || string.IsNullOrWhiteSpace(dto.ApellidosNombres))
                return BadRequest(new { message = "DNI y apellidos/nombres son obligatorios." });

            var existe = await _context.Personas.FirstOrDefaultAsync(p => p.Dni == dto.Dni);
            if (existe != null)
                return Ok(new { id = existe.Id, dni = existe.Dni, apellidosNombres = existe.ApellidosNombres, yaExistia = true });

            var persona = new Persona
            {
                Dni = dto.Dni.Trim(),
                ApellidosNombres = dto.ApellidosNombres.Trim(),
                CorreoPersonal = dto.Email?.Trim(),
                TelefonoPersonal = dto.Telefono?.Trim()
            };

            _context.Personas.Add(persona);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { id = persona.Id, dni = persona.Dni, apellidosNombres = persona.ApellidosNombres });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> Update(int id, [FromBody] PersonaUpsertDto dto)
        {
            var persona = await _context.Personas.FindAsync(id);
            if (persona == null)
                return NotFound(new { message = $"Persona con ID {id} no encontrada." });

            if (!string.IsNullOrWhiteSpace(dto.Dni)) persona.Dni = dto.Dni.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ApellidosNombres)) persona.ApellidosNombres = dto.ApellidosNombres.Trim();
            if (dto.Email != null) persona.CorreoPersonal = dto.Email.Trim();
            if (dto.Telefono != null) persona.TelefonoPersonal = dto.Telefono.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { id = persona.Id, dni = persona.Dni, apellidosNombres = persona.ApellidosNombres });
        }
    }

    public class PersonaUpsertDto
    {
        public string Dni { get; set; } = string.Empty;
        public string ApellidosNombres { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? FechaNacimiento { get; set; }
    }
}
