using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;

namespace Asistencia.Services.Implements
{
    public interface IAsignacionTurnoService
    {
        Task<PagedResult<AsignacionTurnoResponseDto>> GetAllAsync(PaginationDto pagination);
        Task<AsignacionTurnoResponseDto?> GetByIdAsync(int id);
        Task<AsignacionTurno> AddAsync(AsignacionTurnoCreateDto request);
        Task UpdateAsync(int id, AsignacionTurnoUpdateDto request);
        Task DeleteAsync(int id);
    }
}
