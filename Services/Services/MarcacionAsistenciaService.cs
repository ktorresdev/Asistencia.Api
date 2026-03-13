using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Threading.Tasks;

namespace Asistencia.Services.Services
{
    public class MarcacionAsistenciaService : IMarcacionAsistenciaService
    {
        private readonly MarcacionAsistenciaDbContext _context;
        private static readonly TimeSpan EarlyWindowTolerance = TimeSpan.FromHours(2);
        private static readonly TimeSpan LateWindowTolerance = TimeSpan.FromHours(2);
        private static readonly TimeSpan DefaultStartTime = TimeSpan.FromHours(8);
        private static readonly TimeSpan DefaultEndTime = TimeSpan.FromHours(18).Add(TimeSpan.FromMinutes(30));

        private sealed class ShiftContext
        {
            public bool HasAssignedShift { get; init; }
            public bool HasActiveSchedule { get; init; }
            public HorarioDetalle? ScheduleDetail { get; init; }
            public DateTime? ScheduledStart { get; init; }
            public DateTime? ScheduledEnd { get; init; }
            public DateTime? WindowStart { get; init; }
            public DateTime? WindowEnd { get; init; }
        }

        public MarcacionAsistenciaService(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        private (DateTime scheduledStart, DateTime scheduledEnd) BuildScheduledRange(HorarioDetalle detalle, DateTime baseDate)
        {
            var isOvernight = detalle.SalidaDiaSiguiente || detalle.HoraFin < detalle.HoraInicio;
            var scheduledStart = baseDate.Date.Add(detalle.HoraInicio);
            var scheduledEnd = isOvernight
                ? baseDate.Date.AddDays(1).Add(detalle.HoraFin)
                : baseDate.Date.Add(detalle.HoraFin);

            return (scheduledStart, scheduledEnd);
        }

        private (DateTime scheduledStart, DateTime scheduledEnd) BuildScheduledRangeFromMatchedWindow(HorarioDetalle detalle, DateTime now)
        {
            var isOvernight = detalle.SalidaDiaSiguiente || detalle.HoraFin < detalle.HoraInicio;
            var baseDate = (isOvernight && now.TimeOfDay <= detalle.HoraFin)
                ? now.Date.AddDays(-1)
                : now.Date;

            return BuildScheduledRange(detalle, baseDate);
        }

        private async Task<ShiftContext> ResolveShiftContextAsync(int trabajadorId, DateTime now, bool includeTodayFallback, bool includeDefaultFallback)
        {
            var today = now.Date;

            var asignacion = await _context.AsignacionesTurno
                .Include(a => a.Turno)
                    .ThenInclude(t => t!.HorariosTurno!)
                        .ThenInclude(ht => ht.HorariosDetalle)
                .FirstOrDefaultAsync(a =>
                    a.TrabajadorId == trabajadorId &&
                    a.FechaInicioVigencia.Date <= now.Date &&
                    (a.FechaFinVigencia == null || a.FechaFinVigencia.Value.Date >= now.Date) &&
                    a.EsVigente == true);

            var turno = asignacion?.Turno;
            if (turno == null)
            {
                return new ShiftContext
                {
                    HasAssignedShift = false,
                    HasActiveSchedule = false
                };
            }

            var horarioTurno = turno.HorariosTurno?.FirstOrDefault(ht => ht.EsActivo == true);
            if (horarioTurno == null || horarioTurno.HorariosDetalle == null || !horarioTurno.HorariosDetalle.Any())
            {
                return new ShiftContext
                {
                    HasAssignedShift = true,
                    HasActiveSchedule = false
                };
            }

            foreach (var detalle in horarioTurno.HorariosDetalle)
            {
                if (TryMatchHorarioDetalleWindow(detalle, now, EarlyWindowTolerance, LateWindowTolerance, out var windowStart, out var windowEnd))
                {
                    var scheduledRange = BuildScheduledRangeFromMatchedWindow(detalle, now);
                    return new ShiftContext
                    {
                        HasAssignedShift = true,
                        HasActiveSchedule = true,
                        ScheduleDetail = detalle,
                        ScheduledStart = scheduledRange.scheduledStart,
                        ScheduledEnd = scheduledRange.scheduledEnd,
                        WindowStart = windowStart,
                        WindowEnd = windowEnd
                    };
                }
            }

            if (includeTodayFallback)
            {
                var detalleHoy = horarioTurno.HorariosDetalle.FirstOrDefault(hd => IsDiaSemanaMatch(hd.DiaSemana, today));
                if (detalleHoy != null)
                {
                    var scheduledRange = BuildScheduledRange(detalleHoy, today);
                    return new ShiftContext
                    {
                        HasAssignedShift = true,
                        HasActiveSchedule = true,
                        ScheduleDetail = detalleHoy,
                        ScheduledStart = scheduledRange.scheduledStart,
                        ScheduledEnd = scheduledRange.scheduledEnd,
                        WindowStart = scheduledRange.scheduledStart.Subtract(EarlyWindowTolerance),
                        WindowEnd = scheduledRange.scheduledEnd.Add(LateWindowTolerance)
                    };
                }
            }

            if (includeDefaultFallback)
            {
                var defaultStart = today.Add(DefaultStartTime);
                var defaultEnd = today.Add(DefaultEndTime);
                return new ShiftContext
                {
                    HasAssignedShift = true,
                    HasActiveSchedule = true,
                    ScheduledStart = defaultStart,
                    ScheduledEnd = defaultEnd,
                    WindowStart = today,
                    WindowEnd = today.AddDays(1).AddTicks(-1)
                };
            }

            return new ShiftContext
            {
                HasAssignedShift = true,
                HasActiveSchedule = true
            };
        }

        public async Task<PagedResult<MarcacionAsistencia>> GetAllAsync(PaginationDto pagination)
        {
            var query = _context.MarcacionesAsistencia.AsQueryable();
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();
            return new PagedResult<MarcacionAsistencia>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = pagination.PageSize,
                CurrentPage = pagination.PageNumber,
                TotalPages = (int)System.Math.Ceiling(totalCount / (double)pagination.PageSize)
            };
        }

