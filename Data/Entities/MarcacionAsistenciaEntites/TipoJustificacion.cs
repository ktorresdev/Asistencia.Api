using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class TipoJustificacion
    {
        public int Id { get; set; }
        public required string NombreTipo { get; set; }
        public bool RequiereAdjunto { get; set; }
        public bool EsActivo { get; set; }
    }
}