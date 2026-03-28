import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { RrhhService } from '../../core/services/rrhh.service';
import { ToastService } from '../../core/services/toast.service';
import { TrabajadorMapped, AusenciaRegistrada } from '../../core/models/rrhh.models';

interface TipoAusencia {
  key: string;
  label: string;
  sub: string;
  color: string;
}

@Component({
  selector: 'app-ausencias',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ausencias.component.html',
  styleUrl: './ausencias.component.scss'
})
export class AusenciasComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  toast = inject(ToastService);

  allTrabajadores = signal<TrabajadorMapped[]>([]);
  loading = signal(false);
  saving = signal(false);

  // form
  selectedTrabajador = signal<TrabajadorMapped | null>(null);
  tipoSeleccionado = signal<string>('VACACIONES');
  fechaInicio = signal<string>(new Date().toISOString().split('T')[0]);
  fechaFin = signal<string>(new Date().toISOString().split('T')[0]);

  // drawer
  drawerOpen = signal(false);

  // buscador trabajador
  searchQuery = signal('');
  dropOpen = signal(false);

  // lista de ausencias registradas
  ausencias = signal<AusenciaRegistrada[]>([]);
  loadingLista = signal(false);
  listFechaInicio = signal<string>(this.primerDiaMes());
  listFechaFin = signal<string>(this.ultimoDiaMes());
  listTipo = signal<string>('');
  listQuery = signal<string>('');
  deletingId = signal<number | null>(null);
  listPagina = signal(0);
  readonly LIST_PAGE_SIZE = 15;

  filteredAusencias = computed(() => {
    const q = this.listQuery().toLowerCase();
    return this.ausencias().filter(a =>
      !q || a.trabajadorNombre.toLowerCase().includes(q) || a.dni.includes(q)
    );
  });

  listTotalPaginas = computed(() => Math.max(1, Math.ceil(this.filteredAusencias().length / this.LIST_PAGE_SIZE)));

  listPaginaActual = computed(() => {
    const start = this.listPagina() * this.LIST_PAGE_SIZE;
    return this.filteredAusencias().slice(start, start + this.LIST_PAGE_SIZE);
  });

  filteredTrabajadores = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return [];
    return this.allTrabajadores().filter(w =>
      w.name.toLowerCase().includes(q) || w.dni.includes(q)
    ).slice(0, 60);
  });

  readonly TIPOS: TipoAusencia[] = [
    { key: 'VACACIONES',       label: 'Vacaciones',       sub: 'Descanso anual remunerado',   color: 'var(--acc)'  },
    { key: 'DESCANSO_MEDICO',  label: 'Descanso Médico',  sub: 'Certificado médico / reposo', color: 'var(--red)'  },
    { key: 'MATERNIDAD',       label: 'Maternidad',       sub: 'Licencia pre y post natal',   color: 'var(--pur)'  },
    { key: 'PATERNIDAD',       label: 'Paternidad',       sub: 'Licencia por nacimiento',     color: 'var(--tea)'  },
    { key: 'PERMISO_SIN_GOCE', label: 'Permiso sin goce', sub: 'Permiso no remunerado',       color: 'var(--amb)'  },
  ];

  diasCalculados = computed(() => {
    const ini = new Date(this.fechaInicio());
    const fin = new Date(this.fechaFin());
    if (!this.fechaInicio() || !this.fechaFin() || fin < ini) return 0;
    return Math.round((fin.getTime() - ini.getTime()) / 86400000) + 1;
  });

  ngOnInit(): void {
    this.loadTrabajadores();
    this.loadAusencias();
  }

  private primerDiaMes(): string {
    const d = new Date(); d.setDate(1);
    return d.toISOString().split('T')[0];
  }

  private ultimoDiaMes(): string {
    const d = new Date(); d.setMonth(d.getMonth() + 1); d.setDate(0);
    return d.toISOString().split('T')[0];
  }

  private loadTrabajadores(): void {
    this.loading.set(true);
    this.rrhhService.getTrabajadores(1, 200).pipe(
      switchMap(first => {
        const total = first.totalPages ?? 1;
        if (total <= 1) return of([first]);
        const rest = Array.from({ length: total - 1 }, (_, i) =>
          this.rrhhService.getTrabajadores(i + 2, 200)
        );
        return forkJoin([of(first), ...rest]);
      })
    ).subscribe({
      next: pages => {
        const all = pages.flatMap(p => p.items ?? []);
        this.allTrabajadores.set(
          all
            .filter(w => w.idEstado !== 11)
            .map(w => ({
              id: w.id, name: w.apellidosNombres, dni: w.dni,
              sucursalId: w.sucursalId, idEstado: w.idEstado,
              tipo: (w.tipoTurno ?? '').toUpperCase().includes('ROT') ? 'ROT' : 'FIJ',
              idTurno: w.idTurno, idHorarioTurno: w.idHorarioTurno
            } as TrabajadorMapped))
            .sort((a, b) => a.name.localeCompare(b.name))
        );
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  loadAusencias(): void {
    this.listPagina.set(0);
    this.loadingLista.set(true);
    this.rrhhService.getAusencias({
      fechaInicio: this.listFechaInicio(),
      fechaFin: this.listFechaFin(),
      tipo: this.listTipo() || undefined
    }).subscribe({
      next: data => { this.ausencias.set(data); this.loadingLista.set(false); },
      error: ()   => { this.loadingLista.set(false); }
    });
  }

  selectTrabajador(w: TrabajadorMapped): void {
    this.selectedTrabajador.set(w);
    this.searchQuery.set(w.name);
    this.dropOpen.set(false);
  }

  clearTrabajador(): void {
    this.selectedTrabajador.set(null);
    this.searchQuery.set('');
  }

  closeDrop(): void {
    setTimeout(() => this.dropOpen.set(false), 150);
  }

  getTipoInfo(key: string): TipoAusencia {
    return this.TIPOS.find(t => t.key === key) ?? this.TIPOS[0];
  }

  tipoColor(key: string): string {
    return this.getTipoInfo(key).color;
  }

  tipoLabel(key: string): string {
    return this.getTipoInfo(key).label;
  }

  openDrawer(): void {
    this.clearTrabajador();
    this.tipoSeleccionado.set('VACACIONES');
    this.fechaInicio.set(new Date().toISOString().split('T')[0]);
    this.fechaFin.set(new Date().toISOString().split('T')[0]);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  save(): void {
    const trab = this.selectedTrabajador();
    const ini  = this.fechaInicio();
    const fin  = this.fechaFin();
    const tipo = this.tipoSeleccionado();

    if (!trab) { this.toast.err('Selecciona un trabajador'); return; }
    if (!ini || !fin) { this.toast.err('Selecciona el rango de fechas'); return; }
    if (new Date(fin) < new Date(ini)) { this.toast.err('La fecha fin no puede ser anterior al inicio'); return; }

    const programaciones: any[] = [];
    const d = new Date(ini);
    const dFin = new Date(fin);
    while (d <= dFin) {
      programaciones.push({
        trabajadorId: trab.id,
        fecha: d.toISOString().split('T')[0],
        idHorarioTurno: null,
        esDescanso: false,
        esDiaBoleta: false,
        esVacaciones: false,
        tipoAusencia: tipo
      });
      d.setDate(d.getDate() + 1);
    }

    this.saving.set(true);
    this.rrhhService.saveProgramacion({ fechaInicio: ini, fechaFin: fin, programaciones }).subscribe({
      next: res => {
        const info = this.getTipoInfo(tipo);
        this.toast.ok(`${info.label} registrada · ${res?.registrosGrabados ?? programaciones.length} días`);
        this.saving.set(false);
        this.closeDrawer();
        this.loadAusencias();
      },
      error: err => {
        this.toast.errHttp(err, 'Error al registrar ausencia');
        this.saving.set(false);
      }
    });
  }

  deleteAusencia(a: AusenciaRegistrada): void {
    this.deletingId.set(a.id);
    this.rrhhService.deleteAusencia(a.id).subscribe({
      next: () => {
        this.toast.ok(`Ausencia eliminada · ${a.trabajadorNombre.split(' ').slice(0,2).join(' ')} · ${a.fecha}`);
        this.ausencias.update(list => list.filter(x => x.id !== a.id));
        this.deletingId.set(null);
      },
      error: () => {
        this.toast.err('Error al eliminar la ausencia');
        this.deletingId.set(null);
      }
    });
  }
}
