import { Component, inject, signal, computed, OnInit, OnDestroy, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RrhhService } from '../../core/services/rrhh.service';
import { WeekService } from '../../core/services/week.service';
import { ToastService } from '../../core/services/toast.service';
import { HorarioTurno, SucursalCentro, PtsDiaRecord, ProgramacionSemanalRequest } from '../../core/models/rrhh.models';

interface PtsItem {
  trabajadorId: number;
  trabajadorNombre: string;
  sucursalId?: number;
  tipo?: string;
}

interface CellState {
  estado: 'W' | 'D' | 'B' | 'V' | 'A' | '?';
  htId?: number | null;
  tipoAusencia?: string | null;
}

@Component({
  selector: 'app-programacion',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './programacion.component.html',
  styleUrl: './programacion.component.scss'
})
export class ProgramacionComponent implements OnInit, OnDestroy {
  private rrhhService = inject(RrhhService);
  week = inject(WeekService);
  toast = inject(ToastService);

  sedes = signal<SucursalCentro[]>([]);
  horariosTurno = signal<HorarioTurno[]>([]);
  htPorTurno = signal<Record<number, HorarioTurno[]>>({});

  allPtsItems = signal<PtsItem[]>([]);
  ptsSemana = signal<Record<number, Record<string, PtsDiaRecord>>>({});
  ptsOverrides = signal<Record<string, CellState>>({});

  // filters
  filterSede = signal('');
  searchQuery = signal('');

  // vac drawer
  vacOpen = signal(false);
  vacTrabId = signal<number>(0);
  vacInicio = signal('');
  vacFin = signal('');

  // confirm modal
  confirmOpen = signal(false);

  saving = signal(false);
  loading = signal(false);

