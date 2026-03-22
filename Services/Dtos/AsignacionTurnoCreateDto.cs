using System;

namespace Asistencia.Services.Dtos
{
    public class AsignacionTurnoCreateDto
    {
        public int TrabajadorId { get; set; }
        public int TurnoId { get; set; }
        public int? HorarioTurnoId { get; set; }
        public DateOnly FechaInicioVigencia { get; set; }
        public DateOnly? FechaFinVigencia { get; set; }
        public string? MotivoCambio { get; set; }
        public int? AprobadoPor { get; set; }
    }
}