using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        public AuthController(IAuthService auth) => _auth = auth;

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
                return Ok(res);
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
                        ? "No se pudo iniciar sesión. Verifica los datos ingresados."
                        : ex.Message,
                    code = "INVALID_LOGIN_REQUEST"
                });
            }
            catch (Exception)
            {
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
    }
}
