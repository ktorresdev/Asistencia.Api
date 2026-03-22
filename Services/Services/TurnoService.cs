﻿using Asistencia.Data.DbContexts;
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
    public class TurnoService : ITurnoService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public TurnoService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddAsync(TurnoCreateDto request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreCodigo))
                throw new ArgumentException("El nombre/código del turno es requerido.");

            // Validar que no exista otro turno con el mismo código
            var codigoExistente = await _context.Turnos
                .AnyAsync(t => t.NombreCodigo.ToLower() == request.NombreCodigo.ToLower());

            if (codigoExistente)
                throw new ArgumentException($"Ya existe un turno con el código '{request.NombreCodigo}'.");

            // Validar que el TipoTurno exista
            var tipoTurnoExiste = await _context.TipoTurnos
                .AnyAsync(tt => tt.Id == request.TipoTurnoId);

            if (!tipoTurnoExiste)
                throw new KeyNotFoundException($"TipoTurno con ID {request.TipoTurnoId} no encontrado.");

            try
            {
                var turno = new Turno
                {
                    TipoTurnoId = request.TipoTurnoId,
                    NombreCodigo = request.NombreCodigo,
                    ToleranciaIngreso = request.ToleranciaIngreso,
                    ToleranciaSalida = request.ToleranciaSalida,
                    EsActivo = request.EsActivo
                };

                _context.Turnos.Add(turno);
                await _context.SaveChangesAsync();
                return turno.Id;
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error al crear el turno. Verifica que los datos sean válidos.", ex);
            }
        }

        public async Task DeleteAsync(int id)
        {
            var turno = await _context.Turnos.FindAsync(id);
            if (turno == null)
            {
                throw new KeyNotFoundException($"Turno con ID {id} no encontrado.");
            }

            // Validar que no haya asignaciones de turno asociadas
            var tieneAsignaciones = await _context.AsignacionesTurno
                .AnyAsync(a => a.TurnoId == id);

            if (tieneAsignaciones)
            {
                throw new InvalidOperationException($"No se puede eliminar el turno porque tiene asignaciones asociadas. Primero elimina las asignaciones.");
            }

            // Validar que no haya horarios asociados
            var tieneHorarios = await _context.HorariosTurno
                .AnyAsync(h => h.TurnoId == id);

            if (tieneHorarios)
            {
                throw new InvalidOperationException($"No se puede eliminar el turno porque tiene horarios asociados. Primero elimina los horarios.");
            }

            try
            {
                _context.Turnos.Remove(turno);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error al eliminar el turno.", ex);
            }
        }

        public async Task<PagedResult<Turno>> GetAllAsync(PaginationDto pagination)
        {
            var query = _context.Turnos
                .Include(t => t.TipoTurno)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(t => t.NombreCodigo)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<Turno>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = pagination.PageSize,
                CurrentPage = pagination.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            };
        }

        public async Task<Turno?> GetByIdAsync(int id)
        {
            return await _context.Turnos
                .Include(t => t.TipoTurno)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task UpdateAsync(int id, TurnoUpdateDto request)
        {
            var existingTurno = await _context.Turnos.FindAsync(id);
            if (existingTurno == null)
            {
                throw new KeyNotFoundException($"Turno con ID {id} no encontrado.");
            }

            if (string.IsNullOrWhiteSpace(request.NombreCodigo))
                throw new ArgumentException("El nombre/código del turno es requerido.");

            // Validar que no exista otro turno con el mismo código (excluyendo el actual)
            var codigoExistente = await _context.Turnos
                .Where(t => t.Id != id && t.NombreCodigo.ToLower() == request.NombreCodigo.ToLower())
                .AnyAsync();

            if (codigoExistente)
                throw new ArgumentException($"Ya existe otro turno con el código '{request.NombreCodigo}'.");

            // Validar que el TipoTurno exista
            var tipoTurnoExiste = await _context.TipoTurnos
                .AnyAsync(tt => tt.Id == request.TipoTurnoId);

            if (!tipoTurnoExiste)
                throw new KeyNotFoundException($"TipoTurno con ID {request.TipoTurnoId} no encontrado.");

            existingTurno.NombreCodigo = request.NombreCodigo;
            existingTurno.ToleranciaSalida = request.ToleranciaSalida;
            existingTurno.ToleranciaIngreso = request.ToleranciaIngreso;
            existingTurno.TipoTurnoId = request.TipoTurnoId;
            existingTurno.EsActivo = request.EsActivo;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error al actualizar el turno. Verifica que los datos sean válidos.", ex);
            }
        }
    }
}
