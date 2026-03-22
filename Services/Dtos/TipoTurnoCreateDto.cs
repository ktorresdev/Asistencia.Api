using System.ComponentModel.DataAnnotations;

namespace Asistencia.Services.Dtos
{
    public class TipoTurnoCreateDto
    {
        [Required]
        [StringLength(50)]
        public string NombreTipo { get; set; } = string.Empty;
    }
}
