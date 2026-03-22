using System.ComponentModel.DataAnnotations;

namespace Asistencia.Services.Dtos
{
    public class SucursalCentroCreateDto
    {
        [Required]
        [StringLength(50)]
        public string NombreSucursal { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Direccion { get; set; }

        public decimal? LatitudCentro { get; set; }

        public decimal? LongitudCentro { get; set; }

        public int? PerimetroM { get; set; }

        public bool EsActivo { get; set; } = true;
    }
}
