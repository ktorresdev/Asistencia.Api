import { Component, inject, Input, Output, EventEmitter } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../core/services/auth.service';
import { AppState } from './shell.component';

interface NavItem {
  label: string;
  route: string;
  dot: string;
  adminOnly?: boolean;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <div class="logo-zone">
      <img class="logo-img logo-light" src="assets/img/logofondoclaro.ico" alt="Energigas" />
      <img class="logo-img logo-dark"  src="assets/img/logofondooscuro.ico" alt="Energigas" />
      <div class="logo-sub">Control de Asistencia</div>
    </div>

    <nav class="nav">
      <div class="nav-sec">Operaciones</div>
      @if (auth.isSupervisor()) {
        @for (item of supervisorNav; track item.route) {
          <a class="nav-item" [routerLink]="item.route" routerLinkActive="active" (click)="navClick.emit()">
            <span class="nav-dot" [style.background]="item.dot"></span>
            {{ item.label }}
          </a>
        }
      } @else {
        @for (item of mainNav; track item.route) {
          @if (!item.adminOnly || auth.isAdminOrSupervisor()) {
            <a class="nav-item" [routerLink]="item.route" routerLinkActive="active" (click)="navClick.emit()">
              <span class="nav-dot" [style.background]="item.dot"></span>
              {{ item.label }}
            </a>
          }
        }
        @if (auth.isSuperAdmin()) {
          <div class="nav-sec">Maestros</div>
          @for (item of maestrosNav; track item.route) {
            <a class="nav-item" [routerLink]="item.route" routerLinkActive="active" (click)="navClick.emit()">
              <span class="nav-dot" [style.background]="item.dot"></span>
              {{ item.label }}
            </a>
          }
        }
      }
    </nav>

