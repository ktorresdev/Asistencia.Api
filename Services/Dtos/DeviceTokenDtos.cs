using System;
using System.ComponentModel.DataAnnotations;

namespace Asistencia.Services.Dtos
{
    public record CreateDeviceTokenRequest(
        [param: Required(ErrorMessage = "Ingresa tu usuario para continuar.")]
        [param: MinLength(3, ErrorMessage = "El usuario debe tener al menos 3 caracteres.")]
        string Username,
        [param: Required(ErrorMessage = "Ingresa tu contraseña para continuar.")]
        [param: MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        string Password,
        string? DeviceId
    );

    public record DeviceTokenDto(int Id, string? DeviceId, DateTime CreatedAt, bool Revoked);
}
