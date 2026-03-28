import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RrhhService } from '../../../core/services/rrhh.service';
import { ToastService } from '../../../core/services/toast.service';
import { TipoTurno } from '../../../core/models/rrhh.models';

@Component({
  selector: 'app-tipo-turno',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <ng-container>
      <div class="toolbar">
        <div class="srch-wrap"><span class="srch-ico">⌕</span><input class="srch" type="text" placeholder="Buscar..." [ngModel]="searchQuery()" (ngModelChange)="onSearch($event)" /></div>
        <span style="flex:1"></span>
        <button class="btn btn-primary btn-sm" (click)="openNew()">+ Nuevo tipo</button>
        @if (loading()) { <span class="spin"></span> }
      </div>
      <div class="grid-outer">
        <div class="tbl-wrap">
        <table class="tbl">
          <thead><tr><th>ID</th><th>Nombre</th><th>Descripción</th><th>Acciones</th></tr></thead>
          <tbody>
            @for (t of paginated(); track t.id) {
              <tr>
                <td style="font-family:var(--mono);color:var(--txt3)">{{ t.id }}</td>
                <td style="font-weight:600">{{ t.nombreTipo }}</td>
                <td style="color:var(--txt2)">{{ t.descripcion ?? '—' }}</td>
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
        <span class="drawer-title">{{ editingId() ? 'Editar tipo' : 'Nuevo tipo de turno' }}</span>
        <button class="btn-icon" (click)="close()">✕</button>
      </div>
      <div class="drawer-body">
        <div class="fld"><label class="fld-lbl">Nombre *</label><input class="inp" [ngModel]="form().nombreTipo" (ngModelChange)="form.update(f=>({...f,nombreTipo:$event}))" placeholder="ROTATIVO, FIJO..." /></div>
        <div class="fld"><label class="fld-lbl">Descripción</label><input class="inp" [ngModel]="form().descripcion" (ngModelChange)="form.update(f=>({...f,descripcion:$event}))" /></div>
        <div class="info-box">El nombre debe incluir "ROTATIVO" o "ROT" para que el sistema lo trate como turno rotativo, o "FIJO" para turno fijo.</div>
      </div>
      <div class="drawer-ftr">
        <button class="btn btn-primary" (click)="save()">Guardar</button>
        <button class="btn" (click)="close()">Cancelar</button>
      </div>
    </div>
  `,
  styles: [`:host{display:flex;flex-direction:column;flex:1;min-height:0;overflow:hidden;}`]
})
export class TipoTurnoComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  toast = inject(ToastService);
  tipos = signal<TipoTurno[]>([]);
  loading = signal(false);
  drawerOpen = signal(false);
  editingId = signal<number | null>(null);
  form = signal<Partial<TipoTurno>>({ nombreTipo: '', descripcion: '' });

  searchQuery = signal('');
  page = signal(1);
  readonly pageSize = 10;

  filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.tipos() : this.tipos().filter(t =>
      t.nombreTipo.toLowerCase().includes(q) || (t.descripcion ?? '').toLowerCase().includes(q)
    );
  });
  totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));
  paginated = computed(() => {
    const p = this.page() - 1;
    return this.filtered().slice(p * this.pageSize, (p + 1) * this.pageSize);
  });
  onSearch(q: string): void { this.searchQuery.set(q); this.page.set(1); }

  ngOnInit(): void { this.load(); }
  load(): void {
    this.loading.set(true);
    this.rrhhService.getTiposTurno().subscribe({ next: t => { this.tipos.set(t); this.loading.set(false); }, error: () => this.loading.set(false) });
  }
  openNew(): void { this.editingId.set(null); this.form.set({ nombreTipo: '', descripcion: '' }); this.drawerOpen.set(true); }
  openEdit(t: TipoTurno): void { this.editingId.set(t.id); this.form.set({ ...t }); this.drawerOpen.set(true); }
  close(): void { this.drawerOpen.set(false); }
  save(): void {
    if (!this.form().nombreTipo) { this.toast.err('Nombre requerido'); return; }
    const id = this.editingId();
    if (id) {
      this.rrhhService.updateTipoTurno(id, this.form()).subscribe({ next: () => { this.toast.ok('Actualizado'); this.close(); this.load(); }, error: () => this.toast.err('Error') });
    } else {
      this.rrhhService.createTipoTurno(this.form()).subscribe({ next: () => { this.toast.ok('Creado'); this.close(); this.load(); }, error: () => this.toast.err('Error') });
    }
  }
  delete(id: number): void {
    if (!confirm('¿Eliminar?')) return;
    this.rrhhService.deleteTipoTurno(id).subscribe({ next: () => { this.toast.ok('Eliminado'); this.load(); }, error: () => this.toast.err('Error') });
  }
}
