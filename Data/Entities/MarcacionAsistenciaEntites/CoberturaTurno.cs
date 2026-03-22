using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class CoberturaTurno
    {
        public int Id { get; set; }
        public DateOnly Fecha { get; set; }
        public int IdTrabajadorCubre { get; set; }
        public int IdTrabajadorAusente { get; set; }
        public int IdHorarioTurnoOriginal { get; set; }
        public string TipoCobertura { get; set; } = string.Empty; // COBERTURA|CAMBIO|ANTICIPO
        public DateOnly? FechaSwapDevolucion { get; set; }
        public int? AprobadoPor { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public int? IdCoberturaReciproca { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Trabajador TrabajadorCubre { get; set; } = null!;
        public virtual Trabajador TrabajadorAusente { get; set; } = null!;
        public virtual HorarioTurno HorarioTurnoOriginal { get; set; } = null!;
    }
}
