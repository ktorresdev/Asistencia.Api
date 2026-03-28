import { Component, inject, signal, computed, OnInit, OnDestroy, effect, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { RrhhService } from '../../core/services/rrhh.service';
import { WeekService } from '../../core/services/week.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthService } from '../../core/services/auth.service';
import { ShellComponent, AppState } from '../../layout/shell.component';
import {
  TrabajadorMapped, HorarioTurno, SucursalCentro,
  PtsDiaRecord, ProgramacionSemanalRequest, CoberturaCreateDto, Cobertura
} from '../../core/models/rrhh.models';

type ShiftCode = 'M' | 'T' | 'N' | 'D' | 'B' | 'V' | 'F' | 'X' | 'S' | null;

interface ShiftEdit {
  worker: TrabajadorMapped;
  dayIndex: number;
}

@Component({
  selector: 'app-horario',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './horario.component.html',
  styleUrl: './horario.component.scss'
})
export class HorarioComponent implements OnInit, OnDestroy {
  private rrhhService = inject(RrhhService);
  week = inject(WeekService);
  toast = inject(ToastService);
  auth = inject(AuthService);

  // state from shell (passed via router outlet - we load independently)
  sedes = signal<SucursalCentro[]>([]);
  horariosTurno = signal<HorarioTurno[]>([]);
  allTrabajadores = signal<TrabajadorMapped[]>([]);
  htPorTurno = signal<Record<number, HorarioTurno[]>>({});

  // filter state
  filterTipo = signal<string>('ALL');
  searchQuery = signal<string>('');
  sedeFilter = signal<string>('');

  // PTS data: { [workerId]: { [dateISO]: PtsDiaRecord } }
  ptsSemana = signal<Record<number, Record<string, PtsDiaRecord>>>({});

  // local overrides before saving
  shiftOverrides = signal<Record<string, ShiftCode>>({});
  htOverrides = signal<Record<string, number>>({});

  loading = signal(false);

  // shift drawer
  shiftEdit = signal<ShiftEdit | null>(null);
  tempShift = signal<string>('W');
  tempHtId = signal<number | null>(null);
  drawerOpen = signal(false);

  // cobertura shortcut
  coberturaOpen    = signal(false);
  cobTipo          = signal('COBERTURA');
  cobEsReemplazo   = signal(true);
  cobMotivo        = signal('FSGH');
  cobDev           = signal('');
  cobTieneDescanso = signal(false);
  cobHt            = signal<number | null>(null);
  cobCubre         = signal<number | null>(null);
  cobCubreQuery    = signal('');
  cobCubreOpen     = signal(false);
  savingCob        = signal(false);

  // coberturas de la semana para detectar dobletas en la grilla
  coberturasWeek = signal<Cobertura[]>([]);

  readonly COB_TIPOS = ['COBERTURA', 'CAMBIO'];
  readonly COB_MOTIVOS = [
    { k: 'FSGH',    label: 'Sin goce de haber',   sub: 'El día no será remunerado' },
    { k: 'FCGH',    label: 'Con goce de haber',   sub: 'El día sí será remunerado' },
    { k: 'PERMISO', label: 'Permiso autorizado',  sub: 'Aprobado por jefatura' },
    { k: 'MEDICO',  label: 'Descanso médico',     sub: 'Con certificado médico' },
  ];
  readonly COB_MSGS: Record<string, string> = {
    COBERTURA: 'El trabajador ausente quedará registrado con falta cubierta y quien lo reemplaza quedará registrado como cobertura extra. Si cubrió en su día de descanso, indica la fecha de devolución del descanso compensatorio.',
    CAMBIO: 'Ambos trabajadores intercambian su turno ese día. Se registra el cambio para los dos, sin que ninguno quede como faltante.',
  };

  filteredCobCubre = computed(() => {
    const q = this.cobCubreQuery().toLowerCase();
    const editId = this.shiftEdit()?.worker.id;
    return this.allTrabajadores()
      .filter(w => w.idEstado !== 11 && w.id !== editId)
      .filter(w => !q || w.name.toLowerCase().includes(q) || w.dni.includes(q))
      .slice(0, 30);
  });

  readonly DAYS = ['LUN', 'MAR', 'MIE', 'JUE', 'VIE', 'SAB', 'DOM'];
  private readonly DIAS_FULL = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];
  readonly CHIP_OPTIONS = [
    { k: 'W', n: 'Trabaja', t: 'según horario', bg: '#0a1a2e', fg: '#7dd3fc' },
    { k: 'D', n: 'Descanso', t: 'día libre', bg: '#1c1409', fg: '#fcd34d' },
    { k: 'B', n: 'Falta', t: 'ausencia con descuento', bg: '#052e16', fg: '#86efac' },
    { k: 'V', n: 'Vacaciones', t: '', bg: '#0c1a2e', fg: '#93c5fd' },
  ];

  // Track previous offset so the effect only fires on actual week changes, not on init.
  // On init, loadData() is called from loadAllWorkers() after workers are ready.
  private prevOffset = this.week.weekOffset();

  private weekEffect = effect(() => {
    const offset = this.week.weekOffset();
    if (offset !== untracked(() => this.prevOffset)) {
      this.prevOffset = offset;
      untracked(() => this.loadData());
    }
  });

  ngOnInit(): void {
    this.loadMasterData();
  }

  ngOnDestroy(): void {
    this.weekEffect.destroy();
  }

  private loadMasterData(): void {
    this.rrhhService.getSucursales().subscribe(s => this.sedes.set(s));
    this.rrhhService.getHorariosTurno().subscribe(ht => {
      this.horariosTurno.set(ht);
      const map: Record<number, HorarioTurno[]> = {};
      ht.forEach(h => { if (!map[h.turnoId]) map[h.turnoId] = []; map[h.turnoId].push(h); });
      this.htPorTurno.set(map);
    });
    this.loadAllWorkers();
  }

  private loadAllWorkers(): void {
    this.loading.set(true);
    // Backend PaginationDto caps pageSize at 50 — load page 1 first, then remaining pages
    this.rrhhService.getTrabajadores(1, 50).pipe(
      switchMap(first => {
        const totalPages = first.totalPages ?? 1;
        if (totalPages <= 1) return of([first]);
        const rest = Array.from({ length: totalPages - 1 }, (_, i) =>
          this.rrhhService.getTrabajadores(i + 2, 50)
        );
        return forkJoin([of(first), ...rest]);
      })
    ).subscribe({
      next: pages => {
        const allItems = pages.flatMap(p => p.items ?? []);
        const mapped = allItems.map(w => {
          const tipoU = (w.tipoTurno ?? '').toUpperCase();
          const tipo: 'ROT' | 'FIJ' = tipoU.includes('ROT') ? 'ROT' : 'FIJ';
          return {
            id: w.id, name: w.apellidosNombres, dni: w.dni,
            sucursalId: w.sucursalId, idEstado: w.idEstado, tipo,
            idTurno: w.idTurno, idHorarioTurno: w.idHorarioTurno,
            horarioTurnoNombre: w.horarioTurnoNombre
          } as TrabajadorMapped;
        });
        this.allTrabajadores.set(mapped);
        this.loadData();
      },
      error: () => this.loading.set(false)
    });
  }

  loadData(): void {
    const dates = this.week.weekDates();
    const fi = this.week.toISO(dates[0]);
    const ff = this.week.toISO(dates[6]);
    this.loading.set(true);

    forkJoin({
      prog: this.rrhhService.getProgramacion(fi, ff, 1, 999),
      cobs: this.rrhhService.getCoberturas()
    }).subscribe({
      next: ({ prog: data, cobs }) => {
        // Filtrar coberturas de la semana actual
        const weekIsos = this.week.weekDates().map(d => this.week.toISO(d));
        this.coberturasWeek.set(
          (cobs ?? []).filter(c => c.estado !== 'RECHAZADO' && weekIsos.some(d => (c.fecha ?? '').startsWith(d)))
        );

        const pts: Record<number, Record<string, PtsDiaRecord>> = {};
        const tipoPerWorker: Record<number, { tipo: 'ROT' | 'FIJ'; turnoId?: number }> = {};

        (data.items ?? []).forEach((item: any) => {
          const wid = item.trabajadorId;
          pts[wid] = {};
          // tipoTurnoNombre comes from the item level (worker's TipoTurno.NombreTipo)
          const tipoTurnoNombre = (item.tipoTurnoNombre ?? '').toUpperCase();
          const tipo: 'ROT' | 'FIJ' = tipoTurnoNombre.includes('ROT') ? 'ROT' : 'FIJ';
          tipoPerWorker[wid] = { tipo, turnoId: item.turnoId };

          (item.dias ?? []).forEach((dia: any) => {
            const fecha = (dia.fecha ?? '').split('T')[0];
            pts[wid][fecha] = {
              fecha, horarioTurnoId: dia.horarioTurnoId,
              horarioTurnoNombre: dia.horarioTurnoNombre,
              turnoId: dia.turnoId, estado: dia.estado,
              tipoAusencia: dia.tipoAusencia ?? null
            };
          });
        });

        // Enrich workers with ROT/FIJ type from TipoTurno
        this.allTrabajadores.update(list => list.map(w => {
          const t = tipoPerWorker[w.id];
          if (!t) return w;
          return { ...w, tipo: t.tipo, idTurno: w.idTurno ?? t.turnoId };
        }));

        this.ptsSemana.set(pts);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  // ── COMPUTED ──────────────────────────────────────────────
  filteredWorkers = computed(() => {
    const q = this.searchQuery().toLowerCase();
    const sf = this.sedeFilter();
    const tipo = this.filterTipo();
    return this.allTrabajadores().filter(w => {
      const mT = tipo === 'ALL' || w.tipo === tipo;
      const mQ = !q || w.name.toLowerCase().includes(q) || w.dni.includes(q);
      const mS = !sf || String(w.sucursalId) === sf;
      return mT && mQ && mS && w.idEstado !== 11;
    });
  });

  stats = computed(() => {
    let asig = 0, desc = 0, sinA = 0;
    this.filteredWorkers().forEach(w => {
      for (let i = 0; i < 5; i++) {
        const s = this.getShift(w, i);
        if (s === 'D' || s === 'B' || s === 'V') desc++;
        else if (s) asig++;
        else if (w.tipo === 'ROT') sinA++;
      }
    });
    return { total: this.filteredWorkers().length, asig, desc, sinA };
  });

  // ── SHIFT RESOLUTION ─────────────────────────────────────
  getShift(w: TrabajadorMapped, dayIdx: number): ShiftCode {
    const override = this.shiftOverrides()[`${w.id}_${dayIdx}`];
    if (override !== undefined) return override;
    return this.getShiftFromPTS(w, dayIdx);
  }

  private getShiftFromPTS(w: TrabajadorMapped, dayIdx: number): ShiftCode {
    const dates = this.week.weekDates();
    const dateISO = this.week.toISO(dates[dayIdx]);
    const rec = this.ptsSemana()[w.id]?.[dateISO];
    if (!rec) return null;
    // tipoAusencia tiene prioridad — siempre se muestra como 'V' (vacación/ausencia)
    if (rec.tipoAusencia) return 'V';
    if (!rec.estado) return null;
    const e = rec.estado.toLowerCase();
    if (e === 'descanso') return 'D';
    if (e === 'boleta' || e === 'dia-boleta') return 'B';
    if (e === 'vacaciones') return 'V';
    if (e === 'trabaja' || e === 'asignado') {
      const htN = (rec.horarioTurnoNombre ?? '').toUpperCase();
      if (htN.includes('MAÑANA')) return 'M';
      if (htN.includes('TARDE')) return 'T';
      if (htN.includes('NOCHE')) return 'N';
      return w.tipo === 'FIJ' ? 'F' : 'M';
    }
    return null;
  }

  /**
   * Shift to display in the grid — overrides PTS when worker is the ausente in a COBERTURA
   * and the SP didn't update their PTS record (still shows M/T/N/F instead of B/D).
   * Uses motivoFalta when available: FCGH / PERMISO / MEDICO → 'D', otherwise → 'B'.
   */
  getDisplayShift(w: TrabajadorMapped, dayIdx: number): ShiftCode {
    const shift = this.getShift(w, dayIdx);
    // If PTS already shows an absence code, trust it
    if (shift === 'D' || shift === 'B' || shift === 'V' || shift === null) return shift;
    const dateISO = this.week.toISO(this.week.weekDates()[dayIdx]);
    const cobAusente = this.getCoberturaAsAusente(w.id, dateISO);
    if (!cobAusente || cobAusente.tipoCobertura !== 'COBERTURA') return shift;
    // Worker is the ausente — PTS still shows working shift — derive absence type
    const motivo = (cobAusente.motivoFalta ?? '').toUpperCase();
    if (motivo === 'FCGH' || motivo === 'PERMISO' || motivo === 'MEDICO') return 'D';
    return 'B';
  }

  getChipClass(code: ShiftCode): string {
    return code ? `chip chip-${code}` : 'chip chip-empty';
  }

  getChipLabel(code: ShiftCode): string {
    if (!code) return '·';
    if (code === 'B') return 'F';    // Falta → F
    if (code === 'F') return 'FIJ';  // Fijo trabajando → FIJ
    return code;
  }

  getHtName(id?: number | null): string {
    if (!id) return '—';
    return this.horariosTurno().find(h => h.id === id)?.nombreHorario ?? `id=${id}`;
  }

  getSedeName(sucursalId: number): string {
    return this.sedes().find(s => s.id === sucursalId)?.nombreSucursal ?? '—';
  }

  getHorarioHoras(htId: number | null | undefined, dayIdx: number, turnoId?: number | null): string | null {
    if (!htId) return null; // sin htId no hay horas que mostrar (ROT sin programar)
    const norm = (s: string) => s.trim().toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const diaTarget = norm(this.DIAS_FULL[dayIdx] ?? '').substring(0, 3);
    const fmt = (t: string) => t?.substring(0, 5) ?? '';
    const tryHt = (id: number): string | null => {
      const ht = this.horariosTurno().find(h => h.id === id);
      if (!ht?.horariosDetalle?.length) return null;
      const det = ht.horariosDetalle.find(d => norm(d.diaSemana).startsWith(diaTarget));
      return det ? `${fmt(det.horaInicio)}–${fmt(det.horaFin)}` : null;
    };
    const r = tryHt(htId);
    if (r) return r;
    // Solo si tenemos htId concreto pero no cubre ese día, buscar en hermanos del turno
    // (ej. FIJ con HORARIO L-V asignado pero el sábado lo cubre HORARIO SABADO)
    if (turnoId) {
      for (const ht of (this.htPorTurno()[turnoId] ?? []).filter(h => h.id !== htId)) {
        const r2 = tryHt(ht.id);
        if (r2) return r2;
      }
    }
    return null;
  }

  getHtForDay(w: TrabajadorMapped, dayIdx: number): number | null {
    const key = `${w.id}_${dayIdx}`;
    if (this.htOverrides()[key]) return this.htOverrides()[key];
    const dates = this.week.weekDates();
    const dateISO = this.week.toISO(dates[dayIdx]);
    return this.ptsSemana()[w.id]?.[dateISO]?.horarioTurnoId ?? null;
  }

  getHtOptions(w: TrabajadorMapped): HorarioTurno[] {
    if (w.idTurno && this.htPorTurno()[w.idTurno]) return this.htPorTurno()[w.idTurno];
    if (w.tipo === 'ROT') {
      return this.horariosTurno().filter(h =>
        /MAÑANA|TARDE|NOCHE|DIURNO|NOCTURNO/i.test(h.nombreHorario)
      );
    }
    return this.horariosTurno();
  }

  // ── DRAWER ────────────────────────────────────────────────
  openShiftDrawer(worker: TrabajadorMapped, dayIdx: number): void {
    if (!this.auth.isAdminOrSupervisor()) return;
    // No permitir editar días con ausencia registrada
    const dates = this.week.weekDates();
    const dateISO = this.week.toISO(dates[dayIdx]);
    if (this.ptsSemana()[worker.id]?.[dateISO]?.tipoAusencia) return;
    const currentShift = this.getShift(worker, dayIdx);
    const htId = this.getHtForDay(worker, dayIdx);
    this.shiftEdit.set({ worker, dayIndex: dayIdx });
    this.tempShift.set(this.resolveShiftToOption(currentShift));
    this.tempHtId.set(htId ?? worker.idHorarioTurno ?? null);
    this.drawerOpen.set(true);
  }

  private resolveShiftToOption(code: ShiftCode): string {
    if (!code || code === 'M' || code === 'T' || code === 'N' || code === 'F') return 'W';
    return code;
  }

  closeShiftDrawer(): void {
    this.drawerOpen.set(false);
    this.shiftEdit.set(null);
    this.tempShift.set('W');
    this.tempHtId.set(null);
    this.coberturaOpen.set(false);
    this.cobTipo.set('COBERTURA');
    this.cobEsReemplazo.set(true);
    this.cobMotivo.set('FSGH');
    this.cobDev.set('');
    this.cobTieneDescanso.set(false);
    this.cobHt.set(null);
    this.cobCubre.set(null);
    this.cobCubreQuery.set('');
  }

  /** Abre/cierra la sección de cobertura y auto-pobla el turno del afectado */
  openCoberturaSection(): void {
    const edit = this.shiftEdit();
    if (!edit) return;
    // No permitir cobertura si el trabajador tiene descanso, vacaciones o falta
    const shift = this.getShift(edit.worker, edit.dayIndex);
    if (shift === 'D' || shift === 'V' || shift === 'B') return;
    if (!this.coberturaOpen()) {
      const htId = this.getHtForDay(edit.worker, edit.dayIndex);
      this.cobHt.set(htId);
      this.cobTieneDescanso.set(false);
      this.cobDev.set('');
    }
    this.coberturaOpen.set(!this.coberturaOpen());
  }

  /** Lista de turnos disponibles del afectado ese día (incluye dobletas por cobertura previa) */
  getAfectadoHtOptions(w: TrabajadorMapped, dayIdx: number): HorarioTurno[] {
    const dateISO = this.week.toISO(this.week.weekDates()[dayIdx]);
    const primaryHtId = this.getHtForDay(w, dayIdx);
    const extraIds = this.coberturasWeek()
      .filter(c => c.idTrabajadorCubre === w.id && (c.fecha ?? '').startsWith(dateISO))
      .map(c => c.idHorarioTurnoOriginal)
      .filter((id): id is number => !!id);
    const allIds = [...new Set([...(primaryHtId ? [primaryHtId] : []), ...extraIds])];
    return allIds
      .map(id => this.horariosTurno().find(h => h.id === id))
      .filter((h): h is HorarioTurno => !!h);
  }

  /** Cobertura activa donde el trabajador es quien cubre (dobleta) */
  getDobletaCobertura(wid: number, dateISO: string): Cobertura | null {
    return this.coberturasWeek().find(c =>
      c.idTrabajadorCubre === wid && (c.fecha ?? '').startsWith(dateISO)
    ) ?? null;
  }

  /** Cobertura activa donde el trabajador es el ausente (su turno fue cubierto) */
  getCoberturaAsAusente(wid: number, dateISO: string): Cobertura | null {
    return this.coberturasWeek().find(c =>
      c.idTrabajadorAusente === wid && (c.fecha ?? '').startsWith(dateISO)
    ) ?? null;
  }

  /** Chip class para un HT por nombre (reutiliza los mismos colores del grid) */
  getChipClassByHtName(htName: string): string {
    const l = this.getHtLetter(htName);
    return l ? `chip chip-${l}` : 'chip chip-empty';
  }

  selectShift(k: string): void { this.tempShift.set(k); }
  selectHt(id: number): void { this.tempHtId.set(id); }

  saveShift(): void {
    const edit = this.shiftEdit();
    if (!edit) return;
    const { worker, dayIndex } = edit;
    const dates = this.week.weekDates();
    const fecha = this.week.toISO(dates[dayIndex]);
    const htId = this.tempHtId() ?? worker.idHorarioTurno ?? 0;

    let estado = this.tempShift();
    if (worker.tipo === 'ROT' && estado === 'W') {
      const htN = (this.getHtName(htId)).toUpperCase();
      if (htN.includes('MAÑANA')) estado = 'M';
      else if (htN.includes('TARDE')) estado = 'T';
      else if (htN.includes('NOCHE')) estado = 'N';
      else estado = 'M';
    } else if (worker.tipo === 'FIJ' && estado === 'W') {
      estado = 'F';
    }

    this.shiftOverrides.update(ov => ({ ...ov, [`${worker.id}_${dayIndex}`]: estado as ShiftCode }));
    if (htId) this.htOverrides.update(ov => ({ ...ov, [`${worker.id}_${dayIndex}`]: htId }));

    const body: ProgramacionSemanalRequest = {
      fechaInicio: fecha, fechaFin: fecha,
      programaciones: [{
        trabajadorId: worker.id, fecha, idHorarioTurno: htId || null,
        esDescanso: estado === 'D', esDiaBoleta: estado === 'B', esVacaciones: estado === 'V'
      }]
    };

    this.rrhhService.saveProgramacion(body).subscribe({
      next: () => {
        this.toast.ok('Turno guardado');
        // Limpiar override local y recargar desde la API para que quede consistente
        this.shiftOverrides.update(ov => {
          const n = { ...ov };
          delete n[`${worker.id}_${dayIndex}`];
          return n;
        });
        this.htOverrides.update(ov => {
          const n = { ...ov };
          delete n[`${worker.id}_${dayIndex}`];
          return n;
        });
        this.loadData();
      },
      error: err => {
        // Revertir override si el save falló
        this.shiftOverrides.update(ov => {
          const n = { ...ov };
          delete n[`${worker.id}_${dayIndex}`];
          return n;
        });
        this.toast.err(err?.error?.message || 'Error al guardar el turno');
      }
    });

    this.closeShiftDrawer();
  }

  clearShift(): void {
    const edit = this.shiftEdit();
    if (!edit) return;
    this.shiftOverrides.update(ov => {
      const n = { ...ov };
      delete n[`${edit.worker.id}_${edit.dayIndex}`];
      return n;
    });
    this.closeShiftDrawer();
  }

  // ── PUBLISH WEEK ─────────────────────────────────────────
  publishWeek(): void {
    const dates = this.week.weekDates();
    const workers = this.filteredWorkers();
    const programaciones: any[] = [];

    workers.forEach(w => {
      dates.forEach((d, i) => {
        const s = this.getShift(w, i);
        if (!s) return;
        const htId = this.getHtForDay(w, i) ?? w.idHorarioTurno ?? 0;
        programaciones.push({
          trabajadorId: w.id, fecha: this.week.toISO(d), idHorarioTurno: htId,
          esDescanso: s === 'D', esDiaBoleta: s === 'B', esVacaciones: s === 'V'
        });
      });
    });

    if (!programaciones.length) { this.toast.warn('No hay cambios para publicar'); return; }

    this.rrhhService.saveProgramacion({
      fechaInicio: this.week.toISO(dates[0]),
      fechaFin: this.week.toISO(dates[6]),
      programaciones
    }).subscribe({
      next: res => {
        this.toast.ok(`Publicado · ${res?.registrosGrabados ?? programaciones.length} registros`);
        this.shiftOverrides.set({});
        this.htOverrides.set({});
        this.loadData();
      },
      error: () => this.toast.err('Error al publicar')
    });
  }

  setFilterTipo(t: string): void { this.filterTipo.set(t); }

  weekDates = computed(() => this.week.weekDates());

  private getShiftOrder(w: TrabajadorMapped): number {
    const norm = (s: string) => s.toUpperCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const check = (name: string): number => {
      const n = norm(name);
      if (n.includes('MANANA') || n.includes('DIURNO')) return 0;
      if (n.includes('TARDE')) return 1;
      if (n.includes('NOCHE') || n.includes('NOCTURNO')) return 2;
      return -1;
    };

    // 1. Contar turnos reales de la semana actual
    const counts = [0, 0, 0]; // [Mañana, Tarde, Noche]
    const firstInWeek = [-1, -1, -1]; // primer día (índice) en que aparece cada turno
    for (let i = 0; i < 7; i++) {
      const htId = this.getHtForDay(w, i);
      if (!htId) continue;
      const order = check(this.getHtName(htId));
      if (order < 0) continue;
      counts[order]++;
      if (firstInWeek[order] === -1) firstInWeek[order] = i;
    }

    const totalSemana = counts[0] + counts[1] + counts[2];
    if (totalSemana > 0) {
      const max = Math.max(...counts);
      const ganadores = [0, 1, 2].filter(o => counts[o] === max);
      // Un solo ganador → mayoría clara
      if (ganadores.length === 1) return ganadores[0];
      // Empate → gana el que apareció primero en la semana (Lun tiene prioridad)
      return ganadores.reduce((a, b) => firstInWeek[a] <= firstInWeek[b] ? a : b);
    }

    // 2. Sin datos de semana → usar horario base del trabajador
    const base = w.horarioTurnoNombre ?? this.getHtName(w.idHorarioTurno);
    const fromBase = check(base);
    if (fromBase >= 0) return fromBase;

    // 3. Sin nada → sin turno asignado
    return 3;
  }

  private readonly TURNO_LABELS: Record<number, string> = { 0: 'Turno Mañana', 1: 'Turno Tarde', 2: 'Turno Noche', 3: 'Sin turno asignado' };

  workersBySede = computed(() => {
    const sedeMap = new Map<number, TrabajadorMapped[]>();
    this.filteredWorkers().forEach(w => {
      if (!sedeMap.has(w.sucursalId)) sedeMap.set(w.sucursalId, []);
      sedeMap.get(w.sucursalId)!.push(w);
    });

    return Array.from(sedeMap.entries())
      .map(([, workers]) => {
        const sedeName = this.getSedeName(workers[0].sucursalId);

        // Separar ROT y FIJ
        const rot = workers.filter(w => w.tipo === 'ROT');
        const fij = workers.filter(w => w.tipo !== 'ROT').sort((a, b) => a.name.localeCompare(b.name));

        // Sub-agrupar ROT por turno
        const rotByTurno = new Map<number, TrabajadorMapped[]>();
        rot.forEach(w => {
          const order = this.getShiftOrder(w);
          if (!rotByTurno.has(order)) rotByTurno.set(order, []);
          rotByTurno.get(order)!.push(w);
        });

        const subGroups: { label: string; isRot: boolean; workers: TrabajadorMapped[] }[] = [];
        Array.from(rotByTurno.entries())
          .sort(([a], [b]) => a - b)
          .forEach(([order, ws]) => {
            subGroups.push({
              label: this.TURNO_LABELS[order],
              isRot: true,
              workers: ws.sort((a, b) => a.name.localeCompare(b.name))
            });
          });

        if (fij.length) subGroups.push({ label: 'Turno Fijo', isRot: false, workers: fij });

        return { sedeName, totalWorkers: workers.length, subGroups };
      })
      .sort((a, b) => a.sedeName.localeCompare(b.sedeName));
  });

  getWorkerNameById(id: number): string {
    const w = this.allTrabajadores().find(x => x.id === id);
    return w ? w.name.split(' ').slice(0, 2).join(' ') : `#${id}`;
  }

  selectCobCubre(w: TrabajadorMapped): void {
    this.cobCubre.set(w.id);
    this.cobCubreQuery.set(w.name);
    this.cobCubreOpen.set(false);
  }

  closeCobDrop(): void { setTimeout(() => this.cobCubreOpen.set(false), 150); }

  saveCobertura(): void {
    const edit = this.shiftEdit();
    if (!edit) return;
    if (!this.cobCubre()) { this.toast.err('Selecciona quién cubre'); return; }
    if (this.cobTieneDescanso() && !this.cobDev()) {
      this.toast.err('Indica la fecha del descanso compensatorio');
      return;
    }

    const fecha = this.week.toISO(this.week.weekDates()[edit.dayIndex]);
    this.savingCob.set(true);

    const body: CoberturaCreateDto = {
      fecha: fecha + 'T00:00:00',
      idTrabajadorCubre: this.cobCubre(),
      idTrabajadorAusente: edit.worker.id, // siempre el trabajador afectado — la BD no permite null
      idHorarioTurnoOriginal: this.cobHt() ?? this.getHtForDay(edit.worker, edit.dayIndex),
      tipoCobertura: this.cobTipo(),
      motivoFalta: this.cobTipo() === 'COBERTURA' && this.cobEsReemplazo() ? this.cobMotivo() : null,
      fechaSwapDevolucion: this.cobTieneDescanso() && this.cobDev() ? this.cobDev() + 'T00:00:00' : null,
      esSoloAsignacion: !this.cobEsReemplazo()
    };

    this.rrhhService.createCobertura(body).subscribe({
      next: () => {
        this.toast.ok('Cobertura registrada correctamente');
        this.savingCob.set(false);
        this.coberturaOpen.set(false);
        this.cobTipo.set('COBERTURA'); this.cobEsReemplazo.set(true);
        this.cobMotivo.set('FSGH'); this.cobDev.set('');
        this.cobTieneDescanso.set(false);
        this.cobCubre.set(null); this.cobCubreQuery.set('');
        this.loadData(); // refresca la grilla para mostrar la dobleta
      },
      error: err => {
        // Si el servidor guardó pero devolvió un error secundario, lo informamos de forma clara
        const msg = err?.error?.message ?? err?.message ?? '';
        if (err?.status >= 200 && err?.status < 300) {
          this.toast.ok('Cobertura registrada correctamente');
          this.loadData();
        } else {
          this.toast.err(msg || 'Ocurrió un error al guardar la cobertura. Verifica los datos e intenta nuevamente.');
        }
        this.savingCob.set(false);
      }
    });
  }

  readonly today = new Date();

  getHtColor(name: string): string {
    const n = name.toUpperCase();
    if (n.includes('MAÑANA') || n.includes('DIURNO')) return '#5eead4';
    if (n.includes('TARDE')) return '#93c5fd';
    return '#c4b5fd';
  }

  getHtLetter(name: string): string {
    const n = name.toUpperCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    if (n.includes('MANANA') || n.includes('DIURNO') || n.includes('FIJO') || n.includes('FIJA')) return 'M';
    if (n.includes('TARDE')) return 'T';
    if (n.includes('NOCHE') || n.includes('NOCTURNO')) return 'N';
    return 'F'; // fallback: fixed shift
  }
}
