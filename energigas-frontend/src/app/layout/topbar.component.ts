import { Component, computed, EventEmitter, inject, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs/operators';
import { AuthService } from '../core/services/auth.service';
import { ThemeService } from '../core/services/theme.service';
import { WeekService } from '../core/services/week.service';
import { AppState } from './shell.component';

/** Rutas que usan el navegador de semana */
const WEEK_ROUTES = ['/horario', '/programacion'];

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button class="burger" (click)="toggleSidebar.emit()" title="Menú">
      <span></span><span></span><span></span>
    </button>

    <span class="topbar-gap"></span>

    @if (showWeekNav()) {
      <div class="wk-nav">
        <button class="wk-btn" (click)="week.prev()">&#8249;</button>
        <span class="wk-lbl">{{ week.weekLabel() }}</span>
        <button class="wk-btn" (click)="week.next()">&#8250;</button>
      </div>
    }

    <button class="btn-icon" (click)="theme.toggle()" title="Cambiar tema">
      {{ theme.theme() === 'dark' ? '☀' : '🌙' }}
    </button>

    <button class="logout-btn" (click)="auth.logout()">Salir</button>
  `,
  styles: [`
    :host {
      display: flex;
      align-items: center;
      padding: 0 20px;
      gap: 12px;
      background: var(--surf);
      border-bottom: 1px solid var(--brd);
      animation: fadeUp .2s ease both;
    }
    .topbar-gap { flex: 1; }
    .burger {
      display: flex;
      flex-direction: column;
      justify-content: center;
      gap: 5px;
      width: 34px; height: 34px;
      padding: 6px;
      border: 1px solid var(--brd);
      border-radius: 8px;
      background: transparent;
      cursor: pointer;
      flex-shrink: 0;
    }
    .burger:hover { background: var(--surf2); border-color: var(--brd2); }
    .burger span {
      display: block;
      height: 2px;
      background: var(--txt);
      border-radius: 2px;
      transition: background .15s;
    }
    .burger:hover span { background: var(--acc); }

    .wk-nav { flex-shrink: 1; min-width: 0; }
    .wk-lbl { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

    @media (max-width: 520px) {
      :host { padding: 0 8px; gap: 5px; }
      .wk-lbl { font-size: 9px; min-width: 0; max-width: 110px; }
      .wk-btn { width: 20px; height: 20px; font-size: 13px; }
    }
  `]
})
export class TopbarComponent {
  @Input()  state!: AppState;
  @Output() toggleSidebar = new EventEmitter<void>();

  auth   = inject(AuthService);
  theme  = inject(ThemeService);
  week   = inject(WeekService);
  private router = inject(Router);

  private currentUrl = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map((e: any) => e.urlAfterRedirects as string),
      startWith(this.router.url)
    ),
    { initialValue: this.router.url }
  );

  showWeekNav = computed(() =>
    WEEK_ROUTES.some(r => this.currentUrl().startsWith(r))
  );
}
