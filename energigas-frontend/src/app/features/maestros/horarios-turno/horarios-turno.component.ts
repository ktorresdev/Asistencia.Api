import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RrhhService } from '../../../core/services/rrhh.service';
import { ToastService } from '../../../core/services/toast.service';
import { HorarioTurno, HorarioDetalle, HorarioDetalleRequest, Turno } from '../../../core/models/rrhh.models';

interface DetalleForm {
  diaSemana: string;
  horaInicio: string;
  horaFin: string;
  salidaDiaSiguiente: boolean;
  tiempoRefrigerioMinutos: number;
}

const BLANK_DETALLE: DetalleForm = {
  diaSemana: 'Lunes', horaInicio: '08:00', horaFin: '17:00',
  salidaDiaSiguiente: false, tiempoRefrigerioMinutos: 60
};

@Component({
  selector: 'app-horarios-turno',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <ng-container>
      <div class="toolbar">
        <div class="srch-wrap"><span class="srch-ico">⌕</span>
          <input class="srch" type="text" placeholder="Buscar..." [ngModel]="searchQuery()" (ngModelChange)="onSearch($event)" />
        </div>
        <select class="sel" style="max-width:200px" [ngModel]="filterTurno()" (ngModelChange)="onFilterTurno(+$event)">
          <option [value]="0">Todos los turnos</option>
          @for (t of turnos(); track t.id) { <option [value]="t.id">{{ t.nombreCodigo }}</option> }
        </select>
        <button class="btn btn-primary btn-sm" (click)="openNew()">+ Nuevo horario</button>
        @if (loading()) { <span class="spin"></span> }
      </div>

      <div class="grid-outer">
        <div class="tbl-wrap">
        <table class="tbl">
          <thead><tr><th>ID</th><th>Nombre horario</th><th>Turno</th><th>Días</th><th>Activo</th><th>Acciones</th></tr></thead>
          <tbody>
            @for (h of paginated(); track h.id) {
              <tr>
                <td style="font-family:var(--mono);color:var(--txt3)">{{ h.id }}</td>
                <td style="font-weight:600">{{ h.nombreHorario }}</td>
                <td>{{ getTurnoName(h.turnoId) }}</td>
                <td>
                  @if ((h.horariosDetalle?.length ?? 0) > 0) {
                    <span style="font-family:var(--mono);font-size:11px;color:var(--acc)">{{ h.horariosDetalle!.length }} día(s)</span>
                  } @else {
                    <span style="color:var(--danger);font-size:11px">sin detalle</span>
                  }
                </td>
                <td><span class="ec" [class]="h.esActivo ? 'ec-ok' : 'ec-err'">{{ h.esActivo ? 'SÍ' : 'NO' }}</span></td>
                <td style="display:flex;gap:4px">
                  <button class="btn btn-sm" (click)="openEdit(h)">Editar</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
        </div>
      </div>
      <div class="pagination">
        <button class="btn btn-sm" [disabled]="page() <= 1" (click)="page.set(page()-1)">&#8249;</button>
        <span class="page-info">{{ page() }} / {{ totalPages() }} · {{ filtered().length }} registros</span>
        <button class="btn btn-sm" [disabled]="page() >= totalPages()" (click)="page.set(page()+1)">&#8250;</button>
      </div>
    </ng-container>

    <div class="overlay" [class.open]="drawerOpen()" (click)="closeDrawer()"></div>

    <!-- DRAWER 560px -->
    <div class="drawer" style="width:560px" [class.open]="drawerOpen()">
      <div class="drawer-hdr">
        <span class="drawer-title">{{ editingId() ? 'Editar horario' : 'Nuevo horario' }}</span>
        <button class="btn-icon" (click)="closeDrawer()">✕</button>
      </div>

      <!-- SECCIÓN 1: HorarioTurno -->
      <div class="drawer-body" style="border-bottom:1px solid var(--brd);padding-bottom:16px">
        <div class="fld">
          <label class="fld-lbl">Turno *</label>
          <select class="sel" [ngModel]="htForm().turnoId" (ngModelChange)="htForm.update(f=>({...f,turnoId:+$event}))">
            @for (t of turnos(); track t.id) { <option [value]="t.id">{{ t.nombreCodigo }}</option> }
          </select>
        </div>
        <div class="fld">
          <label class="fld-lbl">Nombre del horario *</label>
          <input class="inp" [ngModel]="htForm().nombreHorario" (ngModelChange)="htForm.update(f=>({...f,nombreHorario:$event}))" placeholder="Ej: Turno Mañana 8h" />
        </div>
        <div class="fld">
          <label class="fld-lbl">Activo</label>
          <select class="sel" [ngModel]="htForm().esActivo" (ngModelChange)="htForm.update(f=>({...f,esActivo:$event==='true'||$event===true}))">
            <option [value]="true">Sí</option>
            <option [value]="false">No</option>
          </select>
        </div>
        <div class="info-box" style="margin-top:8px">
          Incluye "MAÑANA", "TARDE" o "NOCHE" en el nombre para identificación visual.
        </div>
      </div>
      <div style="padding:10px 20px;border-bottom:1px solid var(--brd)">
        <button class="btn btn-primary btn-sm" (click)="saveHt()" [disabled]="savingHt()">
          {{ savingHt() ? 'Guardando...' : (savedHtId() ? 'Actualizar horario' : 'Guardar y configurar días →') }}
        </button>
      </div>

      <!-- SECCIÓN 2: Detalles (días y horas) — visible solo tras guardar el HT -->
      @if (savedHtId()) {
        <div style="padding:12px 20px 6px;font-size:11px;font-weight:700;letter-spacing:.08em;color:var(--txt2);text-transform:uppercase;background:var(--surf2);border-bottom:1px solid var(--brd)">
          Días y horarios
        </div>

        <!-- Tabla de detalles existentes -->
        <div style="overflow-x:auto">
          <table class="tbl" style="font-size:12px">
            <thead>
              <tr>
                <th>Día</th><th>Entrada</th><th>Salida</th><th>Sale sig.</th><th>Refrig.</th><th></th>
              </tr>
            </thead>
            <tbody>
              @if (detalles().length === 0) {
                <tr><td colspan="6" style="color:var(--txt3);text-align:center;padding:12px">Sin días configurados</td></tr>
              }
              @for (d of detalles(); track d.id) {
                @if (editingDetalleId() === d.id) {
                  <!-- FILA EN EDICIÓN -->
                  <tr style="background:var(--surf2)">
                    <td><select class="sel" style="min-width:110px" [ngModel]="dForm().diaSemana" (ngModelChange)="dForm.update(f=>({...f,diaSemana:$event}))">
                      @for (dia of DIAS; track dia) { <option [value]="dia">{{ dia }}</option> }
                    </select></td>
                    <td><input type="time" class="inp" style="width:90px" [ngModel]="dForm().horaInicio" (ngModelChange)="dForm.update(f=>({...f,horaInicio:$event}))" /></td>
                    <td><input type="time" class="inp" style="width:90px" [ngModel]="dForm().horaFin" (ngModelChange)="dForm.update(f=>({...f,horaFin:$event}))" /></td>
                    <td><input type="checkbox" [ngModel]="dForm().salidaDiaSiguiente" (ngModelChange)="dForm.update(f=>({...f,salidaDiaSiguiente:$event}))" /></td>
                    <td><input type="number" class="inp" style="width:60px" min="0" max="120" [ngModel]="dForm().tiempoRefrigerioMinutos" (ngModelChange)="dForm.update(f=>({...f,tiempoRefrigerioMinutos:+$event}))" /></td>
                    <td style="display:flex;gap:4px;flex-wrap:nowrap">
                      <button class="btn btn-sm btn-primary" (click)="saveDetalle()">✓</button>
                      <button class="btn btn-sm" (click)="cancelDetalle()">✕</button>
                    </td>
                  </tr>
                } @else {
                  <!-- FILA NORMAL -->
                  <tr>
                    <td style="font-weight:600">{{ normalizeDia(d.diaSemana) }}</td>
                    <td style="font-family:var(--mono)">{{ toHm(d.horaInicio) }}</td>
                    <td style="font-family:var(--mono)">{{ toHm(d.horaFin) }}{{ d.salidaDiaSiguiente ? ' +1' : '' }}</td>
                    <td>{{ d.salidaDiaSiguiente ? 'Sí' : 'No' }}</td>
                    <td>{{ d.tiempoRefrigerioMinutos }} min</td>
                    <td style="display:flex;gap:4px;flex-wrap:nowrap">
                      <button class="btn btn-sm" (click)="editDetalle(d)">✏</button>
                      <button class="btn btn-sm btn-danger" (click)="deleteDetalle(d.id)">🗑</button>
                    </td>
                  </tr>
                }
              }
              <!-- FILA AGREGAR NUEVO -->
              @if (editingDetalleId() === -1) {
                <tr style="background:var(--surf2)">
                  <td><select class="sel" style="min-width:110px" [ngModel]="dForm().diaSemana" (ngModelChange)="dForm.update(f=>({...f,diaSemana:$event}))">
                    @for (dia of DIAS; track dia) { <option [value]="dia">{{ dia }}</option> }
                  </select></td>
                  <td><input type="time" class="inp" style="width:90px" [ngModel]="dForm().horaInicio" (ngModelChange)="dForm.update(f=>({...f,horaInicio:$event}))" /></td>
                  <td><input type="time" class="inp" style="width:90px" [ngModel]="dForm().horaFin" (ngModelChange)="dForm.update(f=>({...f,horaFin:$event}))" /></td>
                  <td><input type="checkbox" [ngModel]="dForm().salidaDiaSiguiente" (ngModelChange)="dForm.update(f=>({...f,salidaDiaSiguiente:$event}))" /></td>
                  <td><input type="number" class="inp" style="width:60px" min="0" max="120" [ngModel]="dForm().tiempoRefrigerioMinutos" (ngModelChange)="dForm.update(f=>({...f,tiempoRefrigerioMinutos:+$event}))" /></td>
                  <td style="display:flex;gap:4px;flex-wrap:nowrap">
                    <button class="btn btn-sm btn-primary" (click)="saveDetalle()">✓</button>
                    <button class="btn btn-sm" (click)="cancelDetalle()">✕</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        @if (editingDetalleId() === null) {
          <div style="padding:10px 20px">
            <button class="btn btn-sm btn-primary" (click)="addDetalle()">+ Agregar día</button>
          </div>
        }
      }

      <div class="drawer-ftr">
        <button class="btn" (click)="closeDrawer()">Cerrar</button>
      </div>
    </div>
  `,
  styles: [`:host{display:flex;flex-direction:column;flex:1;min-height:0;overflow:hidden;}`]
})
export class HorariosTurnoComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  toast = inject(ToastService);

  readonly DIAS = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];

  private readonly DIA_NORM: Record<string, string> = {
    LUNES: 'Lunes', MARTES: 'Martes', MIERCOLES: 'Miércoles', MIÉRCOLES: 'Miércoles',
    JUEVES: 'Jueves', VIERNES: 'Viernes', SABADO: 'Sábado', SÁBADO: 'Sábado', DOMINGO: 'Domingo'
  };

  normalizeDia(d: string): string { return this.DIA_NORM[d.toUpperCase()] ?? d; }

  horarios = signal<HorarioTurno[]>([]);
  turnos = signal<Turno[]>([]);
  filterTurno = signal(0);
  loading = signal(false);
  drawerOpen = signal(false);
  editingId = signal<number | null>(null);
  savingHt = signal(false);

  htForm = signal<{ turnoId: number; nombreHorario: string; esActivo: boolean }>({
    turnoId: 0, nombreHorario: '', esActivo: true
  });

  // Detalles
  savedHtId = signal<number | null>(null);
  detalles = signal<HorarioDetalle[]>([]);
  editingDetalleId = signal<number | null>(null); // -1 = nuevo, null = ninguno, N = editando id N
  dForm = signal<DetalleForm>({ ...BLANK_DETALLE });

  searchQuery = signal('');
  page = signal(1);
  readonly pageSize = 10;

  filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    const f = this.filterTurno();
    return this.horarios().filter(h => {
      const mT = !f || h.turnoId === f;
      const mQ = !q || h.nombreHorario.toLowerCase().includes(q) || this.getTurnoName(h.turnoId).toLowerCase().includes(q);
      return mT && mQ;
    });
  });
  totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));
  paginated = computed(() => {
    const p = this.page() - 1;
    return this.filtered().slice(p * this.pageSize, (p + 1) * this.pageSize);
  });

  ngOnInit(): void {
    this.rrhhService.getTurnos().subscribe(t => {
      this.turnos.set(t);
      if (t.length) this.htForm.update(f => ({ ...f, turnoId: t[0].id }));
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.rrhhService.getHorariosTurno().subscribe({
      next: h => { this.horarios.set(h); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openNew(): void {
    this.editingId.set(null);
    this.savedHtId.set(null);
    this.detalles.set([]);
    this.editingDetalleId.set(null);
    this.htForm.set({ turnoId: this.turnos()[0]?.id ?? 0, nombreHorario: '', esActivo: true });
    this.drawerOpen.set(true);
  }

  openEdit(h: HorarioTurno): void {
    this.editingId.set(h.id);
    this.savedHtId.set(h.id);
    this.htForm.set({ turnoId: h.turnoId, nombreHorario: h.nombreHorario, esActivo: h.esActivo });
    this.detalles.set(h.horariosDetalle ?? []);
    this.editingDetalleId.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    if (this.editingDetalleId() !== null) { this.cancelDetalle(); }
    this.drawerOpen.set(false);
    this.load();
  }

  saveHt(): void {
    const f = this.htForm();
    if (!f.nombreHorario || !f.turnoId) { this.toast.err('Nombre y turno requeridos'); return; }
    this.savingHt.set(true);
    const id = this.editingId();
    if (id) {
      this.rrhhService.updateHorarioTurno(id, { id, ...f }).subscribe({
        next: () => { this.toast.ok('Horario actualizado'); this.savingHt.set(false); },
        error: () => { this.toast.err('Error al actualizar'); this.savingHt.set(false); }
      });
    } else {
      this.rrhhService.createHorarioTurno(f).subscribe({
        next: (created: any) => {
          this.editingId.set(created.id);
          this.savedHtId.set(created.id);
          this.toast.ok('Horario creado — ahora configura los días');
          this.savingHt.set(false);
        },
        error: (err: any) => { this.toast.errHttp(err, 'Error al crear'); this.savingHt.set(false); }
      });
    }
  }

  // ── DETALLES ─────────────────────────────────────────────────

  addDetalle(): void {
    this.editingDetalleId.set(-1);
    this.dForm.set({ ...BLANK_DETALLE });
  }

  editDetalle(d: HorarioDetalle): void {
    this.editingDetalleId.set(d.id);
    this.dForm.set({
      diaSemana: this.normalizeDia(d.diaSemana),
      horaInicio: this.toHm(d.horaInicio),
      horaFin: this.toHm(d.horaFin),
      salidaDiaSiguiente: d.salidaDiaSiguiente,
      tiempoRefrigerioMinutos: d.tiempoRefrigerioMinutos
    });
  }

  cancelDetalle(): void { this.editingDetalleId.set(null); }

  saveDetalle(): void {
    const f = this.dForm();
    if (!f.horaInicio || !f.horaFin) { this.toast.err('Hora inicio y fin requeridos'); return; }
    const htId = this.savedHtId()!;
    const payload: HorarioDetalleRequest = {
      diaSemana: f.diaSemana,
      horaInicio: f.horaInicio,
      horaFin: f.horaFin,
      salidaDiaSiguiente: f.salidaDiaSiguiente,
      tiempoRefrigerioMinutos: f.tiempoRefrigerioMinutos
    };

    const editId = this.editingDetalleId();
    if (editId === -1) {
      this.rrhhService.createHorarioDetalle(htId, payload).subscribe({
        next: d => {
          this.detalles.update(arr => [...arr, d]);
          this.editingDetalleId.set(null);
          this.toast.ok('Día agregado');
        },
        error: err => this.toast.errHttp(err, 'Error al agregar día')
      });
    } else if (editId !== null) {
      this.rrhhService.updateHorarioDetalle(htId, editId, payload).subscribe({
        next: () => {
          this.detalles.update(arr => arr.map(d => d.id === editId ? {
            ...d,
            diaSemana: f.diaSemana,
            horaInicio: f.horaInicio + ':00',
            horaFin: f.horaFin + ':00',
            salidaDiaSiguiente: f.salidaDiaSiguiente,
            tiempoRefrigerioMinutos: f.tiempoRefrigerioMinutos
          } : d));
          this.editingDetalleId.set(null);
          this.toast.ok('Día actualizado');
        },
        error: err => this.toast.errHttp(err, 'Error al actualizar día')
      });
    }
  }

  deleteDetalle(id: number): void {
    if (!confirm('¿Eliminar este día?')) return;
    this.rrhhService.deleteHorarioDetalle(this.savedHtId()!, id).subscribe({
      next: () => { this.detalles.update(arr => arr.filter(d => d.id !== id)); this.toast.ok('Día eliminado'); },
      error: () => this.toast.err('Error al eliminar')
    });
  }

  toHm(t?: string): string { return t ? t.substring(0, 5) : ''; }
  onSearch(q: string): void { this.searchQuery.set(q); this.page.set(1); }
  onFilterTurno(v: number): void { this.filterTurno.set(v); this.page.set(1); }
  getTurnoName(id: number): string { return this.turnos().find(t => t.id === id)?.nombreCodigo ?? '—'; }
}
