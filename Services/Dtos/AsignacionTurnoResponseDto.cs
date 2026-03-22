namespace Asistencia.Services.Dtos
{
    /// <summary>
    /// DTO de respuesta para AsignacionTurno sin referencias circulares
    /// </summary>
    public class AsignacionTurnoResponseDto
    {
        public int Id { get; set; }

        // Trabajador
        public int TrabajadorId { get; set; }
        public string? TrabajadorNombre { get; set; }
        public string? TrabajadorDni { get; set; }

        // Turno (sin incluir HorariosTurno para evitar ciclos)
        public int TurnoId { get; set; }
        public string? TurnoNombre { get; set; }
        public string? TipoTurno { get; set; }

        // Horario Turno (sin incluir Turno para evitar ciclos)
        public int? HorarioTurnoId { get; set; }
        public string? HorarioTurnoNombre { get; set; }

        // Vigencia
        public bool EsVigente { get; set; }
        public DateOnly FechaInicioVigencia { get; set; }
        public DateOnly? FechaFinVigencia { get; set; }

        // Metadatos
        public string? MotivoCambio { get; set; }
        public int? AprobadoPor { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
