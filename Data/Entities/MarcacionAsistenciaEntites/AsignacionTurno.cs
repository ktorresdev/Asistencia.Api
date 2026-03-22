using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class AsignacionTurno
    {
        public int Id { get; set; }
        public int TrabajadorId { get; set; }
        public int TurnoId { get; set; }
        public int? HorarioTurnoId { get; set; }
        public DateOnly FechaInicioVigencia { get; set; }
        public DateOnly? FechaFinVigencia { get; set; }
        public bool EsVigente { get; set; }
        public string? MotivoCambio { get; set; }
        public int? AprobadoPor { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Trabajador? Trabajador { get; set; }
        public virtual Turno? Turno { get; set; }
        public virtual HorarioTurno? HorarioTurno { get; set; }
    }
}