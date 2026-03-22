using System.ComponentModel.DataAnnotations;

namespace Asistencia.Services.Dtos
{
    public class ProgramacionTurnoSemanalBulkCreateDto
    {
        [Required]
        public DateOnly FechaInicio { get; set; }

        [Required]
        public DateOnly FechaFin { get; set; }

        [Required]
        public List<ProgramacionTurnoSemanalDiaDto> Programaciones { get; set; } = new();
    }

    public class ProgramacionTurnoSemanalDiaDto
    {
        [Required]
        public int TrabajadorId { get; set; }

        [Required]
        public DateOnly Fecha { get; set; }

        [Required]
        public int IdHorarioTurno { get; set; }

        public bool EsDescanso { get; set; } = false;

        public bool EsDiaBoleta { get; set; } = false;

        public bool EsVacaciones { get; set; } = false;
    }
}
