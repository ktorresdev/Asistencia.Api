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
    public interface ISucursalCentroService
    {
        Task<PagedResult<SucursalCentro>> GetAllAsync(PaginationDto pagination);
        Task<SucursalCentro?> GetByIdAsync(int id);
        Task AddAsync(SucursalCentro sucursalCentro);
        Task UpdateAsync(int id, SucursalCentro sucursalCentro);
        Task DeleteAsync(int id);
    }
}
