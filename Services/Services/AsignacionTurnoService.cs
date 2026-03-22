using System;
using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;

namespace Asistencia.Services.Services
{
    public class AsignacionTurnoService : IAsignacionTurnoService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public AsignacionTurnoService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        public async Task<AsignacionTurno> AddAsync(AsignacionTurnoCreateDto request)
        {
            if (request.FechaFinVigencia.HasValue && request.FechaFinVigencia.Value < request.FechaInicioVigencia)
            {
                throw new ArgumentException("La fecha fin de vigencia no puede ser menor que la fecha inicio.");
            }

            var trabajadorExiste = await _context.Trabajadores.AnyAsync(t => t.Id == request.TrabajadorId);
            if (!trabajadorExiste)
            {
                throw new KeyNotFoundException($"Trabajador con ID {request.TrabajadorId} no encontrado.");
            }

            var turno = await _context.Turnos.Include(t => t.TipoTurno).FirstOrDefaultAsync(t => t.Id == request.TurnoId);
            if (turno == null)
            {
                throw new KeyNotFoundException($"Turno con ID {request.TurnoId} no encontrado.");
            }

            // Si el turno es rotativo, preferir exigir horarioTurnoId
            var nombreTipo = turno.TipoTurno?.NombreTipo ?? string.Empty;
            var esRotativo = nombreTipo.ToUpperInvariant().Contains("ROT");
            if (esRotativo && !request.HorarioTurnoId.HasValue)
            {
                throw new ArgumentException("Para turnos rotativos se requiere HorarioTurnoId.");
            }

            // Si se suministra HorarioTurnoId, validar que exista y pertenezca al turno
            if (request.HorarioTurnoId.HasValue)
            {
                var horarioValido = await _context.HorariosTurno
                    .AnyAsync(h => h.Id == request.HorarioTurnoId.Value && h.TurnoId == request.TurnoId);
                if (!horarioValido)
                {
                    throw new KeyNotFoundException($"HorarioTurno con ID {request.HorarioTurnoId} no encontrado o no pertenece al turno {request.TurnoId}.");
                }
            }

            // Validar solapamiento de vigencias para el trabajador
            var newStart = request.FechaInicioVigencia;
            var newEnd = request.FechaFinVigencia ?? DateOnly.MaxValue;

            var existeSolapamiento = await _context.AsignacionesTurno
                .AnyAsync(a => a.TrabajadorId == request.TrabajadorId &&
                               newStart <= (a.FechaFinVigencia ?? DateOnly.MaxValue) &&
                               newEnd >= a.FechaInicioVigencia);

            if (existeSolapamiento)
            {
                throw new ArgumentException("La vigencia solapa con otra asignación existente.");
            }

            var asignacion = new AsignacionTurno
            {
                TrabajadorId = request.TrabajadorId,
                TurnoId = request.TurnoId,
                HorarioTurnoId = request.HorarioTurnoId,
                FechaInicioVigencia = request.FechaInicioVigencia,
                FechaFinVigencia = request.FechaFinVigencia,
                EsVigente = true // Por defecto, una nueva asignación es vigente
            };

            // opcionales
            asignacion.MotivoCambio = request.MotivoCambio;
            asignacion.AprobadoPor = request.AprobadoPor;
            asignacion.CreatedAt = DateTime.UtcNow;

            var asignacionesVigentes = await _context.AsignacionesTurno
                .Where(a => a.TrabajadorId == request.TrabajadorId && a.EsVigente)
                .ToListAsync();

            foreach (var vigente in asignacionesVigentes)
            {
                vigente.EsVigente = false;
                vigente.FechaFinVigencia = request.FechaInicioVigencia.AddDays(-1);
                vigente.UpdatedAt = DateTime.UtcNow;
            }

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

