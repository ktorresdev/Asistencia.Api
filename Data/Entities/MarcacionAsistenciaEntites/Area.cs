using System;
using System.Collections.Generic;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class Area
    {
        public int Id { get; set; }
        public required string NombreArea { get; set; }
        public string? Descripcion { get; set; }
        public bool EsActivo { get; set; }
    }
}
