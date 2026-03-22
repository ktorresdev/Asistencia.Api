using System.ComponentModel.DataAnnotations;

namespace Asistencia.Services.Dtos
{
    public class TipoTurnoUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string NombreTipo { get; set; } = string.Empty;
    }
}
