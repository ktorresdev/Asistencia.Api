using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class TrabajadorSucursal
    {
        public int Id { get; set; }
        public int TrabajadorId { get; set; }
        public int SucursalId { get; set; }
        public bool EsSucursalPrincipal { get; set; }
        public bool PuedeGestionar { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }

        public virtual Trabajador Trabajador { get; set; } = null!;
        public virtual SucursalCentro Sucursal { get; set; } = null!;
    }
}
