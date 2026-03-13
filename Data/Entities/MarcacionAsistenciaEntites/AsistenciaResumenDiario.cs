using System;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class AsistenciaResumenDiario
    {
        public long Id { get; set; }
        public int TrabajadorId { get; set; }
        public DateTime FechaAsistencia { get; set; }
        public DateTime? HoraEntradaTeorica { get; set; }
        public DateTime? HoraSalidaTeorica { get; set; }
        public DateTime? HoraEntradaReal { get; set; }
        public DateTime? HoraSalidaReal { get; set; }
        public int MinutosTardanza { get; set; }
        public int MinutosExtra { get; set; }
        public required string EstadoAsistencia { get; set; }
        public virtual Trabajador Trabajador { get; set; } = null!;
    }
}