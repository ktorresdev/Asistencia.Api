using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class HorarioTurno
    {
        public int Id { get; set; }
        public int TurnoId { get; set; }
        public required string NombreHorario { get; set; }
        public bool EsActivo { get; set; }
        public virtual Turno Turno { get; set; } = null!;
        public virtual ICollection<HorarioDetalle> HorariosDetalle { get; set; } = new List<HorarioDetalle>();
    }
}