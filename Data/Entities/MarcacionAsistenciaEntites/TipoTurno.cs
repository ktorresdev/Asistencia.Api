using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class TipoTurno
    {
        public int Id { get; set; }
        public required string NombreTipo { get; set; }
    }
}