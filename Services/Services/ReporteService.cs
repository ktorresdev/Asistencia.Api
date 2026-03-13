using Asistencia.Data.DbContexts;
using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Services.Implements;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Asistencia.Services.Services
{
    public class ReporteService : IReportesService
    {
        private readonly ILogger _logger;
        private readonly MarcacionAsistenciaDbContext _context;
        private readonly string _connectionString;

        public ReporteService(
            ILogger<ReporteService> logger,
            MarcacionAsistenciaDbContext context,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _context = context;
            _connectionString = configuration.GetConnectionString("RrhhConnection")
                ?? throw new InvalidOperationException("Connection string 'RrhhConnection' no encontrada.");
        }

        public async Task<List<ResponseHorasExtraDto>> GetHorasExtra(FiltroReporteDto filtro)
        {
            //// Filtro por trabajador es recomendado para este reporte
            //if (!filtro.IdTrabajador.HasValue)
            //{
            //    return new List<ResponseHorasExtraDto>();
            //}

            //var marcaciones = await _context.MarcacionesAsistencia
            //    .Where(m => m.TrabajadorId == filtro.IdTrabajador.Value &&
            //                m.FechaHora.Date >= filtro.FechaInicio.Date &&
            //                m.FechaHora.Date <= filtro.FechaFin.Date)
            //    .OrderBy(m => m.FechaHora)
            //    .ToListAsync();

            //var asignaciones = await _context.AsignacionesTurno
            //    .Include(a => a.Turno.HorariosTurno).ThenInclude(ht => ht.HorariosDetalle)
            //    .Where(a => a.TrabajadorId == filtro.IdTrabajador.Value && a.EsVigente)
            //    .ToListAsync();

            //var trabajador = await _context.Trabajadores.FindAsync(filtro.IdTrabajador.Value);
            //if (trabajador == null || !trabajador.HorasExtraConf)
            //{
            //    // Si el trabajador no está configurado para horas extra, no se reporta nada.
            //    return new List<ResponseHorasExtraDto>();
            //}

            //var resultados = new List<ResponseHorasExtraDto>();
            //var marcacionesPorDia = marcaciones.Where(m => m != null).GroupBy(m => m.FechaHora.Date);

            //foreach (var grupo in marcacionesPorDia)
            //{
            //    var fecha = grupo.Key;
            //    var marcacionSalida = grupo.LastOrDefault(m => m != null && m.TipoMarcacion == "Salida");

            //    if (marcacionSalida == null) continue;

            //    var detalleHorario = GetDetalleHorarioParaFecha(asignaciones, fecha);
            //    if (detalleHorario == null) continue;

            //    var horaFinProgramada = detalleHorario.HoraFin.ToTimeSpan;
            //    var horaSalidaMarcada = marcacionSalida.FechaHora.TimeOfDay;

            //    if (horaSalidaMarcada > horaFinProgramada)
            //    {
            //        var horasExtra = horaSalidaMarcada - horaFinProgramada;
            //        resultados.Add(new ResponseHorasExtraDto
            //        {
            //            Fecha = fecha,
            //            HoraSalidaProgramada = horaFinProgramada,
            //            HoraSalidaMarcada = horaSalidaMarcada,
            //            MinutosExtra = (int)horasExtra.TotalMinutes
            //        });
            //    }
            //}

            return new List<ResponseHorasExtraDto>();//resultados;
        }

        public async Task<List<ResponseTardanzasDto>> GetTardanzas(FiltroReporteDto filtro)
        {
            //var query = from marcacion in _context.MarcacionesAsistencia
            //            join trabajador in _context.Trabajadores on marcacion.TrabajadorId equals trabajador.Id
            //            join persona in _context.Personas on trabajador.PersonaId equals persona.Id
            //            where marcacion.TipoMarcacion == "Entrada" &&
            //                  marcacion.FechaHora.Date >= filtro.FechaInicio.Date &&
            //                  marcacion.FechaHora.Date <= filtro.FechaFin.Date &&
            //                  (!filtro.IdTrabajador.HasValue || trabajador.Id == filtro.IdTrabajador.Value)
            //            select new { marcacion, trabajador, persona };

            //var marcacionesEntrada = await query.ToListAsync();
            //var resultados = new List<ResponseTardanzasDto>();

            //foreach (var item in marcacionesEntrada)
            //{
            //    var asignacion = await _context.AsignacionesTurno
            //        .Include(a => a.Turno.HorariosTurno).ThenInclude(ht => ht.HorariosDetalle)
            //        .FirstOrDefaultAsync(a => a.TrabajadorId == item.trabajador.Id &&
            //                                  a.FechaInicioVigencia.Date <= item.marcacion.FechaHora.Date &&
            //                                  (a.FechaFinVigencia == null || a.FechaFinVigencia.Value.Date >= item.marcacion.FechaHora.Date) &&
            //                                  a.EsVigente);

            //    // Si no hay asignación no podemos seguir con este registro
            //    if (asignacion == null) continue;

            //    var detalleHorario = GetDetalleHorarioParaFecha(new List<Asistencia.Data.Entities.MarcacionAsistenciaEntites.AsignacionTurno> { asignacion }, item.marcacion.FechaHora);
            //    if (detalleHorario == null) continue;

            //    // Usar operadores seguros por si faltan valores en Turno
            //    var tolerancia = TimeSpan.FromMinutes(asignacion.Turno?.ToleranciaIngreso ?? 0);
            //    var horaEntradaProgramada = detalleHorario.HoraInicio.ToTimeSpan();
            //    var horaLimite = horaEntradaProgramada.Add(tolerancia);

            //    if (item.marcacion.FechaHora.TimeOfDay > horaLimite)
            //    {
            //        var minutosTarde = (item.marcacion.FechaHora.TimeOfDay - horaEntradaProgramada).TotalMinutes;
            //        resultados.Add(new ResponseTardanzasDto
            //        {
            //            //NombreTrabajador = item.persona.ApellidosNombres,
            //            //Fecha = item.marcacion.FechaHora.Date,
            //            //HoraEntradaProgramada = horaEntradaProgramada,
            //            //HoraEntradaMarcada = item.marcacion.FechaHora.TimeOfDay,
            //            //MinutosTarde = (int)minutosTarde
            //        });
            //    }
            //}
            return new List<ResponseTardanzasDto>();
        }
        public async Task<List<ResponseInasistenciasDto>> GetInasistencias(FiltroReporteDto filtro)
        {
            //// Devolver lista vacía en vez de null para evitar excepciones al consumir el resultado
            //return new List<ResponseInasistenciasDto>();
            //var resultados = new List<ResponseInasistenciasDto>();

            //try
            //{
            //    await using var connection = new SqlConnection(_connectionString);
            //    await using var command = new SqlCommand("sp_reporte_inasistencia", connection)
            //    {
            //        CommandType = CommandType.StoredProcedure
            //    };

            //    // Ajusta los nombres de parámetros según tu SP si fueran distintos
            //    command.Parameters.AddWithValue("@fechaInicio", filtro.FechaInicio);
            //    command.Parameters.AddWithValue("@fechaFin", filtro.FechaFin);

            //    await connection.OpenAsync();

            //    await using var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            //    while (await reader.ReadAsync())
            //    {
            //        // Intentar obtener columnas con distintos nombres posibles (alias en SP)
            //        var ordFechaSolicitud = "fecha";
            //        var ordFechaJustificada = "";
            //        var ordMotivoEstado = reader.GetOrdinalAny("motivoEstado", "motivo_estado", "motivo");
            //        var ordAutorizadoPor = reader.GetOrdinalAny("autorizadoPor", "autorizado_por", "usuario_autoriza");

            //        var dto = new ResponseInasistenciasDto
            //        {
            //            fechaSolicitud = ordFechaSolicitud >= 0 && !reader.IsDBNull(ordFechaSolicitud) ? reader.GetDateTime(ordFechaSolicitud) : (DateTime?)null,
            //            fechaJustificada = ordFechaJustificada >= 0 && !reader.IsDBNull(ordFechaJustificada) ? reader.GetDateTime(ordFechaJustificada) : (DateTime?)null,
            //            motivoEstado = ordMotivoEstado >= 0 && !reader.IsDBNull(ordMotivoEstado) ? reader.GetString(ordMotivoEstado) : null,
            //            autorizadoPor = ordAutorizadoPor >= 0 && !reader.IsDBNull(ordAutorizadoPor) ? reader.GetString(ordAutorizadoPor) : null
            //        };

            //        resultados.Add(dto);
            //    }

            //    return resultados;
            //}
            //catch (SqlException ex)
            //{
            //    _logger.LogError(ex, "Error SQL al ejecutar sp_reporte_inasistencia");
            //    throw;
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error inesperado al obtener inasistencias");
            //    throw;
            //}

            return new List<ResponseInasistenciasDto>();
        }

        public async Task<List<ResponseResumenDto>> GetResumen(FiltroReporteDto filtro)
        {
                var fechaInicio = filtro.FechaInicio.Date;
                var fechaFin = filtro.FechaFin.Date;

                // 1. Determinar qué trabajadores procesar
                var queryTrabajadores = _context.Trabajadores.AsQueryable();

                if (filtro.IdTrabajador.HasValue && filtro.IdTrabajador.Value != 0)
                {
                    queryTrabajadores = queryTrabajadores.Where(t => t.Id == filtro.IdTrabajador.Value);
                }

                // Proyectar a un objeto anónimo para evitar problemas con booleanos nulos
                var trabajadoresAProcesar = await queryTrabajadores
                    .Select(t => new
                    {
                        Id = t.Id,
                        Persona = t.Persona, // Asumiendo que Persona no tiene booleanos nulos problemáticos
                        Cargo = t.Cargo
                                             // Agrega aquí otras propiedades de Trabajador si las necesitas, manejando nulos
                    })
                    .ToListAsync();

                var idsTrabajadores = trabajadoresAProcesar.Select(t => t.Id).ToList();

                // 2. Obtener todos los datos necesarios en bloque para eficiencia
                var todasLasMarcaciones = await _context.MarcacionesAsistencia
                    .Where(m => idsTrabajadores.Contains(m.TrabajadorId) && m.FechaHora.Date >= fechaInicio && m.FechaHora.Date <= fechaFin)
                    .ToListAsync();

                var todasLasJustificaciones = await _context.Justificaciones
                    .Where(j => idsTrabajadores.Contains(j.TrabajadorId) && j.FechaJustificada.Date >= fechaInicio && j.FechaJustificada.Date <= fechaFin)
                    .ToListAsync();

                var todasLasHorasExtra = await _context.SolicitudesHorasExtra
                    .Where(se => idsTrabajadores.Contains(se.TrabajadorId) && se.FechaSolicitud.Date >= fechaInicio && se.FechaSolicitud.Date <= fechaFin)
                    .ToListAsync();

                var todasLasAsignaciones = await _context.AsignacionesTurno
                    .Include(a => a.Turno)
                        .ThenInclude(t => t!.HorariosTurno!)
                            .ThenInclude(ht => ht.HorariosDetalle)
                    .Where(a => idsTrabajadores.Contains(a.TrabajadorId) && a.EsVigente)
                    .ToListAsync();

                var listaResumen = new List<ResponseResumenDto>();

                // 3. Procesar cada trabajador
                foreach (var trabajador in trabajadoresAProcesar)
                {
                    // Filtrar los datos en memoria para el trabajador actual
                    var susMarcaciones = todasLasMarcaciones.Where(m => m.TrabajadorId == trabajador.Id);
                    var susAsignaciones = todasLasAsignaciones.Where(a => a.TrabajadorId == trabajador.Id);

                    // Calcular días trabajados
                    var diasTrabajados = susMarcaciones.Select(m => m.FechaHora.Date).Distinct().Count();

                    // Calcular días justificados
                    var diasJustificados = todasLasJustificaciones.Where(j => j.TrabajadorId == trabajador.Id).Select(j => j.FechaJustificada.Date).Distinct().Count();

                    // Calcular horas extra
                    var horasExtra = todasLasHorasExtra.Where(se => se.TrabajadorId == trabajador.Id).Sum(se => se.HorasSolicitadas);

                    // Calcular inasistencias
                    int diasInasistencias = 0;
                    var fechasConMarcacion = susMarcaciones.Select(m => m.FechaHora.Date).ToHashSet();

                    for (var dia = fechaInicio; dia <= fechaFin; dia = dia.AddDays(1))
                    {
                        var detalleHorario = GetDetalleHorarioParaFecha(susAsignaciones, dia);
                        if (detalleHorario != null && !fechasConMarcacion.Contains(dia))
                        {
                            diasInasistencias++;
                        }
                    }

                    listaResumen.Add(new ResponseResumenDto
                    {
                        Nombre = trabajador.Persona?.ApellidosNombres, // Ajustado a las propiedades del DTO
                        Dni = trabajador.Persona?.Dni, // Ajustado a las propiedades del DTO
                        DiasTrabajados = diasTrabajados, // Ajustado a las propiedades del DTO
                        DiasInasistencias = diasInasistencias, // Ajustado a las propiedades del DTO
                        DiasJustificados = diasJustificados, // Ajustado a las propiedades del DTO
                        HorasExtra = horasExtra, // Ajustado a las propiedades del DTO
                        CargoArea = trabajador.Cargo
                    });
                }

                return listaResumen;
            
        }

        private HorarioDetalle? GetDetalleHorarioParaFecha(IEnumerable<Asistencia.Data.Entities.MarcacionAsistenciaEntites.AsignacionTurno> asignaciones, DateTime fecha)
        {
            // 1. Validar que la lista de asignaciones no sea nula
            if (asignaciones == null) return null;

            // 2. Encontrar la asignación activa para la fecha dada
            var asignacionActiva = asignaciones.FirstOrDefault(a => a != null && a.FechaInicioVigencia.Date <= fecha.Date && (a.FechaFinVigencia == null || a.FechaFinVigencia.Value.Date >= fecha.Date));
            
            // 3. Validar la asignación y sus propiedades anidadas de forma segura
            if (asignacionActiva?.Turno?.HorariosTurno == null) return null!;

            // 4. Encontrar el horario activo dentro del turno
            var horarioTurno = asignacionActiva.Turno?.HorariosTurno?.FirstOrDefault(ht => ht.EsActivo);
            if (horarioTurno?.HorariosDetalle == null) return null!;

            // 5. Encontrar el detalle del día específico (ISO: Lunes=1, Domingo=7)
            string diaSemanaString = fecha.ToString("dddd", new System.Globalization.CultureInfo("en-US")); // 'Monday', 'Tuesday', etc.
            return horarioTurno.HorariosDetalle.FirstOrDefault(hd => hd.DiaSemana.Equals(diaSemanaString, StringComparison.OrdinalIgnoreCase))!;
        }

        public async Task<IEnumerable<ReporteInconsistenciaDto>> GetInconsistenciasAsync(string? area = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var hasAreaFilter = !string.IsNullOrWhiteSpace(area);
                var sql = @"
                    SELECT *
                    FROM dbo.VW_REPORTE_INCONSISTENCIAS
                    WHERE (@Area IS NULL OR Area = @Area)
                    ORDER BY Fecha DESC";

                return await connection.QueryAsync<ReporteInconsistenciaDto>(sql, new
                {
                    Area = hasAreaFilter ? area : null
                });

                //var query = await connection.QueryAsync<ReporteInconsistenciaDto>(sql);

                //var totalCount = query.Count();

                //var item = query
                //    .Skip(3)
                //    .Take(3)
                //    .ToList();

                //return item;
            }
        }

        public async Task<IEnumerable<ReporteAsistenciaDto>> GetResumenAsistenciaAsync(DateTime inicio, DateTime fin, string? dni, string? area)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@FechaInicio", inicio);
                parameters.Add("@FechaFin", fin);
                // Pasamos NULL si el string viene vacío o nulo
                parameters.Add("@Dni", string.IsNullOrWhiteSpace(dni) ? null : dni);
                parameters.Add("@Area", string.IsNullOrWhiteSpace(area) ? null : area);

                return await connection.QueryAsync<ReporteAsistenciaDto>(
                    "dbo.SP_REPORTE_ASISTENCIA_FILTRADO",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        public async Task<IEnumerable<ReporteTardanzaDto>> GetTardanzasAsync(DateTime inicio, DateTime fin, string? area)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@FechaInicio", inicio);
                parameters.Add("@FechaFin", fin);
                parameters.Add("@Area", string.IsNullOrWhiteSpace(area) ? null : area);

                return await connection.QueryAsync<ReporteTardanzaDto>(
                    "dbo.SP_REPORTE_DETALLE_TARDANZAS",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        public async Task<IEnumerable<ReporteHorasExtrasDto>> GetHorasExtrasAsync(DateTime inicio, DateTime fin, string? area)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@FechaInicio", inicio);
                parameters.Add("@FechaFin", fin);
                parameters.Add("@Area", string.IsNullOrWhiteSpace(area) ? null : area);

                return await connection.QueryAsync<ReporteHorasExtrasDto>(
                    "dbo.SP_REPORTE_DETALLE_HORAS_EXTRAS",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        public async Task<IEnumerable<ReporteTrabajadorJefeDto>> GetTrabajadoresPorJefeYFechaAsync(DateTime fecha, int jefeId, string? area = null)
        {
            var fechaDia = fecha.Date;
            var fechaDiaFin = fechaDia.AddDays(1);
            var areaNormalizada = string.IsNullOrWhiteSpace(area) ? null : area.Trim();

            if (areaNormalizada == null)
            {
                areaNormalizada = await _context.Trabajadores
                    .AsNoTracking()
                    .Where(t => t.Id == jefeId)
                    .Select(t => t.AreaDepartamento)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(areaNormalizada))
                {
                    areaNormalizada = areaNormalizada.Trim();
                }
            }

            var trabajadores = await _context.Trabajadores
                .AsNoTracking()
                .Include(t => t.Persona)
                .Where(t => t.JefeInmediatoId == jefeId
                    && (areaNormalizada == null || t.AreaDepartamento == areaNormalizada)
                    && (t.FechaIngreso == null || t.FechaIngreso.Value.Date <= fechaDia)
                    && (t.FechaBaja == null || t.FechaBaja.Value.Date >= fechaDia)
                    && t.IdEstado != 11)
                .Select(t => new
                {
                    t.Id,
                    Nombre = t.Persona.ApellidosNombres,
                    Dni = t.Persona.Dni
                })
                .OrderBy(t => t.Nombre)
                .ToListAsync();

            if (!trabajadores.Any())
            {
                return Enumerable.Empty<ReporteTrabajadorJefeDto>();
            }

            var trabajadoresIds = trabajadores.Select(t => t.Id).ToList();

            var marcaciones = await _context.MarcacionesAsistencia
                .AsNoTracking()
                .Where(m => trabajadoresIds.Contains(m.TrabajadorId)
                    && m.FechaHora >= fechaDia
                    && m.FechaHora < fechaDiaFin)
                .Select(m => new
                {
                    m.TrabajadorId,
                    m.TipoMarcacion,
                    m.FechaHora
                })
                .ToListAsync();

            var marcacionesPorTrabajador = marcaciones
                .ToLookup(m => m.TrabajadorId);

            var resultado = new List<ReporteTrabajadorJefeDto>(trabajadores.Count);

            foreach (var trabajador in trabajadores)
            {
                var marcasDelDia = marcacionesPorTrabajador[trabajador.Id];

                var horaEntrada = marcasDelDia
                    .Where(m => string.Equals(m.TipoMarcacion, "Entrada", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(m => m.FechaHora)
                    .Select(m => (DateTime?)m.FechaHora)
                    .FirstOrDefault();

                var horaSalida = marcasDelDia
                    .Where(m => string.Equals(m.TipoMarcacion, "Salida", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.FechaHora)
                    .Select(m => (DateTime?)m.FechaHora)
                    .FirstOrDefault();

                var estado = "Sin marcación";
                if (horaEntrada.HasValue && horaSalida.HasValue)
                {
                    estado = "Completo";
                }
                else if (horaEntrada.HasValue)
                {
                    estado = "Pendiente salida";
                }
                else if (horaSalida.HasValue)
                {
                    estado = "Salida sin entrada";
                }

                resultado.Add(new ReporteTrabajadorJefeDto
                {
                    IdTrabajador = trabajador.Id,
                    Nombre = trabajador.Nombre,
                    Dni = trabajador.Dni,
                    Entrada = horaEntrada?.ToString("HH:mm:ss"),
                    Salida = horaSalida?.ToString("HH:mm:ss"),
                    Estado = estado
                });
            }

            return resultado;
        }
    }
}