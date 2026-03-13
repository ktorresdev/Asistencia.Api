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
    public class AsignacionTurnoService : IAsignacionTurnoService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public AsignacionTurnoService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }
        public async Task<AsignacionTurno?> AddAsync(AsignacionTurno request)
        {
            var asignacion = new AsignacionTurno
            {
                TrabajadorId = request.TrabajadorId,
                TurnoId = request.TurnoId,
                FechaInicioVigencia = request.FechaInicioVigencia,
                FechaFinVigencia = request.FechaFinVigencia,
                EsVigente = true // Por defecto, una nueva asignación es vigente
            };

            await _context.AsignacionesTurno.AddAsync(asignacion);
            await _context.SaveChangesAsync();

            return asignacion;
        }

        public async Task DeleteAsync(int id)
        {
            var asignacion = await _context.AsignacionesTurno.FindAsync(id);
            if (asignacion == null)
            {
                throw new KeyNotFoundException($"Asignación de turno con ID {id} no encontrada.");
            }

            _context.AsignacionesTurno.Remove(asignacion);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<AsignacionTurno>> GetAllAsync(PaginationDto pagination)
        {
            var query = _context.AsignacionesTurno
                                .Include(a => a.Trabajador)
                                .ThenInclude(t => t!.Persona)
                                .Include(a => a.Turno)
                                .AsQueryable();

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<AsignacionTurno>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = pagination.PageSize,
                CurrentPage = pagination.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            };
        }

        public async Task<AsignacionTurno?> GetByIdAsync(int id)
        {
            return await _context.AsignacionesTurno
                                 .Include(a => a.Trabajador)
                                 .ThenInclude(t => t!.Persona)
                                 .Include(a => a.Turno)
                                 .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task UpdateAsync(int id, AsignacionTurnoUpdateDto request)
        {
            var existingAsignacion = await _context.AsignacionesTurno.FindAsync(id);
            if (existingAsignacion == null)
            {
                throw new KeyNotFoundException($"Asignación de turno con ID {id} no encontrada.");
            }
        
            existingAsignacion.TrabajadorId = request.TrabajadorId;
            existingAsignacion.TurnoId = request.TurnoId;
            existingAsignacion.FechaInicioVigencia = request.FechaInicioVigencia;
            existingAsignacion.FechaFinVigencia = request.FechaFinVigencia;
            existingAsignacion.EsVigente = request.EsVigente;
        
            _context.AsignacionesTurno.Update(existingAsignacion);
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(int id, AsignacionTurno request)
        {
            throw new NotImplementedException();
        }
    }
}
