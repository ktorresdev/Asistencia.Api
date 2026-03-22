using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface ICoberturaTurnoService
    {
        Task<CoberturaTurno> RegistrarAsync(CoberturaTurnoCreateDto dto);
        Task<CoberturaTurno> AprobarAsync(int id, int aprobadoPor);
        Task<CoberturaTurno> RechazarAsync(int id);
        Task<PagedResult<CoberturaTurno>> GetAllAsync(PaginationDto pagination, string? estado = null, DateOnly? fecha = null);
        Task<PagedResult<CoberturaTurno>> GetByTrabajadorAsync(int idTrabajador, PaginationDto pagination);
    }

    public class CoberturaTurnoCreateDto
    {
        public DateOnly Fecha { get; set; }
        public int IdTrabajadorCubre { get; set; }
        public int IdTrabajadorAusente { get; set; }
        public int IdHorarioTurnoOriginal { get; set; }
        public string TipoCobertura { get; set; } = string.Empty;
        public DateOnly? FechaSwapDevolucion { get; set; }
        public int? AprobadoPor { get; set; }
    }
}
