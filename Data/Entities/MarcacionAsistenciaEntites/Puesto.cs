using System;
using System.Collections.Generic;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class Puesto
    {
        public int Id { get; set; }
        public required string NombrePuesto { get; set; }
        public int? IdArea { get; set; }
        public bool EsActivo { get; set; }

        public virtual Area? Area { get; set; }
    }
}
