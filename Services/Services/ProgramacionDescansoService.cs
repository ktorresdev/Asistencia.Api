using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Asistencia.Services.Services
{
    public class ProgramacionDescansoService : IProgramacionDescansoService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public ProgramacionDescansoService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProgramacionDescanso>> GetSemanaAsync(int idTrabajador, DateOnly fechaLunes)
        {
            var fechaFinSemana = fechaLunes.AddDays(6);

            var descansos = await _context.ProgramacionDescansos
                .Where(p => p.TrabajadorId == idTrabajador &&
                            p.Fecha >= fechaLunes &&
                            p.Fecha <= fechaFinSemana)
                .OrderBy(p => p.Fecha)
                .ToListAsync();

            return descansos;
        }

        public async Task<ProgramacionDescanso> UpsertDiaAsync(int idTrabajador, DateOnly fecha, bool esDescanso, bool esDiaBoleta, int createdBy)
        {
            var existente = await _context.ProgramacionDescansos
                .FirstOrDefaultAsync(p => p.TrabajadorId == idTrabajador && p.Fecha == fecha);

            if (existente != null)
            {
                existente.EsDescanso = esDescanso;
                existente.EsDiaBoleta = esDiaBoleta;
                existente.UpdatedAt = DateTime.UtcNow;
                _context.ProgramacionDescansos.Update(existente);
            }
            else
            {
                var nuevo = new ProgramacionDescanso
                {
                    TrabajadorId = idTrabajador,
                    Fecha = fecha,
                    EsDescanso = esDescanso,
                    EsDiaBoleta = esDiaBoleta,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProgramacionDescansos.Add(nuevo);
                existente = nuevo;
            }

            await _context.SaveChangesAsync();
            return existente;
        }
    }
}
