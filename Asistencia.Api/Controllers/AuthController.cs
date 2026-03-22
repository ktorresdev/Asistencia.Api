using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Asistencia.Data.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;

namespace Asistencia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly MarcacionAsistenciaDbContext _db;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService auth, MarcacionAsistenciaDbContext db, ILogger<AuthController> logger)
        {
            _auth = auth;
            _db = db;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest req)
        {
            await _auth.RegisterAsync(req);
            return Created(string.Empty, null);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest req)
        {
            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new
                    {
                        field = x.Key,
                        message = x.Value!.Errors.First().ErrorMessage
                    })
                    .ToList();

                return BadRequest(new
                {
                    message = "No se pudo iniciar sesión. Revisa los datos ingresados.",
                    code = "LOGIN_VALIDATION_ERROR",
                    errors = errores
                });
            }

            string? clientId = null;
            if (Request.Headers.ContainsKey("X-Client-Id"))
            {
                clientId = Request.Headers["X-Client-Id"].ToString();
            }

            try
            {
                var res = await _auth.LoginAsync(req, clientId);
                await RegistrarAuditoriaLoginAsync(res.user?.Id, req.Username, "OK", null);
                return Ok(res);
            }
            catch (UnauthorizedAccessException)
            {
                await RegistrarAuditoriaLoginAsync(null, req.Username, "FAIL", "INVALID_CREDENTIALS");
                return Unauthorized(new
                {
                    message = "Usuario o contraseña incorrectos. Verifica tus datos e inténtalo nuevamente.",
                    code = "INVALID_CREDENTIALS"
                });
            }
            catch (ArgumentException ex)
            {
                await RegistrarAuditoriaLoginAsync(null, req.Username, "FAIL", "INVALID_LOGIN_REQUEST");
                return BadRequest(new
                {
                    message = string.IsNullOrWhiteSpace(ex.Message)
                        ? "No se pudo iniciar sesión. Verifica los datos ingresados."
                        : ex.Message,
                    code = "INVALID_LOGIN_REQUEST"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en login para usuario {Username}.", req.Username);
                await RegistrarAuditoriaLoginAsync(null, req.Username, "FAIL", "LOGIN_INTERNAL_ERROR");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "No pudimos iniciar sesión por un problema interno. Inténtalo en unos minutos.",
                        code = "LOGIN_INTERNAL_ERROR"
                    });
            }
        }

        [HttpPost("device-token")]
        public async Task<IActionResult> CreateDeviceToken(CreateDeviceTokenRequest req)
        {
            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new
                    {
                        field = x.Key,
                        message = x.Value!.Errors.First().ErrorMessage
                    })
                    .ToList();

                return BadRequest(new
                {
                    message = "No se pudo generar el token del dispositivo. Revisa los datos ingresados.",
                    code = "DEVICE_TOKEN_VALIDATION_ERROR",
                    errors = errores
                });
            }

            try
            {
                // Validate credentials
                await _auth.LoginAsync(new LoginRequest(req.Username, req.Password));

                var user = await _auth.GetUserByUsernameAsync(req.Username) ?? throw new UnauthorizedAccessException();
                var token = await _auth.CreateDeviceTokenAsync(user.Id, req.DeviceId);
                return Ok(new { deviceToken = token });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new
                {
                    message = "Usuario o contraseña incorrectos. Verifica tus datos e inténtalo nuevamente.",
                    code = "INVALID_CREDENTIALS"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = string.IsNullOrWhiteSpace(ex.Message)
                        ? "No se pudo generar el token del dispositivo. Verifica los datos ingresados."
                        : ex.Message,
                    code = "INVALID_DEVICE_TOKEN_REQUEST"
                });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "No pudimos generar el token del dispositivo por un problema interno. Inténtalo en unos minutos.",
                        code = "DEVICE_TOKEN_INTERNAL_ERROR"
                    });
            }
        }

        [HttpGet("device-tokens")]
        [Authorize]
        public async Task<IActionResult> GetDeviceTokens()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null) return Forbid();
            if (!int.TryParse(userIdClaim.Value, out var userId)) return Forbid();

            var tokens = await _auth.GetDeviceTokensAsync(userId);
            return Ok(tokens);
        }

        [HttpDelete("device-tokens/{id:int}")]
        [Authorize]
        public async Task<IActionResult> RevokeDeviceToken(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null) return Forbid();
            if (!int.TryParse(userIdClaim.Value, out var userId)) return Forbid();

            try
            {
                await _auth.RevokeDeviceTokenAsync(id, userId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // Nota: no usar endpoints separados para roles. Usar un único `login` y proteger rutas con roles/políticas.

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequest req)
        {
            var res = await _auth.RefreshAsync(req);
            return Ok(res);
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] RefreshRequest req)
        {
            await _auth.RevokeRefreshTokenAsync(req.RefreshToken);
            return NoContent();
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
        {
            await _auth.RevokeRefreshTokenAsync(req.RefreshToken);
            return NoContent();
        }

        [HttpGet("sucursales")]
        [Authorize]
        public async Task<IActionResult> GetSucursales()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Forbid();
            }

            var trabajadorId = await _db.Trabajadores
                .Where(t => t.UserId == userId)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();

            if (!trabajadorId.HasValue)
            {
                return Ok(Array.Empty<object>());
            }

            List<SucursalAdminDto> sucursales;

            try
            {
                // Si existe el SP en BD, se usa como fuente principal.
                sucursales = await _db.Database
                    .SqlQueryRaw<SucursalAdminDto>("EXEC dbo.SP_GET_SUCURSALES_ADMIN {0}", trabajadorId.Value)
                    .ToListAsync();
            }
            catch
            {
                // Fallback a query directa para no bloquear el frontend.
                sucursales = await _db.Database
                    .SqlQueryRaw<SucursalAdminDto>(@"
                        SELECT
                            ts.id_sucursal AS IdSucursal,
                            s.nombre_sucursal AS NombreSucursal,
                            ts.es_sucursal_principal AS EsSucursalPrincipal,
                            ts.puede_gestionar AS PuedeGestionar
                        FROM dbo.TRABAJADOR_SUCURSALES ts
                        INNER JOIN dbo.SUCURSAL s ON s.id_sucursal = ts.id_sucursal
                        WHERE ts.id_trabajador = {0}
                          AND (ts.fecha_fin IS NULL OR ts.fecha_fin >= CAST(GETDATE() AS DATE))
                        ORDER BY ts.es_sucursal_principal DESC, s.nombre_sucursal ASC", trabajadorId.Value)
                    .ToListAsync();
            }

            return Ok(sucursales);
        }

        private async Task RegistrarAuditoriaLoginAsync(int? userId, string? username, string resultado, string? motivo)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var usernameIntentado = string.IsNullOrWhiteSpace(username) ? "(SIN_USUARIO)" : username.Trim();

            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO dbo.AUDIT_LOGIN (user_id, username_intentado, ip_address, resultado, motivo_fallo)
                    VALUES ({userId}, {usernameIntentado}, {ip}, {resultado}, {motivo})");
            }
            catch (SqlException ex) when (ex.Number == 208 || ex.Number == 4060)
            {
                _logger.LogWarning(ex, "No se pudo registrar AUDIT_LOGIN (tabla o base no disponible).");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo registrar AUDIT_LOGIN por un error no controlado.");
            }
        }

        private sealed class SucursalAdminDto
        {
            public int IdSucursal { get; set; }
            public string NombreSucursal { get; set; } = string.Empty;
            public bool EsSucursalPrincipal { get; set; }
            public bool PuedeGestionar { get; set; }
        }
    }
}
