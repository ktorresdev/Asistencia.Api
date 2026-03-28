import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { RrhhService } from '../../core/services/rrhh.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthService } from '../../core/services/auth.service';
import { Cobertura, CoberturaCreateDto, HorarioTurno, TrabajadorMapped } from '../../core/models/rrhh.models';

@Component({
  selector: 'app-coberturas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './coberturas.component.html',
  styleUrl: './coberturas.component.scss'
})
export class CoberturasComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  toast = inject(ToastService);
  auth = inject(AuthService);

  coberturas = signal<Cobertura[]>([]);
  horariosTurno = signal<HorarioTurno[]>([]);
  allTrabajadores = signal<TrabajadorMapped[]>([]);

  cobFil = signal<string>('todos');
  drawerOpen = signal(false);
  esReemplazo = signal(true);

  // form state
  cobTipo = signal<string>('COBERTURA');
  cobMotivo = signal<string>('FSGH');
  cfFecha = signal<string>(new Date().toISOString().split('T')[0]);
  cfAusente = signal<number>(0);
  cfCubre = signal<number | null>(null);
  cfHt = signal<number | null>(null);
  cfDev = signal<string>('');

  // buscador trabajadores
  ausenteQuery = signal('');
  cubreQuery = signal('');
  ausenteOpen = signal(false);
  cubreOpen = signal(false);

  filteredAusente = computed(() => {
    const q = this.ausenteQuery().toLowerCase();
    return this.allTrabajadores().filter(w =>
      !q || w.name.toLowerCase().includes(q) || w.dni.includes(q)
    ).slice(0, 40);
  });

  filteredCubre = computed(() => {
    const q = this.cubreQuery().toLowerCase();
    return this.allTrabajadores().filter(w =>
      !q || w.name.toLowerCase().includes(q) || w.dni.includes(q)
    ).slice(0, 40);
  });

  loading = signal(false);

  readonly TIPOS = ['COBERTURA', 'CAMBIO'];
  readonly MOTIVOS = [
    { k: 'FSGH', label: 'Sin goce de haber', sub: 'El día no será remunerado' },
    { k: 'FCGH', label: 'Con goce de haber',  sub: 'El día sí será remunerado' },
    { k: 'PERMISO', label: 'Permiso autorizado', sub: 'Permiso aprobado por jefatura' },
    { k: 'OTRO', label: 'Otro motivo', sub: 'Especificar en observaciones' },
  ];

  readonly COB_MSGS: Record<string, string> = {
    COBERTURA: 'El trabajador ausente quedará registrado con falta cubierta y quien lo reemplaza quedará registrado como cobertura extra. Si el que cubre lo hizo en su día de descanso, indica la fecha de devolución para que se le genere automáticamente un día de descanso compensatorio.',
    CAMBIO: 'Ambos trabajadores intercambian su turno ese día. Se registra el cambio para los dos, sin que ninguno quede como faltante.',
  };

  ngOnInit(): void {
    this.rrhhService.getHorariosTurno().subscribe(ht => this.horariosTurno.set(ht));
    this.loadTrabajadores();
    this.loadCoberturas();
  }

  private loadTrabajadores(): void {
    this.rrhhService.getTrabajadores(1, 50).pipe(
      switchMap(first => {
        const totalPages = first.totalPages ?? 1;
        if (totalPages <= 1) return of([first]);
        const rest = Array.from({ length: totalPages - 1 }, (_, i) =>
          this.rrhhService.getTrabajadores(i + 2, 50)
        );
        return forkJoin([of(first), ...rest]);
      })
    ).subscribe(pages => {
      const all = pages.flatMap(p => p.items ?? []);
      this.allTrabajadores.set(all
        .filter(w => w.idEstado !== 11)
        .map(w => ({
          id: w.id, name: w.apellidosNombres, dni: w.dni,
          sucursalId: w.sucursalId, idEstado: w.idEstado,
          tipo: (w.tipoTurno ?? '').toUpperCase().includes('ROT') ? 'ROT' : 'FIJ',
          idTurno: w.idTurno, idHorarioTurno: w.idHorarioTurno
        } as TrabajadorMapped))
        .sort((a, b) => a.name.localeCompare(b.name))
      );
    });
  }

  loadCoberturas(): void {
    this.loading.set(true);
    this.rrhhService.getCoberturas().subscribe({
      next: c => { this.coberturas.set(c); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  filtered = computed(() => {
    const f = this.cobFil();
    return f === 'todos' ? this.coberturas() : this.coberturas().filter(c => c.estado === f);
  });

  pendingCount = computed(() => this.coberturas().filter(c => c.estado === 'PENDIENTE').length);

  getCobId(c: Cobertura): number { return c.idCobertura ?? (c as any).id ?? 0; }

  getWorkerName(id?: number): string {
    if (!id) return '—';
    const w = this.allTrabajadores().find(x => x.id === id);
    return w ? w.name.split(' ').slice(0, 2).join(' ') : `#${id}`;
  }

  getHtName(id?: number): string {
    if (!id) return '—';
    return this.horariosTurno().find(h => h.id === id)?.nombreHorario ?? `id_ht=${id}`;
  }

  getEstadoCls(estado: string): string {
    const m: Record<string, string> = { PENDIENTE: 'ec-warn', APROBADO: 'ec-ok', RECHAZADO: 'ec-err', EJECUTADO: 'ec-info', DEVUELTO: 'ec-tea' };
    return `ec ${m[estado] ?? 'ec-gray'}`;
  }

  aprobar(id: number): void {
    this.rrhhService.aprobarCobertura(id).subscribe({
      next: () => { this.toast.ok('Aprobada'); this.loadCoberturas(); },
      error: () => this.toast.err('Error al aprobar')
    });
  }

  rechazar(id: number): void {
    this.rrhhService.rechazarCobertura(id).subscribe({
      next: () => { this.toast.ok('Rechazada'); this.loadCoberturas(); },
      error: () => this.toast.err('Error al rechazar')
    });
  }

  selectAusente(w: TrabajadorMapped): void {
    this.cfAusente.set(w.id);
    this.ausenteQuery.set(w.name);
    this.ausenteOpen.set(false);
  }

  selectCubre(w: TrabajadorMapped): void {
    this.cfCubre.set(w.id);
    this.cubreQuery.set(w.name);
    this.cubreOpen.set(false);
  }

  clearCubre(): void {
    this.cfCubre.set(null);
    this.cubreQuery.set('');
  }

  closeDrops(): void {
    setTimeout(() => {
      this.ausenteOpen.set(false);
      this.cubreOpen.set(false);
    }, 150);
  }

  openDrawer(): void {
    this.cobTipo.set('COBERTURA');
    this.cobMotivo.set('FSGH');
    this.esReemplazo.set(true);
    this.cfFecha.set(new Date().toISOString().split('T')[0]);
    this.cfAusente.set(0);
    this.cfCubre.set(null);
    this.cfHt.set(null);
    this.cfDev.set('');
    this.ausenteQuery.set('');
    this.cubreQuery.set('');
    this.ausenteOpen.set(false);
    this.cubreOpen.set(false);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void { this.drawerOpen.set(false); }

  save(): void {
    const fecha = this.cfFecha();
    const ausente = this.cfAusente() || 0;
    const cubre = this.cfCubre() || null;
    const ht = this.cfHt() || null;
    const dev = this.cfDev() || null;
    if (!fecha) { this.toast.err('Selecciona fecha'); return; }
    if (!cubre) { this.toast.err('Selecciona el trabajador que cubre'); return; }
    if (!ausente) { this.toast.err('Selecciona el trabajador afectado'); return; }
    if (cubre === ausente) { this.toast.err('El trabajador que cubre no puede ser el mismo afectado'); return; }

    const body: CoberturaCreateDto = {
      fecha: fecha + 'T00:00:00',
      idTrabajadorCubre: cubre,
      idTrabajadorAusente: ausente,
      idHorarioTurnoOriginal: ht,
      tipoCobertura: this.cobTipo(),
      motivoFalta: this.cobTipo() === 'COBERTURA' && this.esReemplazo() ? this.cobMotivo() : null,
      fechaSwapDevolucion: dev ? dev + 'T00:00:00' : null,
      esSoloAsignacion: !this.esReemplazo()
    };

    this.rrhhService.createCobertura(body).subscribe({
      next: () => { this.toast.ok(`${this.cobTipo()} registrada`); this.closeDrawer(); this.loadCoberturas(); },
      error: err => this.toast.errHttp(err)
    });
  }
}
