using Asistencia.Data.Entities.MarcacionAsistenciaEntites;

namespace Asistencia.Services.Implements
{
    public interface IHorarioResolverService
    {
        Task<IEnumerable<HorarioDetalle>> GetDetallesForDateAsync(int horarioTurnoId, DateTime date);
        Task<HorarioDetalle?> ResolveDetalleForDateTimeAsync(int horarioTurnoId, DateTime dateTime);
    }
}
