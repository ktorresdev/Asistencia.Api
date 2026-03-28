import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { RrhhService } from '../../core/services/rrhh.service';
import { DashboardResumen } from '../../core/models/rrhh.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
<div class="dash-wrap">

  <!-- HEADER -->
  <div class="dash-hdr">
    <div>
      <div class="dash-title">Panel de control</div>
      <div class="dash-sub">{{ fechaLabel() }}</div>
    </div>
    <button class="btn btn-primary btn-sm" (click)="load()" [disabled]="loading()">
      @if (loading()) { <span class="spin" style="width:14px;height:14px;border-width:2px"></span> }
      @else { Actualizar }
    </button>
  </div>

  @if (error()) {
    <div class="dash-err">{{ error() }}</div>
  }

  @if (!loading() && data()) {

    <!-- ROW 1 -->
    <div class="dash-section-lbl">Asistencia de hoy</div>
    <div class="kpi-row">

      <a class="kpi-card" routerLink="/resumen" style="--c:#34d399;--cb:rgba(52,211,153,.12)">
        <div class="kpi-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20 6L9 17l-5-5"/>
          </svg>
        </div>
        <div class="kpi-body">
          <div class="kpi-val">{{ data()!.presenteHoy }}</div>
          <div class="kpi-lbl">Presentes</div>
        </div>
        <div class="kpi-link">Ver resumen →</div>
      </a>

      <a class="kpi-card" routerLink="/tardanzas" style="--c:#f59e0b;--cb:rgba(245,158,11,.12)">
        <div class="kpi-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <polyline points="12 6 12 12 16 14"/>
          </svg>
        </div>
        <div class="kpi-body">
          <div class="kpi-val">{{ data()!.tardanzaHoy }}</div>
          <div class="kpi-lbl">Tardanzas</div>
        </div>
        <div class="kpi-link">Ver asistencia →</div>
      </a>

      <a class="kpi-card" routerLink="/resumen" style="--c:#f87171;--cb:rgba(248,113,113,.12)">
        <div class="kpi-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <line x1="15" y1="9" x2="9" y2="15"/>
            <line x1="9" y1="9" x2="15" y2="15"/>
          </svg>
        </div>
        <div class="kpi-body">
          <div class="kpi-val">{{ data()!.faltaHoy }}</div>
          <div class="kpi-lbl">Faltas</div>
        </div>
        <div class="kpi-link">Ver resumen →</div>
      </a>

      <a class="kpi-card" routerLink="/reportes" style="--c:#4f8ef7;--cb:rgba(79,142,247,.12)">
        <div class="kpi-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="18" y1="20" x2="18" y2="10"/>
            <line x1="12" y1="20" x2="12" y2="4"/>
            <line x1="6"  y1="20" x2="6"  y2="14"/>
          </svg>
        </div>
        <div class="kpi-body">
          <div class="kpi-val">{{ data()!.porcentajeAsistencia }}%</div>
          <div class="kpi-lbl">% Asistencia</div>
        </div>
        <div class="kpi-bar-wrap">
          <div class="kpi-bar" [style.width.%]="data()!.porcentajeAsistencia"></div>
        </div>
        <div class="kpi-link">Ver reportes →</div>
      </a>

    </div>

    <!-- ROW 2 -->
    <div class="dash-section-lbl">Esta semana · {{ semanaLabel() }}</div>
    <div class="kpi-row">

      <a class="kpi-card" routerLink="/trabajadores" style="--c:#38bdf8;--cb:rgba(56,189,248,.12)">
        <div class="kpi-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
            <circle cx="9" cy="7" r="4"/>
            <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
            <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
          </svg>
        </div>
        <div class="kpi-body">
          <div class="kpi-val">{{ data()!.totalTrabajadores }}</div>
          <div class="kpi-lbl">Trabajadores activos</div>
        </div>
        <div class="kpi-link">Ver trabajadores →</div>
      </a>

      <a class="kpi-card" routerLink="/programacion"
         [style]="data()!.sinProgramacion > 0
           ? '--c:#f87171;--cb:rgba(248,113,113,.12)'
           : '--c:#34d399;--cb:rgba(52,211,153,.12)'">
        <div class="kpi-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
            <line x1="16" y1="2" x2="16" y2="6"/>
            <line x1="8"  y1="2" x2="8"  y2="6"/>
            <line x1="3"  y1="10" x2="21" y2="10"/>
          </svg>
        </div>
        <div class="kpi-body">
          <div class="kpi-val">{{ data()!.sinProgramacion }}</div>
          <div class="kpi-lbl">Sin programación</div>
          <div class="kpi-sub">
            @if (data()!.sinProgramacion > 0) { requieren atención } @else { al día }
          </div>
        </div>
        <div class="kpi-link">Ver programación →</div>
      </a>

      <a class="kpi-card" routerLink="/ausencias" style="--c:#a78bfa;--cb:rgba(167,139,250,.12)">
        <div class="kpi-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
            <line x1="16" y1="2" x2="16" y2="6"/>
            <line x1="8"  y1="2" x2="8"  y2="6"/>
            <line x1="3"  y1="10" x2="21" y2="10"/>
            <line x1="9"  y1="16" x2="15" y2="16"/>
          </svg>
        </div>
        <div class="kpi-body">
          <div class="kpi-val">{{ data()!.ausenciasSemana }}</div>
          <div class="kpi-lbl">Ausencias</div>
          <div class="kpi-sub">vacaciones / descansos</div>
        </div>
        <div class="kpi-link">Ver ausencias →</div>
      </a>

      <a class="kpi-card" routerLink="/coberturas"
         [style]="data()!.coberturasPendientes > 0
           ? '--c:#f59e0b;--cb:rgba(245,158,11,.12)'
           : '--c:#34d399;--cb:rgba(52,211,153,.12)'">
        <div class="kpi-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
          </svg>
        </div>
        <div class="kpi-body">
          <div class="kpi-val">{{ data()!.coberturasPendientes }}</div>
          <div class="kpi-lbl">Coberturas pendientes</div>
          <div class="kpi-sub">
            @if (data()!.coberturasPendientes > 0) { pendientes de aprobar } @else { sin pendientes }
          </div>
        </div>
        <div class="kpi-link">Ver coberturas →</div>
      </a>

    </div>

  } @else if (loading()) {
    <div class="dash-loading">
      <span class="spin"></span>
      <span style="color:var(--txt3)">Cargando indicadores...</span>
    </div>
  }

