import { Component, HostBinding, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar.component';
import { TopbarComponent } from './topbar.component';
import { ToastComponent } from '../shared/components/toast/toast.component';
import { ThemeService } from '../core/services/theme.service';
import { RrhhService } from '../core/services/rrhh.service';
import { SucursalCentro, HorarioTurno, Turno, TipoTurno, TrabajadorResponseDto, TrabajadorMapped } from '../core/models/rrhh.models';

export interface AppState {
  sedes: SucursalCentro[];
  horariosTurno: HorarioTurno[];
  tiposTurno: TipoTurno[];
  allTurnos: Turno[];
  allTrabajadores: TrabajadorMapped[];
  htPorTurno: Record<number, HorarioTurno[]>;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent, ToastComponent],
  template: `
    <app-sidebar [class.open]="sidebarOpen()" [state]="state()" (navClick)="closeSidebarOnMobile()" />
    <app-topbar  [state]="state()" (toggleSidebar)="sidebarOpen.set(!sidebarOpen())" />
    <main class="shell-main">
      <router-outlet />
    </main>
    <div class="mob-overlay" [class.active]="sidebarOpen()" (click)="sidebarOpen.set(false)"></div>
    <app-toast />
  `,
  styles: [`
    /* ── Layout base ───────────────────────────────────── */
    :host {
      display: grid;
      grid-template-areas: "sidebar topbar" "sidebar main";
      grid-template-columns: 0px 1fr;
      grid-template-rows: 54px 1fr;
      height: 100vh;
      overflow: hidden;
      transition: grid-template-columns .25s cubic-bezier(.4,0,.2,1);
    }
    :host.sidebar-open {
      grid-template-columns: 222px 1fr;
    }

    /* ── Sidebar (desktop: parte del grid) ─────────────── */
    app-sidebar {
      grid-area: sidebar;
      overflow: hidden;
    }

    app-topbar  { grid-area: topbar; }

    .shell-main {
      grid-area: main;
      overflow: hidden;
      display: flex;
      flex-direction: column;
      min-height: 0;
    }

    /* ── Overlay (solo mobile) ─────────────────────────── */
    .mob-overlay {
      display: none;
      position: fixed; inset: 0;
      background: rgba(0,0,0,.45);
      z-index: 499;
      pointer-events: none;
      opacity: 0;
      transition: opacity .2s ease;
    }

    /* ── Mobile (≤768px): sidebar = overlay flotante ────── */
    @media (max-width: 768px) {
      :host,
      :host.sidebar-open {
        grid-template-columns: 0px 1fr;
      }
      app-sidebar {
        position: fixed; left: 0; top: 0; bottom: 0;
        width: 222px; z-index: 500;
        transform: translateX(-100%);
        transition: transform .25s cubic-bezier(.4,0,.2,1);
        box-shadow: 4px 0 32px rgba(0,0,0,.3);
        overflow-y: auto;
      }
      app-sidebar.open {
        transform: translateX(0);
      }
      .mob-overlay { display: block; }
      .mob-overlay.active {
        pointer-events: auto;
        opacity: 1;
      }
    }
  `]
})
export class ShellComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  themeService = inject(ThemeService);

  /** Empieza abierto en desktop, cerrado en mobile */
  sidebarOpen = signal(window.innerWidth >= 768);

  @HostBinding('class.sidebar-open') get hostSidebarOpen() { return this.sidebarOpen(); }

  state = signal<AppState>({
    sedes: [],
    horariosTurno: [],
    tiposTurno: [],
    allTurnos: [],
    allTrabajadores: [],
    htPorTurno: {}
  });

  ngOnInit(): void {
    this.loadMasterData();
  }

  /** En mobile cierra el sidebar al navegar; en desktop lo deja abierto */
  closeSidebarOnMobile(): void {
    if (window.innerWidth < 768) this.sidebarOpen.set(false);
  }

  private loadMasterData(): void {
    this.rrhhService.getSucursales().subscribe(sedes =>
      this.state.update(s => ({ ...s, sedes }))
    );

    this.rrhhService.getHorariosTurno().subscribe(horariosTurno => {
      const htPorTurno: Record<number, HorarioTurno[]> = {};
      horariosTurno.forEach(h => {
        if (!htPorTurno[h.turnoId]) htPorTurno[h.turnoId] = [];
        htPorTurno[h.turnoId].push(h);
      });
      this.state.update(s => ({ ...s, horariosTurno, htPorTurno }));
    });

    this.rrhhService.getTiposTurno().subscribe(tiposTurno =>
      this.state.update(s => ({ ...s, tiposTurno }))
    );

    this.rrhhService.getTurnos().subscribe(allTurnos =>
      this.state.update(s => ({ ...s, allTurnos }))
    );

    this.loadAllWorkers();
  }

  private loadAllWorkers(): void {
    this.rrhhService.getTrabajadores(1, 200).subscribe(res => {
      const mapped = (res.items ?? []).map(w => this.mapWorker(w));
      this.state.update(s => ({ ...s, allTrabajadores: mapped }));
    });
  }

  private mapWorker(w: TrabajadorResponseDto): TrabajadorMapped {
    const tipoU = (w.tipoTurno ?? '').toUpperCase();
    const tipo: 'ROT' | 'FIJ' = tipoU.includes('ROT') ? 'ROT' : 'FIJ';
    return {
      id: w.id, name: w.apellidosNombres, dni: w.dni,
      sucursalId: w.sucursalId, idEstado: w.idEstado, tipo,
      idTurno: w.idTurno, idHorarioTurno: w.idHorarioTurno,
      horarioTurnoNombre: w.horarioTurnoNombre
    };
  }
}
