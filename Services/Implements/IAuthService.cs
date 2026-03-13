using Asistencia.Data.Entities.UserEntites;
using Asistencia.Services.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request, string? clientId = null);
        Task<AuthResponse> RefreshAsync(RefreshRequest request);
        Task RegisterAsync(RegisterRequest request);
        Task RevokeRefreshTokenAsync(string refreshToken);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<string> CreateDeviceTokenAsync(int userId, string? deviceId = null);
        Task<User?> ValidateDeviceTokenAsync(string token);
        Task<IEnumerable<Asistencia.Services.Dtos.DeviceTokenDto>> GetDeviceTokensAsync(int userId);
        Task RevokeDeviceTokenAsync(int deviceTokenId, int userId);
    }
}
