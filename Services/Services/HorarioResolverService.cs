using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;

namespace Asistencia.Services.Services
{
    public class HorarioResolverService : IHorarioResolverService
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public HorarioResolverService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        private static string ToSpanishDayName(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday    => "LUNES",
            DayOfWeek.Tuesday   => "MARTES",
            DayOfWeek.Wednesday => "MIERCOLES",
            DayOfWeek.Thursday  => "JUEVES",
            DayOfWeek.Friday    => "VIERNES",
            DayOfWeek.Saturday  => "SABADO",
            DayOfWeek.Sunday    => "DOMINGO",
            _ => throw new ArgumentOutOfRangeException(nameof(day))
        };

        // Devuelve todos los detalles aplicables para la fecha (según diaSemana)
        public async Task<IEnumerable<HorarioDetalle>> GetDetallesForDateAsync(int horarioTurnoId, DateTime date)
        {
            var diaSemana = ToSpanishDayName(date.DayOfWeek);
            var detalles = await _context.HorariosDetalle
                .Where(d => d.HorarioTurnoId == horarioTurnoId && d.DiaSemana == diaSemana)
                .ToListAsync();

            return detalles;
        }

        // Resuelve el detalle activo para un DateTime dado. La heurística por ahora:
        // - si hay un detalle cuyo rango horaInicio-horaFin (considerando salidaDiaSiguiente) contiene la hora, lo devuelve
        // - si hay varios, devuelve el que tenga horaInicio más cercana al DateTime
        public async Task<HorarioDetalle?> ResolveDetalleForDateTimeAsync(int horarioTurnoId, DateTime dateTime)
        {
            var diaSemana = ToSpanishDayName(dateTime.DayOfWeek);
            var detalles = await _context.HorariosDetalle
                .Where(d => d.HorarioTurnoId == horarioTurnoId && d.DiaSemana == diaSemana)
                .ToListAsync();

            var time = dateTime.TimeOfDay;

            // Buscar coincidencia directa
            foreach (var d in detalles)
            {
                var start = d.HoraInicio;
                var end = d.HoraFin;

                if (d.SalidaDiaSiguiente)
                {
                    // termina al día siguiente
                    if (time >= start || time <= end)
                        return d;
                }
                else
                {
                    if (time >= start && time <= end)
                        return d;
                }
            }

            // Si no hay coincidencia directa, devolver el detalle con horaInicio más cercana
            HorarioDetalle? closest = null;
            var minDiff = TimeSpan.MaxValue;
            foreach (var d in detalles)
            {
                var diff = (d.HoraInicio - time).Duration();
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = d;
                }
            }

            return closest;
        }
    }
}
