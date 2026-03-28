import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/common.models';
import {
  SucursalCentro, TipoTurno, Turno, HorarioTurno, HorarioTurnoRequest, HorarioDetalle, HorarioDetalleRequest,
  Trabajador, TrabajadorResponseDto, TrabajadorCreateDto, CrearTrabajadorCompletoDto, TrabajadorSucursal,
  AsignacionTurnoCreateDto, AsignacionTurnoResponse,
  ProgramacionSemanalRequest, PtsResponse, PtsDiaRecord,
  Cobertura, CoberturaCreateDto,
  ResumenDiario, Notificacion, TardanzaReporte, AusenciaRegistrada, DashboardResumen
} from '../models/rrhh.models';

@Injectable({ providedIn: 'root' })
export class RrhhService {
  private readonly api = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // ── SUCURSALES ──────────────────────────────────────────────
  getSucursales(): Observable<SucursalCentro[]> {
    return this.http.get<PagedResult<SucursalCentro>>(`${this.api}/api/Rrhh/Sucursales?pageSize=500`).pipe(map(r => r.items));
  }

  createSucursal(data: Partial<SucursalCentro>): Observable<SucursalCentro> {
    return this.http.post<SucursalCentro>(`${this.api}/api/Rrhh/Sucursales`, data);
  }

