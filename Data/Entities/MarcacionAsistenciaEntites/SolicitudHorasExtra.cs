using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class SolicitudHorasExtra
    {
        public int Id { get; set; }
        public int TrabajadorId { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public decimal HorasSolicitadas { get; set; }
        public string? Motivo { get; set; }
        public int IdEstado { get; set; }
        public int? IdJefeAprueba { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        public virtual Trabajador Trabajador { get; set; } = null!;
        public virtual MaestroEstado Estado { get; set; } = null!;
    }
}