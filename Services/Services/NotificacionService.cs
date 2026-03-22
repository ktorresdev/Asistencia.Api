using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.UserEntites;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asistencia.Services.Services
{
    public class NotificacionService : INotificacionService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public NotificacionService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Notificacion>> GetNoLeidasAsync(int userId)
        {
            return await _context.Notificaciones
                .Where(n => n.UserId == userId && !n.Leida)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notificacion> MarcarLeidaAsync(int idNotificacion, int userId)
        {
            var notificacion = await _context.Notificaciones.FindAsync(idNotificacion);
            if (notificacion == null)
                throw new KeyNotFoundException($"Notificación con ID {idNotificacion} no encontrada.");

            if (notificacion.UserId != userId)
                throw new UnauthorizedAccessException("No tienes permisos para marcar esta notificación como leída.");

            notificacion.Leida = true;
            notificacion.LeidaAt = DateTime.UtcNow;

            _context.Notificaciones.Update(notificacion);
            await _context.SaveChangesAsync();

            return notificacion;
        }

        public async Task MarcarTodasLeidasAsync(int userId)
        {
            await _context.Notificaciones
                .Where(n => n.UserId == userId && !n.Leida)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.Leida, true)
                    .SetProperty(n => n.LeidaAt, DateTime.UtcNow));
        }

        public async Task<Notificacion> CrearAsync(int userId, string tipo, string titulo, string mensaje)
        {
            var notificacion = new Notificacion
            {
                UserId = userId,
                Tipo = tipo,
                Titulo = titulo,
                Mensaje = mensaje,
                Leida = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            return notificacion;
        }
    }
}
