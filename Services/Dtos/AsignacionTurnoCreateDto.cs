using System;

namespace Asistencia.Services.Dtos
{
    public class AsignacionTurnoCreateDto
    {
        public int TrabajadorId { get; set; }
        public int TurnoId { get; set; }
        public DateTime FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
    }
}