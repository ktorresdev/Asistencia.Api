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

        public async Task<PagedResult<SucursalCentro>> GetAllAsync(PaginationDto pagination, string? search = null)
        {
            var query = _context.SucursalCentros.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(s => s.NombreSucursal.ToLower().Contains(searchLower));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(s => s.NombreSucursal)
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

            // Validar que el nombre de sucursal no esté duplicado
            var nombreDuplicado = await _context.SucursalCentros
                .Where(s => s.Id != id && s.NombreSucursal.ToLower() == sucursalCentro.NombreSucursal.ToLower())
                .AnyAsync();

            if (nombreDuplicado)
            {
                throw new ArgumentException($"Ya existe una sucursal con el nombre '{sucursalCentro.NombreSucursal}'.");
            }

            existingSucursalCentro.NombreSucursal = sucursalCentro.NombreSucursal;
            existingSucursalCentro.Direccion = sucursalCentro.Direccion;
            existingSucursalCentro.LatitudCentro = sucursalCentro.LatitudCentro;
            existingSucursalCentro.LongitudCentro = sucursalCentro.LongitudCentro;
            existingSucursalCentro.PerimetroM = sucursalCentro.PerimetroM;
            existingSucursalCentro.EsActivo = sucursalCentro.EsActivo;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error al actualizar la sucursal. Verifica que los datos sean válidos.", ex);
            }
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
            // Validar que el nombre de sucursal no esté duplicado
            var nombreDuplicado = await _context.SucursalCentros
                .AnyAsync(s => s.NombreSucursal.ToLower() == sucursalCentro.NombreSucursal.ToLower());

            if (nombreDuplicado)
            {
                throw new ArgumentException($"Ya existe una sucursal con el nombre '{sucursalCentro.NombreSucursal}'.");
            }

            try
            {
                _context.SucursalCentros.Add(sucursalCentro);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error al crear la sucursal. Verifica que los datos sean válidos.", ex);
            }
        }
    }
}
