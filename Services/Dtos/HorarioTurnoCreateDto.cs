using System.ComponentModel.DataAnnotations;

namespace Asistencia.Services.Dtos
{
    public class HorarioTurnoCreateDto
    {
        [Required]
        public int TurnoId { get; set; }

        [Required]
        [StringLength(50)]
        public string NombreHorario { get; set; } = string.Empty;

        public bool EsActivo { get; set; } = true;
    }
}
