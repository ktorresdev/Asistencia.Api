using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Data.Entities.UserEntites;
using Asistencia.Data.DbContexts;
using Asistencia.Services.Dtos;
using Asistencia.Services.Implements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asistencia.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/Rrhh/[controller]")]
    public class TrabajadoresController : ControllerBase
    {
        private readonly ITrabajadorService _trabajadorService;
        private readonly MarcacionAsistenciaDbContext _context;

        public TrabajadoresController(ITrabajadorService trabajadorService, MarcacionAsistenciaDbContext context)
        {
            _trabajadorService = trabajadorService;
            _context = context;
        }

        // GET: api/Rrhh/Trabajadores
        [HttpGet]
        public async Task<ActionResult> GetAllTrabajadores(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? sucursalId = null,
            [FromQuery] string? tipo = null)
        {
            pageSize = Math.Min(pageSize, 200);

            // ADMIN (jefe local): filtra por jefatura o por sede según contexto.
            // SUPERADMIN: ve todo.
            int? jefeId = null;
            bool soloSede = false; // true = el admin está en comisión, filtra solo por SucursalId

            if (User.IsInRole("ADMIN") && !User.IsInRole("SUPERADMIN"))
            {
                var claim = User.FindFirst("trabajador_id")?.Value;
                if (!int.TryParse(claim, out var tid))
                    return Ok(new { items = Array.Empty<object>(), totalCount = 0, pageSize, currentPage = pageNumber, totalPages = 0 });

                jefeId = tid;

                // Si viene sucursalId y NO es su sede principal → es comisión → ve todos los de esa sede
                if (sucursalId.HasValue)
                {
                    var esSedePrincipal = await _context.Trabajadores
                        .AnyAsync(t => t.Id == tid && t.SucursalId == sucursalId.Value);

                    if (!esSedePrincipal)
                    {
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var tieneComision = await _context.TrabajadorSucursales
                            .AnyAsync(ts =>
                                ts.TrabajadorId == tid &&
                                ts.SucursalId == sucursalId.Value &&
                                ts.FechaInicio <= today &&
                                (ts.FechaFin == null || ts.FechaFin.Value >= today));

                        if (tieneComision)
                        {
                            soloSede = true; // filtra por sede, no por jefe
                            jefeId = null;
                        }
                    }
                }
            }

            var query = _context.Trabajadores
                .Include(t => t.Persona)
                .Include(t => t.Sucursal)
                .Include(t => t.User)
                .Include(t => t.AsignacionesTurno.Where(a => a.EsVigente))
                    .ThenInclude(a => a.Turno)
                        .ThenInclude(t => t.TipoTurno)
                .Include(t => t.AsignacionesTurno.Where(a => a.EsVigente))
                    .ThenInclude(a => a.HorarioTurno)
                .AsQueryable();

            if (jefeId.HasValue)
                query = query.Where(t => t.JefeInmediatoId == jefeId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.ToLower();
                query = query.Where(t =>
                    t.Persona!.ApellidosNombres.ToLower().Contains(q) ||
                    t.Persona!.Dni.Contains(search));
            }

            if (sucursalId.HasValue)
                query = query.Where(t => t.SucursalId == sucursalId.Value);

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                var esRot = tipo.ToUpperInvariant().Contains("ROT");
                query = esRot
                    ? query.Where(t => t.AsignacionesTurno.Any(a => a.EsVigente && a.Turno!.TipoTurno!.NombreTipo.ToUpper().Contains("ROT")))
                    : query.Where(t => !t.AsignacionesTurno.Any(a => a.EsVigente && a.Turno!.TipoTurno!.NombreTipo.ToUpper().Contains("ROT")));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(t => t.Persona!.ApellidosNombres)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = items.Select(t =>
            {
                var asig = t.AsignacionesTurno?.FirstOrDefault();
                return new
                {
                    id = t.Id,
                    personaId = t.PersonaId,
                    sucursalId = t.SucursalId,
                    idEstado = t.IdEstado,
                    dni = t.Persona?.Dni,
                    apellidosNombres = t.Persona?.ApellidosNombres,
                    nombreSucursal = t.Sucursal?.NombreSucursal,
                    tipoTurno = asig?.Turno?.TipoTurno?.NombreTipo,
                    idTurno = asig?.TurnoId,
                    idHorarioTurno = asig?.HorarioTurnoId,
                    horarioTurnoNombre = asig?.HorarioTurno?.NombreHorario,
                    username = t.User?.Username,
                    userId = t.UserId
                };
            });

            return Ok(new
            {
                items = result,
                totalCount,
                pageSize,
                currentPage = pageNumber,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // GET: api/Rrhh/Trabajadores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Trabajador>> GetTrabajadorById(int id)
        {
            var trabajador = await _trabajadorService.GetByIdAsync(id);

            if (trabajador == null)
            {
                return NotFound($"No se encontró el trabajador con ID {id}.");
            }

            return Ok(trabajador);
        }

        // POST: api/Rrhh/Trabajadores/crear-completo
        [HttpPost("crear-completo")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> CrearTrabajadorCompleto([FromBody] CrearTrabajadorCompletoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return Conflict(new { message = "El nombre de usuario ya está en uso." });

            // Find or create Persona by DNI
            var persona = await _context.Personas.FirstOrDefaultAsync(p => p.Dni == dto.Dni);

            await using var tx = await _context.Database.BeginTransactionAsync();
            var step = "init";
            try
            {
                if (persona == null)
                {
                    step = "save-persona";
                    persona = new Persona
                    {
                        Dni = dto.Dni,
                        ApellidosNombres = dto.ApellidosNombres,
                        CorreoPersonal = dto.Email,
                        TelefonoPersonal = dto.Telefono
                    };
                    _context.Personas.Add(persona);
                    await _context.SaveChangesAsync();
                }

                step = "save-user";
                var user = new User
                {
                    Username = dto.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Email = dto.Email,
                    Role = dto.Role.ToUpperInvariant(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                step = "save-trabajador";
                // Raw SQL insert to avoid EF Core omitting horas_extra_conf=false (HasDefaultValue(false) sentinel issue)
                var trabajadorIds = await _context.Database.SqlQuery<int>(
                    $@"INSERT INTO TRABAJADORES
                        (id_persona, id_user, id_sucursal, cargo, area_departamento,
                         id_jefe_inmediato, marcaje_en_zona, tomar_foto, fecha_ingreso,
                         id_estado, horas_extra_conf)
                       OUTPUT INSERTED.id_trabajador
                       VALUES
                        ({persona.Id}, {user.Id}, {dto.SucursalId}, {dto.Cargo}, {dto.AreaDepartamento},
                         {dto.JefeInmediatoId}, {(dto.MarcajeEnZona ? 1 : 0)}, {(dto.TomarFoto ? 1 : 0)},
                         {dto.FechaIngreso}, {10}, {0})"
                ).ToListAsync();
                var trabajadorId = trabajadorIds.First();

                if (dto.TurnoId.HasValue)
                {
                    step = "save-asignacion";
                    var asignacion = new AsignacionTurno
                    {
                        TrabajadorId = trabajadorId,
                        TurnoId = dto.TurnoId.Value,
                        HorarioTurnoId = dto.HorarioTurnoId,
                        FechaInicioVigencia = dto.FechaInicioVigencia ?? DateOnly.FromDateTime(DateTime.Today),
                        EsVigente = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.AsignacionesTurno.Add(asignacion);
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
                return StatusCode(StatusCodes.Status201Created, new
                {
                    trabajadorId,
                    personaId = persona.Id,
                    userId = user.Id
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Error al crear el trabajador.", detail });
            }
        }

        // POST: api/Rrhh/Trabajadores
        [HttpPost]
        [Authorize(Roles = "SUPERADMIN")]
        public async Task<ActionResult<Trabajador>> CreateTrabajador([FromBody] TrabajadorDto trabajador)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _trabajadorService.AddAsync(trabajador);
            var nuevoTrabajador = trabajador; // O asigna el objeto correcto si AddAsync retorna el Trabajador creado

            return CreatedAtAction(nameof(GetTrabajadorById), new { id = trabajador.PersonaId }, trabajador);
        }

        // PUT: api/Rrhh/Trabajadores/5
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPERADMIN")]
        public async Task<IActionResult> UpdateTrabajador(int id, [FromBody] TrabajadorDto trabajador)
        {
            //if (id != trabajador.PersonaId)
            //{
            //    return BadRequest("El ID del trabajador en la URL no coincide con el del cuerpo de la solicitud.");
            //}

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _trabajadorService.UpdateAsync(id, trabajador);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return NoContent();
        }

        // DELETE: api/Rrhh/Trabajadores/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPERADMIN")]
        public async Task<IActionResult> DeleteTrabajador(int id)
        {
            try
            {
                await _trabajadorService.DeleteAsync(id);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return NoContent();
        }

        [HttpPut("{id}/baja")]
        [Authorize(Roles = "SUPERADMIN")]
        public async Task<IActionResult> DarDeBaja(int id)
        {
            var trab = await _context.Trabajadores.FindAsync(id);
            if (trab == null) return NotFound();
            trab.IdEstado = 11;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id}/reactivar")]
        [Authorize(Roles = "SUPERADMIN")]
        public async Task<IActionResult> Reactivar(int id)
        {
            var trab = await _context.Trabajadores.FindAsync(id);
            if (trab == null) return NotFound();
            trab.IdEstado = 10;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("~/api/trabajadores/{id:int}/turno-vigente")]
        public async Task<IActionResult> GetTurnoVigente(int id)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var asignacion = await _context.AsignacionesTurno
                .AsNoTracking()
                .Include(a => a.Turno)
                    .ThenInclude(t => t!.HorariosTurno)
                        .ThenInclude(ht => ht.HorariosDetalle)
                .Include(a => a.HorarioTurno)
                    .ThenInclude(h => h.HorariosDetalle)
                .FirstOrDefaultAsync(a => a.TrabajadorId == id
                    && a.EsVigente
                    && a.FechaInicioVigencia <= today
                    && (a.FechaFinVigencia == null || a.FechaFinVigencia.Value >= today));

            if (asignacion == null)
            {
                return NotFound(new { message = "El trabajador no tiene turno vigente." });
            }

            var horario = asignacion.HorarioTurno ?? asignacion.Turno?.HorariosTurno?.FirstOrDefault(h => h.EsActivo)
                ?? asignacion.Turno?.HorariosTurno?.FirstOrDefault();

            var response = new
            {
                trabajadorId = id,
                asignacionId = asignacion.Id,
                turno = new
                {
                    id = asignacion.Turno?.Id,
                    codigo = asignacion.Turno?.NombreCodigo,
                    tipoTurnoId = asignacion.Turno?.TipoTurnoId,
                    esActivo = asignacion.Turno?.EsActivo
                },
                vigencia = new
                {
                    inicio = asignacion.FechaInicioVigencia,
                    fin = asignacion.FechaFinVigencia,
                    esVigente = asignacion.EsVigente
                },
                horario = horario == null ? null : new
                {
                    idHorarioTurno = horario.Id,
                    nombreHorario = horario.NombreHorario,
                    detalles = horario.HorariosDetalle
                        .OrderBy(d => d.DiaSemana)
                        .Select(d => new
                        {
                            diaSemana = d.DiaSemana,
                            horaInicio = d.HoraInicio.ToString(@"hh\:mm"),
                            horaFin = d.HoraFin.ToString(@"hh\:mm"),
                            salidaDiaSiguiente = d.SalidaDiaSiguiente
                        })
                }
            };

            return Ok(response);
        }

        [HttpPost("~/api/trabajadores/{id:int}/asignar-turno")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> AsignarTurnoTrabajador(int id, [FromBody] AsignarTurnoTrabajadorRequest request)
        {
            if (request.FechaFinVigencia.HasValue && request.FechaFinVigencia.Value < request.FechaInicioVigencia)
            {
                return BadRequest(new { message = "La fecha fin no puede ser menor que la fecha inicio." });
            }

            var trabajadorExiste = await _context.Trabajadores.AnyAsync(t => t.Id == id);
            if (!trabajadorExiste)
            {
                return NotFound(new { message = $"No existe trabajador con ID {id}." });
            }

            var turno = await _context.Turnos.Include(t => t.TipoTurno).FirstOrDefaultAsync(t => t.Id == request.TurnoId);
            if (turno == null)
            {
                return NotFound(new { message = $"No existe turno con ID {request.TurnoId}." });
            }

            // Si el turno es rotativo, preferir exigir HorarioTurnoId
            var nombreTipo = turno.TipoTurno?.NombreTipo ?? string.Empty;
            var esRotativo = nombreTipo.ToUpperInvariant().Contains("ROT");
            if (esRotativo && !request.HorarioTurnoId.HasValue)
            {
                return BadRequest(new { message = "Para turnos rotativos se requiere HorarioTurnoId." });
            }

            // Si se proporcionó HorarioTurnoId, validar que exista y pertenezca al turno
            if (request.HorarioTurnoId.HasValue)
            {
                var horarioValido = await _context.HorariosTurno
                    .AnyAsync(h => h.Id == request.HorarioTurnoId.Value && h.TurnoId == request.TurnoId);
                if (!horarioValido)
                {
                    return NotFound(new { message = $"HorarioTurno con ID {request.HorarioTurnoId} no encontrado o no pertenece al turno {request.TurnoId}." });
                }
            }

            // Validar solapamiento de vigencias para el trabajador
            var newStart = request.FechaInicioVigencia;
            var newEnd = request.FechaFinVigencia ?? DateOnly.MaxValue;
            var existeSolapamiento = await _context.AsignacionesTurno
                .AnyAsync(a => a.TrabajadorId == id &&
                               newStart <= (a.FechaFinVigencia ?? DateOnly.MaxValue) &&
                               newEnd >= a.FechaInicioVigencia);

            if (existeSolapamiento)
            {
                return Conflict(new { message = "La vigencia de la nueva asignación solapa con otra existente para el trabajador." });
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            var asignacionesVigentes = await _context.AsignacionesTurno
                .Where(a => a.TrabajadorId == id && a.EsVigente)
                .ToListAsync();

            foreach (var vigente in asignacionesVigentes)
            {
                vigente.EsVigente = false;
                vigente.FechaFinVigencia = request.FechaInicioVigencia.AddDays(-1);
                vigente.UpdatedAt = DateTime.UtcNow;
            }

            var nuevaAsignacion = new AsignacionTurno
            {
                TrabajadorId = id,
                TurnoId = request.TurnoId,
                HorarioTurnoId = request.HorarioTurnoId,
                FechaInicioVigencia = request.FechaInicioVigencia,
                FechaFinVigencia = request.FechaFinVigencia,
                EsVigente = true,
                MotivoCambio = request.MotivoCambio,
                AprobadoPor = request.AprobadoPorTrabajadorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AsignacionesTurno.Add(nuevaAsignacion);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                asignacionId = nuevaAsignacion.Id,
                trabajadorId = nuevaAsignacion.TrabajadorId,
                turnoId = nuevaAsignacion.TurnoId,
                fechaInicioVigencia = nuevaAsignacion.FechaInicioVigencia,
                fechaFinVigencia = nuevaAsignacion.FechaFinVigencia,
                esVigente = nuevaAsignacion.EsVigente
            });
        }

        /// <summary>
        /// Devuelve todas las sedes activas a las que está asignado el trabajador.
        /// La Flutter app usa este endpoint para mostrar el selector de sede antes de marcar.
        /// </summary>
        [HttpGet("{id:int}/sucursales-disponibles")]
        public async Task<IActionResult> GetSucursalesDisponibles(int id)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Sede principal del trabajador
            var trabajador = await _context.Trabajadores
                .AsNoTracking()
                .Include(t => t.Sucursal)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trabajador == null)
                return NotFound(new { message = $"No existe trabajador con ID {id}." });

            var sedes = new List<object>();

            if (trabajador.Sucursal != null)
            {
                sedes.Add(new
                {
                    id = trabajador.Sucursal.Id,
                    nombre = trabajador.Sucursal.NombreSucursal,
                    nombreSucursal = trabajador.Sucursal.NombreSucursal,
                    direccion = trabajador.Sucursal.Direccion,
                    latitud = trabajador.Sucursal.LatitudCentro,
                    longitud = trabajador.Sucursal.LongitudCentro,
                    perimetroM = trabajador.Sucursal.PerimetroM,
                    esPrincipal = true,
                    puedeGestionar = false,
                    fechaInicio = (DateOnly?)null,
                    fechaFin = (DateOnly?)null
                });
            }

            // Sedes adicionales vigentes en TRABAJADOR_SUCURSALES
            var adicionales = await _context.TrabajadorSucursales
                .AsNoTracking()
                .Include(ts => ts.Sucursal)
                .Where(ts =>
                    ts.TrabajadorId == id &&
                    ts.SucursalId != trabajador.SucursalId &&
                    ts.FechaInicio <= today &&
                    (ts.FechaFin == null || ts.FechaFin.Value >= today))
                .ToListAsync();

            foreach (var ts in adicionales)
            {
                sedes.Add(new
                {
                    id = ts.Sucursal.Id,
                    nombre = ts.Sucursal.NombreSucursal,
                    nombreSucursal = ts.Sucursal.NombreSucursal,
                    direccion = ts.Sucursal.Direccion,
                    latitud = ts.Sucursal.LatitudCentro,
                    longitud = ts.Sucursal.LongitudCentro,
                    perimetroM = ts.Sucursal.PerimetroM,
                    esPrincipal = ts.EsSucursalPrincipal,
                    puedeGestionar = ts.PuedeGestionar,
                    fechaInicio = ts.FechaInicio,
                    fechaFin = ts.FechaFin
                });
            }

            return Ok(new { trabajadorId = id, sedes });
        }

        /// <summary>
        /// Asigna una sede adicional a un trabajador (para comisiones temporales o cobertura).
        /// </summary>
        [HttpPost("{id:int}/sucursales")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> AsignarSede(int id, [FromBody] AsignarSedeRequest request)
        {
            var trabajadorExiste = await _context.Trabajadores.AnyAsync(t => t.Id == id);
            if (!trabajadorExiste)
                return NotFound(new { message = $"No existe trabajador con ID {id}." });

            var sucursalExiste = await _context.SucursalCentros.AnyAsync(s => s.Id == request.SucursalId);
            if (!sucursalExiste)
                return NotFound(new { message = $"No existe sede con ID {request.SucursalId}." });

            // Evitar duplicado activo o re-abrir uno existente
            var today = DateOnly.FromDateTime(DateTime.Today);
            var existente = await _context.TrabajadorSucursales
                .FirstOrDefaultAsync(ts => ts.TrabajadorId == id && ts.SucursalId == request.SucursalId);

            if (existente != null)
            {
                // Si ya está vigente, conflicto
                if (existente.FechaInicio <= today && (existente.FechaFin == null || existente.FechaFin.Value >= today))
                {
                    return Conflict(new { message = "El trabajador ya tiene esa sede asignada y vigente." });
                }

                // Si existía pero no estaba vigente, lo "re-abrimos" actualizando sus datos
                existente.PuedeGestionar = request.PuedeGestionar;
                existente.FechaInicio = request.FechaInicio;
                existente.FechaFin = request.FechaFin;
                
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = existente.Id,
                    trabajadorId = id,
                    sucursalId = existente.SucursalId,
                    fechaInicio = existente.FechaInicio,
                    fechaFin = existente.FechaFin,
                    puedeGestionar = existente.PuedeGestionar,
                    reopened = true
                });
            }

            var nueva = new Asistencia.Data.Entities.MarcacionAsistenciaEntites.TrabajadorSucursal
            {
                TrabajadorId = id,
                SucursalId = request.SucursalId,
                EsSucursalPrincipal = false,
                PuedeGestionar = request.PuedeGestionar,
                FechaInicio = request.FechaInicio,
                FechaFin = request.FechaFin
            };

            _context.TrabajadorSucursales.Add(nueva);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                id = nueva.Id,
                trabajadorId = id,
                sucursalId = nueva.SucursalId,
                fechaInicio = nueva.FechaInicio,
                fechaFin = nueva.FechaFin,
                puedeGestionar = nueva.PuedeGestionar
            });
        }

        /// <summary>
        /// Remueve (o cierra) una sede adicional de un trabajador.
        /// </summary>
        [HttpDelete("{id:int}/sucursales/{sucursalId:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> RemoverSede(int id, int sucursalId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var asignacion = await _context.TrabajadorSucursales
                .FirstOrDefaultAsync(ts =>
                    ts.TrabajadorId == id &&
                    ts.SucursalId == sucursalId &&
                    (ts.FechaFin == null || ts.FechaFin.Value >= today));

            if (asignacion == null)
                return NotFound(new { message = "No se encontró asignación vigente de esa sede para el trabajador." });

            // Cerrar la vigencia al día de hoy en lugar de borrar (auditoría)
            asignacion.FechaFin = today.AddDays(-1);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        public sealed class AsignarSedeRequest
        {
            public int SucursalId { get; set; }
            public bool PuedeGestionar { get; set; }
            public DateOnly FechaInicio { get; set; }
            public DateOnly? FechaFin { get; set; }
        }

        public sealed class AsignarTurnoTrabajadorRequest
        {
            public int TurnoId { get; set; }
            public int? HorarioTurnoId { get; set; }
            public DateOnly FechaInicioVigencia { get; set; }
            public DateOnly? FechaFinVigencia { get; set; }
            public string? MotivoCambio { get; set; }
            public int? AprobadoPorTrabajadorId { get; set; }
        }
    }
}
