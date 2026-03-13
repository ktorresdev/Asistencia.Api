using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class MarcacionAsistencia
    {
        public long Id { get; set; }
        public int TrabajadorId { get; set; }
        public DateTime FechaHora { get; set; }
        public required string TipoMarcacion { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string? FotoUrl { get; set; }
        public bool? UbicacionValida { get; set; }
        //public string TokenValidacion { get; set; }

        public virtual Trabajador Trabajador { get; set; } = null!;
    }
}
