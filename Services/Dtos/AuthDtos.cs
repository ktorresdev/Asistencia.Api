using System;
using System.ComponentModel.DataAnnotations;

namespace Asistencia.Services.Dtos
{
    public record LoginRequest(
        [param: Required(ErrorMessage = "Ingresa tu usuario para continuar.")]
        [param: MinLength(3, ErrorMessage = "El usuario debe tener al menos 3 caracteres.")]
        string Username,
        [param: Required(ErrorMessage = "Ingresa tu contraseña para continuar.")]
        [param: MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        string Password
    );
    public record RegisterRequest(string Username, string Password, string? Email);

    public record AuthUserDto(int Id, string Username, string? Email, string Role);

    public record AuthSucursalDto(
        int Id,
        string NombreSucursal,
        string? Direccion,
        decimal? LatitudCentro,
        decimal? LongitudCentro,
        int? PerimetroM,
        bool EsActivo
    );

    public record AuthTrabajadorDto(
        int Id,
        int UserId,
        string? CorreoCorporativo,
        string? Cargo,
        string? AreaDepartamento,
        int? SucursalId,
        bool? TomarFoto,
        AuthSucursalDto? Sucursal,
        IReadOnlyList<AuthSucursalDto>? SucursalesAsignadas
    );

    public record AuthPersonaDto(
        int Id,
        string ApellidosNombres,
        string? CorreoPersonal,
        string Dni
    );

    public record AuthResponse
        (string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, AuthUserDto? user, string Role, AuthTrabajadorDto? trabajador, AuthPersonaDto? persona);
    public record RefreshRequest(string RefreshToken);
}