  readonly DAYS_SH = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'];
  private readonly DIAS_FULL = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];

  private weekEffect = effect(() => {
    const _ = this.week.weekOffset();
    this.ptsOverrides.set({});
    this.loadPTS();
  });

  ngOnInit(): void {
    this.rrhhService.getSucursales().subscribe(s => this.sedes.set(s));
    this.rrhhService.getHorariosTurno().subscribe(ht => {
      this.horariosTurno.set(ht);
      const map: Record<number, HorarioTurno[]> = {};
      ht.forEach(h => { if (!map[h.turnoId]) map[h.turnoId] = []; map[h.turnoId].push(h); });
      this.htPorTurno.set(map);
    });
    this.loadPTS();
  }

  ngOnDestroy(): void {
    this.weekEffect.destroy();
  }

  loadPTS(): void {
    const dates = this.week.weekDates();
    const fi = this.week.toISO(dates[0]);
    const ff = this.week.toISO(dates[6]);
    this.loading.set(true);

    this.rrhhService.getProgramacion(fi, ff, 1, 999).subscribe({
      next: data => {
        const items = (data.items ?? [])
          .map((item: any) => ({
            trabajadorId: item.trabajadorId,
            trabajadorNombre: item.trabajadorNombre,
            sucursalId: item.sucursalId,
            tipo: item.tipoTurnoNombre ?? item.tipo ?? ''
          }))
          .filter((item: any) => (item.tipo as string).toUpperCase().includes('ROT'));
        this.allPtsItems.set(items);

        const pts: Record<number, Record<string, PtsDiaRecord>> = {};
        items.forEach((item: any) => {
          const wid = item.trabajadorId;
          pts[wid] = {};
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
        this.ptsSemana.set(pts);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  getAPIEstado(wid: number, dateISO: string): 'W' | 'D' | 'B' | 'V' | 'A' | '?' {
    const rec = this.ptsSemana()[wid]?.[dateISO];
    if (!rec) return '?';
    // tipoAusencia tiene prioridad — celda bloqueada
    if (rec.tipoAusencia) return 'A';
    const e = (rec.estado ?? '').toLowerCase();
    if (e === 'descanso') return 'D';
    if (e === 'boleta' || e === 'dia-boleta') return 'B';
    if (e === 'vacaciones') return 'V';
    if (e === 'trabaja' || e === 'asignado') return 'W';
    return '?';
  }

  getCellState(wid: number, dayIdx: number): CellState {
    const dates = this.week.weekDates();
    const dateISO = this.week.toISO(dates[dayIdx]);
    const key = `${wid}_${dayIdx}_${this.week.weekOffset()}`;
    const ov = this.ptsOverrides()[key];
    if (ov) return ov;
    const rec = this.ptsSemana()[wid]?.[dateISO];
    const api = this.getAPIEstado(wid, dateISO);
    const htId = rec?.horarioTurnoId;
    if (api === 'A') return { estado: 'A', htId, tipoAusencia: rec?.tipoAusencia };
    // Días sin asignar arrancan en 'W' (Trabaja) por defecto
    return { estado: api === '?' ? 'W' : api, htId };
  }

  isSaved(wid: number): boolean {
    const dates = this.week.weekDates();
    return dates.some(d => this.getAPIEstado(wid, this.week.toISO(d)) !== '?');
  }

  /** El día ya tiene registro en la BD (no '?') */
  isDaySaved(wid: number, dayIdx: number): boolean {
    const dateISO = this.week.toISO(this.week.weekDates()[dayIdx]);
    return this.getAPIEstado(wid, dateISO) !== '?';
  }

  /** El usuario modificó esta celda en la sesión actual */
  hasPendingChange(wid: number, dayIdx: number): boolean {
    const key = `${wid}_${dayIdx}_${this.week.weekOffset()}`;
    return !!this.ptsOverrides()[key];
  }

  /** True si la fecha del día es anterior a hoy */
  isPastDay(dayIdx: number): boolean {
    const d = this.week.weekDates()[dayIdx];
    const today = new Date(); today.setHours(0, 0, 0, 0);
    const day   = new Date(d); day.setHours(0, 0, 0, 0);
    return day < today;
  }

  /** Cuántos trabajadores visibles NO tienen registro para ese día */
  countPendingDay(dayIdx: number): number {
    return this.ptsItems().filter(item => {
      const cs = this.getCellState(item.trabajadorId, dayIdx);
      return cs.estado !== 'A' && !this.isDaySaved(item.trabajadorId, dayIdx);
    }).length;
  }

  cyclePTS(wid: number, dayIdx: number): void {
    if (this.isPastDay(dayIdx)) return; // día pasado — no editable
    if (this.getCellState(wid, dayIdx).estado === 'A') return; // bloqueado por ausencia
    const current = this.getCellState(wid, dayIdx).estado;
    const item = this.ptsItems().find(x => x.trabajadorId === wid);
    const isFij = (item?.tipo ?? '').toUpperCase().includes('FIJ');
    const cycleRot: Record<string, 'W' | 'D' | 'B' | 'V'> = { W: 'D', D: 'B', B: 'V', V: 'W', '?': 'W' };
    const cycleFij: Record<string, 'W' | 'D' | 'V'> = { W: 'D', D: 'V', V: 'W', '?': 'W' };
    const next = (isFij ? cycleFij : cycleRot)[current] ?? 'W';
    const key = `${wid}_${dayIdx}_${this.week.weekOffset()}`;
    this.ptsOverrides.update(ov => {
      const prev = this.getCellState(wid, dayIdx);
      return { ...ov, [key]: { estado: next, htId: prev.htId } };
    });
  }

  setHt(wid: number, dayIdx: number, htId: string): void {
    const key = `${wid}_${dayIdx}_${this.week.weekOffset()}`;
    this.ptsOverrides.update(ov => {
      const prev = ov[key] ?? this.getCellState(wid, dayIdx);
      return { ...ov, [key]: { ...prev, htId: htId ? parseInt(htId) : null } };
    });
  }

  getHtOptions(item: PtsItem): HorarioTurno[] {
    if (item.tipo?.toUpperCase().includes('ROT')) {
      return this.horariosTurno().filter(h => /MAÑANA|TARDE|NOCHE|DIURNO|NOCTURNO/i.test(h.nombreHorario));
    }
    return this.horariosTurno();
  }

  getBtnClass(estado: string): string {
    const m: Record<string, string> = { W: 'pts-w', D: 'pts-d', B: 'pts-b', V: 'pts-v', A: 'pts-a' };
    return `pts-btn ${m[estado] ?? ''}`;
  }

  getBtnLabel(estado: string): string {
    const m: Record<string, string> = { W: 'Trabaja', D: 'DESC', B: 'Falta', V: 'VAC', A: 'AUS' };
    return m[estado] ?? estado;
  }

  getAusenciaAbrev(tipoAusencia: string | null | undefined): string {
    const m: Record<string, string> = {
      VACACIONES: 'VAC', DESCANSO_MEDICO: 'MED', MATERNIDAD: 'MAT',
      PATERNIDAD: 'PAT', PERMISO_SIN_GOCE: 'PSG'
    };
    return m[tipoAusencia ?? ''] ?? (tipoAusencia?.slice(0, 3) ?? 'AUS');
  }

  weekDates = computed(() => this.week.weekDates());

  // ── CONFIRM MODAL ─────────────────────────────────────────
  confirmStats = computed(() => {
    const dates = this.week.weekDates();
    let trabajadores = 0;
    let dias = 0;
    let sinHorario = 0;
    this.ptsItems().forEach(item => {
      let tieneDia = false;
      dates.forEach((_, i) => {
        const cs = this.getCellState(item.trabajadorId, i);
        if (cs.estado === 'A') return; // ausencia registrada — no cuenta en PTS
        if (cs.estado === 'W' && !cs.htId) { sinHorario++; return; }
        dias++;
        tieneDia = true;
      });
      if (tieneDia) trabajadores++;
    });
    return { trabajadores, dias, sinHorario };
  });

  openConfirm(): void {
    const stats = this.confirmStats();
    if (!stats.dias && !stats.sinHorario) {
      this.toast.warn('No hay días asignados para guardar');
      return;
    }
    this.confirmOpen.set(true);
  }

  closeConfirm(): void { this.confirmOpen.set(false); }

  async savePTS(): Promise<void> {
    this.confirmOpen.set(false);
    this.saving.set(true);
    const dates = this.week.weekDates();
    const programaciones: any[] = [];
    const sinHorario: string[] = [];

    this.ptsItems().forEach(item => {
      const wid = item.trabajadorId;
      dates.forEach((d, i) => {
        const cs = this.getCellState(wid, i);
        if (cs.estado === '?' || cs.estado === 'A') return; // 'A' = ausencia registrada, no modificar

        const esExcepcion = ['D', 'B', 'V'].includes(cs.estado);
        if (!cs.htId && !esExcepcion) {
          sinHorario.push(`${item.trabajadorNombre.split(' ')[0]}-${this.DAYS_SH[i]}`);
          return;
        }
        programaciones.push({
          trabajadorId: wid, fecha: this.week.toISO(d),
          idHorarioTurno: cs.htId ?? null,
          esDescanso: cs.estado === 'D',
          esDiaBoleta: cs.estado === 'B',
          esVacaciones: cs.estado === 'V'
        });
      });
    });

    if (sinHorario.length) {
      this.toast.warn(`${sinHorario.length} día(s) sin horario: ${sinHorario.slice(0, 4).join(', ')}`);
    }

    if (!programaciones.length) {
      this.toast.warn('No hay días asignados para guardar');
      this.saving.set(false);
      return;
    }

    this.rrhhService.saveProgramacion({
      fechaInicio: this.week.toISO(dates[0]),
      fechaFin: this.week.toISO(dates[6]),
      programaciones
    }).subscribe({
      next: res => {
        this.toast.ok(`${res?.mensaje ?? 'Guardado'} · ${res?.registrosGrabados ?? programaciones.length} registros`);
        this.ptsOverrides.set({});
        this.loadPTS();
        this.saving.set(false);
      },
      error: err => {
        const e = err.error ?? {};
        if (e.horariosDisponibles?.length) {
          const hn = e.horariosDisponibles.slice(0, 5).map((h: any) => h.nombreHorario).join(', ');
          this.toast.err(`${this.toast.extractHttpMsg(err)} — disponibles: ${hn}`);
        } else {
          this.toast.errHttp(err, 'Error al guardar');
        }
        this.saving.set(false);
      }
    });
  }

  applyFilters(): void {
    this.ptsOverrides.set({});
  }

  // Vacaciones drawer
  openVac(): void { this.vacOpen.set(true); }
  closeVac(): void { this.vacOpen.set(false); }

  saveVac(): void {
    const trabId = this.vacTrabId();
    const inicio = this.vacInicio();
    const fin = this.vacFin();
    if (!inicio || !fin) { this.toast.err('Selecciona rango de fechas'); return; }
    const programaciones: any[] = [];
    const d = new Date(inicio);
    const dFin = new Date(fin);
    while (d <= dFin) {
      programaciones.push({ trabajadorId: trabId, fecha: this.week.toISO(d), idHorarioTurno: null, esDescanso: false, esDiaBoleta: false, esVacaciones: true });
      d.setDate(d.getDate() + 1);
    }
    this.rrhhService.saveProgramacion({ fechaInicio: inicio, fechaFin: fin, programaciones }).subscribe({
      next: res => { this.toast.ok(`Vacaciones marcadas · ${res?.registrosGrabados ?? programaciones.length} días`); this.closeVac(); this.loadPTS(); },
      error: err => this.toast.errHttp(err)
    });
  }

  getSedeName(sucursalId?: number): string {
    if (!sucursalId) return '—';
    return this.sedes().find(s => s.id === sucursalId)?.nombreSucursal ?? '—';
  }

  getTipoClass(tipo?: string): string {
    return (tipo ?? '').toUpperCase().includes('ROT') ? 'tipo-ROT' : 'tipo-FIJ';
  }

  getTipoLabel(tipo?: string): string {
    return (tipo ?? '').toUpperCase().includes('ROT') ? 'ROT' : 'FIJ';
  }

  getHtName(id?: number | null): string {
    if (!id) return '—';
    return this.horariosTurno().find(h => h.id === id)?.nombreHorario ?? `id=${id}`;
  }

  getHorarioHoras(htId: number | null | undefined, dayIdx: number): string | null {
    if (!htId) return null;
    const ht = this.horariosTurno().find(h => h.id === htId);
    if (!ht?.horariosDetalle?.length) return null;
    const norm = (s: string) => s.trim().toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const diaTarget = norm(this.DIAS_FULL[dayIdx] ?? '').substring(0, 3);
    const detalle = ht.horariosDetalle.find(d => norm(d.diaSemana).startsWith(diaTarget));
    if (!detalle) return null;
    const fmt = (t: string) => t?.substring(0, 5) ?? '';
    return `${fmt(detalle.horaInicio)}–${fmt(detalle.horaFin)}`;
  }

  ptsItems = computed(() => {
    const q = this.searchQuery().toLowerCase();
    const sf = this.filterSede();
    return this.allPtsItems().filter(item => {
      const mQ = !q || item.trabajadorNombre.toLowerCase().includes(q);
      const mS = !sf || String(item.sucursalId) === sf;
      return mQ && mS;
    });
  });

  savedItems = computed(() => this.ptsItems().filter(item => this.isSaved(item.trabajadorId)));
  pendingItems = computed(() => this.ptsItems().filter(item => !this.isSaved(item.trabajadorId)));

  ptsItemsBySede = computed(() => {
    const map = new Map<number, PtsItem[]>();
    this.ptsItems().forEach(item => {
      const sid = item.sucursalId ?? 0;
      if (!map.has(sid)) map.set(sid, []);
      map.get(sid)!.push(item);
    });
    return Array.from(map.entries())
      .map(([sucursalId, items]) => ({
        sucursalId,
        sedeName: this.getSedeName(sucursalId),
        saved: items.filter(i => this.isSaved(i.trabajadorId)),
        pending: items.filter(i => !this.isSaved(i.trabajadorId))
      }))
      .sort((a, b) => a.sedeName.localeCompare(b.sedeName));
  });
}
