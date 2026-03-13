using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface ITurnoService
    {
        Task<PagedResult<Turno>> GetAllAsync(PaginationDto pagination);
        Task<Turno?> GetByIdAsync(int id);
        Task AddAsync(Turno request);
        Task UpdateAsync(int id, Turno request);
        Task DeleteAsync(int id);
    }
}
