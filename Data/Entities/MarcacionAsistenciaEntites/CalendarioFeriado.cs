using System;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class CalendarioFeriado
    {
        public DateTime Fecha { get; set; }
        public string? Descripcion { get; set; }
        public bool EsFeriado { get; set; }
    }
}