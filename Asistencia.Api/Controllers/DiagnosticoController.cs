using Asistencia.Data.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Asistencia.Api.Controllers
{
    /// <summary>
    /// Herramientas de diagnóstico para RRHH: por qué un trabajador (por DNI)
    /// no puede iniciar sesión o no puede marcar, y qué horario tiene asignado.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
    [Route("api/Rrhh/[controller]")]
    public class DiagnosticoController : ControllerBase
    {
        private readonly MarcacionAsistenciaDbContext _context;

        public DiagnosticoController(MarcacionAsistenciaDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: api/Rrhh/Diagnostico/login?dni=12345678
        // ¿Por qué una persona no puede loguear?
        // ============================================================
        [HttpGet("login")]
        public async Task<IActionResult> DiagnosticoLogin([FromQuery] string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
                return BadRequest(new { message = "DNI requerido." });
            dni = dni.Trim();

            var persona = await _context.Personas.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Dni == dni);

            if (persona == null)
                return Ok(new
                {
                    dni,
                    puedeLoguear = false,
                    motivo = "DNI no registrado en el sistema.",
                    recomendacion = "Registrar a la persona / trabajador.",
                    persona = (object?)null
                });

            var trabajador = await _context.Trabajadores.AsNoTracking()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.PersonaId == persona.Id);

            if (trabajador == null)
                return Ok(new
                {
                    dni,
                    puedeLoguear = false,
                    motivo = "La persona existe pero no está registrada como trabajador.",
                    recomendacion = "Crear el trabajador asociado a esta persona.",
                    persona = new { persona.Id, persona.ApellidosNombres, persona.Dni }
                });

            if (trabajador.UserId == null || trabajador.User == null)
                return Ok(new
                {
                    dni,
                    puedeLoguear = false,
                    motivo = "El trabajador no tiene un usuario de acceso creado.",
                    recomendacion = "Crear un usuario para el trabajador (módulo Trabajadores → Usuario y acceso).",
                    persona = new { persona.Id, persona.ApellidosNombres, persona.Dni },
                    trabajador = new { trabajador.Id, trabajador.IdEstado }
                });

            var user = trabajador.User;
            var estadoTexto = trabajador.IdEstado == 10 ? "ACTIVO" : (trabajador.IdEstado == 11 ? "CESADO" : $"ESTADO {trabajador.IdEstado}");

            // Últimos intentos de login fallidos para el username
            var ultimosFallos = await _context.AuditLogins.AsNoTracking()
                .Where(a => a.UsernameIntentado == user.Username && a.Resultado == "FAIL")
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new { a.CreatedAt, a.MotivoFallo, a.IpAddress })
                .ToListAsync();

            var ultimoOk = await _context.AuditLogins.AsNoTracking()
                .Where(a => a.UsernameIntentado == user.Username && a.Resultado == "OK")
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => (DateTime?)a.CreatedAt)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                dni,
                puedeLoguear = true,
                motivo = $"El usuario existe (username: {user.Username}, rol: {user.Role}). " +
                         "Si el inicio de sesión falla, lo más probable es contraseña incorrecta. " +
                         "Use el módulo Trabajadores → Usuario y acceso para restablecerla.",
                recomendacion = trabajador.IdEstado != 10
                    ? "OJO: el trabajador está CESADO/INACTIVO; puede iniciar sesión pero no podrá marcar asistencia."
                    : "Si olvidó su contraseña, restablézcala desde el módulo de Trabajadores.",
                persona = new { persona.Id, persona.ApellidosNombres, persona.Dni },
                usuario = new { user.Id, user.Username, user.Role, user.Email },
                estadoTrabajador = estadoTexto,
                ultimoLoginExitoso = ultimoOk,
                ultimosIntentosFallidos = ultimosFallos
            });
        }

        // ============================================================
        // GET: api/Rrhh/Diagnostico/marcacion?dni=12345678&fecha=2026-06-09
        // ¿Por qué no puede marcar? ¿Qué horario tiene ese día?
        // ============================================================
        [HttpGet("marcacion")]
        public async Task<IActionResult> DiagnosticoMarcacion([FromQuery] string dni, [FromQuery] DateOnly? fecha = null)
        {
            if (string.IsNullOrWhiteSpace(dni))
                return BadRequest(new { message = "DNI requerido." });
            dni = dni.Trim();

            var fechaConsulta = fecha ?? DateOnly.FromDateTime(DateTime.Today);
            var diaSemana = DiaSemanaEs(fechaConsulta);

            var trabajador = await _context.Trabajadores.AsNoTracking()
                .Include(t => t.Persona)
                .Include(t => t.Sucursal)
                .Include(t => t.TrabajadorSucursales!)
                .FirstOrDefaultAsync(t => t.Persona.Dni == dni);

            if (trabajador == null)
                return Ok(new
                {
                    dni,
                    fecha = fechaConsulta,
                    puedeMarcar = false,
                    motivo = "No existe un trabajador con ese DNI.",
                    recomendacion = "Verifique el DNI o registre al trabajador."
                });

            var baseInfo = new
            {
                trabajadorId = trabajador.Id,
                nombre = trabajador.Persona?.ApellidosNombres,
                dni,
                fecha = fechaConsulta,
                diaSemana,
                sede = trabajador.Sucursal?.NombreSucursal,
                marcajeEnZona = trabajador.MarcajeEnZona,
                tomarFoto = trabajador.TomarFoto
            };

            // 1) Estado del trabajador
            if (trabajador.IdEstado != 10)
                return Ok(Merge(baseInfo, new
                {
                    puedeMarcar = false,
                    motivo = "El trabajador está CESADO/INACTIVO (id_estado != 10).",
                    recomendacion = "Reactivar al trabajador si debe marcar."
                }));

            // 2) Sede
            var tieneSede = trabajador.SucursalId != null || (trabajador.TrabajadorSucursales?.Any() ?? false);
            if (!tieneSede)
                return Ok(Merge(baseInfo, new
                {
                    puedeMarcar = false,
                    motivo = "El trabajador no tiene ninguna sede asignada.",
                    recomendacion = "Asignar una sede principal al trabajador."
                }));

            // 3) Programación del día (PTS) — ausencias / descanso / vacaciones
            var pts = await _context.ProgramacionTurnosSemanal.AsNoTracking()
                .Include(p => p.HorarioTurno)!.ThenInclude(h => h!.HorariosDetalle)
                .FirstOrDefaultAsync(p => p.TrabajadorId == trabajador.Id && p.Fecha == fechaConsulta);

            if (pts != null)
            {
                if (!string.IsNullOrWhiteSpace(pts.TipoAusencia))
                    return Ok(Merge(baseInfo, new
                    {
                        puedeMarcar = false,
                        motivo = $"Ese día tiene una ausencia programada: {pts.TipoAusencia}.",
                        recomendacion = "Si debe trabajar, retirar la ausencia en Programación."
                    }));

                if (pts.EsVacaciones)
                    return Ok(Merge(baseInfo, new
                    {
                        puedeMarcar = false,
                        motivo = "Ese día está de VACACIONES.",
                        recomendacion = "Si debe trabajar, ajustar la programación del día."
                    }));

                if (pts.EsDescanso && !pts.EsDescansoLaborado)
                    return Ok(Merge(baseInfo, new
                    {
                        puedeMarcar = false,
                        motivo = "Ese día es DESCANSO.",
                        recomendacion = "Si va a trabajar su descanso, marcar el día como 'Descanso laborado' en Programación / Horario Semanal."
                    }));
            }

            // 4) Resolver horario del día: PTS.id_horario_turno o el de la asignación vigente
            var asignacion = await _context.AsignacionesTurno.AsNoTracking()
                .Include(a => a.HorarioTurno)!.ThenInclude(h => h!.HorariosDetalle)
                .Where(a => a.TrabajadorId == trabajador.Id && a.EsVigente
                            && fechaConsulta >= a.FechaInicioVigencia
                            && (a.FechaFinVigencia == null || fechaConsulta <= a.FechaFinVigencia))
                .FirstOrDefaultAsync();

            int? horarioTurnoId = pts?.IdHorarioTurno ?? asignacion?.HorarioTurnoId;
            string fuente = pts?.IdHorarioTurno != null ? "PROGRAMACION_SEMANAL"
                           : (asignacion?.HorarioTurnoId != null ? "ASIGNACION_TURNO" : "NINGUNA");

            if (horarioTurnoId == null && asignacion == null)
                return Ok(Merge(baseInfo, new
                {
                    puedeMarcar = false,
                    motivo = "No tiene un turno/asignación vigente para esa fecha.",
                    recomendacion = "Asignar un turno al trabajador (vigente para la fecha)."
                }));

            // Buscar el HorarioDetalle del día de la semana
            var detalle = await _context.HorariosDetalle.AsNoTracking()
                .FirstOrDefaultAsync(d => d.HorarioTurnoId == horarioTurnoId && d.DiaSemana == diaSemana);

            if (detalle == null)
                return Ok(Merge(baseInfo, new
                {
                    puedeMarcar = false,
                    motivo = $"No tiene un horario configurado para el día {diaSemana}.",
                    recomendacion = "Revisar el HorarioTurno asignado: falta el detalle para ese día de la semana.",
                    horarioTurnoId,
                    fuenteHorario = fuente
                }));

            // Marcaciones ya registradas ese día
            var marcas = await _context.MarcacionesAsistencia.AsNoTracking()
                .Where(m => m.TrabajadorId == trabajador.Id && m.FechaHora.Date == fechaConsulta.ToDateTime(TimeOnly.MinValue).Date)
                .OrderBy(m => m.FechaHora)
                .Select(m => new { m.TipoMarcacion, m.FechaHora })
                .ToListAsync();

            var entradaTeorica = fechaConsulta.ToDateTime(TimeOnly.FromTimeSpan(detalle.HoraInicio));
            var salidaBase = fechaConsulta.ToDateTime(TimeOnly.FromTimeSpan(detalle.HoraFin));
            var salidaTeorica = detalle.SalidaDiaSiguiente ? salidaBase.AddDays(1) : salidaBase;

            var puedeMarcar = true;
            var motivo = pts?.EsDescansoLaborado == true
                ? "Día de DESCANSO LABORADO: el trabajador puede marcar con el horario indicado."
                : "El trabajador tiene horario y puede marcar.";
            var notaZona = trabajador.MarcajeEnZona
                ? "Requiere estar dentro de la zona/GPS de su sede; si está fuera o usa GPS falso, la marcación será rechazada."
                : "No requiere validación de zona (puede marcar desde cualquier ubicación).";

            return Ok(Merge(baseInfo, new
            {
                puedeMarcar,
                motivo,
                notaZona,
                horario = new
                {
                    horarioTurnoId,
                    nombreHorario = (pts?.HorarioTurno ?? asignacion?.HorarioTurno)?.NombreHorario,
                    fuente,
                    diaSemana,
                    entradaTeorica,
                    salidaTeorica,
                    salidaDiaSiguiente = detalle.SalidaDiaSiguiente,
                    esDescansoLaborado = pts?.EsDescansoLaborado ?? false
                },
                marcacionesDelDia = marcas
            }));
        }

        // ── Helpers ──────────────────────────────────────────────
        private static string DiaSemanaEs(DateOnly fecha)
        {
            return fecha.DayOfWeek switch
            {
                DayOfWeek.Monday => "LUNES",
                DayOfWeek.Tuesday => "MARTES",
                DayOfWeek.Wednesday => "MIERCOLES",
                DayOfWeek.Thursday => "JUEVES",
                DayOfWeek.Friday => "VIERNES",
                DayOfWeek.Saturday => "SABADO",
                _ => "DOMINGO"
            };
        }

        // Combina dos objetos anónimos en un diccionario para la respuesta.
        private static object Merge(object a, object b)
        {
            var dict = new System.Collections.Generic.Dictionary<string, object?>();
            foreach (var p in a.GetType().GetProperties()) dict[p.Name] = p.GetValue(a);
            foreach (var p in b.GetType().GetProperties()) dict[p.Name] = p.GetValue(b);
            return dict;
        }
    }
}
