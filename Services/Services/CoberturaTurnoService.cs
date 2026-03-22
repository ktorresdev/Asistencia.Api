using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Asistencia.Services.Services
{
    public class CoberturaTurnoService : ICoberturaTurnoService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public CoberturaTurnoService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        public async Task<CoberturaTurno> RegistrarAsync(CoberturaTurnoCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TipoCobertura))
                throw new ArgumentException("TipoCobertura es obligatorio.");

            var tipoValido = new[] { "COBERTURA", "CAMBIO", "ANTICIPO" }.Contains(dto.TipoCobertura.ToUpperInvariant());
            if (!tipoValido)
                throw new ArgumentException("TipoCobertura debe ser COBERTURA, CAMBIO o ANTICIPO.");

            if (dto.TipoCobertura.ToUpperInvariant() == "ANTICIPO" && !dto.FechaSwapDevolucion.HasValue)
                throw new ArgumentException("Para tipo ANTICIPO, FechaSwapDevolucion es obligatorio.");

            if (dto.IdTrabajadorCubre == dto.IdTrabajadorAusente)
                throw new ArgumentException("IdTrabajadorCubre no puede ser igual a IdTrabajadorAusente.");

            var trabajadorCubreExiste = await _context.Trabajadores.AnyAsync(t => t.Id == dto.IdTrabajadorCubre);
            if (!trabajadorCubreExiste)
                throw new KeyNotFoundException($"Trabajador cubre con ID {dto.IdTrabajadorCubre} no encontrado.");

            var trabajadorAusenteExiste = await _context.Trabajadores.AnyAsync(t => t.Id == dto.IdTrabajadorAusente);
            if (!trabajadorAusenteExiste)
                throw new KeyNotFoundException($"Trabajador ausente con ID {dto.IdTrabajadorAusente} no encontrado.");

            var horarioExiste = await _context.HorariosTurno.AnyAsync(h => h.Id == dto.IdHorarioTurnoOriginal);
            if (!horarioExiste)
                throw new KeyNotFoundException($"HorarioTurno con ID {dto.IdHorarioTurnoOriginal} no encontrado.");

            var cobertura = new CoberturaTurno
            {
                Fecha = dto.Fecha,
                IdTrabajadorCubre = dto.IdTrabajadorCubre,
                IdTrabajadorAusente = dto.IdTrabajadorAusente,
                IdHorarioTurnoOriginal = dto.IdHorarioTurnoOriginal,
                TipoCobertura = dto.TipoCobertura.ToUpperInvariant(),
                FechaSwapDevolucion = dto.FechaSwapDevolucion,
                AprobadoPor = dto.AprobadoPor,
                Estado = "PENDIENTE",
                CreatedAt = DateTime.UtcNow
            };

            _context.CoberturasTurno.Add(cobertura);
            await _context.SaveChangesAsync();

            return cobertura;
        }

        public async Task<CoberturaTurno> AprobarAsync(int id, int aprobadoPor)
        {
            var cobertura = await _context.CoberturasTurno.FindAsync(id);
            if (cobertura == null)
                throw new KeyNotFoundException($"Cobertura con ID {id} no encontrada.");

            cobertura.Estado = "APROBADO";
            cobertura.AprobadoPor = aprobadoPor;
            cobertura.UpdatedAt = DateTime.UtcNow;

            _context.CoberturasTurno.Update(cobertura);
            await _context.SaveChangesAsync();

            return cobertura;
        }

        public async Task<CoberturaTurno> RechazarAsync(int id)
        {
            var cobertura = await _context.CoberturasTurno.FindAsync(id);
            if (cobertura == null)
                throw new KeyNotFoundException($"Cobertura con ID {id} no encontrada.");

            cobertura.Estado = "RECHAZADO";
            cobertura.UpdatedAt = DateTime.UtcNow;

            _context.CoberturasTurno.Update(cobertura);
            await _context.SaveChangesAsync();

            return cobertura;
        }

        public async Task<PagedResult<CoberturaTurno>> GetAllAsync(PaginationDto pagination, string? estado = null, DateOnly? fecha = null)
        {
            var query = _context.CoberturasTurno
                .Include(c => c.TrabajadorCubre)
                .Include(c => c.TrabajadorAusente)
                .Include(c => c.HorarioTurnoOriginal)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(c => c.Estado.ToUpperInvariant() == estado.ToUpperInvariant());

            if (fecha.HasValue)
                query = query.Where(c => c.Fecha == fecha.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<CoberturaTurno>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = pagination.PageSize,
                CurrentPage = pagination.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            };
        }

        public async Task<PagedResult<CoberturaTurno>> GetByTrabajadorAsync(int idTrabajador, PaginationDto pagination)
        {
            var query = _context.CoberturasTurno
                .Include(c => c.TrabajadorCubre)
                .Include(c => c.TrabajadorAusente)
                .Include(c => c.HorarioTurnoOriginal)
                .Where(c => c.IdTrabajadorCubre == idTrabajador || c.IdTrabajadorAusente == idTrabajador)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<CoberturaTurno>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = pagination.PageSize,
                CurrentPage = pagination.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            };
        }
    }
}
