export interface TardanzaReporte {
  dni: string;
  nombre: string;
  area: string | null;
  fecha: string;
  estado: string;
  hora_Turno: string;
  hora_Marcacion: string;
  minutos_Late: number;
  tiempo_Tardanza_Texto: string;
}

export interface DashboardResumen {
  totalTrabajadores: number;
  presenteHoy: number;
  tardanzaHoy: number;
  faltaHoy: number;
  porcentajeAsistencia: number;
  ausenciasSemana: number;
  coberturasPendientes: number;
  sinProgramacion: number;
  fechaConsulta: string;
  inicioSemana: string;
  finSemana: string;
}

export interface AusenciaRegistrada {
  id: number;
  trabajadorId: number;
  trabajadorNombre: string;
  dni: string;
  fecha: string;
  tipoAusencia: string;
}

export interface SucursalCentro {
  id: number;
  nombreSucursal: string;
  direccion?: string;
  latitudCentro?: number;
  longitudCentro?: number;
  perimetroM?: number;
  esActivo?: boolean;
}

export interface TipoTurno {
  id: number;
  nombreTipo: string;
  descripcion?: string;
}

export interface Turno {
  id: number;
  nombreCodigo: string;
  tipoTurnoId: number;
  tipoTurno?: TipoTurno;
  descripcion?: string;
  esActivo: boolean;
}

export interface HorarioDetalle {
  id: number;
  horarioTurnoId: number;
  diaSemana: string;
  horaInicio: string;       // "HH:mm:ss" from API
  horaFin: string;
  horaInicioRefrigerio?: string;
  horaFinRefrigerio?: string;
  tiempoRefrigerioMinutos: number;
  salidaDiaSiguiente: boolean;
}

export interface HorarioDetalleRequest {
  diaSemana: string;
  horaInicio: string;       // "HH:mm:ss" sent to API
  horaFin: string;
  horaInicioRefrigerio?: string | null;
  horaFinRefrigerio?: string | null;
  tiempoRefrigerioMinutos: number;
  salidaDiaSiguiente: boolean;
}

export interface HorarioTurno {
  id: number;
  turnoId: number;
  nombreHorario: string;
  esActivo: boolean;
  turno?: Turno;
  horariosDetalle?: HorarioDetalle[];
}

export interface HorarioTurnoRequest {
  turnoId: number;
  nombreHorario: string;
  esActivo: boolean;
}

export interface Persona {
  id: number;
  dni: string;
  apellidosNombres: string;
  fechaNacimiento?: string;
  telefono?: string;
  email?: string;
}

export interface PersonaCreateDto {
  dni: string;
  apellidosNombres: string;
  fechaNacimiento?: string;
  telefono?: string;
  email?: string;
}

export interface Trabajador {
  id: number;
  personaId: number;
  sucursalId: number;
  idEstado: number;
  persona?: Persona;
  sucursal?: SucursalCentro;
}

export interface TrabajadorResponseDto {
  id: number;
  personaId: number;
  sucursalId: number;
  idEstado: number;
  dni: string;
  apellidosNombres: string;
  nombreSucursal?: string;
  tipoTurno?: string;
  idTurno?: number;
  idHorarioTurno?: number;
  horarioTurnoNombre?: string;
  username?: string;
  userId?: number;
}

export interface TrabajadorMapped {
  id: number;
  name: string;
  dni: string;
  sucursalId: number;
  idEstado: number;
  tipo: 'ROT' | 'FIJ';
  idTurno?: number;
  idHorarioTurno?: number;
  horarioTurnoNombre?: string;
}

export interface TrabajadorSucursal {
  id: number;
  trabajadorId: number;
  sucursalId: number;
  nombreSucursal?: string;
  esPrincipal: boolean;
  puedeGestionar: boolean;
  fechaInicio: string;
  fechaFin?: string;
}

export interface TrabajadorCreateDto {
  personaId: number;
  sucursalId: number;
  idEstado: number;
}

export interface CrearTrabajadorCompletoDto {
  // Persona
  dni: string;
  apellidosNombres: string;
  email?: string;
  telefono?: string;
  // Usuario
  username: string;
  password: string;
  role: string;
  // Trabajador
  sucursalId?: number;
  cargo?: string;
  areaDepartamento?: string;
  jefeInmediatoId?: number;
  marcajeEnZona: boolean;
  tomarFoto: boolean;
  fechaIngreso?: string;
  // Turno (opcional)
  turnoId?: number;
  horarioTurnoId?: number;
  fechaInicioVigencia?: string;
}

export interface AsignacionTurnoCreateDto {
  trabajadorId: number;
  turnoId: number;
  horarioTurnoId?: number;
  fechaInicioVigencia: string;
  fechaFinVigencia?: string;
  motivoCambio?: string;
  aprobadoPor?: string;
}

export interface AsignacionTurnoResponse {
  id: number;
  trabajadorId: number;
  trabajadorNombre: string;
  trabajadorDni: string;
  turnoId: number;
  turnoNombre: string;
  tipoTurno: string;
  horarioTurnoId?: number;
  horarioTurnoNombre?: string;
  esVigente: boolean;
  fechaInicioVigencia: string;
  fechaFinVigencia?: string;
  motivoCambio?: string;
  aprobadoPor?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface ProgramacionSemanalRequest {
  fechaInicio: string;
  fechaFin: string;
  programaciones: ProgramacionDiaDto[];
}

export interface ProgramacionDiaDto {
  trabajadorId: number;
  fecha: string;
  idHorarioTurno: number | null;
  esDescanso: boolean;
  esDiaBoleta: boolean;
  esVacaciones: boolean;
  tipoAusencia?: string | null;
}

export interface ProgramacionSemanalItem {
  trabajadorId: number;
  trabajadorNombre: string;
  sucursalId?: number;
  tipo?: string;
}

export interface PtsResponse {
  items: ProgramacionSemanalItem[];
  totalCount: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

export interface PtsDiaRecord {
  fecha: string;
  horarioTurnoId?: number;
  horarioTurnoNombre?: string;
  turnoId?: number;
  estado?: string;
  tipoAusencia?: string | null;
  esDescanso?: boolean;
  esDiaBoleta?: boolean;
  esVacaciones?: boolean;
}

export interface Cobertura {
  idCobertura?: number;
  id?: number;
  fecha: string;
  idTrabajadorCubre?: number;
  idTrabajadorAusente: number;
  idHorarioTurnoOriginal?: number;
  tipoCobertura?: string;
  tipo?: string;
  estado: string;
  motivoFalta?: string;
  fechaSwapDevolucion?: string;
}

export interface CoberturaCreateDto {
  fecha: string;
  idTrabajadorCubre?: number | null;
  idTrabajadorAusente?: number | null;
  idHorarioTurnoOriginal?: number | null;
  tipoCobertura: string;
  motivoFalta?: string | null;
  fechaSwapDevolucion?: string | null;
  esSoloAsignacion?: boolean;
}

export interface ResumenDiario {
  trabajadorId: number;
  trabajadorNombre: string;
  dni?: string;
  fecha: string;
  tipoResumen: string;
  horasTeoricas?: number;
  horasReales?: number;
  minutosTardanza?: number;
  esHoraExtra?: boolean;
  observacion?: string;
}

export interface Notificacion {
  id: number;
  titulo: string;
  mensaje: string;
  tipo: string;
  fechaCreacion: string;
  leida: boolean;
  trabajadorId?: number;
}

export interface ReporteAsistenciaParams {
  fechaInicio: string;
  fechaFin: string;
  sucursalId?: number;
  trabajadorId?: number;
}
