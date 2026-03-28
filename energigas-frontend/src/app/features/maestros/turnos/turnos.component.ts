import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RrhhService } from '../../../core/services/rrhh.service';
import { ToastService } from '../../../core/services/toast.service';
import { Turno, TipoTurno } from '../../../core/models/rrhh.models';

@Component({
  selector: 'app-turnos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <ng-container>
      <div class="toolbar">
        <div class="srch-wrap"><span class="srch-ico">⌕</span><input class="srch" type="text" placeholder="Buscar..." [ngModel]="searchQuery()" (ngModelChange)="onSearch($event)" /></div>
        <span style="flex:1"></span>
        <button class="btn btn-primary btn-sm" (click)="openNew()">+ Nuevo turno</button>
        @if (loading()) { <span class="spin"></span> }
      </div>
      <div class="grid-outer">
        <div class="tbl-wrap">
        <table class="tbl">
          <thead><tr><th>Código</th><th>Tipo</th><th>Activo</th><th>Acciones</th></tr></thead>
          <tbody>
            @for (t of paginated(); track t.id) {
              <tr>
                <td style="font-weight:600">{{ t.nombreCodigo }}</td>
                <td>
                  <span class="tipo-tag" [class]="getTipoClass(t)">{{ getTipoLabel(t) }}</span>
                  {{ getTipoName(t.tipoTurnoId) }}
                </td>
                <td><span class="ec" [class]="t.esActivo ? 'ec-ok' : 'ec-err'">{{ t.esActivo ? 'SÍ' : 'NO' }}</span></td>
                <td style="display:flex;gap:4px">
                  <button class="btn btn-sm" (click)="openEdit(t)">Editar</button>
                  <button class="btn btn-sm btn-danger" (click)="delete(t.id)">Eliminar</button>
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
    <div class="overlay" [class.open]="drawerOpen()" (click)="close()"></div>
    <div class="drawer" [class.open]="drawerOpen()">
      <div class="drawer-hdr">
        <span class="drawer-title">{{ editingId() ? 'Editar turno' : 'Nuevo turno' }}</span>
        <button class="btn-icon" (click)="close()">✕</button>
      </div>
      <div class="drawer-body">
        <div class="fld"><label class="fld-lbl">Código / Nombre *</label><input class="inp" [ngModel]="form().nombreCodigo" (ngModelChange)="form.update(f=>({...f,nombreCodigo:$event}))" placeholder="T-ROT-A" /></div>
        <div class="fld">
          <label class="fld-lbl">Tipo de turno *</label>
          <select class="sel" [ngModel]="form().tipoTurnoId" (ngModelChange)="form.update(f=>({...f,tipoTurnoId:+$event}))">
            @for (tt of tiposTurno(); track tt.id) { <option [value]="tt.id">{{ tt.nombreTipo }}</option> }
          </select>
        </div>
        <div class="fld">
          <label class="fld-lbl">Activo</label>
          <select class="sel" [ngModel]="form().esActivo" (ngModelChange)="form.update(f=>({...f,esActivo:$event==='true'}))">
            <option [value]="true">Sí</option>
            <option [value]="false">No</option>
          </select>
        </div>
      </div>
      <div class="drawer-ftr">
        <button class="btn btn-primary" (click)="save()">Guardar</button>
        <button class="btn" (click)="close()">Cancelar</button>
      </div>
    </div>
  `,
  styles: [`:host{display:flex;flex-direction:column;flex:1;min-height:0;overflow:hidden;}`]
})
export class TurnosComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  toast = inject(ToastService);
  turnos = signal<Turno[]>([]);
  tiposTurno = signal<TipoTurno[]>([]);
  loading = signal(false);
  drawerOpen = signal(false);
  editingId = signal<number | null>(null);
  form = signal<Partial<Turno>>({ nombreCodigo: '', tipoTurnoId: 0, esActivo: true });

  searchQuery = signal('');
  page = signal(1);
  readonly pageSize = 10;

  filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.turnos() : this.turnos().filter(t =>
      t.nombreCodigo.toLowerCase().includes(q) || this.getTipoName(t.tipoTurnoId).toLowerCase().includes(q)
    );
  });
  totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));
  paginated = computed(() => {
    const p = this.page() - 1;
    return this.filtered().slice(p * this.pageSize, (p + 1) * this.pageSize);
  });
  onSearch(q: string): void { this.searchQuery.set(q); this.page.set(1); }

  ngOnInit(): void {
    this.rrhhService.getTiposTurno().subscribe(tt => { this.tiposTurno.set(tt); if (tt.length) this.form.update(f => ({ ...f, tipoTurnoId: tt[0].id })); });
    this.load();
  }
  load(): void {
    this.loading.set(true);
    this.rrhhService.getTurnos().subscribe({ next: t => { this.turnos.set(t); this.loading.set(false); }, error: () => this.loading.set(false) });
  }
  openNew(): void { this.editingId.set(null); this.form.set({ nombreCodigo: '', tipoTurnoId: this.tiposTurno()[0]?.id ?? 0, esActivo: true }); this.drawerOpen.set(true); }
  openEdit(t: Turno): void { this.editingId.set(t.id); this.form.set({ ...t }); this.drawerOpen.set(true); }
  close(): void { this.drawerOpen.set(false); }
  save(): void {
    if (!this.form().nombreCodigo || !this.form().tipoTurnoId) { this.toast.err('Código y tipo requeridos'); return; }
    const id = this.editingId();
    if (id) {
      this.rrhhService.updateTurno(id, this.form()).subscribe({ next: () => { this.toast.ok('Actualizado'); this.close(); this.load(); }, error: () => this.toast.err('Error') });
    } else {
      this.rrhhService.createTurno(this.form()).subscribe({ next: () => { this.toast.ok('Creado'); this.close(); this.load(); }, error: () => this.toast.err('Error') });
    }
  }
  delete(id: number): void {
    if (!confirm('¿Eliminar?')) return;
    this.rrhhService.deleteTurno(id).subscribe({ next: () => { this.toast.ok('Eliminado'); this.load(); }, error: () => this.toast.err('Error') });
  }
  getTipoName(id: number): string { return this.tiposTurno().find(t => t.id === id)?.nombreTipo ?? '—'; }
  getTipoClass(t: Turno): string {
    const tipo = this.tiposTurno().find(tt => tt.id === t.tipoTurnoId);
    return (tipo?.nombreTipo ?? '').toUpperCase().includes('ROT') ? 'tipo-ROT' : 'tipo-FIJ';
  }
  getTipoLabel(t: Turno): string {
    const tipo = this.tiposTurno().find(tt => tt.id === t.tipoTurnoId);
    return (tipo?.nombreTipo ?? '').toUpperCase().includes('ROT') ? 'ROT' : 'FIJ';
  }
}
