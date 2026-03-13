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
    public interface ITipoTurnoService
    {
        Task<PagedResult<TipoTurno>> GetAllAsync(PaginationDto pagination);
        Task<TipoTurno?> GetByIdAsync(int id);
        Task AddAsync(TipoTurno tipoTurno);
        Task UpdateAsync(int id, TipoTurno tipoTurno);
        Task DeleteAsync(int id);
    }
}
