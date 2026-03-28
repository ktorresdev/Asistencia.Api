import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RrhhService } from '../../../core/services/rrhh.service';
import { ToastService } from '../../../core/services/toast.service';
import { SucursalCentro } from '../../../core/models/rrhh.models';

@Component({
  selector: 'app-sucursales',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sucursales.component.html',
  styleUrl: './sucursales.component.scss'
})
export class SucursalesComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  toast = inject(ToastService);

  sedes = signal<SucursalCentro[]>([]);
  loading = signal(false);
  drawerOpen = signal(false);
  editingId = signal<number | null>(null);
  form = signal<Partial<SucursalCentro>>({ nombreSucursal: '', direccion: '', latitudCentro: undefined, longitudCentro: undefined, perimetroM: 200, esActivo: true });

  searchQuery = signal('');
  page = signal(1);
  readonly pageSize = 10;

  filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.sedes() : this.sedes().filter(s =>
      s.nombreSucursal.toLowerCase().includes(q) || (s.direccion ?? '').toLowerCase().includes(q)
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
    this.rrhhService.getSucursales().subscribe({ next: s => { this.sedes.set(s); this.loading.set(false); }, error: () => this.loading.set(false) });
  }

  openNew(): void {
    this.editingId.set(null);
    this.form.set({ nombreSucursal: '', direccion: '', latitudCentro: undefined, longitudCentro: undefined, perimetroM: 200, esActivo: true });
    this.drawerOpen.set(true);
  }

  openEdit(s: SucursalCentro): void {
    this.editingId.set(s.id);
    this.form.set({ ...s });
    this.drawerOpen.set(true);
  }

  close(): void { this.drawerOpen.set(false); }

  save(): void {
    const f = this.form();
    if (!f.nombreSucursal) { this.toast.err('Nombre requerido'); return; }
    const id = this.editingId();
    if (id) {
      this.rrhhService.updateSucursal(id, f).subscribe({ next: () => { this.toast.ok('Actualizado'); this.close(); this.load(); }, error: () => this.toast.err('Error') });
    } else {
      this.rrhhService.createSucursal(f).subscribe({ next: () => { this.toast.ok('Creado'); this.close(); this.load(); }, error: () => this.toast.err('Error') });
    }
  }

  delete(id: number): void {
    if (!confirm('¿Eliminar esta sede?')) return;
    this.rrhhService.deleteSucursal(id).subscribe({ next: () => { this.toast.ok('Eliminado'); this.load(); }, error: () => this.toast.err('Error al eliminar') });
  }
}
