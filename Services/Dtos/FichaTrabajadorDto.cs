using System;

namespace Asistencia.Services.Dtos
{
    public class CreateTrabajadorDto
    {
        // Persona
        public required string Dni { get; set; }
        public required string ApellidosNombres { get; set; }
        public required string CorreoPersonal { get; set; }
        public required string TelefonoPersonal { get; set; }

        // Trabajador
        public int? IdUser { get; set; }
        public int IdSucursal { get; set; }
        public int? IdJefeInmediato { get; set; }
        public required string Cargo { get; set; }
        public required string AreaDepartamento { get; set; }
        public DateTime FechaIngreso { get; set; }
        public int IdEstado { get; set; }
        public bool MarcajeEnZona { get; set; }
    }
}
