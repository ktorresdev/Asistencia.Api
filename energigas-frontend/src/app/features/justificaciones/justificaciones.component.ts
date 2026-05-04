import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { RrhhService } from '../../core/services/rrhh.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthService } from '../../core/services/auth.service';
import { Justificacion, JustificacionCreateDto, TipoJustificacion, TrabajadorMapped, DocumentoJustificacion } from '../../core/models/rrhh.models';

@Component({
  selector: 'app-justificaciones',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './justificaciones.component.html',
  styleUrl: './justificaciones.component.scss'
})
export class JustificacionesComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  toast = inject(ToastService);
  auth = inject(AuthService);

  justificaciones = signal<Justificacion[]>([]);
  tiposJustificacion = signal<TipoJustificacion[]>([]);
  allTrabajadores = signal<TrabajadorMapped[]>([]);

  filtroEstado = signal<string>('todos');
  filtroFecha = signal<string>('');
  filtroTrabajador = signal<string>('');

  drawerOpen = signal(false);
  detalleOpen = signal(false);
  selectedJustificacion = signal<Justificacion | null>(null);

  // form state
  formTrabajadorId = signal<number>(0);
  formTipoId = signal<number>(0);
  formFecha = signal<string>(new Date().toISOString().split('T')[0]);
  formMotivo = signal<string>('');
  formArchivos = signal<File[]>([]);

  // search trabajador
  trabajadorQuery = signal('');
  trabajadorOpen = signal(false);

  filteredTrabajadores = computed(() => {
    const q = this.trabajadorQuery().toLowerCase();
    return this.allTrabajadores().filter(w =>
      !q || w.name.toLowerCase().includes(q) || w.dni.includes(q)
    ).slice(0, 40);
  });

  loading = signal(false);

  readonly TIPOS_ARCHIVO = ['PDF', 'DOC', 'DOCX', 'JPG', 'PNG', 'WEBP'];
  readonly MAX_ARCHIVOS = 5;
  readonly MAX_ARCHIVO_SIZE_MB = 10; // Asumiendo 10MB por archivo

  ngOnInit(): void {
    this.rrhhService.getTiposJustificacion().subscribe(t => this.tiposJustificacion.set(t));
    this.loadTrabajadores();
    this.loadJustificaciones();
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

  loadJustificaciones(): void {
    this.loading.set(true);
    const filters: any = {};
    if (this.filtroEstado() !== 'todos') filters.estado = this.filtroEstado();
    if (this.filtroFecha()) filters.fecha = this.filtroFecha();
    if (!this.auth.isSuperAdmin() && this.auth.currentUser()?.trabajadorId) {
      // Solo trabajadores ven sus propias justificaciones si no son admin
      if (!this.auth.isAdminOrSupervisor()) {
        filters.idTrabajador = this.auth.currentUser()?.trabajadorId;
      }
    }
    
    this.rrhhService.getJustificaciones(filters).subscribe({
      next: j => { this.justificaciones.set(j); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  filtered = computed(() => {
    let items = this.justificaciones();
    const q = this.filtroTrabajador().toLowerCase();
    
    if (q) {
      items = items.filter(j =>
        j.nombreTrabajador.toLowerCase().includes(q) ||
        j.tipoJustificacion.toLowerCase().includes(q)
      );
    }
    
    return items;
  });

  pendingCount = computed(() => this.justificaciones().filter(j => j.estado === 'PENDIENTE').length);

  getJustificacionId(j: Justificacion): number {
    return j.id ?? 0;
  }

  getEstadoBadgeClass(estado: string): string {
    const classes: Record<string, string> = {
      PENDIENTE: 'badge-warn',
      APROBADO: 'badge-ok',
      RECHAZADO: 'badge-err'
    };
    return classes[estado] ?? 'badge-gray';
  }

  getEstadoColor(estado: string): string {
    const colors: Record<string, string> = {
      PENDIENTE: '#FFA500',
      APROBADO: '#28A745',
      RECHAZADO: '#DC3545'
    };
    return colors[estado] ?? '#999';
  }

  getTrabajadorName(id: number): string {
    const w = this.allTrabajadores().find(x => x.id === id);
    return w ? w.name.split(' ').slice(0, 2).join(' ') : `#${id}`;
  }

  getTipoName(id: number): string {
    const t = this.tiposJustificacion().find(x => x.id === id);
    return t?.nombreTipo ?? `id_tipo=${id}`;
  }

  openDrawer(): void {
    // Si el usuario es trabajador, asignar su ID automáticamente
    if (!this.auth.isAdminOrSupervisor() && this.auth.currentUser()?.trabajadorId) {
      this.formTrabajadorId.set(this.auth.currentUser()!.trabajadorId!);
    } else {
      this.formTrabajadorId.set(0);
    }
    
    this.formTipoId.set(0);
    this.formFecha.set(new Date().toISOString().split('T')[0]);
    this.formMotivo.set('');
    this.formArchivos.set([]);
    this.trabajadorQuery.set('');
    this.trabajadorOpen.set(false);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  selectTrabajador(w: TrabajadorMapped): void {
    this.formTrabajadorId.set(w.id);
    this.trabajadorQuery.set(w.name);
    this.trabajadorOpen.set(false);
  }

  onFileSelected(event: any): void {
    const files: File[] = event.target.files;
    if (!files || files.length === 0) return;

    const currentCount = this.formArchivos().length;
    const available = this.MAX_ARCHIVOS - currentCount;
    const filesToAdd: File[] = [];

    for (let i = 0; i < files.length && i < available; i++) {
      const file = files[i];
      
      // Validar tipo
      const ext = file.name.split('.').pop()?.toUpperCase();
      if (!ext || !this.TIPOS_ARCHIVO.includes(ext)) {
        this.toast.err(`Tipo no permitido: ${ext}`);
        continue;
      }

      // Validar tamaño
      if (file.size > this.MAX_ARCHIVO_SIZE_MB * 1024 * 1024) {
        this.toast.err(`${file.name} supera el límite de ${this.MAX_ARCHIVO_SIZE_MB}MB`);
        continue;
      }

      filesToAdd.push(file);
    }

    if (filesToAdd.length > 0) {
      this.formArchivos.set([...this.formArchivos(), ...filesToAdd]);
    }

    if (currentCount + filesToAdd.length >= this.MAX_ARCHIVOS) {
      this.toast.show('Limite de archivos alcanzado', 'warn');
    }
  }

  removeArchivo(index: number): void {
    const current = this.formArchivos();
    this.formArchivos.set(current.filter((_, i) => i !== index));
  }

  closeDrops(): void {
    setTimeout(() => {
      this.trabajadorOpen.set(false);
    }, 150);
  }

  save(): void {
    const trabajadorId = this.formTrabajadorId();
    const tipoId = this.formTipoId();
    const fecha = this.formFecha();
    const archivos = this.formArchivos();
    const motivo = this.formMotivo();

    // Validaciones
    if (!fecha) {
      this.toast.err('Selecciona la fecha a justificar');
      return;
    }
    if (tipoId === 0) {
      this.toast.err('Selecciona un tipo de justificación');
      return;
    }
    if (!this.auth.isAdminOrSupervisor() && trabajadorId === 0) {
      this.toast.err('Selecciona un trabajador');
      return;
    }

    // Validar si requiere adjunto
    const tipoSeleccionado = this.tiposJustificacion().find(t => t.id === tipoId);
    if (tipoSeleccionado?.requiereAdjunto && archivos.length === 0) {
      this.toast.err('Este tipo de justificación requiere al menos 1 archivo');
      return;
    }

    // Si es trabajador, usar su propio ID
    const finalTrabajadorId = this.auth.isAdminOrSupervisor() ? trabajadorId : (this.auth.currentUser()?.trabajadorId ?? 0);

    const dto: JustificacionCreateDto = {
      trabajadorId: finalTrabajadorId,
      tipoJustificacionId: tipoId,
      fechaJustificada: fecha + 'T00:00:00',
      motivo: motivo || undefined,
      archivos: archivos
    };

    this.rrhhService.crearJustificacion(dto).subscribe({
      next: () => {
        this.toast.ok('Justificación registrada');
        this.closeDrawer();
        this.loadJustificaciones();
      },
      error: err => this.toast.errHttp(err)
    });
  }

  openDetalle(j: Justificacion): void {
    this.selectedJustificacion.set(j);
    this.detalleOpen.set(true);
  }

  closeDetalle(): void {
    this.detalleOpen.set(false);
    this.selectedJustificacion.set(null);
  }

  downloadArchivo(doc: DocumentoJustificacion): void {
    // Abrir en nueva ventana o descargar
    window.open(doc.url, '_blank');
  }

  aprobar(id: number): void {
    this.rrhhService.aprobarJustificacion(id).subscribe({
      next: () => {
        this.toast.ok('Justificación aprobada');
        this.closeDetalle();
        this.loadJustificaciones();
      },
      error: () => this.toast.err('Error al aprobar')
    });
  }

  rechazar(id: number): void {
    this.rrhhService.rechazarJustificacion(id).subscribe({
      next: () => {
        this.toast.ok('Justificación rechazada');
        this.closeDetalle();
        this.loadJustificaciones();
      },
      error: () => this.toast.err('Error al rechazar')
    });
  }
}
