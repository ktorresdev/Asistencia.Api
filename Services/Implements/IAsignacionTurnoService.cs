using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface IAsignacionTurnoService
    {
        Task<PagedResult<AsignacionTurno>> GetAllAsync(PaginationDto pagination);
        Task<AsignacionTurno?> GetByIdAsync(int id);
        Task<AsignacionTurno?> AddAsync(AsignacionTurno request);
        Task UpdateAsync(int id, AsignacionTurno request);
        Task DeleteAsync(int id);
    }
}
