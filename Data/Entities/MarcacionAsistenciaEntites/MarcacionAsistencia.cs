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
        /// <summary>true si el cliente reportó que la ubicación proviene de un proveedor simulado (GPS falso).</summary>
        public bool? EsMockLocation { get; set; }
        /// <summary>Sede de pertenencia del trabajador (para reportes).</summary>
        public int? SucursalId { get; set; }
        /// <summary>Sede donde físicamente se realizó la marcación (auditoría).</summary>
        public int? SucursalMarcacionId { get; set; }
        public string? TokenValidacion { get; set; }

        public virtual Trabajador Trabajador { get; set; } = null!;
        public virtual SucursalCentro? Sucursal { get; set; }
        public virtual SucursalCentro? SucursalMarcacion { get; set; }
    }
}
