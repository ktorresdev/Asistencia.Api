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
        public DateTime FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
        public bool EsVigente { get; set; }

        public virtual Trabajador? Trabajador { get; set; }
        public virtual Turno? Turno { get; set; }
    }
}