    <div class="sidebar-btm">
      <div class="user-pill">
        <div class="avatar">{{ initials }}</div>
        <div style="flex:1;min-width:0">
          <div style="font-size:12px;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">
            {{ auth.currentUser()?.username }}
          </div>
          <div style="font-size:10px;color:var(--txt3);font-family:var(--mono)">
            {{ auth.currentUser()?.role }}
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      background: var(--surf);
      border-right: 1px solid var(--brd);
      overflow: hidden;
      min-width: 0;
    }
    .logo-zone {
      padding: 15px 18px;
      border-bottom: 1px solid var(--brd);
      flex-shrink: 0;
      animation: fadeUp .25s ease both;
    }
    .logo-img {
      height: 36px; width: auto; max-width: 160px; display: block;
    }
    .logo-light { display: none; }
    .logo-dark  { display: block; }
    :host-context([data-theme="light"]) .logo-light { display: block; }
    :host-context([data-theme="light"]) .logo-dark  { display: none; }
    .logo-sub { font-size: 10px; color: var(--txt3); font-family: var(--mono); margin-top: 4px; }
    .nav { flex: 1; overflow-y: auto; padding: 8px; }
    .nav-sec {
      font-size: 9px; color: var(--txt3); font-family: var(--mono);
      letter-spacing: .14em; text-transform: uppercase; padding: 12px 10px 4px;
    }
    .nav-item {
      display: flex; align-items: center; gap: 8px; padding: 7px 10px;
      border-radius: 8px; cursor: pointer; font-size: 13px; font-weight: 500;
      color: var(--txt2); transition: all .18s cubic-bezier(.4,0,.2,1); border: 1px solid transparent;
      margin-bottom: 1px; user-select: none; text-decoration: none;
      animation: navItemIn .28s ease both;
      position: relative; overflow: hidden;
    }
    .nav-item::before {
      content: '';
      position: absolute; left: 0; top: 50%; transform: translateY(-50%);
      width: 3px; height: 0; background: var(--acc); border-radius: 0 3px 3px 0;
      transition: height .2s cubic-bezier(.4,0,.2,1);
    }
    .nav-item:hover { background: var(--surf2); color: var(--txt); transform: translateX(2px); }
    .nav-item.active {
      background: var(--acc-bg); color: var(--acc);
      border-color: rgba(79,142,247,.2);
      transform: translateX(2px);
    }
    .nav-item.active::before { height: 60%; }
    .nav-item:nth-child(1) { animation-delay: .04s; }
    .nav-item:nth-child(2) { animation-delay: .08s; }
    .nav-item:nth-child(3) { animation-delay: .12s; }
    .nav-item:nth-child(4) { animation-delay: .15s; }
    .nav-item:nth-child(5) { animation-delay: .17s; }
    .nav-item:nth-child(6) { animation-delay: .19s; }
    .nav-item:nth-child(7) { animation-delay: .20s; }
    .nav-item:nth-child(8) { animation-delay: .21s; }
    @keyframes navItemIn {
      from { opacity: 0; transform: translateX(-10px); }
      to   { opacity: 1; transform: translateX(0); }
    }
    .nav-dot {
      width: 7px; height: 7px; border-radius: 50%; flex-shrink: 0;
      transition: transform .2s cubic-bezier(.34,1.56,.64,1);
    }
    .nav-item.active .nav-dot { transform: scale(1.3); }
    .nav-item:hover .nav-dot { transform: scale(1.15); }
    .sidebar-btm { padding: 10px; border-top: 1px solid var(--brd); flex-shrink: 0; }
    .user-pill {
      display: flex; align-items: center; gap: 8px; padding: 8px 10px;
      background: var(--surf2); border-radius: 8px; border: 1px solid var(--brd);
      transition: all .18s;
    }
    .user-pill:hover { border-color: var(--brd2); background: var(--surf3); }
    .avatar {
      width: 28px; height: 28px; border-radius: 50%; background: var(--acc-bg);
      color: var(--acc); font-size: 11px; font-weight: 600;
      display: flex; align-items: center; justify-content: center; flex-shrink: 0;
      transition: transform .2s cubic-bezier(.34,1.56,.64,1);
    }
    .user-pill:hover .avatar { transform: scale(1.1); }
  `]
})
export class SidebarComponent {
  @Input()  state!: AppState;
  @Output() navClick = new EventEmitter<void>();
  auth = inject(AuthService);

  mainNav: NavItem[] = [
    { label: 'Dashboard',          route: '/dashboard',      dot: 'var(--acc)' },
    { label: 'Horario Semanal',    route: '/horario',        dot: 'var(--acc)' },
    { label: 'Programación (PTS)', route: '/programacion',   dot: 'var(--grn)', adminOnly: true },
    { label: 'Coberturas',         route: '/coberturas',     dot: 'var(--pur)', adminOnly: true },
    { label: 'Ausencias',          route: '/ausencias',      dot: 'var(--red)', adminOnly: true },
    { label: 'Trabajadores',       route: '/trabajadores',   dot: 'var(--tea)', adminOnly: true },
    { label: 'Resumen del Día',    route: '/resumen',        dot: 'var(--amb)', adminOnly: true },
    { label: 'Notificaciones',     route: '/notificaciones', dot: 'var(--red)' },
    { label: 'Asistencias',        route: '/tardanzas',      dot: 'var(--tea)', adminOnly: true },
    { label: 'Reportes',           route: '/reportes',       dot: 'var(--grn)', adminOnly: true },
  ];

  // SUPERVISOR: subconjunto de vistas disponibles (sus trabajadores + horario + reportes)
  supervisorNav: NavItem[] = [
    { label: 'Dashboard',          route: '/dashboard',      dot: 'var(--acc)' },
    { label: 'Horario Semanal',    route: '/horario',        dot: 'var(--acc)' },
    { label: 'Programación (PTS)', route: '/programacion',   dot: 'var(--grn)' },
    { label: 'Ausencias',          route: '/ausencias',      dot: 'var(--red)' },
    { label: 'Trabajadores',       route: '/trabajadores',   dot: 'var(--tea)' },
    { label: 'Asistencias',        route: '/tardanzas',      dot: 'var(--tea)' },
    { label: 'Reportes',           route: '/reportes',       dot: 'var(--grn)' },
    { label: 'Notificaciones',     route: '/notificaciones', dot: 'var(--red)' },
  ];

  maestrosNav: NavItem[] = [
    { label: 'Sucursales',    route: '/maestros/sucursales',     dot: 'var(--acc)' },
    { label: 'Tipos de Turno', route: '/maestros/tipo-turno',   dot: 'var(--grn)' },
    { label: 'Turnos',         route: '/maestros/turnos',        dot: 'var(--pur)' },
    { label: 'Horarios',       route: '/maestros/horarios-turno', dot: 'var(--amb)' },
  ];

  get initials(): string {
    return (this.auth.currentUser()?.username ?? '').slice(0, 2).toUpperCase();
  }
}