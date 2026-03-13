using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class HorarioDetalle
    {
        public int Id { get; set; }
        public int HorarioTurnoId { get; set; }
        public required string DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public TimeSpan? HoraInicioRefrigerio { get; set; }
        public TimeSpan? HoraFinRefrigerio { get; set; }
        public int TiempoRefrigerioMinutos { get; set; }
        public bool SalidaDiaSiguiente { get; set; }

        public virtual HorarioTurno HorarioTurno { get; set; } = null!;
    }
}