using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asistencia.Data.Entities.MarcacionAsistenciaEntites
{
    public class SucursalCentro
    {
        public int Id { get; set; }
        public required string NombreSucursal { get; set; }
        public string? Direccion { get; set; }
        public decimal? LatitudCentro { get; set; }
        public decimal? LongitudCentro { get; set; }
        public int? PerimetroM { get; set; }
        public bool EsActivo { get; set; }
    }
}