        public async Task<PagedResult<AsignacionTurnoResponseDto>> GetAllAsync(PaginationDto pagination)
        {
            var query = _context.AsignacionesTurno
                                .Include(a => a.Trabajador)
                                .ThenInclude(t => t!.Persona)
                                .Include(a => a.Turno)
                                .ThenInclude(t => t!.TipoTurno)
                                .Include(a => a.HorarioTurno)
                                .AsQueryable();

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(a => new AsignacionTurnoResponseDto
                {
                    Id = a.Id,
                    TrabajadorId = a.TrabajadorId,
                    TrabajadorNombre = a.Trabajador!.Persona!.ApellidosNombres,
                    TrabajadorDni = a.Trabajador.Persona.Dni,
                    TurnoId = a.TurnoId,
                    TurnoNombre = a.Turno!.NombreCodigo,
                    TipoTurno = a.Turno.TipoTurno!.NombreTipo,
                    HorarioTurnoId = a.HorarioTurnoId,
                    HorarioTurnoNombre = a.HorarioTurno!.NombreHorario,
                    EsVigente = a.EsVigente,
                    FechaInicioVigencia = a.FechaInicioVigencia,
                    FechaFinVigencia = a.FechaFinVigencia,
                    MotivoCambio = a.MotivoCambio,
                    AprobadoPor = a.AprobadoPor,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToListAsync();

            return new PagedResult<AsignacionTurnoResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = pagination.PageSize,
                CurrentPage = pagination.PageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            };
        }

        public async Task<AsignacionTurnoResponseDto?> GetByIdAsync(int id)
        {
            return await _context.AsignacionesTurno
                                 .Include(a => a.Trabajador)
                                 .ThenInclude(t => t!.Persona)
                                 .Include(a => a.Turno)
                                 .ThenInclude(t => t!.TipoTurno)
                                 .Include(a => a.HorarioTurno)
                                 .Where(a => a.Id == id)
                                 .Select(a => new AsignacionTurnoResponseDto
                                 {
                                     Id = a.Id,
                                     TrabajadorId = a.TrabajadorId,
                                     TrabajadorNombre = a.Trabajador!.Persona!.ApellidosNombres,
                                     TrabajadorDni = a.Trabajador.Persona.Dni,
                                     TurnoId = a.TurnoId,
                                     TurnoNombre = a.Turno!.NombreCodigo,
                                     TipoTurno = a.Turno.TipoTurno!.NombreTipo,
                                     HorarioTurnoId = a.HorarioTurnoId,
                                     HorarioTurnoNombre = a.HorarioTurno!.NombreHorario,
                                     EsVigente = a.EsVigente,
                                     FechaInicioVigencia = a.FechaInicioVigencia,
                                     FechaFinVigencia = a.FechaFinVigencia,
                                     MotivoCambio = a.MotivoCambio,
                                     AprobadoPor = a.AprobadoPor,
                                     CreatedAt = a.CreatedAt,
                                     UpdatedAt = a.UpdatedAt
                                 })
                                 .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(int id, AsignacionTurnoUpdateDto request)
        {
            if (request.FechaFinVigencia.HasValue && request.FechaFinVigencia.Value < request.FechaInicioVigencia)
            {
                throw new ArgumentException("La fecha fin de vigencia no puede ser menor que la fecha inicio.");
            }

            var existingAsignacion = await _context.AsignacionesTurno.FindAsync(id);
            if (existingAsignacion == null)
            {
                throw new KeyNotFoundException($"Asignación de turno con ID {id} no encontrada.");
            }

            var trabajadorExiste = await _context.Trabajadores.AnyAsync(t => t.Id == request.TrabajadorId);
            if (!trabajadorExiste)
            {
                throw new KeyNotFoundException($"Trabajador con ID {request.TrabajadorId} no encontrado.");
            }

            var turnoExiste = await _context.Turnos.AnyAsync(t => t.Id == request.TurnoId);
            if (!turnoExiste)
            {
                throw new KeyNotFoundException($"Turno con ID {request.TurnoId} no encontrado.");
            }
        
            existingAsignacion.TrabajadorId = request.TrabajadorId;
            existingAsignacion.TurnoId = request.TurnoId;
            existingAsignacion.HorarioTurnoId = request.HorarioTurnoId;
            existingAsignacion.FechaInicioVigencia = request.FechaInicioVigencia;
            existingAsignacion.FechaFinVigencia = request.FechaFinVigencia;
            existingAsignacion.EsVigente = request.EsVigente;
            existingAsignacion.MotivoCambio = request.MotivoCambio;
            existingAsignacion.AprobadoPor = request.AprobadoPor;
            existingAsignacion.UpdatedAt = DateTime.UtcNow;

            if (request.EsVigente)
            {
                var asignacionesVigentes = await _context.AsignacionesTurno
                    .Where(a => a.TrabajadorId == request.TrabajadorId && a.EsVigente && a.Id != id)
                    .ToListAsync();

                foreach (var vigente in asignacionesVigentes)
                {
                    vigente.EsVigente = false;
                }
            }
        
            _context.AsignacionesTurno.Update(existingAsignacion);
            await _context.SaveChangesAsync();
        }
    }
}
