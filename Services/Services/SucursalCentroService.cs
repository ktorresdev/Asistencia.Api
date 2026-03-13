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
    public class SucursalCentroService : ISucursalCentroService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public SucursalCentroService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<SucursalCentro>> GetAllAsync(PaginationDto pagination)
        {
            var query = _context.SucursalCentros.AsQueryable();

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<SucursalCentro>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = pagination.PageSize,
                CurrentPage = pagination.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            };
        }

        public async Task<SucursalCentro?> GetByIdAsync(int id)
        {
            return await _context.SucursalCentros.FindAsync(id);
        }

        public async Task UpdateAsync(int id, SucursalCentro sucursalCentro)
        {
            var existingSucursalCentro = await _context.SucursalCentros.FindAsync(id);
            if (existingSucursalCentro == null)
            {
                throw new KeyNotFoundException($"SucursalCentro con ID {id} no encontrado.");
            }

            existingSucursalCentro.NombreSucursal = sucursalCentro.NombreSucursal;
            existingSucursalCentro.Direccion = sucursalCentro.Direccion;
            existingSucursalCentro.LatitudCentro = sucursalCentro.LatitudCentro;
            existingSucursalCentro.LongitudCentro = sucursalCentro.LongitudCentro;
            existingSucursalCentro.PerimetroM = sucursalCentro.PerimetroM;
            existingSucursalCentro.EsActivo = sucursalCentro.EsActivo;

            _context.SucursalCentros.Update(existingSucursalCentro);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var sucursalCentro = await _context.SucursalCentros.FindAsync(id);
            if (sucursalCentro == null)
            {
                throw new KeyNotFoundException($"SucursalCentro con ID {id} no encontrado.");
            }

            _context.SucursalCentros.Remove(sucursalCentro);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(SucursalCentro sucursalCentro)
        {
            _context.SucursalCentros.Add(sucursalCentro);
            await _context.SaveChangesAsync();
        }
    }
}