  updateSucursal(id: number, data: Partial<SucursalCentro>): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Rrhh/Sucursales/${id}`, { id, ...data });
  }

  deleteSucursal(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/Rrhh/Sucursales/${id}`);
  }

  // ── TIPOS TURNO ──────────────────────────────────────────────
  getTiposTurno(): Observable<TipoTurno[]> {
    return this.http.get<PagedResult<TipoTurno>>(`${this.api}/api/Rrhh/TipoTurno`).pipe(map(r => r.items));
  }

  createTipoTurno(data: Partial<TipoTurno>): Observable<TipoTurno> {
    return this.http.post<TipoTurno>(`${this.api}/api/Rrhh/TipoTurno`, data);
  }

  updateTipoTurno(id: number, data: Partial<TipoTurno>): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Rrhh/TipoTurno/${id}`, { id, ...data });
  }

  deleteTipoTurno(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/Rrhh/TipoTurno/${id}`);
  }

  // ── TURNOS ──────────────────────────────────────────────────
  getTurnos(): Observable<Turno[]> {
    return this.http.get<PagedResult<Turno>>(`${this.api}/api/Rrhh/Turnos`).pipe(map(r => r.items));
  }

  createTurno(data: Partial<Turno>): Observable<Turno> {
    return this.http.post<Turno>(`${this.api}/api/Rrhh/Turnos`, data);
  }

  updateTurno(id: number, data: Partial<Turno>): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Rrhh/Turnos/${id}`, { id, ...data });
  }

  deleteTurno(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/Rrhh/Turnos/${id}`);
  }

  // ── HORARIOS TURNO ───────────────────────────────────────────
  getHorariosTurno(): Observable<HorarioTurno[]> {
    return this.http.get<HorarioTurno[]>(`${this.api}/api/Rrhh/HorarioTurno`);
  }

  createHorarioTurno(data: HorarioTurnoRequest): Observable<HorarioTurno> {
    return this.http.post<HorarioTurno>(`${this.api}/api/Rrhh/HorarioTurno`, data);
  }

  updateHorarioTurno(id: number, data: Partial<HorarioTurno>): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Rrhh/HorarioTurno/${id}`, { id, ...data });
  }

  // ── HORARIO DETALLE ──────────────────────────────────────────
  createHorarioDetalle(horarioTurnoId: number, data: HorarioDetalleRequest): Observable<HorarioDetalle> {
    return this.http.post<HorarioDetalle>(`${this.api}/api/Rrhh/HorarioTurno/${horarioTurnoId}/detalles`, this.toTimeSpanDto(data));
  }

  updateHorarioDetalle(horarioTurnoId: number, detalleId: number, data: HorarioDetalleRequest): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Rrhh/HorarioTurno/${horarioTurnoId}/detalles/${detalleId}`, this.toTimeSpanDto(data));
  }

  deleteHorarioDetalle(horarioTurnoId: number, detalleId: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/Rrhh/HorarioTurno/${horarioTurnoId}/detalles/${detalleId}`);
  }

  private toTimeSpanDto(data: HorarioDetalleRequest): any {
    return {
      ...data,
      horaInicio: this.ensureSec(data.horaInicio),
      horaFin: this.ensureSec(data.horaFin),
      horaInicioRefrigerio: data.horaInicioRefrigerio ? this.ensureSec(data.horaInicioRefrigerio) : null,
      horaFinRefrigerio: data.horaFinRefrigerio ? this.ensureSec(data.horaFinRefrigerio) : null,
    };
  }

  private ensureSec(t: string): string { return t.length === 5 ? t + ':00' : t; }

  // ── TRABAJADORES ─────────────────────────────────────────────
  getTrabajadores(pageNumber = 1, pageSize = 20, search = '', sucursalId?: number, tipo?: string): Observable<PagedResult<TrabajadorResponseDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (sucursalId) params = params.set('sucursalId', sucursalId);
    if (tipo) params = params.set('tipo', tipo);
    return this.http.get<any>(`${this.api}/api/Rrhh/Trabajadores`, { params }).pipe(
      map(r => {
        // Handle both camelCase (after AddJsonOptions fix) and PascalCase (legacy)
        const itemsArr: any[] = r.items ?? r.Items ?? [];
        const mapped = itemsArr.map((w: any) => ({
          id: w.id ?? w.Id,
          personaId: w.personaId ?? w.PersonaId,
          sucursalId: w.sucursalId ?? w.SucursalId,
          idEstado: w.idEstado ?? w.IdEstado,
          dni: w.persona?.dni ?? w.Persona?.Dni ?? w.dni ?? w.Dni ?? '',
          apellidosNombres: w.persona?.apellidosNombres ?? w.Persona?.ApellidosNombres ?? w.apellidosNombres ?? w.ApellidosNombres ?? '',
          nombreSucursal: w.sucursal?.nombreSucursal ?? w.Sucursal?.NombreSucursal ?? w.nombreSucursal ?? w.NombreSucursal ?? '',
          tipoTurno: w.tipoTurno ?? w.TipoTurno,
          idTurno: w.idTurno ?? w.IdTurno,
          idHorarioTurno: w.idHorarioTurno ?? w.IdHorarioTurno,
          horarioTurnoNombre: w.horarioTurnoNombre ?? w.HorarioTurnoNombre
        } as TrabajadorResponseDto));
        return {
          items: mapped,
          totalCount: r.totalCount ?? r.TotalCount ?? 0,
          pageSize: r.pageSize ?? r.PageSize ?? pageSize,
          currentPage: r.currentPage ?? r.CurrentPage ?? pageNumber,
          totalPages: r.totalPages ?? r.TotalPages ?? 1
        } as PagedResult<TrabajadorResponseDto>;
      })
    );
  }

  getTrabajadorById(id: number): Observable<TrabajadorResponseDto> {
    return this.http.get<TrabajadorResponseDto>(`${this.api}/api/Rrhh/Trabajadores/${id}`);
  }

  createTrabajador(data: TrabajadorCreateDto): Observable<Trabajador> {
    return this.http.post<Trabajador>(`${this.api}/api/Rrhh/Trabajadores`, data);
  }

  crearTrabajadorCompleto(data: CrearTrabajadorCompletoDto): Observable<{ trabajadorId: number; personaId: number; userId: number }> {
    return this.http.post<any>(`${this.api}/api/Rrhh/Trabajadores/crear-completo`, data);
  }

  updateTrabajador(id: number, data: Partial<TrabajadorCreateDto>): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Rrhh/Trabajadores/${id}`, data);
  }

  darDeBaja(id: number): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Rrhh/Trabajadores/${id}/baja`, {});
  }

  reactivar(id: number): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Rrhh/Trabajadores/${id}/reactivar`, {});
  }

  // ── SEDES POR TRABAJADOR ──────────────────────────────────────
  getSucursalesDisponibles(trabajadorId: number): Observable<TrabajadorSucursal[]> {
    return this.http.get<{ trabajadorId: number; sedes: any[] }>(
      `${this.api}/api/Rrhh/Trabajadores/${trabajadorId}/sucursales-disponibles`
    ).pipe(map(r => r.sedes.map(s => ({
      id: s.id,
      trabajadorId,
      sucursalId: s.id,
      nombreSucursal: s.nombre,
      esPrincipal: s.esPrincipal,
      puedeGestionar: s.puedeGestionar,
      fechaInicio: s.fechaInicio ?? '',
      fechaFin: s.fechaFin
    } as TrabajadorSucursal))));
  }

  asignarSede(trabajadorId: number, data: { sucursalId: number; puedeGestionar: boolean; fechaInicio: string; fechaFin?: string }): Observable<any> {
    return this.http.post(`${this.api}/api/Rrhh/Trabajadores/${trabajadorId}/sucursales`, data);
  }

  removerSede(trabajadorId: number, sucursalId: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/Rrhh/Trabajadores/${trabajadorId}/sucursales/${sucursalId}`);
  }

  // ── PERSONAS ─────────────────────────────────────────────────
  getPersonas(search = ''): Observable<any[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<any[]>(`${this.api}/api/Rrhh/Personas`, { params });
  }

  createPersona(data: any): Observable<any> {
    return this.http.post<any>(`${this.api}/api/Rrhh/Personas`, data);
  }

  updatePersona(id: number, data: any): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Rrhh/Personas/${id}`, data);
  }

  // ── ASIGNACION TURNO ─────────────────────────────────────────
  getAsignaciones(pageNumber = 1, pageSize = 20): Observable<PagedResult<AsignacionTurnoResponse>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<AsignacionTurnoResponse>>(`${this.api}/api/Rrhh/AsignacionTurno`, { params });
  }

  createAsignacion(data: AsignacionTurnoCreateDto): Observable<any> {
    return this.http.post<any>(`${this.api}/api/Rrhh/AsignacionTurno`, data);
  }

  asignarTurno(trabajadorId: number, data: { turnoId: number; horarioTurnoId?: number | null; fechaInicioVigencia: string; motivoCambio?: string }): Observable<any> {
    return this.http.post<any>(`${this.api}/api/trabajadores/${trabajadorId}/asignar-turno`, data);
  }

  updateAsignacion(id: number, data: any): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Rrhh/AsignacionTurno/${id}`, data);
  }

  deleteAsignacion(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/Rrhh/AsignacionTurno/${id}`);
  }

  // ── PROGRAMACION SEMANAL ─────────────────────────────────────
  getProgramacion(
    fechaInicio: string, fechaFin: string,
    pageNumber = 1, pageSize = 20,
    tipoTurno = '', sucursalId?: number, search = ''
  ): Observable<PtsResponse> {
    let params = new HttpParams()
      .set('fechaInicio', fechaInicio)
      .set('fechaFin', fechaFin)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    if (tipoTurno) params = params.set('tipoTurno', tipoTurno);
    if (sucursalId) params = params.set('sucursalId', sucursalId);
    if (search) params = params.set('search', search);
    return this.http.get<PtsResponse>(`${this.api}/api/Rrhh/ProgramacionSemanal`, { params });
  }

  getProgramacionByTrabajador(trabajadorId: number, fechaInicio: string, fechaFin: string): Observable<PtsDiaRecord[]> {
    const params = new HttpParams()
      .set('fechaInicio', fechaInicio)
      .set('fechaFin', fechaFin);
    return this.http.get<PtsDiaRecord[]>(`${this.api}/api/Rrhh/ProgramacionSemanal/${trabajadorId}`, { params });
  }

  saveProgramacion(data: ProgramacionSemanalRequest): Observable<any> {
    return this.http.post<any>(`${this.api}/api/Rrhh/ProgramacionSemanal`, data);
  }

  getAusencias(params: { fechaInicio?: string; fechaFin?: string; trabajadorId?: number; tipo?: string }): Observable<AusenciaRegistrada[]> {
    let p = new HttpParams();
    if (params.fechaInicio) p = p.set('fechaInicio', params.fechaInicio);
    if (params.fechaFin)    p = p.set('fechaFin', params.fechaFin);
    if (params.trabajadorId) p = p.set('trabajadorId', params.trabajadorId);
    if (params.tipo)        p = p.set('tipo', params.tipo);
    return this.http.get<AusenciaRegistrada[]>(`${this.api}/api/Rrhh/ProgramacionSemanal/ausencias`, { params: p });
  }

  deleteAusencia(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/api/Rrhh/ProgramacionSemanal/ausencias/${id}`);
  }

  // ── COBERTURAS ───────────────────────────────────────────────
  getCoberturas(): Observable<Cobertura[]> {
    return this.http.get<Cobertura[]>(`${this.api}/api/Coberturas`);
  }

  createCobertura(data: CoberturaCreateDto): Observable<any> {
    return this.http.post<any>(`${this.api}/api/Coberturas`, data);
  }

  aprobarCobertura(id: number): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Coberturas/${id}/aprobar`, {});
  }

  rechazarCobertura(id: number): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Coberturas/${id}/rechazar`, {});
  }

  // ── RESUMEN DIARIO ───────────────────────────────────────────
  getResumenDiario(fecha: string, sucursalId?: number): Observable<ResumenDiario[]> {
    const params = new HttpParams().set('fecha', fecha);
    return this.http.get<any[]>(`${this.api}/api/Asistencia/resumen`, { params }).pipe(
      map(items => items.map(r => ({
        trabajadorId: r.idTrabajador,
        trabajadorNombre: r.nombre,
        dni: r.dni,
        fecha: r.fechaAsistencia,
        tipoResumen: r.estadoAsistencia,
        minutosTardanza: r.minutosTardanza,
        esHoraExtra: r.minutosExtra > 0
      } as ResumenDiario)))
    );
  }

  // ── NOTIFICACIONES ───────────────────────────────────────────
  getNotificaciones(): Observable<Notificacion[]> {
    return this.http.get<any[]>(`${this.api}/api/Notificaciones`).pipe(
      map(items => items.map(n => ({
        id: n.idNotificacion,
        titulo: n.titulo,
        mensaje: n.mensaje,
        tipo: n.tipo,
        fechaCreacion: n.createdAt,
        leida: n.leida
      } as Notificacion)))
    );
  }

  marcarLeida(id: number): Observable<void> {
    return this.http.put<void>(`${this.api}/api/Notificaciones/${id}/leer`, {});
  }

  // ── TARDANZAS ────────────────────────────────────────────────
  getTardanzas(fechaInicio: string, fechaFin: string): Observable<TardanzaReporte[]> {
    // API espera MM/DD/YYYY
    const toApiDate = (iso: string) => {
      const [y, m, d] = iso.split('-');
      return `${m}/${d}/${y}`;
    };
    const params = new HttpParams()
      .set('fechaInicio', toApiDate(fechaInicio))
      .set('fechaFin', toApiDate(fechaFin));
    return this.http.get<TardanzaReporte[]>(`${this.api}/api/Reporte/tardanzas`, { params });
  }

  // ── DASHBOARD ────────────────────────────────────────────────
  getDashboard(): Observable<DashboardResumen> {
    return this.http.get<DashboardResumen>(`${this.api}/api/Dashboard/resumen`);
  }

  // ── REPORTES ─────────────────────────────────────────────────
  getReporteAsistencia(params: any): Observable<any[]> {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([k, v]) => { if (v != null) httpParams = httpParams.set(k, String(v)); });
    return this.http.get<any[]>(`${this.api}/api/Reporte/resumen`, { params: httpParams });
  }
}