</div>
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      flex: 1;
      min-height: 0;
      overflow-y: auto;
      animation: fadeUp .22s ease both;
    }
    .dash-wrap {
      padding: 20px 24px;
      display: flex;
      flex-direction: column;
      gap: 14px;
      width: 100%;
      box-sizing: border-box;
    }
    .dash-hdr {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 4px;
    }
    .dash-title { font-size: 18px; font-weight: 700; }
    .dash-sub   { font-size: 11px; color: var(--txt3); font-family: var(--mono); margin-top: 2px; }
    .dash-err   { background: var(--red-bg); color: var(--red); border-radius: 8px; padding: 10px 14px; font-size: 13px; }
    .dash-section-lbl {
      font-size: 9px; font-family: var(--mono); color: var(--txt3);
      letter-spacing: .12em; text-transform: uppercase; padding: 4px 0 0;
    }
    .dash-loading {
      display: flex; align-items: center; gap: 12px;
      padding: 40px; color: var(--txt3);
    }

    /* ── KPI CARDS ── */
    .kpi-row {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 12px;
    }
    @media (max-width: 900px) { .kpi-row { grid-template-columns: repeat(2, 1fr); } }
    @media (max-width: 480px) { .kpi-row { grid-template-columns: 1fr 1fr; } }

    .kpi-card {
      background: var(--surf);
      border: 1px solid var(--brd);
      border-radius: 10px;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 10px;
      position: relative;
      transition: transform .15s ease, box-shadow .15s ease;
      text-decoration: none;
      color: inherit;
      cursor: pointer;
    }
    .kpi-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 20px rgba(0,0,0,.1);
      border-color: var(--c, var(--brd));
    }
    .kpi-card:hover .kpi-link { opacity: 1; }
    .kpi-link {
      font-size: 10px;
      color: var(--c, var(--acc));
      font-family: var(--mono);
      opacity: 0;
      transition: opacity .15s ease;
      margin-top: auto;
    }

    .kpi-icon {
      width: 36px; height: 36px;
      border-radius: 8px;
      background: var(--cb, var(--surf2));
      color: var(--c, var(--txt3));
      display: flex; align-items: center; justify-content: center;
      flex-shrink: 0;
    }
    .kpi-icon svg { width: 18px; height: 18px; }

    .kpi-body  { display: flex; flex-direction: column; gap: 2px; }
    .kpi-val   { font-size: 30px; font-weight: 700; line-height: 1; font-family: var(--mono); color: var(--c, var(--txt)); }
    .kpi-lbl   { font-size: 12px; color: var(--txt2); font-weight: 500; margin-top: 2px; }
    .kpi-sub   { font-size: 10px; color: var(--txt3); margin-top: 1px; }

    .kpi-bar-wrap { height: 3px; background: var(--brd); border-radius: 2px; }
    .kpi-bar      { height: 3px; background: var(--c, var(--acc)); border-radius: 2px; transition: width .8s cubic-bezier(.4,0,.2,1); }

  `]
})
export class DashboardComponent implements OnInit {
  private readonly svc = inject(RrhhService);

  data    = signal<DashboardResumen | null>(null);
  loading = signal(false);
  error   = signal('');

  readonly today = new Date();

  fechaLabel = computed(() => {
    const d = this.data()?.fechaConsulta ?? this.today.toISOString().slice(0, 10);
    return `Hoy · ${d.split('-').reverse().join('/')}`;
  });

  semanaLabel = computed(() => {
    if (!this.data()) return '';
    const ini = this.data()!.inicioSemana.split('-').reverse().join('/');
    const fin = this.data()!.finSemana.split('-').reverse().join('/');
    return `${ini} – ${fin}`;
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set('');
    this.svc.getDashboard().subscribe({
      next:  d => { this.data.set(d); this.loading.set(false); },
      error: e => { this.error.set(e?.error?.message ?? 'Error al cargar indicadores'); this.loading.set(false); }
    });
  }
}
