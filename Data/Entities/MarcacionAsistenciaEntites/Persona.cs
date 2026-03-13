using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class Persona
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }
        public required string Dni { get; set; }
        public required string ApellidosNombres { get; set; }
        public string? CorreoPersonal { get; set; }
        public string? TelefonoPersonal { get; set; }
    }
}