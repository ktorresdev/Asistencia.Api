using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.UserEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly MarcacionAsistenciaDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(MarcacionAsistenciaDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task RegisterAsync(RegisterRequest request)
        {
            if (await _db.Users.AnyAsync(u => u.Username == request.Username))
                throw new InvalidOperationException("Usuario ya existe.");

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Email = request.Email
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
           
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, string? clientId = null)
        {
            if (request is null)
                throw new ArgumentException("Debe enviar los datos de inicio de sesión.");

            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("El usuario es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("La contraseña es obligatoria.");

            var username = request.Username.Trim();

            var user = await _db.Users.Include(u => u.RefreshTokens)
                                      .FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Usuario o contraseña incorrectos.");

            AuthTrabajadorDto? trabajadorInfo = null;
            AuthPersonaDto? trabajadorPersona = null;
            string? areaDepartamento = null;

            var normalizedRole = NormalizeRole(user.Role);

            if (normalizedRole == "TRABAJADOR" || normalizedRole == "ADMIN")
            {
                var trabajadorData = await _db.Trabajadores
                    .Include(t => t.Persona)
                    .Include(t => t.Sucursal)
                    .FirstOrDefaultAsync(t => t.UserId == user.Id);

                if (trabajadorData != null)
                {
                    var sucursal = trabajadorData.Sucursal == null
                        ? null
                        : new AuthSucursalDto(
                            trabajadorData.Sucursal.Id,
                            trabajadorData.Sucursal.NombreSucursal,
                            trabajadorData.Sucursal.Direccion,
                            trabajadorData.Sucursal.LatitudCentro,
                            trabajadorData.Sucursal.LongitudCentro,
                            trabajadorData.Sucursal.PerimetroM,
                            trabajadorData.Sucursal.EsActivo
                        );

                    trabajadorInfo = new AuthTrabajadorDto(
                        trabajadorData.Id,
                        trabajadorData.UserId,
                        trabajadorData.CorreoCorporativo,
                        trabajadorData.Cargo,
                        trabajadorData.AreaDepartamento,
                        trabajadorData.SucursalId,
                        trabajadorData.TomarFoto,
                        sucursal
                    );

                    trabajadorPersona = new AuthPersonaDto(
                        trabajadorData.Persona.Id,
                        trabajadorData.Persona.ApellidosNombres,
                        trabajadorData.Persona.CorreoPersonal,
                        trabajadorData.Persona.Dni
                    );

                    areaDepartamento = trabajadorData.AreaDepartamento;
                }
            }

            var accessToken = GenerateJwtToken(user, clientId, areaDepartamento);
            var refreshToken = GenerateRefreshToken();

            int refreshDays = 30;
            var refreshCfg = _config["Jwt:RefreshTokenDays"];
            if (!string.IsNullOrEmpty(refreshCfg))
                int.TryParse(refreshCfg, out refreshDays);

            var userInfo = new AuthUserDto(user.Id, user.Username, user.Email, normalizedRole);

            var rtEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
            };

            // asociar clientId al refresh token si se desea (opcional)
            // Actualmente no guardamos clientId en RefreshToken

            _db.RefreshTokens.Add(rtEntity);
            await _db.SaveChangesAsync();

            return new AuthResponse(accessToken.token, refreshToken, accessToken.expiresAt, userInfo, normalizedRole, trabajadorInfo, trabajadorPersona);
        }

        public async Task<AuthResponse> RefreshAsync(RefreshRequest request)
        {
            var rt = await _db.RefreshTokens.Include(r => r.User)
                          .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

            if (rt == null || rt.Revoked || rt.ExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token inválido o expirado.");

            rt.Revoked = true;
            _db.RefreshTokens.Update(rt);

            var user = rt.User ?? throw new UnauthorizedAccessException("Usuario no encontrado.");
            string? areaDepartamento = null;

            var normalizedRole = NormalizeRole(user.Role);
            if (normalizedRole == "ADMIN" || normalizedRole == "TRABAJADOR")
            {
                areaDepartamento = await _db.Trabajadores
                    .Where(t => t.UserId == user.Id)
                    .Select(t => t.AreaDepartamento)
                    .FirstOrDefaultAsync();
            }

            var accessToken = GenerateJwtToken(user, null, areaDepartamento);
            var newRefreshToken = GenerateRefreshToken();

            int refreshDays = 30;
            var refreshCfg = _config["Jwt:RefreshTokenDays"];
            if (!string.IsNullOrEmpty(refreshCfg))
                int.TryParse(refreshCfg, out refreshDays);

            var newRt = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
            };

            _db.RefreshTokens.Add(newRt);
            await _db.SaveChangesAsync();

            return new AuthResponse(accessToken.token, newRefreshToken, accessToken.expiresAt, null, normalizedRole, null, null);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var rt = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
            if (rt == null) return;
            rt.Revoked = true;
            _db.RefreshTokens.Update(rt);
            await _db.SaveChangesAsync();
        }

        public async Task<string> CreateDeviceTokenAsync(int userId, string? deviceId = null)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId) ?? throw new KeyNotFoundException("Usuario no encontrado.");

            // Generar token y hash
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var token = Convert.ToBase64String(randomBytes);

            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            var tokenHash = Convert.ToBase64String(hashBytes);

            var dt = new Asistencia.Data.Entities.UserEntites.DeviceToken
            {
                TokenHash = tokenHash,
                UserId = userId,
                DeviceId = deviceId,
                CreatedAt = DateTime.UtcNow,
                Revoked = false
            };

            _db.DeviceTokens.Add(dt);
            await _db.SaveChangesAsync();

            return token; // devolver token sin hash (solo el cliente lo verá)
        }

        public async Task<User?> ValidateDeviceTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            var tokenHash = Convert.ToBase64String(hashBytes);

            var dt = await _db.DeviceTokens.Include(d => d.User).FirstOrDefaultAsync(d => d.TokenHash == tokenHash && !d.Revoked);
            return dt?.User;
        }

        public async Task<IEnumerable<DeviceTokenDto>> GetDeviceTokensAsync(int userId)
        {
            var tokens = await _db.DeviceTokens
                .Where(d => d.UserId == userId)
            .Select(d => new DeviceTokenDto(d.Id, d.DeviceId, d.CreatedAt, d.Revoked))
                .ToListAsync();

            return tokens;
        }

        public async Task RevokeDeviceTokenAsync(int deviceTokenId, int userId)
        {
            var dt = await _db.DeviceTokens.FirstOrDefaultAsync(d => d.Id == deviceTokenId && d.UserId == userId);
            if (dt == null) throw new KeyNotFoundException("Device token not found for user.");
            dt.Revoked = true;
            _db.DeviceTokens.Update(dt);
            await _db.SaveChangesAsync();
        }

        // Helpers
        private (string token, DateTime expiresAt) GenerateJwtToken(User user, string? clientId = null, string? area = null)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
            var key = Encoding.UTF8.GetBytes(jwtKey);
            var normalizedRole = NormalizeRole(user.Role);

            // Obtener minutos de acceso con fallback
            int accessMinutes = 15;
            var accessCfg = _config["Jwt:AccessTokenMinutes"];
            if (!string.IsNullOrEmpty(accessCfg))
                int.TryParse(accessCfg, out accessMinutes);

            var expires = DateTime.UtcNow.AddMinutes(accessMinutes);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, normalizedRole),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(clientId))
            {
                claims.Add(new Claim("client", clientId));
            }

            // El area viaja en el token para forzar filtros sin depender del cliente.
            if (normalizedRole == "ADMIN" && !string.IsNullOrWhiteSpace(area))
            {
                claims.Add(new Claim("area", area));
            }

            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        private static string NormalizeRole(string? role)
        {
            return role?.Trim().ToUpperInvariant() switch
            {
                "SUPERADMIN" => "SUPERADMIN",
                "ADMIN" => "ADMIN",
                "TRABAJADOR" => "TRABAJADOR",
                _ => role?.Trim().ToUpperInvariant() ?? string.Empty
            };
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
