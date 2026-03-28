using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

using System.Threading.Tasks;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;

namespace Asistencia.Services.Services
{
    public class HorarioTurnoService : IHorarioTurnoService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public HorarioTurnoService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        public async Task<HorarioTurno> AddAsync(HorarioTurnoRequest horarioTurno)
        {
            var turnoExiste = await _context.Turnos.AnyAsync(t => t.Id == horarioTurno.TurnoId);
            if (!turnoExiste)
                throw new KeyNotFoundException($"Turno con ID {horarioTurno.TurnoId} no encontrado.");

            var request = new HorarioTurno
            {
                NombreHorario = horarioTurno.NombreHorario,
                TurnoId = horarioTurno.TurnoId,
                EsActivo = horarioTurno.EsActivo,
                Turno = null! // Navigation property
            };

            await _context.HorariosTurno.AddAsync(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<HorarioTurno>> GetAllAsync()
        {
            return await _context.HorariosTurno.ToListAsync();
        }

        public async Task<HorarioTurno?> GetByIdAsync(int id)
        {
            return await _context.HorariosTurno.FindAsync(id);
        }

        public async Task UpdateAsync(int id, HorarioTurno request)
        {
            var existingHorarioTurno = await _context.HorariosTurno.FindAsync(id);
            if (existingHorarioTurno == null)
            {
                throw new KeyNotFoundException($"Horario turno no encontrado");
            }

            existingHorarioTurno.TurnoId = request.TurnoId;
            existingHorarioTurno.NombreHorario = request.NombreHorario;
            existingHorarioTurno.EsActivo = request.EsActivo;

            _context.HorariosTurno.Update(existingHorarioTurno);
            await _context.SaveChangesAsync();
        }
    }
}
