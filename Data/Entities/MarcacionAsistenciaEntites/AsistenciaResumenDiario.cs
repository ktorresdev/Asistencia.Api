using System;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class AsistenciaResumenDiario
    {
        public long Id { get; set; }
        public int TrabajadorId { get; set; }
        // Asignación de turno a la que corresponde esta fila. Permite >1 fila por
        // trabajador/día (doble turno). NULL en filas de ausencia sin asignación.
        public int? AsignacionTurnoId { get; set; }
        public DateOnly FechaAsistencia { get; set; }
        public DateTime? HoraEntradaTeorica { get; set; }
        public DateTime? HoraSalidaTeorica { get; set; }
        public DateTime? HoraEntradaReal { get; set; }
        public DateTime? HoraSalidaReal { get; set; }
        public int MinutosTardanza { get; set; }
        public int MinutosExtra { get; set; }
        public required string EstadoAsistencia { get; set; }
        public bool EsDiaBoleta { get; set; }
        public int? IdCoberturaOrigen { get; set; }
        public virtual Trabajador Trabajador { get; set; } = null!;
    }
}