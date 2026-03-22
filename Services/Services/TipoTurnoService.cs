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
                .OrderBy(tt => tt.NombreTipo)
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

        public async Task<int> AddAsync(TipoTurnoCreateDto request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreTipo))
                throw new ArgumentException("El nombre del tipo de turno es requerido.");

            // Validar que no exista otro tipo turno con el mismo nombre
            var nombreExistente = await _context.TipoTurnos
                .AnyAsync(tt => tt.NombreTipo.ToLower() == request.NombreTipo.ToLower());

            if (nombreExistente)
                throw new ArgumentException($"Ya existe un tipo de turno con el nombre '{request.NombreTipo}'.");

            try
            {
                var tipoTurno = new TipoTurno
                {
                    NombreTipo = request.NombreTipo
                };

                _context.TipoTurnos.Add(tipoTurno);
                await _context.SaveChangesAsync();
                return tipoTurno.Id;
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error al crear el tipo de turno. Verifica que los datos sean válidos.", ex);
            }
        }

        public async Task UpdateAsync(int id, TipoTurnoUpdateDto request)
        {
            var existingTipoTurno = await _context.TipoTurnos.FindAsync(id);
            if (existingTipoTurno == null)
            {
                throw new KeyNotFoundException($"TipoTurno con ID {id} no encontrado.");
            }

            if (string.IsNullOrWhiteSpace(request.NombreTipo))
                throw new ArgumentException("El nombre del tipo de turno es requerido.");

            // Validar que no exista otro tipo turno con el mismo nombre (excluyendo el actual)
            var nombreExistente = await _context.TipoTurnos
                .Where(tt => tt.Id != id && tt.NombreTipo.ToLower() == request.NombreTipo.ToLower())
                .AnyAsync();

            if (nombreExistente)
                throw new ArgumentException($"Ya existe otro tipo de turno con el nombre '{request.NombreTipo}'.");

            existingTipoTurno.NombreTipo = request.NombreTipo;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error al actualizar el tipo de turno. Verifica que los datos sean válidos.", ex);
            }
        }

        public async Task DeleteAsync(int id)
        {
            var tipoTurno = await _context.TipoTurnos.FindAsync(id);
            if (tipoTurno == null)
            {
                throw new KeyNotFoundException($"TipoTurno con ID {id} no encontrado.");
            }

            // Validar que no haya turnos asociados
            var tieneTurnos = await _context.Turnos
                .AnyAsync(t => t.TipoTurnoId == id);

            if (tieneTurnos)
            {
                throw new InvalidOperationException($"No se puede eliminar el tipo de turno porque tiene turnos asociados. Primero elimina los turnos.");
            }

            try
            {
                _context.TipoTurnos.Remove(tipoTurno);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error al eliminar el tipo de turno.", ex);
            }
        }
    }
}
