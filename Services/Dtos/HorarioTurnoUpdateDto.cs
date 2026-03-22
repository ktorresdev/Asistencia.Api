using System.ComponentModel.DataAnnotations;

namespace Asistencia.Services.Dtos
{
    public class HorarioTurnoUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int TurnoId { get; set; }

        [Required]
        [StringLength(50)]
        public string NombreHorario { get; set; } = string.Empty;

        public bool EsActivo { get; set; }
    }
}
