using Asistencia.Services.Implements;
using Asistencia.Data.DbContexts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Asistencia.Api.AuthHandlers
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IAuthService _authService;
        private readonly MarcacionAsistenciaDbContext _db;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IAuthService authService,
            MarcacionAsistenciaDbContext db)
            : base(options, logger, encoder, clock)
        {
            _authService = authService;
            _db = db;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.NoResult();

            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader))
                return AuthenticateResult.NoResult();

            if (!authHeader.StartsWith("Bearer "))
                return AuthenticateResult.NoResult();

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrEmpty(token))
                return AuthenticateResult.NoResult();

            // If token looks like JWT (has two dots) don't handle here
            if (token.Count(c => c == '.') == 2)
                return AuthenticateResult.NoResult();

            try
            {
                var user = await _authService.ValidateDeviceTokenAsync(token);
                if (user == null)
                    return AuthenticateResult.Fail("Invalid API key.");

                var normalizedRole = NormalizeRole(user.Role);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, normalizedRole)
                };

                if (normalizedRole == "ADMIN")
                {
                    var area = _db.Trabajadores
                        .Where(t => t.UserId == user.Id)
                        .Select(t => t.AreaDepartamento)
                        .FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(area))
                    {
                        claims.Add(new Claim("area", area));
                    }
                }

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error validating device token");
                return AuthenticateResult.Fail("Error validating API key.");
            }
        }

        private static string NormalizeRole(string? role)
        {
            return role?.Trim().ToUpperInvariant() switch
            {
                "SUPERADMIN" => "SUPERADMIN",
                "SUPERVISOR" => "SUPERVISOR",
                "ADMIN" => "ADMIN",
                "TRABAJADOR" => "TRABAJADOR",
                "EMPLOYEE" => "TRABAJADOR",
                _ => role?.Trim().ToUpperInvariant() ?? string.Empty
            };
        }
    }
}
