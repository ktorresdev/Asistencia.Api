using Asistencia.Data.Entities.UserEntites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asistencia.Services.Implements
{
    public interface INotificacionService
    {
        Task<IEnumerable<Notificacion>> GetNoLeidasAsync(int userId);
        Task<Notificacion> MarcarLeidaAsync(int idNotificacion, int userId);
        Task MarcarTodasLeidasAsync(int userId);
        Task<Notificacion> CrearAsync(int userId, string tipo, string titulo, string mensaje);
    }
}
