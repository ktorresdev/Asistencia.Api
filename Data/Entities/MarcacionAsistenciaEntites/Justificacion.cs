using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class Justificacion
    {
        public int Id { get; set; }
        public int TrabajadorId { get; set; }
        public int TipoJustificacionId { get; set; }
        public DateTime FechaJustificada { get; set; }
        public string? Motivo { get; set; }
        public string? DocumentoAdjuntoUrl { get; set; }
        public int IdEstado { get; set; }
        public DateTime? FechaAutorizacion { get; set; }
        public string? UsuarioAutoriza { get; set; }

        public virtual Trabajador Trabajador { get; set; } = null!;
        public virtual TipoJustificacion TipoJustificacion { get; set; } = null!;
        public virtual MaestroEstado Estado { get; set; } = null!;
    }
}