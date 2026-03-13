using System;
using System.Collections.Generic;

namespace Asistencia.Services.Dtos
{
    public class CargaMasivaTrabajadorFilaDto
    {
        public int NumeroFila { get; set; }
        public required string TipoRegistro { get; set; }
        public required string Dni { get; set; }
        public required string ApellidosNombres { get; set; }
        public required string CorreoPersonal { get; set; }
        public required string TelefonoPersonal { get; set; }
        public int IdSucursal { get; set; }
        public string? DniJefe { get; set; }
        public required string Cargo { get; set; }
        public required string AreaDepartamento { get; set; }
        public DateTime FechaIngreso { get; set; }
        public int IdEstado { get; set; }
        public bool MarcajeEnZona { get; set; }
        public required string Role { get; set; }
    }

    public class CargaMasivaFilaResultadoDto
    {
        public int NumeroFila { get; set; }
        public string? Dni { get; set; }
        public required string Estado { get; set; }
        public required string Mensaje { get; set; }
        public int? IdUserCreado { get; set; }
        public int? IdTrabajadorCreado { get; set; }
    }

    public class CargaMasivaTrabajadoresResultadoDto
    {
        public required string ImportacionId { get; set; }
        public required string NombreArchivo { get; set; }
        public DateTime FechaProcesoUtc { get; set; }
        public int TotalFilas { get; set; }
        public int TotalCargadas { get; set; }
        public int TotalConError { get; set; }
        public required List<CargaMasivaFilaResultadoDto> DetalleFilas { get; set; }
        public string? RutaLogJson { get; set; }
    }
}
