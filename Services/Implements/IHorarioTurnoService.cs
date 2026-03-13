using Asistencia.Data.Entities;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface IHorarioTurnoService
    {
        Task<IEnumerable<HorarioTurno>> GetAllAsync();
        Task<HorarioTurno?> GetByIdAsync(int id);
        Task<HorarioTurno> AddAsync(HorarioTurnoRequest horarioTurno);
        Task UpdateAsync(int id, HorarioTurno horarioTurno);
        Task DeleteAsync(int id);
    }
    public class HorarioTurnoRequest
    {
        public int TurnoId { get; set; }
        public required string NombreHorario { get; set; }
        public bool EsActivo { get; set; }
    }

}
