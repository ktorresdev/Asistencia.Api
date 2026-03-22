using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities;
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
    public class TrabajadorService : ITrabajadorService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public TrabajadorService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Trabajador>> GetAllAsync(PaginationDto paginationDto, string? search = null)
        {
            var query = _context.Trabajadores.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(t =>
                    t.Persona.ApellidosNombres.ToLower().Contains(searchLower) ||
                    t.Persona.Dni.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(t => t.Persona)
                .Include(t => t.Sucursal)
                .Include(t => t.User)
                .Skip((paginationDto.PageNumber - 1) * paginationDto.PageSize)
                .Take(paginationDto.PageSize)
                .ToListAsync();

            return new PagedResult<Trabajador>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = paginationDto.PageSize,
                CurrentPage = paginationDto.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)paginationDto.PageSize)
            };
        }

        public async Task<Trabajador?> GetByIdAsync(int id)
        {
            return await _context.Trabajadores
                                 .Include(t => t.Persona)
                                 .Include(t => t.Sucursal)
                                 .Include(t => t.User)
                                 .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(TrabajadorDto trabajadorDto)
        {
            var trabajador = new Trabajador
            {
                UserId = trabajadorDto.UserId,
                PersonaId = trabajadorDto.PersonaId,
                SucursalId = trabajadorDto.SucursalId,
                CorreoCorporativo = trabajadorDto.CorreoCorporativo,
                Cargo = trabajadorDto.Cargo,
                AreaDepartamento = trabajadorDto.AreaDepartamento,
                IdEstado = trabajadorDto.IdEstado,
                MarcajeEnZona = trabajadorDto.MarcajeEnZona
            };

            _context.Trabajadores.Add(trabajador);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, TrabajadorDto trabajadorDto)
        {
            var trabajador = await _context.Trabajadores.FindAsync(id);
            if (trabajador == null )
            {
                throw new KeyNotFoundException($"Trabajador con ID {id} no encontrado.");
            }

            trabajador.UserId = trabajadorDto.UserId;
            trabajador.PersonaId = trabajadorDto.PersonaId;
            trabajador.SucursalId = trabajadorDto.SucursalId;
            trabajador.CorreoCorporativo = trabajadorDto.CorreoCorporativo;
            trabajador.Cargo = trabajadorDto.Cargo;
            trabajador.AreaDepartamento = trabajadorDto.AreaDepartamento;
            trabajador.IdEstado = trabajadorDto.IdEstado;
            trabajador.MarcajeEnZona = trabajadorDto.MarcajeEnZona;

            _context.Trabajadores.Update(trabajador);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var trabajador = await _context.Trabajadores.FindAsync(id);
            if (trabajador == null)
            {
                throw new KeyNotFoundException($"Trabajador con ID {id} no encontrado.");
            }
            trabajador.IdEstado = 11;

            _context.Trabajadores.Update(trabajador);
            await _context.SaveChangesAsync();
        }
    }
}