        public async Task<MarcacionResponse> AddMarcacionAsync(MarcacionRequest marcacionRequest)
        {
            var now = System.DateTime.Now;
            var shiftContext = await ResolveShiftContextAsync(
                marcacionRequest.IdTrabajador,
                now,
                includeTodayFallback: false,
                includeDefaultFallback: false);

            if (!shiftContext.HasAssignedShift)
            {
                return new MarcacionResponse { Success = false, Code = "ERROR_NO_TURNO", Message = "No tiene un turno asignado para la fecha actual.", Detail = "Verifique asignación de turno del trabajador." };
            }

            // 1. Obtener el trabajador y su centro de costo
            var trabajador = await _context.Trabajadores
                .Include(t => t.Sucursal)
                .FirstOrDefaultAsync(t => t.Id == marcacionRequest.IdTrabajador);


            if (trabajador == null || trabajador.Sucursal == null)
            {
                return new MarcacionResponse { Success = false, Code = "ERROR_TRABAJADOR_NO_ENCONTRADO", Message = "Trabajador no encontrado o sin centro de trabajo asignado.", Detail = "Confirme que el IdTrabajador es correcto y tiene sucursal vinculada." };
            }

            if (!shiftContext.HasActiveSchedule)
            {
                return new MarcacionResponse { Success = false, Code = "ERROR_SIN_HORARIO", Message = "No tiene un horario definido para la fecha actual.", Detail = "Revise la configuración de horarios para el turno asignado." };
            }

            if (!shiftContext.WindowStart.HasValue || !shiftContext.WindowEnd.HasValue)
            {
                return new MarcacionResponse { Success = false, Code = "ERROR_SIN_HORARIO", Message = "No tiene un horario definido para la fecha actual.", Detail = "Revise la configuración de horarios para el turno asignado o márquee dentro de la ventana permitida." };
            }

            var windowStart = shiftContext.WindowStart.Value;
            var windowEnd = shiftContext.WindowEnd.Value;

            // 2. Validar geolocalización (después de confirmar que tiene horario hoy)
            var sucursal = trabajador.Sucursal;
            var distancia = CalcularDistancia(
                marcacionRequest.Latitud, marcacionRequest.Longitud,
                (double)(sucursal.LatitudCentro ?? 0), (double)(sucursal.LongitudCentro ?? 0));

            bool ubicacionValida = distancia <= (sucursal.PerimetroM ?? 0);

            if (!ubicacionValida && trabajador.MarcajeEnZona)
            {
                return new MarcacionResponse { Success = false, Code = "ERROR_FUERA_ZONA", Message = "Marcación fuera del área permitida.", Detail = $"Se encuentra a {distancia:F2} m del centro de trabajo." };
            }

            // --- LÓGICA REVISADA PARA DETERMINAR EL TIPO DE MARCACIÓN Y VALIDACIÓN ---
            // Obtener todas las marcaciones existentes para el trabajador en el día actual
            var existingMarksToday = await _context.MarcacionesAsistencia
                .Where(m => m.TrabajadorId == marcacionRequest.IdTrabajador && m.FechaHora >= windowStart && m.FechaHora <= windowEnd)
                .OrderBy(m => m.FechaHora)
                .ToListAsync();

            string tipoMarcacion;
            if (!existingMarksToday.Any())
            {
                // Si no hay marcaciones para hoy, la primera debe ser una "Entrada".
                tipoMarcacion = "Entrada";
            }
            else
            {
                var lastMark = existingMarksToday.Last();

                if (lastMark.TipoMarcacion == "Entrada")
                {
                    // Si la última marcación fue una "Entrada", la siguiente debe ser una "Salida".
                    tipoMarcacion = "Salida";
                }
                else // lastMark.TipoMarcacion == "Salida"
                {
                    // Si la última marcación fue una "Salida", significa que ya se completó un ciclo de entrada-salida.
                    // Para evitar múltiples "Entradas" en el mismo día se impide una nueva marcación de "Entrada".
                    return new MarcacionResponse { Success = false, Code = "ERROR_SALIDA_REGISTRADA", Message = "Ya registró su salida hoy; no se permite nueva entrada sin configuración de turnos adicionales.", Detail = "Si corresponde, configure turnos adicionales o permita reingresos." };
                }
            }

            // Validación para evitar duplicados (marcaciones iguales en tipo y muy cercanas en el tiempo)
            var existeMarcacionIgual = existingMarksToday.Any(m =>
                m.TipoMarcacion == tipoMarcacion &&
                Math.Abs((m.FechaHora - now).TotalSeconds) < 120 // margen de 2 minutos
            );

            if (existeMarcacionIgual)
            {
                return new MarcacionResponse
                {
                    Success = false,
                    Code = "ERROR_DUPLICADO_RECIENTE",
                    Message = $"Ya existe una marcación de {tipoMarcacion} registrada recientemente.",
                    Detail = "Hay una marcación del mismo tipo dentro de los últimos 120 segundos."
                };
            }

            // 5. Crear y guardar la marcación
            var nuevaMarcacion = new MarcacionAsistencia
            {
                TrabajadorId = marcacionRequest.IdTrabajador,
                FechaHora = now,
                Latitud = (decimal)marcacionRequest.Latitud,
                Longitud = (decimal)marcacionRequest.Longitud,
                TipoMarcacion = tipoMarcacion,
                FotoUrl = marcacionRequest.FotoUrl,
                UbicacionValida = ubicacionValida,
                //TokenValidacion = Guid.NewGuid().ToString() // Generar un token único
            };

            _context.MarcacionesAsistencia.Add(nuevaMarcacion);
            await _context.SaveChangesAsync();

            return new MarcacionResponse
            {
                Success = true,
                Code = "SUCCESS_MARCACION_OK",
                Message = $"Marcación de {tipoMarcacion} registrada con éxito a las {nuevaMarcacion.FechaHora:HH:mm:ss}.",
                Data = nuevaMarcacion
            };
        }

