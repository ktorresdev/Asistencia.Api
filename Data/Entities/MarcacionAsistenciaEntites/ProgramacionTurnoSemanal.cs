using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class ProgramacionTurnoSemanal
    {
        public int Id { get; set; }
        public int TrabajadorId { get; set; }
        public DateOnly Fecha { get; set; }
        public int? IdHorarioTurno { get; set; }
        public bool EsDescanso { get; set; }
        public bool EsDiaBoleta { get; set; }
        public bool EsVacaciones { get; set; }
        [Column("tipo_ausencia")]
        public string? TipoAusencia { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Trabajador Trabajador { get; set; } = null!;
        public virtual HorarioTurno? HorarioTurno { get; set; }
    }
}
