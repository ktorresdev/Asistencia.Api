using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace Asistencia.Services.Services
{
    public class MarcacionAsistenciaService : IMarcacionAsistenciaService
    {
        private readonly MarcacionAsistenciaDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Tolerancia de ventana de marcación: 2 horas antes y después del turno
        private static readonly TimeSpan EarlyWindowTolerance = TimeSpan.FromHours(2);
        private static readonly TimeSpan LateWindowTolerance = TimeSpan.FromHours(2);

        // Horario por defecto si no hay ninguna configuración
        private static readonly TimeSpan DefaultStartTime = TimeSpan.FromHours(8);
        private static readonly TimeSpan DefaultEndTime = TimeSpan.FromHours(18).Add(TimeSpan.FromMinutes(30));

        // ══════════════════════════════════════════════════════════════
        // CLASES INTERNAS
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Resultado de resolver el contexto de horario para un trabajador en un momento dado.
        /// FuenteHorario indica de dónde se obtuvo el horario (útil para debug).
        /// </summary>
        private sealed class ShiftContext
        {
            public bool HasAssignedShift { get; init; }
            public bool HasActiveSchedule { get; init; }
            public HorarioDetalle? ScheduleDetail { get; init; }
            public DateTime? ScheduledStart { get; init; }
            public DateTime? ScheduledEnd { get; init; }
            public DateTime? WindowStart { get; init; }
            public DateTime? WindowEnd { get; init; }

            /// <summary>
            /// Indica de dónde se resolvió el horario:
            /// PTS              → PROGRAMACION_TURNOS_SEMANAL (rotativo con PTS cargada)
            /// PTS_FALLBACK     → PTS existe pero fuera de ventana, se usa como base
            /// ASIGNACION_BASE  → ASIGNACIONES_TURNO (fijo o rotativo sin PTS)
            /// ASIGNACION_FALLBACK → ASIGNACIONES_TURNO fuera de ventana, usado como base
            /// DEFAULT          → Horario genérico 08:00-18:30
            /// SIN_ASIGNACION   → No tiene turno asignado
            /// </summary>
            public string FuenteHorario { get; init; } = "NINGUNA";
        }

        private sealed class SucursalGeofenceDto
        {
            public int IdSucursal { get; set; }
            public string? NombreSucursal { get; set; }
            public decimal? LatitudCentro { get; set; }
            public decimal? LongitudCentro { get; set; }
            public int? PerimetroM { get; set; }
        }

        // ══════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ══════════════════════════════════════════════════════════════

        public MarcacionAsistenciaService(
            MarcacionAsistenciaDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS DE RANGO HORARIO
        // ══════════════════════════════════════════════════════════════

        private (DateTime scheduledStart, DateTime scheduledEnd) BuildScheduledRange(
            HorarioDetalle detalle, DateTime baseDate)
        {
            var isOvernight = detalle.SalidaDiaSiguiente || detalle.HoraFin < detalle.HoraInicio;
            var scheduledStart = baseDate.Date.Add(detalle.HoraInicio);
            var scheduledEnd = isOvernight
                ? baseDate.Date.AddDays(1).Add(detalle.HoraFin)
                : baseDate.Date.Add(detalle.HoraFin);
            return (scheduledStart, scheduledEnd);
        }

        private (DateTime scheduledStart, DateTime scheduledEnd) BuildScheduledRangeFromMatchedWindow(
            HorarioDetalle detalle, DateTime now)
        {
            var isOvernight = detalle.SalidaDiaSiguiente || detalle.HoraFin < detalle.HoraInicio;
            // Si es turno noche y ya pasó medianoche, la base fue ayer
            var baseDate = (isOvernight && now.TimeOfDay <= detalle.HoraFin)
                ? now.Date.AddDays(-1)
                : now.Date;
            return BuildScheduledRange(detalle, baseDate);
        }

        // ══════════════════════════════════════════════════════════════
        // RESOLVER CONTEXTO DE HORARIO
        // Prioridad: PTS (por día) → ASIGNACION_TURNO (base) → DEFAULT
        // ══════════════════════════════════════════════════════════════

        private async Task<ShiftContext> ResolveShiftContextAsync(
            int trabajadorId,
            DateTime now,
            bool includeTodayFallback,
            bool includeDefaultFallback)
        {
            // Extraer fechas a variables locales para que EF Core pueda parametrizarlas
            // correctamente en SQL. EF no puede traducir .Date dentro del lambda.
            var hoy = now.Date;               // DateTime (sin hora)
            var hoyDateOnly = DateOnly.FromDateTime(hoy); // para PTS

            // ── PASO 1: PROGRAMACION_TURNOS_SEMANAL ─────────────────────
            var pts = await _context.ProgramacionTurnosSemanal
                .Include(p => p.HorarioTurno)
                    .ThenInclude(ht => ht!.HorariosDetalle)
                .FirstOrDefaultAsync(p =>
                    p.TrabajadorId == trabajadorId &&
                    p.Fecha == hoyDateOnly &&
                    p.EsDescanso != true &&
                    p.EsDiaBoleta != true &&
                    p.EsVacaciones != true &&
                    p.IdHorarioTurno != null);

            if (pts?.HorarioTurno != null)
            {
                var detallesPts = pts.HorarioTurno.HorariosDetalle;
                if (detallesPts != null && detallesPts.Any())
                {
                    // Intentar hacer match de ventana con cada detalle del HorarioTurno del día
                    foreach (var hd in detallesPts)
                    {
                        if (TryMatchHorarioDetalleWindow(
                                hd, now,
                                EarlyWindowTolerance, LateWindowTolerance,
                                out var ws, out var we))
                        {
                            var range = BuildScheduledRangeFromMatchedWindow(hd, now);
                            return new ShiftContext
                            {
                                HasAssignedShift = true,
                                HasActiveSchedule = true,
                                ScheduleDetail = hd,
                                ScheduledStart = range.scheduledStart,
                                ScheduledEnd = range.scheduledEnd,
                                WindowStart = ws,
                                WindowEnd = we,
                                FuenteHorario = "PTS"
                            };
                        }
                    }

                    // PTS existe pero la hora actual está fuera de ventana.
                    // Usar el detalle de PTS como base si se permite fallback.
                    if (includeTodayFallback)
                    {
                        // Para rotativos los HorariosDetalle del sub-horario suelen
                        // tener un solo registro o uno por día de semana.
                        var detalleBase = detallesPts
                            .FirstOrDefault(hd => IsDiaSemanaMatch(hd.DiaSemana, now.Date))
                            ?? detallesPts.First();

                        var range = BuildScheduledRange(detalleBase, now.Date);
                        return new ShiftContext
                        {
                            HasAssignedShift = true,
                            HasActiveSchedule = true,
                            ScheduleDetail = detalleBase,
                            ScheduledStart = range.scheduledStart,
                            ScheduledEnd = range.scheduledEnd,
                            WindowStart = range.scheduledStart.Subtract(EarlyWindowTolerance),
                            WindowEnd = range.scheduledEnd.Add(LateWindowTolerance),
                            FuenteHorario = "PTS_FALLBACK"
                        };
                    }
                }
            }

            // ── PASO 2: ASIGNACIONES_TURNO base ─────────────────────────
            // Aplica cuando:
            //   - FIJOS que no tienen PTS explícita (su horario es siempre el mismo)
            //   - ROTATIVOS cuya semana aún no fue programada en PTS
            var asignacion = await _context.AsignacionesTurno
                .Include(a => a.HorarioTurno)
                    .ThenInclude(ht => ht!.HorariosDetalle)
                .Include(a => a.Turno)
                    .ThenInclude(t => t!.HorariosTurno!)
                        .ThenInclude(ht => ht.HorariosDetalle)
                .FirstOrDefaultAsync(a =>
                    a.TrabajadorId == trabajadorId &&
                    a.FechaInicioVigencia <= hoyDateOnly &&
                    (a.FechaFinVigencia == null || a.FechaFinVigencia >= hoyDateOnly) &&
                    a.EsVigente == true);

            if (asignacion == null)
            {
                return new ShiftContext
                {
                    HasAssignedShift = false,
                    HasActiveSchedule = false,
                    FuenteHorario = "SIN_ASIGNACION"
                };
            }

            // Obtener el HorarioTurno correcto:
            // Primero usar el HorarioTurnoId directo de la asignación (más específico),
            // si no hay, usar el primero activo del turno.
            var horarioTurnoBase = asignacion.HorarioTurno    // nav directo por HorarioTurnoId
                ?? asignacion.Turno?.HorariosTurno
                    ?.FirstOrDefault(ht => ht.EsActivo == true);

            if (horarioTurnoBase?.HorariosDetalle == null || !horarioTurnoBase.HorariosDetalle.Any())
            {
                return new ShiftContext
                {
                    HasAssignedShift = true,
                    HasActiveSchedule = false,
                    FuenteHorario = "ASIGNACION_SIN_DETALLE"
                };
            }

            // Intentar match de ventana con los detalles del horario base
            foreach (var detalle in horarioTurnoBase.HorariosDetalle)
            {
                if (TryMatchHorarioDetalleWindow(
                        detalle, now,
                        EarlyWindowTolerance, LateWindowTolerance,
                        out var windowStart, out var windowEnd))
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
                        WindowEnd = windowEnd,
                        FuenteHorario = "ASIGNACION_BASE"
                    };
                }
            }

            // Fuera de ventana pero con fallback → usar el detalle del día
            if (includeTodayFallback)
            {
                var detalleHoy = horarioTurnoBase.HorariosDetalle
                    .FirstOrDefault(hd => IsDiaSemanaMatch(hd.DiaSemana, now.Date));
                if (detalleHoy != null)
                {
                    var scheduledRange = BuildScheduledRange(detalleHoy, now.Date);
                    return new ShiftContext
                    {
                        HasAssignedShift = true,
                        HasActiveSchedule = true,
                        ScheduleDetail = detalleHoy,
                        ScheduledStart = scheduledRange.scheduledStart,
                        ScheduledEnd = scheduledRange.scheduledEnd,
                        WindowStart = scheduledRange.scheduledStart.Subtract(EarlyWindowTolerance),
                        WindowEnd = scheduledRange.scheduledEnd.Add(LateWindowTolerance),
                        FuenteHorario = "ASIGNACION_FALLBACK"
                    };
                }
            }

            // ── PASO 3: Horario genérico por defecto ────────────────────
            if (includeDefaultFallback)
            {
                var defaultStart = now.Date.Add(DefaultStartTime);
                var defaultEnd = now.Date.Add(DefaultEndTime);
                return new ShiftContext
                {
                    HasAssignedShift = true,
                    HasActiveSchedule = true,
                    ScheduledStart = defaultStart,
                    ScheduledEnd = defaultEnd,
                    WindowStart = now.Date,
                    WindowEnd = now.Date.AddDays(1).AddTicks(-1),
                    FuenteHorario = "DEFAULT"
                };
            }

            return new ShiftContext
            {
                HasAssignedShift = true,
                HasActiveSchedule = true,
                FuenteHorario = "SIN_VENTANA"
            };
        }

        // ══════════════════════════════════════════════════════════════
        // GET ALL (paginado)
        // ══════════════════════════════════════════════════════════════

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
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            };
        }

        // ══════════════════════════════════════════════════════════════
        // ADD MARCACION (ENTRADA / SALIDA automática)
        // ══════════════════════════════════════════════════════════════

        public async Task<MarcacionResponse> AddMarcacionAsync(MarcacionRequest marcacionRequest)
        {
            // Todas las validaciones han sido comentadas para permitir marcar sin restricciones.
            var now = DateTime.Now;

             //── 1.Resolver contexto de horario ─────────────────────────
             var shiftContext = await ResolveShiftContextAsync(
                 marcacionRequest.IdTrabajador,
                 now,
                 includeTodayFallback: true,
                 includeDefaultFallback: false);

            if (!shiftContext.HasAssignedShift)
            {
                return new MarcacionResponse
                {
                    Success = false,
                    Code = "ERROR_NO_TURNO",
                    Message = "No tiene un turno asignado para la fecha actual.",
                    Detail = $"Verifique asignación de turno del trabajador. [Fuente: {shiftContext.FuenteHorario}]"
                };
            }

            // ── 2. Cargar trabajador y sucursal ──────────────────────────
            var trabajador = await _context.Trabajadores
                .Include(t => t.Sucursal)
                .Include(t => t.TrabajadorSucursales!)
                    .ThenInclude(ts => ts.Sucursal)
                .FirstOrDefaultAsync(t => t.Id == marcacionRequest.IdTrabajador);

            if (trabajador == null)
            {
                return new MarcacionResponse
                {
                    Success = false,
                    Code = "ERROR_TRABAJADOR_NO_ENCONTRADO",
                    Message = "Trabajador no encontrado.",
                    Detail = "Confirme que el IdTrabajador es correcto."
                };
            }

            if (trabajador.Sucursal == null && trabajador.TrabajadorSucursales?.Any() != true)
            {
                return new MarcacionResponse
                {
                    Success = false,
                    Code = "ERROR_SIN_SEDE",
                    Message = "El trabajador no tiene ninguna sede asignada.",
                    Detail = "Asigne al menos una sede al trabajador antes de registrar asistencia."
                };
            }

            if (!shiftContext.HasActiveSchedule)
            {
                return new MarcacionResponse
                {
                    Success = false,
                    Code = "ERROR_SIN_HORARIO",
                    Message = "No tiene un horario definido para la fecha actual.",
                    Detail = $"Revise la configuración de horarios para el turno asignado. [Fuente: {shiftContext.FuenteHorario}]"
                };
            }

            if (!shiftContext.WindowStart.HasValue || !shiftContext.WindowEnd.HasValue)
            {
                return new MarcacionResponse
                {
                    Success = false,
                    Code = "ERROR_SIN_HORARIO",
                    Message = "No tiene un horario definido para la fecha actual.",
                    Detail = "Revise la configuración de horarios para el turno asignado o márquee dentro de la ventana permitida."
                };
            }

            var windowStart = shiftContext.WindowStart.Value;
            var windowEnd = shiftContext.WindowEnd.Value;

             //── 3.Validar geolocalización ───────────────────────────────
             var (geofenceSucursal, distancia, ubicacionValida) = ResolverSucursalPorUbicacion(
                trabajador, marcacionRequest.Latitud, marcacionRequest.Longitud);

            // Bloquear GPS falso solo si el trabajador tiene validación de zona activa
            if (marcacionRequest.EsMockLocation == true && trabajador.MarcajeEnZona)
            {
                return new MarcacionResponse
                {
                    Success = false,
                    Code = "ERROR_GPS_FALSO",
                    Message = "Se detectó una ubicación simulada. No se permite marcar con GPS falso.",
                    Detail = "Desactiva cualquier app de ubicación simulada y vuelve a intentarlo."
                };
            }

            if (!ubicacionValida && trabajador.MarcajeEnZona)
            {
                return new MarcacionResponse
                {
                    Success = false,
                    Code = "ERROR_FUERA_ZONA",
                    Message = "No se encuentra dentro del área de ninguna sede asignada.",
                    Detail = $"Sede más cercana: \"{geofenceSucursal.NombreSucursal}\" a {distancia:F0} m."
                };
            }

             //── 4.Determinar tipo de marcación(ENTRADA / SALIDA) ───────
             var existingMarksToday = await _context.MarcacionesAsistencia
                 .Where(m =>
                     m.TrabajadorId == marcacionRequest.IdTrabajador &&
                     m.FechaHora >= windowStart &&
                     m.FechaHora <= windowEnd)
                 .OrderBy(m => m.FechaHora)
                 .ToListAsync();

            string tipoMarcacion;
            if (!existingMarksToday.Any())
            {
                tipoMarcacion = "ENTRADA";
            }
            else
            {
                var lastMark = existingMarksToday.Last();
                if (string.Equals(lastMark.TipoMarcacion, "ENTRADA", StringComparison.OrdinalIgnoreCase))
                {
                    tipoMarcacion = "SALIDA";
                }
                else
                {
                    return new MarcacionResponse
                    {
                        Success = false,
                        Code = "ERROR_SALIDA_REGISTRADA",
                        Message = "Ya registró su salida hoy; no se permite nueva entrada sin configuración de turnos adicionales.",
                        Detail = "Si corresponde, configure turnos adicionales o permita reingresos."
                    };
                }
            }

             //── 5.Evitar duplicados recientes(ventana 2 minutos) ───────
             var existeMarcacionIgual = existingMarksToday.Any(m =>
                 m.TipoMarcacion == tipoMarcacion &&
                 Math.Abs((m.FechaHora - now).TotalSeconds) < 120);

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

            // ── 6. Guardar marcación ─────────────────────────────────────
            // SucursalId: sede de pertenencia del trabajador (para reportes y agrupación).
            // Si no tiene sede principal definida, se usa la sede donde marcó como fallback.
            var sucursalPertenencia = trabajador.SucursalId ?? geofenceSucursal.Id;

            var nuevaMarcacion = new MarcacionAsistencia
            {
                TrabajadorId = marcacionRequest.IdTrabajador,
                SucursalId = sucursalPertenencia,             // sede de pertenencia (reportes)
                SucursalMarcacionId = geofenceSucursal.Id,    // sede donde marcó físicamente (auditoría)
                FechaHora = now,
                Latitud = (decimal)marcacionRequest.Latitud,
                Longitud = (decimal)marcacionRequest.Longitud,
                TipoMarcacion = tipoMarcacion,
                FotoUrl = marcacionRequest.FotoUrl,
                UbicacionValida = ubicacionValida,
                EsMockLocation = marcacionRequest.EsMockLocation
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

        // ══════════════════════════════════════════════════════════════
        // CALCULATE TIME WORKED
        // ══════════════════════════════════════════════════════════════

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

            var marks = await _context.MarcacionesAsistencia
                .Where(m =>
                    m.TrabajadorId == trabajadorId &&
                    m.FechaHora >= windowStart &&
                    m.FechaHora <= windowEnd)
                .OrderBy(m => m.FechaHora)
                .ToListAsync();

            var lastEntry = marks.FirstOrDefault(m =>
                string.Equals(m.TipoMarcacion, "ENTRADA", StringComparison.OrdinalIgnoreCase));

            var lastExit = lastEntry == null
                ? null
                : marks.LastOrDefault(m =>
                    string.Equals(m.TipoMarcacion, "SALIDA", StringComparison.OrdinalIgnoreCase) &&
                    m.FechaHora > lastEntry.FechaHora);

            TimeSpan timeWorked;
            if (lastEntry == null)
            {
                timeWorked = TimeSpan.Zero;
            }
            else if (lastExit != null && lastExit.FechaHora > lastEntry.FechaHora)
            {
                timeWorked = lastExit.FechaHora - lastEntry.FechaHora;
            }
            else
            {
                timeWorked = now - lastEntry.FechaHora;
            }

            return new TimeWorkedDto
            {
                ScheduledTime = $"({scheduledStart:HH:mm} - {scheduledEnd:HH:mm})",
                TimeWorkedMinutes = timeWorked.TotalMinutes,
                TimeWorkedFormatted = $"{Math.Floor(timeWorked.TotalHours)}h {timeWorked.Minutes}m",
                EntryRegisteredAt = lastEntry?.FechaHora,
                ExitRegisteredAt = lastExit?.FechaHora,
                StatusMessage = $"Cálculo realizado correctamente. [Fuente: {shiftContext.FuenteHorario}]"
            };
        }

        // ══════════════════════════════════════════════════════════════
        // GET MARCADO (pendiente de implementar)
        // ══════════════════════════════════════════════════════════════

        public Task<MarcacionAsistencia> getMarcadoAsistenciaAsync(int trabajadorId)
        {
            throw new NotImplementedException();
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS PRIVADOS
        // ══════════════════════════════════════════════════════════════

        private double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3; // Radio de la Tierra en metros
            var φ1 = lat1 * Math.PI / 180;
            var φ2 = lat2 * Math.PI / 180;
            var Δφ = (lat2 - lat1) * Math.PI / 180;
            var Δλ = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                     Math.Cos(φ1) * Math.Cos(φ2) *
                     Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        /// <summary>
        /// Evalúa automáticamente en cuál sede asignada está el trabajador según su GPS.
        /// - Si está dentro del geofence de alguna → usa la más cercana de esas.
        /// - Si está fuera de todas → devuelve la más cercana con dentroDeZona=false.
        /// </summary>
        private (SucursalCentro sucursal, double distanciaM, bool dentroDeZona) ResolverSucursalPorUbicacion(
            Trabajador trabajador, double lat, double lon)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Construir lista de sedes activas
            var sedes = new List<SucursalCentro>();

            if (trabajador.Sucursal != null)
                sedes.Add(trabajador.Sucursal);

            if (trabajador.TrabajadorSucursales != null)
            {
                var adicionales = trabajador.TrabajadorSucursales
                    .Where(ts =>
                        ts.SucursalId != trabajador.SucursalId &&
                        ts.FechaInicio <= today &&
                        (ts.FechaFin == null || ts.FechaFin.Value >= today) &&
                        ts.Sucursal != null)
                    .Select(ts => ts.Sucursal!);
                sedes.AddRange(adicionales);
            }

            SucursalCentro? mejorSede = null;
            double mejorDistancia = double.MaxValue;
            bool dentroDeZona = false;

            foreach (var sede in sedes)
            {
                if (sede.LatitudCentro == null || sede.LongitudCentro == null) continue;

                var dist = CalcularDistancia(lat, lon,
                    (double)sede.LatitudCentro, (double)sede.LongitudCentro);

                var perimetro = sede.PerimetroM ?? 0;
                var enEstaSede = dist <= perimetro;

                if (enEstaSede && (!dentroDeZona || dist < mejorDistancia))
                {
                    // Dentro de esta sede y es la más cercana encontrada dentro
                    mejorSede = sede;
                    mejorDistancia = dist;
                    dentroDeZona = true;
                }
                else if (!dentroDeZona && dist < mejorDistancia)
                {
                    // Aún no está en ninguna, guardar la más cercana
                    mejorSede = sede;
                    mejorDistancia = dist;
                }
            }

            var sucursalFinal = mejorSede ?? trabajador.Sucursal!;
            var distanciaFinal = mejorSede != null
                ? mejorDistancia
                : (trabajador.Sucursal != null
                    ? CalcularDistancia(lat, lon,
                        (double)(trabajador.Sucursal.LatitudCentro ?? 0),
                        (double)(trabajador.Sucursal.LongitudCentro ?? 0))
                    : 0);

            return (sucursalFinal, distanciaFinal, dentroDeZona);
        }

        /// <summary>
        /// Verifica si el campo DiaSemana del HorarioDetalle corresponde a la fecha dada.
        /// Soporta: número ('1'..'7'), nombre ('Lunes','Mon'), rango ('1-5','Lun-Vie'),
        /// lista ('1,2,3'), y combinaciones de los anteriores separados por coma/punto y coma.
        /// </summary>
        private bool IsDiaSemanaMatch(string diaSemanaRaw, DateTime fecha)
        {
            if (string.IsNullOrWhiteSpace(diaSemanaRaw)) return false;

            var parts = diaSemanaRaw
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p));

            // ISO: Lunes=1 ... Domingo=7
            var isoDay = fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;

            foreach (var part in parts)
            {
                if (part.Contains('-'))
                {
                    var range = part.Split('-', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim()).ToArray();
                    if (range.Length != 2) continue;
                    var start = ParseDayTokenToIso(range[0]);
                    var end = ParseDayTokenToIso(range[1]);
                    if (start == null || end == null) continue;
                    if (start <= end)
                    {
                        if (isoDay >= start && isoDay <= end) return true;
                    }
                    else // wrap-around, ej: Sáb-Mar (6-2)
                    {
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

        /// <summary>
        /// Convierte tokens de día ('1', 'Mon', 'Lun', 'Monday', 'Lunes') a número ISO 1..7.
        /// </summary>
        private int? ParseDayTokenToIso(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            token = token.Trim();

            if (int.TryParse(token, out var n))
            {
                if (n >= 0 && n <= 6) return n == 0 ? 7 : n; // 0(Sun)→7
                if (n >= 1 && n <= 7) return n;
            }

            return token.ToLowerInvariant() switch
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

        /// <summary>
        /// Determina si el HorarioDetalle aplica para 'now' según su ventana de marcación.
        /// Prueba el día actual, el anterior (marcas nocturnas después de medianoche)
        /// y el siguiente (para entradas muy anticipadas).
        /// </summary>
        private bool TryMatchHorarioDetalleWindow(
            HorarioDetalle hd, DateTime now,
            TimeSpan earlyWindow, TimeSpan lateWindow,
            out DateTime windowStart, out DateTime windowEnd)
        {
            windowStart = DateTime.MinValue;
            windowEnd = DateTime.MinValue;

            bool isOvernight = hd.SalidaDiaSiguiente || hd.HoraFin < hd.HoraInicio;

            // Día actual
            if (IsDiaSemanaMatch(hd.DiaSemana, now))
            {
                var s = now.Date.Add(hd.HoraInicio);
                var e = isOvernight ? now.Date.AddDays(1).Add(hd.HoraFin) : now.Date.Add(hd.HoraFin);
                var ws = s.Subtract(earlyWindow);
                var we = e.Add(lateWindow);
                if (now >= ws && now <= we) { windowStart = ws; windowEnd = we; return true; }
            }

            // Día anterior (marcaciones nocturnas después de medianoche)
            var prev = now.Date.AddDays(-1);
            if (IsDiaSemanaMatch(hd.DiaSemana, prev))
            {
                var s = prev.Add(hd.HoraInicio);
                var e = isOvernight ? prev.AddDays(1).Add(hd.HoraFin) : prev.Add(hd.HoraFin);
                var ws = s.Subtract(earlyWindow);
                var we = e.Add(lateWindow);
                if (now >= ws && now <= we) { windowStart = ws; windowEnd = we; return true; }
            }

            // Día siguiente (entradas muy anticipadas, ej: 22:00 para turno que empieza a 00:00)
            var next = now.Date.AddDays(1);
            if (IsDiaSemanaMatch(hd.DiaSemana, next))
            {
                var s = next.Add(hd.HoraInicio);
                var e = isOvernight ? next.AddDays(1).Add(hd.HoraFin) : next.Add(hd.HoraFin);
                var ws = s.Subtract(earlyWindow);
                var we = e.Add(lateWindow);
                if (now >= ws && now <= we) { windowStart = ws; windowEnd = we; return true; }
            }

            return false;
        }
    }
}