        private double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3; // Radio de la Tierra en metros
            var φ1 = lat1 * System.Math.PI / 180;
            var φ2 = lat2 * System.Math.PI / 180;
            var Δφ = (lat2 - lat1) * System.Math.PI / 180;
            var Δλ = (lon2 - lon1) * System.Math.PI / 180;

            var a = System.Math.Sin(Δφ / 2) * System.Math.Sin(Δφ / 2) +
                    System.Math.Cos(φ1) * System.Math.Cos(φ2) *
                    System.Math.Sin(Δλ / 2) * System.Math.Sin(Δλ / 2);
            var c = 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));

            return R * c; // en metros
        }

        // Comprueba si el campo DiaSemana (puede ser '1', 'Mon', 'Lun', '1-5', 'Mon-Fri', '1,2,3')
        // coincide con la fecha dada.
        private bool IsDiaSemanaMatch(string diaSemanaRaw, DateTime fecha)
        {
            if (string.IsNullOrWhiteSpace(diaSemanaRaw)) return false;

            // Normalizar y split por comas o punto y coma
            var parts = diaSemanaRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p));

            var isoDay = (fecha.DayOfWeek == System.DayOfWeek.Sunday) ? 7 : (int)fecha.DayOfWeek; // 1..7

            foreach (var part in parts)
            {
                // Range como '1-5' o 'Mon-Fri' o 'Lun-Vie'
                if (part.Contains('-'))
                {
                    var range = part.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                    if (range.Length != 2) continue;
                    var start = ParseDayTokenToIso(range[0]);
                    var end = ParseDayTokenToIso(range[1]);
                    if (start == null || end == null) continue;
                    // considerar inclusive
                    if (start <= end)
                    {
                        if (isoDay >= start && isoDay <= end) return true;
                    }
                    else
                    {
                        // wrap-around (ej: 6-2)
                        if (isoDay >= start || isoDay <= end) return true;
                    }
                }
                else
                {
                    var val = ParseDayTokenToIso(part);
                    if (val != null && val == isoDay) return true;
                }
            }

            return false;
        }

        // Convierte tokens como '1', 'Mon', 'Lun', 'Monday', 'Lunes' a ISO day number 1..7
        private int? ParseDayTokenToIso(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            token = token.Trim();

            // si es número
            if (int.TryParse(token, out var n))
            {
                // aceptar 0..6 (System.DayOfWeek) o 1..7 (ISO)
                if (n >= 0 && n <= 6)
                {
                    return n == 0 ? 7 : n; // convertir 0(Sun) -> 7
                }
                if (n >= 1 && n <= 7) return n;
            }

            // map de nombres (español e inglés y abreviaturas)
            var lower = token.ToLowerInvariant();
            return lower switch
            {
                "mon" or "monday" or "lun" or "lunes" => 1,
                "tue" or "tues" or "tuesday" or "mar" or "martes" => 2,
                "wed" or "wednesday" or "mie" or "miercoles" or "miércoles" => 3,
                "thu" or "thur" or "thurs" or "thursday" or "jue" or "jueves" => 4,
                "fri" or "friday" or "vie" or "viernes" => 5,
                "sat" or "saturday" or "sab" or "sabado" or "sábado" => 6,
                "sun" or "sunday" or "dom" or "domingo" => 7,
                _ => null
            };
        }

        // Determina si el HorarioDetalle aplica a 'now' y devuelve la ventana (start,end)
        // Soporta turnos overnight (SalidaDiaSiguiente o HoraFin < HoraInicio) y tolerancias.
        private bool TryMatchHorarioDetalleWindow(HorarioDetalle hd, DateTime now, TimeSpan earlyWindow, TimeSpan lateWindow, out DateTime windowStart, out DateTime windowEnd)
        {
            windowStart = DateTime.MinValue;
            windowEnd = DateTime.MinValue;

            bool isOvernight = hd.SalidaDiaSiguiente || hd.HoraFin < hd.HoraInicio;

            // Intentar con el día actual
            if (IsDiaSemanaMatch(hd.DiaSemana, now))
            {
                var start = now.Date.Add(hd.HoraInicio);
                var end = isOvernight ? now.Date.AddDays(1).Add(hd.HoraFin) : now.Date.Add(hd.HoraFin);

                var startWindow = start.Subtract(earlyWindow);
                var endWindow = end.Add(lateWindow);

                if (now >= startWindow && now <= endWindow)
                {
                    windowStart = startWindow;
                    windowEnd = endWindow;
                    return true;
                }
            }

            // Intentar con el día anterior (útil para marcas después de medianoche)
            var prev = now.Date.AddDays(-1);
            if (IsDiaSemanaMatch(hd.DiaSemana, prev))
            {
                var start = prev.Add(hd.HoraInicio);
                var end = isOvernight ? prev.AddDays(1).Add(hd.HoraFin) : prev.Add(hd.HoraFin);

                var startWindow = start.Subtract(earlyWindow);
                var endWindow = end.Add(lateWindow);

                if (now >= startWindow && now <= endWindow)
                {
                    windowStart = startWindow;
                    windowEnd = endWindow;
                    return true;
                }
            }

            // Intentar con el día siguiente como respaldo
            var next = now.Date.AddDays(1);
            if (IsDiaSemanaMatch(hd.DiaSemana, next))
            {
                var start = next.Add(hd.HoraInicio);
                var end = isOvernight ? next.AddDays(1).Add(hd.HoraFin) : next.Add(hd.HoraFin);

                var startWindow = start.Subtract(earlyWindow);
                var endWindow = end.Add(lateWindow);

                if (now >= startWindow && now <= endWindow)
                {
                    windowStart = startWindow;
                    windowEnd = endWindow;
                    return true;
                }
            }

            return false;
        }

        public Task<MarcacionAsistencia> getMarcadoAsistenciaAsync(int trabajadorId)
        {
            throw new NotImplementedException();
        }

        public async Task<TimeWorkedDto> CalculateTimeWorkedAsync(int trabajadorId)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;

            var shiftContext = await ResolveShiftContextAsync(
                trabajadorId,
                now,
                includeTodayFallback: true,
                includeDefaultFallback: true);

            var scheduledStart = shiftContext.ScheduledStart ?? today.Add(DefaultStartTime);
            var scheduledEnd = shiftContext.ScheduledEnd ?? today.Add(DefaultEndTime);
            var windowStart = shiftContext.WindowStart ?? today;
            var windowEnd = shiftContext.WindowEnd ?? today.AddDays(1).AddTicks(-1);

            // 2. Obtener Marcaciones dentro de la ventana calculada (no solo por día calendario)
            var calculatedWindowMarks = await _context.MarcacionesAsistencia
                .Where(m => m.TrabajadorId == trabajadorId && m.FechaHora >= windowStart && m.FechaHora <= windowEnd)
                .OrderBy(m => m.FechaHora)
                .ToListAsync();

            var lastEntry = calculatedWindowMarks.FirstOrDefault(m => m.TipoMarcacion == "Entrada");
            var lastExit = lastEntry == null
                ? null
                : calculatedWindowMarks.LastOrDefault(m => m.TipoMarcacion == "Salida" && m.FechaHora > lastEntry.FechaHora);

            TimeSpan timeWorked;

            // --- Lógica del Cálculo ---
            if (lastEntry == null)
            {
                // Condición 3: No hay entrada.
                timeWorked = TimeSpan.Zero;
            }
            else if (lastExit != null && lastExit.FechaHora > lastEntry.FechaHora)
            {
                // Condición 1: Ya se completó la jornada (Salida posterior a la Entrada).
                timeWorked = lastExit.FechaHora - lastEntry.FechaHora;
            }
            else
            {
                // Condición 2: Entrada registrada, pero Salida pendiente.
                // Se calcula hasta el momento actual.
                timeWorked = now - lastEntry.FechaHora;
            }

            // Devolvemos el DTO con el resultado
            return new TimeWorkedDto
            {
                ScheduledTime = $"({scheduledStart:HH:mm} - {scheduledEnd:HH:mm})",
                TimeWorkedMinutes = timeWorked.TotalMinutes,
                TimeWorkedFormatted = $"{Math.Floor(timeWorked.TotalHours)}h {timeWorked.Minutes}m",
                EntryRegisteredAt = lastEntry?.FechaHora,
                ExitRegisteredAt = lastExit?.FechaHora,
                StatusMessage = "Cálculo realizado correctamente."
            };
        }
    }

}