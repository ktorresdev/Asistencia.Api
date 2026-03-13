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

        public async Task AddAsync(Turno request)
        {
            _context.Turnos.Add(request);
            await _context.SaveChangesAsync();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResult<Turno>> GetAllAsync(PaginationDto pagination)
        {
            var query = _context.Turnos.AsQueryable();

            // 1. Contar el total de registros ANTES de paginar
            var totalCount = await query.CountAsync();

            // 2. Aplicar la paginación a la consulta
            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            // 3. Construir el objeto de respuesta paginada
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
            return await _context.Turnos.FindAsync(id);
        }

        public async Task UpdateAsync(int id, Turno request)
        {
            var existingTurno = await _context.Turnos.FindAsync(id);
            if (existingTurno == null)
            {
                throw new KeyNotFoundException($"Turno no encontrado.");
            }

            existingTurno.ToleranciaSalida = request.ToleranciaSalida;
            existingTurno.ToleranciaIngreso = request.ToleranciaIngreso;
            existingTurno.TipoTurnoId = request.TipoTurnoId;
            existingTurno.EsActivo = request.EsActivo;

            _context.Turnos.Update(existingTurno);
            await _context.SaveChangesAsync();
        }
    }
}
