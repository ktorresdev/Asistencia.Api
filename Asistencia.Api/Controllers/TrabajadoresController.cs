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
        private readonly ILogger<TrabajadoresController> _logger;

        public TrabajadoresController(ITrabajadorService trabajadorService, MarcacionAsistenciaDbContext context, ILogger<TrabajadoresController> logger)
        {
            _trabajadorService = trabajadorService;
            _context = context;
            _logger = logger;
        }

        // GET: api/Rrhh/Trabajadores
        [HttpGet]
        public async Task<ActionResult> GetAllTrabajadores(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? sucursalId = null,
            [FromQuery] int? idArea = null,
            [FromQuery] string? tipo = null)
        {
            try
            {
                pageSize = Math.Min(pageSize, 200);
                _logger.LogInformation("[Trabajadores] GET page={Page} size={Size} search={Search} sucursal={Sucursal} area={Area} tipo={Tipo}",
                    pageNumber, pageSize, search, sucursalId, idArea, tipo);

                int? jefeId = null;
                bool soloSede = false;

                if (User.IsInRole("ADMIN") && !User.IsInRole("SUPERADMIN"))
                {
                    var claim = User.FindFirst("trabajador_id")?.Value;
                    if (!int.TryParse(claim, out var tid))
                    {
                        _logger.LogWarning("[Trabajadores] ADMIN sin claim trabajador_id válido");
                        return Ok(new { items = Array.Empty<object>(), totalCount = 0, pageSize, currentPage = pageNumber, totalPages = 0 });
                    }

                    jefeId = tid;

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
                                soloSede = true;
                                jefeId = null;
                            }
                        }
                    }
                }

                _logger.LogInformation("[Trabajadores] Filtro jefeId={JefeId} soloSede={SoloSede}", jefeId, soloSede);

                var baseQuery = _context.Trabajadores
                    .AsNoTracking()
                    .Include(t => t.Persona)
                    .Include(t => t.Sucursal)
                    .Include(t => t.User)
                    .Include(t => t.Area)
                    .Include(t => t.Puesto)
                    .AsQueryable();

                if (jefeId.HasValue)
                    baseQuery = baseQuery.Where(t => t.JefeInmediatoId == jefeId.Value);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.ToLower();
                    baseQuery = baseQuery.Where(t =>
                        t.Persona!.ApellidosNombres.ToLower().Contains(q) ||
                        t.Persona!.Dni.Contains(search));
                }

                if (sucursalId.HasValue)
                    baseQuery = baseQuery.Where(t => t.SucursalId == sucursalId.Value);

                if (idArea.HasValue)
                    baseQuery = baseQuery.Where(t => t.IdArea == idArea.Value);

                if (!string.IsNullOrWhiteSpace(tipo))
                {
                    var esRot = tipo.ToUpperInvariant().Contains("ROT");
                    var rotWorkerIds = await _context.AsignacionesTurno
                        .Where(a => a.EsVigente && a.Turno!.TipoTurno!.NombreTipo.ToUpper().Contains("ROT"))
                        .Select(a => a.TrabajadorId)
                        .Distinct()
                        .ToListAsync();

                    baseQuery = esRot
                        ? baseQuery.Where(t => rotWorkerIds.Contains(t.Id))
                        : baseQuery.Where(t => !rotWorkerIds.Contains(t.Id));
                }

                _logger.LogInformation("[Trabajadores] Ejecutando COUNT...");
                var totalCount = await baseQuery.CountAsync();
                _logger.LogInformation("[Trabajadores] totalCount={Total}", totalCount);

                var workers = await baseQuery
                    .OrderBy(t => t.Persona!.ApellidosNombres)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("[Trabajadores] workers cargados: {Count}", workers.Count);

                var workerIds = workers.Select(w => w.Id).ToList();
                var asignaciones = await _context.AsignacionesTurno
                    .AsNoTracking()
                    .Where(a => workerIds.Contains(a.TrabajadorId) && a.EsVigente)
                    .Include(a => a.Turno).ThenInclude(t => t!.TipoTurno)
                    .Include(a => a.HorarioTurno)
                    .ToListAsync();

                _logger.LogInformation("[Trabajadores] asignaciones vigentes cargadas: {Count}", asignaciones.Count);

                var asigPorTrabajador = asignaciones
                    .GroupBy(a => a.TrabajadorId)
                    .ToDictionary(g => g.Key, g => g.First());

                var result = workers.Select(t =>
                {
                    asigPorTrabajador.TryGetValue(t.Id, out var asig);
                    return new
                    {
                        id = t.Id,
                        personaId = t.PersonaId,
                        sucursalId = t.SucursalId,
                        idEstado = t.IdEstado,
                        dni = t.Persona?.Dni,
                        apellidosNombres = t.Persona?.ApellidosNombres,
                        nombreSucursal = t.Sucursal?.NombreSucursal,
                        idArea = t.IdArea,
                        nombreArea = t.Area?.NombreArea,
                        idPuesto = t.IdPuesto,
                        nombrePuesto = t.Puesto?.NombrePuesto,
                        jefeInmediatoId = t.JefeInmediatoId,
                        tipoTurno = asig?.Turno?.TipoTurno?.NombreTipo,
                        idTurno = asig?.TurnoId,
                        idHorarioTurno = asig?.HorarioTurnoId,
                        horarioTurnoNombre = asig?.HorarioTurno?.NombreHorario,
                        username = t.User?.Username,
                        userId = t.UserId,
                        role = t.User?.Role
                    };
                }).ToList();

                _logger.LogInformation("[Trabajadores] Proyección completa: {Count} items. Retornando OK.", result.Count);

                return Ok(new
                {
                    items = result,
                    totalCount,
                    pageSize,
                    currentPage = pageNumber,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Trabajadores] ERROR en GetAllTrabajadores page={Page} size={Size}", pageNumber, pageSize);
                return StatusCode(500, new { message = "Error al listar trabajadores.", detail = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // GET: api/Rrhh/Trabajadores/lookup
        // Lista liviana (id + nombre) de trabajadores que pueden ser JEFE, es decir,
        // cuyo usuario tiene un rol distinto de TRABAJADOR (ADMIN/SUPERVISOR/SUPERADMIN).
        // Sin tope de pagina ni filtro de subordinados. Pensada para selectores (jefe inmediato).
        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup()
        {
            var data = await _context.Trabajadores
                .AsNoTracking()
                .Include(t => t.Persona)
                .Include(t => t.User)
                .Where(t => t.User != null
                            && t.User.Role != null
                            && t.User.Role.ToUpper() != "TRABAJADOR"
                            && t.User.Role.ToUpper() != "EMPLOYEE")
                .OrderBy(t => t.Persona!.ApellidosNombres)
                .Select(t => new
                {
                    id = t.Id,
                    apellidosNombres = t.Persona!.ApellidosNombres,
                    dni = t.Persona!.Dni,
                    role = t.User!.Role
                })
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/Rrhh/Trabajadores/existe-dni?dni=12345678
        // Verifica si el DNI ya existe (persona) y si ya está vinculado a un trabajador.
        [HttpGet("existe-dni")]
        public async Task<IActionResult> ExisteDni([FromQuery] string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
                return BadRequest(new { message = "DNI requerido." });

            dni = dni.Trim();
            var persona = await _context.Personas
                .Where(p => p.Dni == dni)
                .Select(p => new { p.Id, p.ApellidosNombres })
                .FirstOrDefaultAsync();

            if (persona == null)
                return Ok(new { existe = false });

            var yaEsTrabajador = await _context.Trabajadores.AnyAsync(t => t.PersonaId == persona.Id);
            return Ok(new
            {
                existe = true,
                personaId = persona.Id,
                apellidosNombres = persona.ApellidosNombres,
                yaEsTrabajador
            });
        }

        // GET: api/Rrhh/Trabajadores/5
        [HttpGet("{id:int}")]
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

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
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

                // Resolver área/puesto por id (preferente). Si llega id, se sincroniza tambien
                // el texto legado (area_departamento / cargo) con el nombre del maestro.
                string? areaTexto = dto.AreaDepartamento;
                if (dto.IdArea.HasValue)
                {
                    var nombreArea = await _context.Areas
                        .Where(a => a.Id == dto.IdArea.Value)
                        .Select(a => a.NombreArea)
                        .FirstOrDefaultAsync();
                    if (nombreArea == null)
                        return NotFound(new { message = $"No existe área con ID {dto.IdArea.Value}." });
                    areaTexto = nombreArea;
                }

                string? cargoTexto = dto.Cargo;
                if (dto.IdPuesto.HasValue)
                {
                    var nombrePuesto = await _context.Puestos
                        .Where(p => p.Id == dto.IdPuesto.Value)
                        .Select(p => p.NombrePuesto)
                        .FirstOrDefaultAsync();
                    if (nombrePuesto == null)
                        return NotFound(new { message = $"No existe puesto con ID {dto.IdPuesto.Value}." });
                    cargoTexto = nombrePuesto;
                }

                // Raw SQL insert to avoid EF Core omitting horas_extra_conf=false (HasDefaultValue(false) sentinel issue)
                var trabajadorIds = await _context.Database.SqlQuery<int>(
                    $@"INSERT INTO TRABAJADORES
                        (id_persona, id_user, id_sucursal, cargo, area_departamento, id_area, id_puesto,
                         id_jefe_inmediato, marcaje_en_zona, tomar_foto, fecha_ingreso,
                         id_estado, horas_extra_conf)
                       OUTPUT INSERTED.id_trabajador
                       VALUES
                        ({persona.Id}, {user.Id}, {dto.SucursalId}, {cargoTexto}, {areaTexto}, {dto.IdArea}, {dto.IdPuesto},
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
            });
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
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
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
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> DarDeBaja(int id)
        {
            var trab = await _context.Trabajadores.FindAsync(id);
            if (trab == null) return NotFound();
            trab.IdEstado = 11;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id}/reactivar")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
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

        /// <summary>
        /// Lista TODAS las asignaciones de turno vigentes del trabajador (1 o más).
        /// Necesario para doble turno: turno-vigente solo devuelve la primera.
        /// </summary>
        [HttpGet("~/api/trabajadores/{id:int}/turnos-vigentes")]
        public async Task<IActionResult> GetTurnosVigentes(int id)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var asignaciones = await _context.AsignacionesTurno
                .AsNoTracking()
                .Include(a => a.Turno)
                .Include(a => a.HorarioTurno)
                    .ThenInclude(h => h!.HorariosDetalle)
                .Where(a => a.TrabajadorId == id
                    && a.EsVigente
                    && a.FechaInicioVigencia <= today
                    && (a.FechaFinVigencia == null || a.FechaFinVigencia.Value >= today))
                .OrderBy(a => a.FechaInicioVigencia)
                .ToListAsync();

            var response = asignaciones.Select(asignacion =>
            {
                var horario = asignacion.HorarioTurno;
                return new
                {
                    trabajadorId = id,
                    asignacionId = asignacion.Id,
                    turno = new
                    {
                        id = asignacion.Turno?.Id,
                        codigo = asignacion.Turno?.NombreCodigo,
                        tipoTurnoId = asignacion.Turno?.TipoTurnoId
                    },
                    vigencia = new
                    {
                        inicio = asignacion.FechaInicioVigencia,
                        fin = asignacion.FechaFinVigencia
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
            });

            return Ok(response);
        }

        /// <summary>
        /// Quita (cierra) una asignación de turno vigente concreta. Se usa para retirar
        /// el 2º turno de un doble turno sin afectar al otro. No la elimina físicamente:
        /// marca es_vigente=0 y fija fecha_fin_vigencia para preservar el histórico.
        /// </summary>
        [HttpDelete("~/api/trabajadores/{id:int}/asignaciones/{asignacionId:int}")]
        [Authorize(Roles = "ADMIN,SUPERADMIN,SUPERVISOR")]
        public async Task<IActionResult> QuitarAsignacionTurno(int id, int asignacionId)
        {
            var asignacion = await _context.AsignacionesTurno
                .FirstOrDefaultAsync(a => a.Id == asignacionId && a.TrabajadorId == id);

            if (asignacion == null)
                return NotFound(new { message = "No se encontró ese turno asignado al trabajador. Es posible que ya haya sido quitado o reemplazado." });

            if (!asignacion.EsVigente)
                return BadRequest(new { message = "Ese turno ya no está activo, así que no hay nada que quitar." });

            asignacion.EsVigente = false;
            asignacion.FechaFinVigencia = DateOnly.FromDateTime(DateTime.Today);
            asignacion.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
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

            // Para turnos ROTATIVOS el horario del dia se define en Programacion Semanal,
            // por lo que NO se exige HorarioTurnoId al asignar. Para FIJOS si conviene tenerlo,
            // pero se deja opcional aqui (la app/front valida lo que corresponda).
            var nombreTipo = turno.TipoTurno?.NombreTipo ?? string.Empty;
            var esRotativo = nombreTipo.ToUpperInvariant().Contains("ROT");

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

            var newStart = request.FechaInicioVigencia;
            var newEnd = request.FechaFinVigencia ?? DateOnly.MaxValue;

            // DOBLE TURNO: si se permite, el trabajador puede tener mas de una asignacion
            // vigente a la vez (por carga de trabajo). En ese caso NO se cierran las vigentes
            // ni se valida solapamiento. Por defecto (reemplazo) si se cierra la vigente actual.
            if (!request.PermitirDobleTurno)
            {
                // Validar solapamiento contra asignaciones NO vigentes (las vigentes se cierran abajo);
                // de lo contrario cualquier cambio de turno chocaria con la asignacion abierta actual.
                // Borde estricto: una asignacion cerrada que TERMINA el mismo dia en que empieza la
                // nueva (ej. se uso "Quitar" hoy y se reasigna hoy) NO debe contar como solape -> '<'.
                // Se ignoran las cerradas sin fecha_fin (registros historicos sin cierre formal).
                var existeSolapamiento = await _context.AsignacionesTurno
                    .AnyAsync(a => a.TrabajadorId == id && !a.EsVigente &&
                                   a.FechaFinVigencia != null &&
                                   newStart < a.FechaFinVigencia.Value &&
                                   newEnd >= a.FechaInicioVigencia);

                if (existeSolapamiento)
                {
                    return Conflict(new { message = "No se puede asignar el turno desde esa fecha porque el trabajador ya tuvo otro turno activo en ese periodo. Elige una fecha de inicio posterior al último día de su turno anterior (por ejemplo, el día de mañana)." });
                }
            }
            else
            {
                // DOBLE TURNO: la marcación se despacha automáticamente al turno cuya
                // ventana horaria contiene la hora marcada. Por eso las ventanas de
                // marcación (turno ± tolerancia) del nuevo turno NO pueden solaparse con
                // las de los turnos ya vigentes. Requiere horario explícito en ambos.
                if (!request.HorarioTurnoId.HasValue)
                {
                    return BadRequest(new { message = "Para un doble turno debes elegir un horario específico para el nuevo turno. Un turno rotativo (sin horario fijo) no se puede usar como segundo turno." });
                }

                var detallesNuevo = await _context.HorariosDetalle
                    .Where(d => d.HorarioTurnoId == request.HorarioTurnoId.Value)
                    .ToListAsync();
                if (detallesNuevo.Count == 0)
                {
                    return BadRequest(new { message = "El horario del nuevo turno no tiene días/horas configurados." });
                }

                var horariosVigentesIds = await _context.AsignacionesTurno
                    .Where(a => a.TrabajadorId == id && a.EsVigente && a.HorarioTurnoId != null)
                    .Select(a => a.HorarioTurnoId!.Value)
                    .ToListAsync();

                var detallesVigentes = await _context.HorariosDetalle
                    .Where(d => horariosVigentesIds.Contains(d.HorarioTurnoId))
                    .ToListAsync();

                var conflicto = EncontrarSolapeVentanas(detallesVigentes, detallesNuevo);
                if (conflicto != null)
                {
                    return Conflict(new { message =
                        "No se puede agregar como doble turno porque el horario del nuevo turno se cruza con uno que el trabajador ya tiene (" + conflicto +
                        "). Si lo que quieres es REEMPLAZAR su turno actual, desmarca la casilla \"Doble turno\". Si de verdad quieres dos turnos, elige horarios que no se crucen (pueden ir pegados, por ejemplo 08:00-16:00 y 16:00-00:00)." });
                }
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
            await using var tx = await _context.Database.BeginTransactionAsync();

            if (!request.PermitirDobleTurno)
            {
                var asignacionesVigentes = await _context.AsignacionesTurno
                    .Where(a => a.TrabajadorId == id && a.EsVigente)
                    .ToListAsync();

                foreach (var vigente in asignacionesVigentes)
                {
                    vigente.EsVigente = false;
                    vigente.FechaFinVigencia = request.FechaInicioVigencia.AddDays(-1);
                    vigente.UpdatedAt = DateTime.UtcNow;
                }
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

        /// <summary>
        /// Cambia el area (departamento) de un trabajador. Movimiento interno.
        /// </summary>
        [HttpPut("{id:int}/area")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> CambiarArea(int id, [FromBody] CambiarAreaRequest request)
        {
            var trabajador = await _context.Trabajadores.FindAsync(id);
            if (trabajador == null)
                return NotFound(new { message = $"No existe trabajador con ID {id}." });

            if (request.IdArea.HasValue)
            {
                var area = await _context.Areas.FirstOrDefaultAsync(a => a.Id == request.IdArea.Value);
                if (area == null)
                    return NotFound(new { message = $"No existe area con ID {request.IdArea.Value}." });

                trabajador.IdArea = area.Id;
                trabajador.AreaDepartamento = area.NombreArea; // mantiene el texto legado sincronizado
            }
            else
            {
                trabajador.IdArea = null;
                trabajador.AreaDepartamento = null;
            }

            await _context.SaveChangesAsync();
            return Ok(new { trabajadorId = id, idArea = trabajador.IdArea });
        }

        /// <summary>
        /// Cambia el puesto de un trabajador.
        /// </summary>
        [HttpPut("{id:int}/puesto")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> CambiarPuesto(int id, [FromBody] CambiarPuestoRequest request)
        {
            var trabajador = await _context.Trabajadores.FindAsync(id);
            if (trabajador == null)
                return NotFound(new { message = $"No existe trabajador con ID {id}." });

            if (request.IdPuesto.HasValue)
            {
                var puesto = await _context.Puestos.FirstOrDefaultAsync(p => p.Id == request.IdPuesto.Value);
                if (puesto == null)
                    return NotFound(new { message = $"No existe puesto con ID {request.IdPuesto.Value}." });

                trabajador.IdPuesto = puesto.Id;
                trabajador.Cargo = puesto.NombrePuesto; // mantiene el texto legado sincronizado
            }
            else
            {
                trabajador.IdPuesto = null;
                trabajador.Cargo = null;
            }

            await _context.SaveChangesAsync();
            return Ok(new { trabajadorId = id, idPuesto = trabajador.IdPuesto });
        }

        /// <summary>
        /// Asigna o actualiza la jefatura (jefe inmediato) de un trabajador.
        /// </summary>
        [HttpPut("{id:int}/jefe")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> AsignarJefe(int id, [FromBody] AsignarJefeRequest request)
        {
            var trabajador = await _context.Trabajadores.FindAsync(id);
            if (trabajador == null)
                return NotFound(new { message = $"No existe trabajador con ID {id}." });

            if (request.JefeInmediatoId.HasValue)
            {
                if (request.JefeInmediatoId.Value == id)
                    return BadRequest(new { message = "Un trabajador no puede ser su propio jefe." });

                var jefeExiste = await _context.Trabajadores.AnyAsync(t => t.Id == request.JefeInmediatoId.Value);
                if (!jefeExiste)
                    return NotFound(new { message = $"No existe el jefe con ID {request.JefeInmediatoId.Value}." });

                trabajador.JefeInmediatoId = request.JefeInmediatoId.Value;
            }
            else
            {
                trabajador.JefeInmediatoId = null;
            }

            await _context.SaveChangesAsync();
            return Ok(new { trabajadorId = id, jefeInmediatoId = trabajador.JefeInmediatoId });
        }

        public sealed class CambiarAreaRequest
        {
            public int? IdArea { get; set; }
        }

        public sealed class CambiarPuestoRequest
        {
            public int? IdPuesto { get; set; }
        }

        public sealed class AsignarJefeRequest
        {
            public int? JefeInmediatoId { get; set; }
        }

        // ── Helpers de validación de solape de horarios (doble turno) ───────────

        /// <summary>
        /// Busca un solape REAL de horario entre los turnos vigentes y el nuevo, solo
        /// cuando pueden caer el mismo día. Permite turnos CONTIGUOS (fin de uno = inicio
        /// del otro, ej. 08-16 y 16-00): el cierre los reparte por el límite teórico.
        /// Solo rechaza cuando los rangos se cruzan de verdad. Devuelve la descripción del
        /// primer conflicto, o null si no hay solape.
        /// </summary>
        private static string? EncontrarSolapeVentanas(
            List<HorarioDetalle> vigentes, List<HorarioDetalle> nuevos)
        {
            foreach (var dv in vigentes)
            {
                var diasV = DiasIso(dv.DiaSemana);
                var (iniV, finV) = RangoMinutos(dv);
                foreach (var dn in nuevos)
                {
                    var diasN = DiasIso(dn.DiaSemana);
                    if (!diasV.Overlaps(diasN)) continue; // distinto día → no aplica
                    var (iniN, finN) = RangoMinutos(dn);
                    // Solape estricto: comparten algún minuto interior. Adyacente (finV==iniN) NO solapa.
                    if (iniV < finN && iniN < finV)
                        return $"vigente {Hhmm(dv.HoraInicio)}-{Hhmm(dv.HoraFin)} vs nuevo {Hhmm(dn.HoraInicio)}-{Hhmm(dn.HoraFin)}";
                }
            }
            return null;
        }

        /// <summary>Rango horario del turno en minutos [inicio, fin]. Maneja turno nocturno.</summary>
        private static (double ini, double fin) RangoMinutos(HorarioDetalle d)
        {
            double ini = d.HoraInicio.TotalMinutes;
            double fin = d.HoraFin.TotalMinutes;
            if (d.SalidaDiaSiguiente || fin < ini) fin += 1440; // cruza medianoche
            return (ini, fin);
        }

        private static string Hhmm(TimeSpan t) => t.ToString(@"hh\:mm");

        /// <summary>Convierte el campo DiaSemana (números, nombres, rangos, listas) a un set ISO 1..7.</summary>
        private static HashSet<int> DiasIso(string raw)
        {
            var set = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(raw)) return set;
            foreach (var part in raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()))
            {
                if (part.Contains('-'))
                {
                    var r = part.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                    if (r.Length != 2) continue;
                    var s = DiaIso(r[0]); var e = DiaIso(r[1]);
                    if (s == null || e == null) continue;
                    if (s <= e) { for (int i = s.Value; i <= e.Value; i++) set.Add(i); }
                    else { for (int i = s.Value; i <= 7; i++) set.Add(i); for (int i = 1; i <= e.Value; i++) set.Add(i); }
                }
                else { var v = DiaIso(part); if (v != null) set.Add(v.Value); }
            }
            return set;
        }

        private static int? DiaIso(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            token = token.Trim();
            if (int.TryParse(token, out var n))
            {
                if (n == 0) return 7;
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
            // Si es true, NO cierra la asignacion vigente: el trabajador queda con doble turno.
            public bool PermitirDobleTurno { get; set; } = false;
        }
    }
}
