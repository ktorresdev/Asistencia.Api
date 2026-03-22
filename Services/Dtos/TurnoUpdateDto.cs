using System.ComponentModel.DataAnnotations;

namespace Asistencia.Services.Dtos
{
    public class TurnoUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int TipoTurnoId { get; set; }

        [Required]
        [StringLength(20)]
        public string NombreCodigo { get; set; } = string.Empty;

        public int? ToleranciaIngreso { get; set; }

        public int? ToleranciaSalida { get; set; }

        public bool EsActivo { get; set; }
    }
}
