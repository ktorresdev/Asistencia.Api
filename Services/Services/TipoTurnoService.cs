using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asistencia.Services.Services
{
    public class TipoTurnoService : ITipoTurnoService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public TipoTurnoService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<TipoTurno>> GetAllAsync(PaginationDto pagination)
        {
            var query = _context.TipoTurnos.AsQueryable();

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<TipoTurno>
                {
                Items = items,
                TotalCount = totalCount,
                PageSize = pagination.PageSize,
                CurrentPage = pagination.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            };
        }

        public async Task<TipoTurno?> GetByIdAsync(int id)
        {
            return await _context.TipoTurnos.FindAsync(id);
        }

        public async Task AddAsync(TipoTurno tipoTurno)
        {
            _context.TipoTurnos.Add(tipoTurno);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, TipoTurno tipoTurno)
        {
            var existingTipoTurno = await _context.TipoTurnos.FindAsync(id);
            if (existingTipoTurno == null)
            {
                throw new KeyNotFoundException($"TipoTurno con ID {id} no encontrado.");
            }

            existingTipoTurno.NombreTipo = tipoTurno.NombreTipo;

            _context.TipoTurnos.Update(existingTipoTurno);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var tipoTurno = await _context.TipoTurnos.FindAsync(id);
            if (tipoTurno == null)
            {
                throw new KeyNotFoundException($"TipoTurno con ID {id} no encontrado.");
            }

            _context.TipoTurnos.Remove(tipoTurno);
            await _context.SaveChangesAsync();
        }
    }
}
