using Asistencia.Data.Entities;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface ITrabajadorService
    {
        Task<PagedResult<Trabajador>> GetAllAsync(PaginationDto pagination, string? search = null);
        Task<Trabajador?> GetByIdAsync(int id);
        Task AddAsync(TrabajadorDto trabajadorDto);
        Task UpdateAsync(int id, TrabajadorDto trabajadorDto);
        Task DeleteAsync(int id);
    }

    public class TrabajadorDto
    {
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
        public bool BonoNocturnoRs { get; set; }
        public bool MarcajeEnZona { get; set; }
    }
}
