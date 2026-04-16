using Asistencia.Data.Entities.MarcacionAsistenciaEntites;
using Asistencia.Data.Entities.UserEntites;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Asistencia.Data.DbContexts
{
    public class MarcacionAsistenciaDbContext : DbContext
    {
        public MarcacionAsistenciaDbContext(DbContextOptions<MarcacionAsistenciaDbContext> options)
            : base(options)
        {
        }
        public DbSet<MaestroEstado> MaestroEstados { get; set; }
        public DbSet<CalendarioFeriado> CalendarioFeriados { get; set; }
        public DbSet<AsistenciaResumenDiario> AsistenciaResumenDiarios { get; set; }

        public DbSet<Persona> Personas { get; set; }
        public DbSet<SucursalCentro> SucursalCentros { get; set; }
        public DbSet<Trabajador> Trabajadores { get; set; }
        public DbSet<TipoTurno> TipoTurnos { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<HorarioTurno> HorariosTurno { get; set; }
        public DbSet<HorarioDetalle> HorariosDetalle { get; set; }
        public DbSet<AsignacionTurno> AsignacionesTurno { get; set; }
        public DbSet<TipoJustificacion> TipoJustificaciones { get; set; }
        public DbSet<Justificacion> Justificaciones { get; set; }
        public DbSet<MarcacionAsistencia> MarcacionesAsistencia { get; set; }
        public DbSet<SolicitudHorasExtra> SolicitudesHorasExtra { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        // Agregar en los DbSet:
        public DbSet<AuditLogin> AuditLogins { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<ProgramacionDescanso> ProgramacionDescansos { get; set; }
        public DbSet<CoberturaTurno> CoberturasTurno { get; set; }
        public DbSet<TrabajadorSucursal> TrabajadorSucursales { get; set; }
        public DbSet<ProgramacionTurnoSemanal> ProgramacionTurnosSemanal { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
               .HasIndex(u => u.Username)
               .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            modelBuilder.Entity<DeviceToken>()
                .HasIndex(dt => dt.TokenHash)
                .IsUnique();

            // MaestroEstado
            modelBuilder.Entity<MaestroEstado>(entity =>
            {
                entity.ToTable("MAESTRO_ESTADOS");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_estado").ValueGeneratedNever();
                entity.Property(e => e.GrupoEstado).HasColumnName("grupo_estado").HasMaxLength(30).IsRequired();
                entity.Property(e => e.NombreEstado).HasColumnName("nombre_estado").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(100);
                entity.Property(e => e.ColorHex).HasColumnName("color_hex").HasMaxLength(10);
            });

            // CalendarioFeriados
            modelBuilder.Entity<CalendarioFeriado>(entity =>
            {
                entity.ToTable("CALENDARIO_FERIADOS");
                entity.HasKey(e => e.Fecha);
                entity.Property(e => e.Fecha).HasColumnName("fecha");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(100);
                entity.Property(e => e.EsFeriado).HasColumnName("es_feriado").HasDefaultValue(true);
            });


            // Persona
            modelBuilder.Entity<Persona>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("PERSONAS");
                entity.Property(e => e.Id).HasColumnName("id_persona");
                entity.HasIndex(e => e.Dni).IsUnique();
                entity.Property(e => e.Dni).HasColumnName("dni").HasMaxLength(15).IsRequired();
                entity.Property(e => e.ApellidosNombres).HasColumnName("apellidos_nombres").HasMaxLength(100).IsRequired();
                entity.Property(e => e.CorreoPersonal).HasColumnName("correo_personal").HasMaxLength(100);
                entity.Property(e => e.TelefonoPersonal).HasColumnName("telefono_personal").HasMaxLength(20);
            });

            // SucursalCentro
            modelBuilder.Entity<SucursalCentro>(entity =>
            {
                entity.ToTable("SUCURSAL");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_sucursal");
                entity.HasIndex(e => e.NombreSucursal).IsUnique();
                entity.Property(e => e.NombreSucursal).HasColumnName("nombre_sucursal").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Direccion).HasColumnName("direccion").HasMaxLength(150);
                entity.Property(e => e.LatitudCentro).HasColumnName("latitud_centro").HasColumnType("decimal(10, 8)");
                entity.Property(e => e.LongitudCentro).HasColumnName("longitud_centro").HasColumnType("decimal(11, 8)");
                entity.Property(e => e.PerimetroM).HasColumnName("perimetro_m");
                entity.Property(e => e.EsActivo).HasColumnName("es_activo");
            });

            // Trabajador
            modelBuilder.Entity<Trabajador>(entity =>
            {
                entity.ToTable("TRABAJADORES");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_trabajador");
                entity.HasIndex(e => e.PersonaId).IsUnique();
                entity.Property(e => e.PersonaId).HasColumnName("id_persona").IsRequired();
                entity.Property(e => e.JefeInmediatoId).HasColumnName("id_jefe_inmediato");
                entity.Property(e => e.SucursalId).HasColumnName("id_sucursal");
                entity.Property(e => e.UserId).HasColumnName("id_user");
                entity.Property(e => e.Cargo).HasColumnName("cargo").HasMaxLength(50);
                entity.Property(e => e.AreaDepartamento).HasColumnName("area_departamento").HasMaxLength(50);
                entity.Property(e => e.IdArea).HasColumnName("id_area");
                entity.Property(e => e.FechaIngreso).HasColumnName("fecha_ingreso");
                entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
                entity.Property(e => e.IdEstado).HasColumnName("id_estado").IsRequired().HasDefaultValue(10);
                entity.Property(e => e.SueldoBruto).HasColumnName("sueldo_bruto").HasColumnType("decimal(10, 2)");
                entity.Property(e => e.CorreoCorporativo).HasColumnName("correo_corporativo").HasMaxLength(100);
                entity.Property(e => e.TelefonoCorporativo).HasColumnName("telefono_corporativo").HasMaxLength(20);
                entity.Property(e => e.HorasExtraConf).HasColumnName("horas_extra_conf").HasDefaultValue(false);
                entity.Property(e => e.MarcajeEnZona).HasColumnName("marcaje_en_zona").HasDefaultValue(true);
                entity.Property(e => e.TomarFoto).HasColumnName("tomar_foto").HasDefaultValue(true);

                entity.HasOne(d => d.Persona).WithMany().HasForeignKey(d => d.PersonaId);
                entity.HasOne(d => d.Estado).WithMany().HasForeignKey(d => d.IdEstado);
                entity.HasOne(d => d.Sucursal).WithMany().HasForeignKey(d => d.SucursalId);
                entity.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);
                entity.HasOne(d => d.JefeInmediato).WithMany().HasForeignKey(d => d.JefeInmediatoId).OnDelete(DeleteBehavior.NoAction);
            });

            // TipoTurno
            modelBuilder.Entity<TipoTurno>(entity =>
            {
                entity.ToTable("TIPO_TURNO");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_tipo_turno");
                entity.HasIndex(e => e.NombreTipo).IsUnique();
                entity.Property(e => e.NombreTipo).HasColumnName("nombre_tipo").HasMaxLength(50).IsRequired();
            });

            // Turno
            modelBuilder.Entity<Turno>(entity =>
            {
                entity.ToTable("TURNOS");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_turno");
                entity.HasIndex(e => e.NombreCodigo).IsUnique();
                entity.Property(e => e.TipoTurnoId).HasColumnName("id_tipo_turno").IsRequired();
                entity.Property(e => e.NombreCodigo).HasColumnName("nombre_codigo").HasMaxLength(20).IsRequired();
                entity.Property(e => e.ToleranciaIngreso).HasColumnName("tolerancia_ingreso");
                entity.Property(e => e.ToleranciaSalida).HasColumnName("tolerancia_salida");
                entity.Property(e => e.EsActivo).HasColumnName("es_activo").HasDefaultValue(true);

                entity.HasOne(d => d.TipoTurno).WithMany().HasForeignKey(d => d.TipoTurnoId);
            });

            // HorarioTurno
            modelBuilder.Entity<HorarioTurno>(entity =>
            {
                entity.ToTable("HORARIOS_TURNO");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_horario_turno");
                entity.Property(e => e.TurnoId).HasColumnName("id_turno").IsRequired();
                entity.Property(e => e.NombreHorario).HasColumnName("nombre_horario").HasMaxLength(50).IsRequired();
                entity.Property(e => e.EsActivo).HasColumnName("es_activo").HasDefaultValue(true);

                entity.HasOne(d => d.Turno).WithMany(p => p.HorariosTurno).HasForeignKey(d => d.TurnoId);
            });

            // HorarioDetalle
            modelBuilder.Entity<HorarioDetalle>(entity =>
            {
                entity.ToTable("HORARIOS_DETALLE");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_detalle");
                entity.Property(e => e.HorarioTurnoId).HasColumnName("id_horario_turno").IsRequired();
                entity.Property(e => e.DiaSemana).HasColumnName("dia_semana").HasMaxLength(15).IsRequired();
                entity.Property(e => e.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time(0)").IsRequired();
                entity.Property(e => e.HoraFin).HasColumnName("hora_fin").HasColumnType("time(0)").IsRequired();
                entity.Property(e => e.HoraInicioRefrigerio).HasColumnName("hora_inicio_refrigerio").HasColumnType("time(0)");
                entity.Property(e => e.HoraFinRefrigerio).HasColumnName("hora_fin_refrigerio").HasColumnType("time(0)");
                entity.Property(e => e.TiempoRefrigerioMinutos).HasColumnName("tiempo_refrigerio_minutos").HasDefaultValue(60);
                entity.Property(e => e.SalidaDiaSiguiente).HasColumnName("salida_dia_siguiente").HasDefaultValue(false);

                entity.HasOne(d => d.HorarioTurno).WithMany(p => p.HorariosDetalle).HasForeignKey(d => d.HorarioTurnoId);
            });

            // AsignacionTurno
            modelBuilder.Entity<AsignacionTurno>(entity =>
            {
                entity.ToTable("ASIGNACIONES_TURNO");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_asignacion").ValueGeneratedOnAdd();

                entity.Property(e => e.TrabajadorId).HasColumnName("id_trabajador").IsRequired();
                entity.Property(e => e.TurnoId).HasColumnName("id_turno").IsRequired();
                entity.Property(e => e.HorarioTurnoId).HasColumnName("id_horario_turno");
                entity.Property(e => e.FechaInicioVigencia).HasColumnName("fecha_inicio_vigencia").IsRequired();
                entity.Property(e => e.FechaFinVigencia).HasColumnName("fecha_fin_vigencia");
                entity.Property(e => e.EsVigente).HasColumnName("es_vigente").HasDefaultValue(true);
                entity.Property(e => e.MotivoCambio).HasColumnName("motivo_cambio").HasMaxLength(30);
                entity.Property(e => e.AprobadoPor).HasColumnName("aprobado_por");
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime2");

                entity.HasOne(d => d.Trabajador).WithMany(p => p.AsignacionesTurno).HasForeignKey(d => d.TrabajadorId);
                entity.HasOne(d => d.Turno).WithMany().HasForeignKey(d => d.TurnoId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(d => d.HorarioTurno).WithMany().HasForeignKey(d => d.HorarioTurnoId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<Trabajador>().WithMany().HasForeignKey(d => d.AprobadoPor).OnDelete(DeleteBehavior.NoAction);
            });

            // TipoJustificacion
            modelBuilder.Entity<TipoJustificacion>(entity =>
            {
                entity.ToTable("TIPO_JUSTIFICACION");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_tipo_justificacion");
                entity.HasIndex(e => e.NombreTipo).IsUnique();
                entity.Property(e => e.NombreTipo).HasColumnName("nombre_tipo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.RequiereAdjunto).HasColumnName("requiere_adjunto").HasDefaultValue(false);
                entity.Property(e => e.EsActivo).HasColumnName("es_activo").HasDefaultValue(true);
            });

            // Justificacion
            modelBuilder.Entity<Justificacion>(entity =>
            {
                entity.ToTable("JUSTIFICACIONES");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_justificacion");
                entity.Property(e => e.TrabajadorId).HasColumnName("id_trabajador").IsRequired();
                entity.Property(e => e.TipoJustificacionId).HasColumnName("id_tipo_justificacion").IsRequired();
                entity.Property(e => e.FechaJustificada).HasColumnName("fecha_justificada").IsRequired();
                entity.Property(e => e.Motivo).HasColumnName("motivo").HasMaxLength(1000);
                entity.Property(e => e.DocumentoAdjuntoUrl).HasColumnName("documento_adjunto_url").HasMaxLength(255);
                entity.Property(e => e.IdEstado).HasColumnName("id_estado").IsRequired().HasDefaultValue(1);
                entity.Property(e => e.FechaAutorizacion).HasColumnName("fecha_autorizacion");
                entity.Property(e => e.UsuarioAutoriza).HasColumnName("usuario_autoriza").HasMaxLength(50);
                entity.Property(e => e.IdAutoriza).HasColumnName("id_autoriza");

                entity.HasOne(d => d.Trabajador).WithMany().HasForeignKey(d => d.TrabajadorId);
                entity.HasOne(d => d.Estado).WithMany().HasForeignKey(d => d.IdEstado);
                entity.HasOne(d => d.TipoJustificacion).WithMany().HasForeignKey(d => d.TipoJustificacionId);
                entity.HasOne<Trabajador>().WithMany().HasForeignKey(d => d.IdAutoriza).OnDelete(DeleteBehavior.NoAction);
            });

            // MarcacionAsistencia
            modelBuilder.Entity<MarcacionAsistencia>(entity =>
            {
                entity.ToTable("MARCACIONES_ASISTENCIA");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_marcacion");
                entity.Property(e => e.TrabajadorId).HasColumnName("id_trabajador").IsRequired();
                entity.Property(e => e.FechaHora).HasColumnName("fecha_hora").IsRequired();
                entity.Property(e => e.TipoMarcacion).HasColumnName("tipo_marcacion").HasMaxLength(15).IsRequired();
                entity.Property(e => e.Latitud).HasColumnName("latitud").HasColumnType("decimal(10, 8)");
                entity.Property(e => e.Longitud).HasColumnName("longitud").HasColumnType("decimal(11, 8)");
                entity.Property(e => e.FotoUrl).HasColumnName("foto_url").HasMaxLength(255);
                entity.Property(e => e.UbicacionValida).HasColumnName("ubicacion_valida");
                entity.Property(e => e.EsMockLocation).HasColumnName("es_mock_location");
                entity.Property(e => e.SucursalId).HasColumnName("id_sucursal");
                entity.Property(e => e.SucursalMarcacionId).HasColumnName("id_sucursal_marcacion");

                // TokenValidacion no se usa por ahora, ignorar en mapeador
                entity.Ignore(e => e.TokenValidacion);

                entity.HasOne(d => d.Trabajador).WithMany().HasForeignKey(d => d.TrabajadorId);
                entity.HasOne(d => d.Sucursal).WithMany().HasForeignKey(d => d.SucursalId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(d => d.SucursalMarcacion).WithMany().HasForeignKey(d => d.SucursalMarcacionId).OnDelete(DeleteBehavior.NoAction);
            });

            // SolicitudHorasExtra
            modelBuilder.Entity<SolicitudHorasExtra>(entity =>
            {
                entity.ToTable("SOLICITUDES_HORAS_EXTRA");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_solicitud");
                entity.Property(e => e.TrabajadorId).HasColumnName("id_trabajador").IsRequired();
                entity.Property(e => e.FechaSolicitud).HasColumnName("fecha_solicitud").IsRequired();
                entity.Property(e => e.HorasSolicitadas).HasColumnName("horas_solicitadas").HasColumnType("decimal(5, 2)").IsRequired();
                entity.Property(e => e.Motivo).HasColumnName("motivo").HasMaxLength(1000);
                entity.Property(e => e.IdEstado).HasColumnName("id_estado").IsRequired().HasDefaultValue(1);
                entity.Property(e => e.IdJefeAprueba).HasColumnName("id_jefe_aprueba");
                entity.Property(e => e.FechaAprobacion).HasColumnName("fecha_aprobacion");

                entity.HasOne(d => d.Trabajador).WithMany().HasForeignKey(d => d.TrabajadorId);
                entity.HasOne(d => d.Estado).WithMany().HasForeignKey(d => d.IdEstado);
                entity.HasOne<Trabajador>().WithMany().HasForeignKey(d => d.IdJefeAprueba);
            });

            // AsistenciaResumenDiario
            modelBuilder.Entity<AsistenciaResumenDiario>(entity =>
            {
                entity.ToTable("ASISTENCIA_RESUMEN_DIARIO");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_resumen");
                entity.HasIndex(e => new { e.TrabajadorId, e.FechaAsistencia }).IsUnique();

                entity.Property(e => e.TrabajadorId).HasColumnName("id_trabajador").IsRequired();
                entity.Property(e => e.FechaAsistencia).HasColumnName("fecha_asistencia").IsRequired();
                entity.Property(e => e.HoraEntradaTeorica).HasColumnName("hora_entrada_teorica");
                entity.Property(e => e.HoraSalidaTeorica).HasColumnName("hora_salida_teorica");
                entity.Property(e => e.HoraEntradaReal).HasColumnName("hora_entrada_real");
                entity.Property(e => e.HoraSalidaReal).HasColumnName("hora_salida_real");
                entity.Property(e => e.MinutosTardanza).HasColumnName("minutos_tardanza").HasDefaultValue(0);
                entity.Property(e => e.MinutosExtra).HasColumnName("minutos_extra").HasDefaultValue(0);
                entity.Property(e => e.EstadoAsistencia).HasColumnName("estado_asistencia").HasMaxLength(30);
                entity.Property(e => e.EsDiaBoleta).HasColumnName("es_dia_boleta").HasDefaultValue(false);
                entity.Property(e => e.IdCoberturaOrigen).HasColumnName("id_cobertura_origen");

                entity.HasOne(d => d.Trabajador).WithMany().HasForeignKey(d => d.TrabajadorId);
            });

            // AuditLogin
            modelBuilder.Entity<AuditLogin>(entity =>
            {
                entity.ToTable("AUDIT_LOGIN");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_audit").ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.UsernameIntentado).HasColumnName("username_intentado").HasMaxLength(100).IsRequired();
                entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45).IsRequired();
                entity.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
                entity.Property(e => e.Resultado).HasColumnName("resultado").HasMaxLength(10).IsRequired();
                entity.Property(e => e.MotivoFallo).HasColumnName("motivo_fallo").HasMaxLength(60);
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();
                entity.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.SetNull);
            });

            // Notificacion
            modelBuilder.Entity<Notificacion>(entity =>
            {
                entity.ToTable("NOTIFICACIONES");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_notificacion").ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).HasColumnName("id_user").IsRequired();
                entity.Property(e => e.Tipo).HasColumnName("tipo").HasMaxLength(30).IsRequired();
                entity.Property(e => e.Titulo).HasColumnName("titulo").HasMaxLength(80).IsRequired();
                entity.Property(e => e.Mensaje).HasColumnName("mensaje").HasMaxLength(300).IsRequired();
                entity.Property(e => e.Leida).HasColumnName("leida").HasDefaultValue(false);
                entity.Property(e => e.LeidaAt).HasColumnName("leida_at").HasColumnType("datetime2");
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();
                entity.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);
            });

            // ProgramacionDescanso
            modelBuilder.Entity<ProgramacionDescanso>(entity =>
            {
                entity.ToTable("PROGRAMACION_DESCANSOS");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_programacion").ValueGeneratedOnAdd();
                entity.Property(e => e.TrabajadorId).HasColumnName("id_trabajador").IsRequired();
                entity.Property(e => e.Fecha).HasColumnName("fecha").IsRequired();
                entity.Property(e => e.EsDescanso).HasColumnName("es_descanso").HasDefaultValue(false);
                entity.Property(e => e.EsDiaBoleta).HasColumnName("es_dia_boleta").HasDefaultValue(false);
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime2");
                entity.HasIndex(e => new { e.TrabajadorId, e.Fecha }).IsUnique();
                entity.HasOne(d => d.Trabajador).WithMany().HasForeignKey(d => d.TrabajadorId);
            });

            // CoberturaTurno
            modelBuilder.Entity<CoberturaTurno>(entity =>
            {
                entity.ToTable("COBERTURA_TURNOS");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id_cobertura").ValueGeneratedOnAdd();
                entity.Property(e => e.Fecha).HasColumnName("fecha").IsRequired();
                entity.Property(e => e.IdTrabajadorCubre).HasColumnName("id_trabajador_cubre").IsRequired();
                entity.Property(e => e.IdTrabajadorAusente).HasColumnName("id_trabajador_ausente").IsRequired();
                entity.Property(e => e.IdHorarioTurnoOriginal).HasColumnName("id_horario_turno_original").IsRequired();
                entity.Property(e => e.TipoCobertura).HasColumnName("tipo_cobertura").HasMaxLength(15).IsRequired();
                entity.Property(e => e.FechaSwapDevolucion).HasColumnName("fecha_swap_devolucion");
                entity.Property(e => e.AprobadoPor).HasColumnName("aprobado_por");
                entity.Property(e => e.Estado).HasColumnName("estado").HasMaxLength(12).HasDefaultValue("PENDIENTE");
                entity.Property(e => e.IdCoberturaReciproca).HasColumnName("id_cobertura_reciproca");
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("datetime2")
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime2");
                entity.HasIndex(e => new { e.Fecha, e.IdTrabajadorAusente }).IsUnique();
                entity.HasOne(d => d.TrabajadorCubre).WithMany().HasForeignKey(d => d.IdTrabajadorCubre).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(d => d.TrabajadorAusente).WithMany().HasForeignKey(d => d.IdTrabajadorAusente).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(d => d.HorarioTurnoOriginal).WithMany().HasForeignKey(d => d.IdHorarioTurnoOriginal).OnDelete(DeleteBehavior.NoAction);
            });

            // TrabajadorSucursal
            modelBuilder.Entity<TrabajadorSucursal>(entity =>
            {
                entity.ToTable("TRABAJADOR_SUCURSALES");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TrabajadorId).HasColumnName("id_trabajador").IsRequired();
                entity.Property(e => e.SucursalId).HasColumnName("id_sucursal").IsRequired();
                entity.Property(e => e.EsSucursalPrincipal).HasColumnName("es_sucursal_principal").HasDefaultValue(false);
                entity.Property(e => e.PuedeGestionar).HasColumnName("puede_gestionar").HasDefaultValue(false);
                entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio").IsRequired();
                entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
                entity.HasIndex(e => new { e.TrabajadorId, e.SucursalId }).IsUnique();
                entity.HasOne(d => d.Trabajador).WithMany(p => p.TrabajadorSucursales).HasForeignKey(d => d.TrabajadorId);
                entity.HasOne(d => d.Sucursal).WithMany().HasForeignKey(d => d.SucursalId).OnDelete(DeleteBehavior.NoAction);
            });


            modelBuilder.Entity<ProgramacionTurnoSemanal>(entity =>
            {
                entity.ToTable("PROGRAMACION_TURNOS_SEMANAL");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TrabajadorId).HasColumnName("id_trabajador").IsRequired();
                entity.Property(e => e.Fecha).HasColumnName("fecha").IsRequired();
                entity.Property(e => e.IdHorarioTurno).HasColumnName("id_horario_turno").IsRequired(false);
                entity.Property(e => e.EsDescanso).HasColumnName("es_descanso").HasDefaultValue(false);
                entity.Property(e => e.EsDiaBoleta).HasColumnName("es_dia_boleta").HasDefaultValue(false);
                entity.Property(e => e.EsVacaciones).HasColumnName("es_vacaciones").HasDefaultValue(false);
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(e => new { e.TrabajadorId, e.Fecha }).IsUnique();
                entity.HasOne(d => d.Trabajador).WithMany().HasForeignKey(d => d.TrabajadorId);
                entity.HasOne(d => d.HorarioTurno).WithMany().HasForeignKey(d => d.IdHorarioTurno)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
            });



        }


    }
}