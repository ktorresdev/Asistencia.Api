using Asistencia.Data.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "ADMIN,SUPERADMIN")]
    [Route("api/Rrhh/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public UsuariosController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        // Roles permitidos. Solo SUPERADMIN puede otorgar SUPERADMIN.
        private static readonly string[] RolesValidos = { "TRABAJADOR", "SUPERVISOR", "ADMIN", "SUPERADMIN" };

        // PUT: api/Rrhh/Usuarios/5  -> actualizar username y/o rol
        [HttpPut("{userId:int}")]
        public async Task<IActionResult> Actualizar(int userId, [FromBody] ActualizarUsuarioRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { message = $"No existe usuario con ID {userId}." });

            // ----- Username -----
            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                var nuevoUsername = request.Username.Trim();
                if (await _context.Users.AnyAsync(u => u.Username == nuevoUsername && u.Id != userId))
                    return Conflict(new { message = "El nombre de usuario ya está en uso." });

                user.Username = nuevoUsername;
            }

            // ----- Rol -----
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var nuevoRol = request.Role.Trim().ToUpperInvariant();
                if (!RolesValidos.Contains(nuevoRol))
                    return BadRequest(new { message = $"Rol inválido. Valores permitidos: {string.Join(", ", RolesValidos)}." });

                // Solo un SUPERADMIN puede asignar el rol SUPERADMIN.
                if (nuevoRol == "SUPERADMIN" && !User.IsInRole("SUPERADMIN"))
                    return Forbid();

                user.Role = nuevoRol;
            }

            await _context.SaveChangesAsync();
            return Ok(new { id = user.Id, username = user.Username, role = user.Role });
        }

        // PUT: api/Rrhh/Usuarios/5/password  -> resetear contraseña
        [HttpPut("{userId:int}/password")]
        public async Task<IActionResult> CambiarPassword(int userId, [FromBody] CambiarPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { message = $"No existe usuario con ID {userId}." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        public sealed class ActualizarUsuarioRequest
        {
            public string? Username { get; set; }
            public string? Role { get; set; }
        }

        public sealed class CambiarPasswordRequest
        {
            public string NewPassword { get; set; } = string.Empty;
        }
    }
}
