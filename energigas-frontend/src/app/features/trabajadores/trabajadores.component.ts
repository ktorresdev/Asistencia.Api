import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RrhhService } from '../../core/services/rrhh.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthService } from '../../core/services/auth.service';
import {
  TrabajadorResponseDto, SucursalCentro, Turno, HorarioTurno, TipoTurno, TrabajadorSucursal,
  CrearTrabajadorCompletoDto
} from '../../core/models/rrhh.models';

interface PersonaForm {
  dni: string; apellidosNombres: string; telefono: string; email: string; fechaNacimiento: string;
}
interface UsuarioForm {
  username: string; password: string; role: string;
}
interface TrabajadorForm {
  sucursalId: number; idEstado: number; cargo: string; areaDepartamento: string;
  jefeInmediatoId: number | null; marcajeEnZona: boolean; tomarFoto: boolean; fechaIngreso: string;
}
interface TurnoForm {
  turnoId: number; horarioTurnoId: number | null; fechaInicioVigencia: string; motivoCambio: string;
}

@Component({
  selector: 'app-trabajadores',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './trabajadores.component.html',
  styleUrl: './trabajadores.component.scss'
})
export class TrabajadoresComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  toast = inject(ToastService);
  auth = inject(AuthService);

  trabajadores = signal<TrabajadorResponseDto[]>([]);
  sedes = signal<SucursalCentro[]>([]);
  turnos = signal<Turno[]>([]);
  horariosTurno = signal<HorarioTurno[]>([]);
  tiposTurno = signal<TipoTurno[]>([]);

  trabFil = signal<string>('ALL');
  searchQuery = signal<string>('');
  sedeFilter = signal<string>('');

  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = signal(1);

  private searchTimer: any;

  loading = signal(false);
  drawerOpen = signal(false);
  wizardStep = signal<1 | 2 | 3 | 4>(1);
  editingId = signal<number | null>(null);

  // Saved IDs from wizard steps
  savedPersonaId = signal<number | null>(null);
  savedTrabajadorId = signal<number | null>(null);

  // Sedes adicionales
  sedesAsignadas = signal<TrabajadorSucursal[]>([]);
  sedeForm = signal({ sucursalId: 0, puedeGestionar: false, fechaInicio: new Date().toISOString().split('T')[0], fechaFin: '' });
  addingSedeLoading = signal(false);
  editSection = signal<string | null>(null);

  personaForm = signal<PersonaForm>({ dni: '', apellidosNombres: '', telefono: '', email: '', fechaNacimiento: '' });
  usuarioForm = signal<UsuarioForm>({ username: '', password: '', role: 'TRABAJADOR' });
  trabForm = signal<TrabajadorForm>({ sucursalId: 0, idEstado: 10, cargo: '', areaDepartamento: '', jefeInmediatoId: null, marcajeEnZona: false, tomarFoto: true, fechaIngreso: '' });
  turnoForm = signal<TurnoForm>({ turnoId: 0, horarioTurnoId: null, fechaInicioVigencia: new Date().toISOString().split('T')[0], motivoCambio: '' });

  ngOnInit(): void {
    this.rrhhService.getSucursales().subscribe(s => { this.sedes.set(s); if (s.length) this.trabForm.update(f => ({ ...f, sucursalId: s[0].id })); });
    this.rrhhService.getTurnos().subscribe(t => { this.turnos.set(t); if (t.length) this.turnoForm.update(f => ({ ...f, turnoId: t[0].id })); });
    this.rrhhService.getHorariosTurno().subscribe(ht => this.horariosTurno.set(ht));
    this.rrhhService.getTiposTurno().subscribe(tt => this.tiposTurno.set(tt));
    this.loadTrabajadores();
  }

  loadTrabajadores(): void {
    this.loading.set(true);
    const search = this.searchQuery();
    const sucursalId = this.sedeFilter() ? +this.sedeFilter() : undefined;
    const tipo = this.trabFil() === 'ALL' ? undefined : this.trabFil();
    this.rrhhService.getTrabajadores(this.page(), this.pageSize(), search, sucursalId, tipo).subscribe({
      next: res => {
        this.trabajadores.set(res.items ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.totalPages.set(res.totalPages ?? 1);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  bySede = computed(() => {
    const map: Record<string, TrabajadorResponseDto[]> = {};
    this.trabajadores().forEach(w => {
      const sedeName = this.sedes().find(s => s.id === w.sucursalId)?.nombreSucursal ?? 'Sin sede';
      if (!map[sedeName]) map[sedeName] = [];
      map[sedeName].push(w);
    });
    return Object.entries(map).sort(([a], [b]) => a.localeCompare(b));
  });

  onFilter(): void {
    this.page.set(1);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.loadTrabajadores(), 300);
  }

  goPage(n: number): void {
    this.page.set(n);
    this.loadTrabajadores();
  }

  getSedeName(id: number): string { return this.sedes().find(s => s.id === id)?.nombreSucursal ?? '—'; }
  getHtName(id?: number): string { if (!id) return '—'; return this.horariosTurno().find(h => h.id === id)?.nombreHorario ?? `id=${id}`; }
  getTipoLabel(w: TrabajadorResponseDto): string { return (w.tipoTurno ?? '').toUpperCase().includes('ROT') ? 'ROT' : 'FIJ'; }
  getTipoClass(w: TrabajadorResponseDto): string { return (w.tipoTurno ?? '').toUpperCase().includes('ROT') ? 'tipo-ROT' : 'tipo-FIJ'; }
  isActivo(w: TrabajadorResponseDto): boolean { return w.idEstado === 10; }

  getHtForTurno(turnoId: number): HorarioTurno[] { return this.horariosTurno().filter(h => h.turnoId === turnoId); }
  turnoEsRotativo(turnoId: number): boolean {
    const turno = this.turnos().find(t => t.id === turnoId);
    const tipo = this.tiposTurno().find(tt => tt.id === turno?.tipoTurnoId);
    return (tipo?.nombreTipo ?? '').toUpperCase().includes('ROT');
  }

  openNewWorker(): void {
    this.editingId.set(null);
    this.savedPersonaId.set(null);
    this.savedTrabajadorId.set(null);
    this.personaForm.set({ dni: '', apellidosNombres: '', telefono: '', email: '', fechaNacimiento: '' });
    this.usuarioForm.set({ username: '', password: '', role: 'TRABAJADOR' });
    const sedeId = this.sedes()[0]?.id ?? 0;
    this.trabForm.set({ sucursalId: sedeId, idEstado: 10, cargo: '', areaDepartamento: '', jefeInmediatoId: null, marcajeEnZona: false, tomarFoto: true, fechaIngreso: '' });
    const turnoId = this.turnos()[0]?.id ?? 0;
    this.turnoForm.set({ turnoId, horarioTurnoId: null, fechaInicioVigencia: new Date().toISOString().split('T')[0], motivoCambio: '' });
    this.wizardStep.set(1);
    this.drawerOpen.set(true);
  }

  openEdit(w: TrabajadorResponseDto): void {
    this.editingId.set(w.id);
    this.savedPersonaId.set(w.personaId);
    this.savedTrabajadorId.set(w.id);
    this.personaForm.set({ dni: w.dni, apellidosNombres: w.apellidosNombres, telefono: '', email: '', fechaNacimiento: '' });
    this.trabForm.set({ sucursalId: w.sucursalId, idEstado: w.idEstado, cargo: '', areaDepartamento: '', jefeInmediatoId: null, marcajeEnZona: false, tomarFoto: true, fechaIngreso: '' });
    this.turnoForm.set({ turnoId: w.idTurno ?? 0, horarioTurnoId: w.idHorarioTurno ?? null, fechaInicioVigencia: new Date().toISOString().split('T')[0], motivoCambio: '' });
    this.editSection.set(null);
    this.sedesAsignadas.set([]);
    this.sedeForm.set({ sucursalId: this.sedes()[0]?.id ?? 0, puedeGestionar: false, fechaInicio: new Date().toISOString().split('T')[0], fechaFin: '' });
    this.loadSedesAsignadas();
    this.drawerOpen.set(true);
  }

  loadSedesAsignadas(): void {
    const id = this.savedTrabajadorId();
    if (!id) return;
    this.rrhhService.getSucursalesDisponibles(id).subscribe(s => this.sedesAsignadas.set(s));
  }

  addSede(): void {
    const f = this.sedeForm();
    if (!f.sucursalId) { this.toast.err('Selecciona una sede'); return; }
    this.addingSedeLoading.set(true);
    this.rrhhService.asignarSede(this.savedTrabajadorId()!, {
      sucursalId: f.sucursalId,
      puedeGestionar: f.puedeGestionar,
      fechaInicio: f.fechaInicio,
      fechaFin: f.fechaFin || undefined
    }).subscribe({
      next: () => {
        this.toast.ok('Sede asignada');
        this.sedeForm.update(f => ({ ...f, sucursalId: this.sedes()[0]?.id ?? 0, fechaFin: '' }));
        this.addingSedeLoading.set(false);
        this.loadSedesAsignadas();
      },
      error: err => {
        this.toast.errHttp(err, 'Error al asignar sede');
        this.addingSedeLoading.set(false);
      }
    });
  }

  removeSede(sucursalId: number): void {
    if (!confirm('¿Remover esta sede del trabajador?')) return;
    this.rrhhService.removerSede(this.savedTrabajadorId()!, sucursalId).subscribe({
      next: () => { this.toast.ok('Sede removida'); this.loadSedesAsignadas(); },
      error: err => this.toast.errHttp(err, 'Error al remover sede')
    });
  }

  closeDrawer(): void { this.drawerOpen.set(false); }

  // STEP 1-3: solo para nuevo trabajador (wizard)
  saveStep1(): void {
    const f = this.personaForm();
    if (!f.dni || !f.apellidosNombres) { this.toast.err('DNI y nombre son obligatorios'); return; }
    this.wizardStep.set(2);
  }

  saveStep2(): void {
    const u = this.usuarioForm();
    if (!u.username) { this.toast.err('El nombre de usuario es obligatorio'); return; }
    if (u.password.length < 6) { this.toast.err('La contraseña debe tener al menos 6 caracteres'); return; }
    this.wizardStep.set(3);
  }

  saveStep3(): void {
    const f = this.trabForm();
    if (!f.sucursalId) { this.toast.err('Selecciona una sede'); return; }
    this.wizardStep.set(4);
  }

  // EDICIÓN: guardado por sección
  toggleSection(s: string): void {
    this.editSection.update(cur => cur === s ? null : s);
  }

  getTurnoName(turnoId: number): string {
    return this.turnos().find(t => t.id === turnoId)?.nombreCodigo ?? '—';
  }

  savePersonaEdit(): void {
    const f = this.personaForm();
    if (!f.dni || !f.apellidosNombres) { this.toast.err('DNI y nombre son obligatorios'); return; }
    this.rrhhService.updatePersona(this.savedPersonaId()!, f).subscribe({
      next: () => { this.toast.ok('Datos personales actualizados'); this.editSection.set(null); this.loadTrabajadores(); },
      error: () => this.toast.err('Error al actualizar persona')
    });
  }

  saveTrabajadorEdit(): void {
    const f = this.trabForm();
    if (!f.sucursalId) { this.toast.err('Selecciona una sede'); return; }
    this.rrhhService.updateTrabajador(this.savedTrabajadorId()!, { personaId: this.savedPersonaId()!, sucursalId: f.sucursalId, idEstado: f.idEstado }).subscribe({
      next: () => { this.toast.ok('Datos laborales actualizados'); this.editSection.set(null); this.loadTrabajadores(); },
      error: () => this.toast.err('Error al actualizar')
    });
  }

  saveTurnoEdit(): void {
    const f = this.turnoForm();
    if (!f.turnoId) { this.toast.err('Selecciona un turno'); return; }
    if (!this.turnoEsRotativo(f.turnoId) && !f.horarioTurnoId) { this.toast.err('Para turnos fijos se requiere un horario'); return; }
    this.rrhhService.asignarTurno(this.savedTrabajadorId()!, {
      turnoId: f.turnoId,
      horarioTurnoId: f.horarioTurnoId ?? null,
      fechaInicioVigencia: f.fechaInicioVigencia,
      motivoCambio: f.motivoCambio || undefined
    }).subscribe({
      next: () => { this.toast.ok('Turno asignado'); this.editSection.set(null); this.loadTrabajadores(); },
      error: err => this.toast.errHttp(err, 'Error al asignar turno')
    });
  }

  // STEP 4: new=Turno+Submit | edit=Sedes (handled in template)
  saveStep4New(): void {
    const tf = this.turnoForm();
    const esRot = tf.turnoId ? this.turnoEsRotativo(tf.turnoId) : true;
    if (tf.turnoId && !esRot && !tf.horarioTurnoId) { this.toast.err('Para turnos fijos se requiere un horario'); return; }

    const pf = this.personaForm();
    const uf = this.usuarioForm();
    const wf = this.trabForm();

    const payload: CrearTrabajadorCompletoDto = {
      dni: pf.dni,
      apellidosNombres: pf.apellidosNombres,
      email: pf.email || undefined,
      telefono: pf.telefono || undefined,
      username: uf.username,
      password: uf.password,
      role: uf.role,
      sucursalId: wf.sucursalId || undefined,
      cargo: wf.cargo || undefined,
      areaDepartamento: wf.areaDepartamento || undefined,
      jefeInmediatoId: wf.jefeInmediatoId ?? undefined,
      marcajeEnZona: wf.marcajeEnZona,
      tomarFoto: wf.tomarFoto,
      fechaIngreso: wf.fechaIngreso || undefined,
      turnoId: tf.turnoId || undefined,
      horarioTurnoId: tf.horarioTurnoId ?? undefined,
      fechaInicioVigencia: tf.turnoId ? tf.fechaInicioVigencia : undefined
    };

    this.loading.set(true);
    this.rrhhService.crearTrabajadorCompleto(payload).subscribe({
      next: () => {
        this.toast.ok('Trabajador creado correctamente');
        this.loading.set(false);
        this.closeDrawer();
        this.loadTrabajadores();
      },
      error: err => {
        this.toast.errHttp(err, 'Error al crear trabajador');
        this.loading.set(false);
      }
    });
  }

  darDeBaja(id: number): void {
    if (!confirm('¿Confirmas dar de baja a este trabajador?')) return;
    this.rrhhService.darDeBaja(id).subscribe({
      next: () => { this.toast.ok('Dado de baja'); this.loadTrabajadores(); },
      error: () => this.toast.err('Error')
    });
  }

  reactivar(id: number): void {
    this.rrhhService.reactivar(id).subscribe({
      next: () => { this.toast.ok('Reactivado'); this.loadTrabajadores(); },
      error: () => this.toast.err('Error')
    });
  }
}
