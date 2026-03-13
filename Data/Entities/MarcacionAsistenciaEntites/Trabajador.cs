using Asistencia.Data.Entities.UserEntites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class Trabajador
    {
        public int Id { get; set; }
        public int PersonaId { get; set; }
        public int? JefeInmediatoId { get; set; }
        public int? SucursalId { get; set; }
        public int UserId { get; set; }
        public string? Cargo { get; set; }
        public string? AreaDepartamento { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public DateTime? FechaBaja { get; set; }
        public int IdEstado { get; set; }
        public decimal? SueldoBruto { get; set; }
        public string? CorreoCorporativo { get; set; }
        public string? TelefonoCorporativo { get; set; }
        public bool HorasExtraConf { get; set; }
        public bool MarcajeEnZona { get; set; }
        public bool TomarFoto { get; set; }

        public virtual Persona Persona { get; set; } = null!;
        public virtual Trabajador? JefeInmediato { get; set; }
        public virtual SucursalCentro? Sucursal { get; set; }
        public virtual User User { get; set; } = null!;
        // Propiedad de navegación para el estado
        public virtual MaestroEstado Estado { get; set; } = null!;
    }
}
