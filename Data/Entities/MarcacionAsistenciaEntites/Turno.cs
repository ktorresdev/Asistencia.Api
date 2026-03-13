using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class Turno
    {
        public int Id { get; set; }
        public int TipoTurnoId { get; set; }
        public required string NombreCodigo { get; set; }
        public int? ToleranciaIngreso { get; set; }
        public int? ToleranciaSalida { get; set; }
        //public TimeOnly? DuracionTotal { get; set; }
        public bool EsActivo { get; set; }

        public virtual TipoTurno TipoTurno { get; set; } = null!;
        public virtual ICollection<HorarioTurno>? HorariosTurno { get; set; } = new List<HorarioTurno>();
    }
}