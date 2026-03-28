import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RrhhService } from '../../core/services/rrhh.service';

interface ReporteRow {
  [key: string]: any;
}

const COL_LABELS: Record<string, string> = {
  dNI:                         'DNI',
  nombre:                      'Nombre',
  cargo:                       'Cargo',
  area:                        'Área',
  dias_Programados_Laborables: 'Prog.',
  dias_Trabajados:             'Trab.',
  dias_Descanso_Feriados:      'Desc/Fer.',
  inasistencias:               'Inasist.',
  dias_Tardanza:               'C/Tard.',
  tiempo_Tardanza:             'Tiempo Tard.',
  faltas:                      'Faltas',
  horas_Extra:                 'H.Extra',
  justificados:                'Justif.',
};

const VISIBLE_COLS = Object.keys(COL_LABELS);

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <ng-container>
      <!-- TOOLBAR -->
      <div class="toolbar">
        <!-- Rango de fechas -->
        <div class="tb-group">
          <span class="fld-lbl">Desde:</span>
          <input type="date" class="inp" style="max-width:150px"
            [ngModel]="fechaInicio()" (ngModelChange)="fechaInicio.set($event)" />
          <span class="fld-lbl">Hasta:</span>
          <input type="date" class="inp" style="max-width:150px"
            [ngModel]="fechaFin()" (ngModelChange)="fechaFin.set($event)" />
        </div>

        <!-- Filtros de búsqueda -->
        <div class="tb-group" style="flex:1;min-width:0">
          <div class="srch-wrap">
            <span class="srch-ico">⌕</span>
            <input class="srch" placeholder="Nombre o DNI..."
              [ngModel]="busqueda()" (ngModelChange)="busqueda.set($event)" />
          </div>
          <input class="inp" style="flex:1 1 140px;max-width:180px" placeholder="Área / depto..."
            [ngModel]="areaFiltro()" (ngModelChange)="areaFiltro.set($event)" />
        </div>

        <!-- Acciones -->
        <div class="tb-group">
          <button class="btn btn-primary btn-sm" (click)="generar()">Buscar</button>
          @if (loading()) { <span class="spin"></span> }
          @if (filasFiltradas().length) {
            <button class="btn btn-sm" (click)="exportarCSV()">↓ CSV</button>
          }
        </div>
      </div>

      <!-- STATS -->
      @if (rows().length) {
        <div class="stats-row">
          <div class="stat">
            <div class="stat-lbl">TRABAJADORES</div>
            <div class="stat-val">{{ filasFiltradas().length }}</div>
            <div class="stat-sub">en vista</div>
          </div>
          <div class="stat">
            <div class="stat-lbl">TOTAL FALTAS</div>
            <div class="stat-val" style="color:var(--red)">{{ totalFaltas() }}</div>
            <div class="stat-sub">acumuladas</div>
          </div>
          <div class="stat">
            <div class="stat-lbl">CON TARDANZA</div>
            <div class="stat-val" style="color:var(--amb)">{{ totalConTardanza() }}</div>
            <div class="stat-sub">trabajadores</div>
          </div>
          <div class="stat">
            <div class="stat-lbl">PÁGINA</div>
            <div class="stat-val">{{ pagina() + 1 }} / {{ totalPaginas() }}</div>
            <div class="stat-sub">de {{ filasFiltradas().length }} registros</div>
          </div>
        </div>
      }

      <!-- TABLE -->
      <div class="grid-outer">
        @if (!rows().length && !loading()) {
          <div style="padding:40px;text-align:center;color:var(--txt3)">
            Configura el rango de fechas y presiona "Generar".
          </div>
        } @else if (rows().length) {
          <div class="tbl-wrap">
          <table class="tbl">
            <thead>
              <tr>
                @for (col of visibleCols; track col) {
                  <th>{{ colLabel(col) }}</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (row of paginaActual(); track $index) {
                <tr>
                  @for (col of visibleCols; track col) {
                    <td style="font-size:11px" [style.color]="cellColor(col, row[col])">
                      {{ row[col] ?? '—' }}
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
          </div>

          <!-- PAGINACION -->
          @if (totalPaginas() > 1) {
            <div class="pag-bar">
              <button class="btn btn-sm" [disabled]="pagina() === 0" (click)="pagina.set(pagina() - 1)">‹ Ant.</button>
              <span style="font-size:12px;color:var(--txt2)">
                Página {{ pagina() + 1 }} de {{ totalPaginas() }}
                · mostrando {{ paginaActual().length }} de {{ filasFiltradas().length }}
              </span>
              <button class="btn btn-sm" [disabled]="pagina() === totalPaginas() - 1" (click)="pagina.set(pagina() + 1)">Sig. ›</button>
            </div>
          }
        }
      </div>
    </ng-container>
  `,
  styles: [`
    :host { display: flex; flex-direction: column; flex: 1; min-height: 0; overflow: hidden; }
    .pag-bar {
      display: flex; align-items: center; justify-content: center; gap: 12px;
      padding: 10px 16px; border-top: 1px solid var(--brd); flex-shrink: 0;
      background: var(--surf);
    }
  `]
})
export class ReportesComponent {
  private rrhhService = inject(RrhhService);

  rows     = signal<ReporteRow[]>([]);
  loading  = signal(false);
  pagina   = signal(0);

  fechaInicio = signal(new Date(Date.now() - 7 * 86400000).toISOString().split('T')[0]);
  fechaFin    = signal(new Date().toISOString().split('T')[0]);
  busqueda    = signal('');
  areaFiltro  = signal('');

  readonly PAGE_SIZE = 50;
  readonly visibleCols = VISIBLE_COLS;

  filasFiltradas = computed(() => {
    const q = this.busqueda().toLowerCase();
    const a = this.areaFiltro().toLowerCase();
    return this.rows().filter(r => {
      if (q && !(
        (r['nombre'] ?? '').toLowerCase().includes(q) ||
        (r['dNI']    ?? '').toLowerCase().includes(q)
      )) return false;
      if (a && !(r['area'] ?? '').toLowerCase().includes(a)) return false;
      return true;
    });
  });

  totalPaginas = computed(() => Math.max(1, Math.ceil(this.filasFiltradas().length / this.PAGE_SIZE)));

  paginaActual = computed(() => {
    const start = this.pagina() * this.PAGE_SIZE;
    return this.filasFiltradas().slice(start, start + this.PAGE_SIZE);
  });

  totalFaltas      = computed(() => this.filasFiltradas().reduce((s, r) => s + (r['faltas'] ?? 0), 0));
  totalConTardanza = computed(() => this.filasFiltradas().filter(r => (r['dias_Tardanza'] ?? 0) > 0).length);

  colLabel(col: string): string { return COL_LABELS[col] ?? col; }

  cellColor(col: string, val: any): string {
    if ((col === 'faltas' || col === 'inasistencias') && Number(val) > 0) return 'var(--red)';
    if (col === 'dias_Tardanza' && Number(val) > 0) return 'var(--amb)';
    if (col === 'dias_Trabajados' && Number(val) > 0) return 'var(--grn)';
    return '';
  }

  generar(): void {
    this.loading.set(true);
    this.pagina.set(0);
    // El backend filtra por `area` (string) — no por sucursalId
    // ADMIN: el backend fuerza el area del token; SUPERADMIN puede pasar area
    this.rrhhService.getReporteAsistencia({
      fechaInicio: this.fechaInicio(),
      fechaFin:    this.fechaFin(),
      area:        this.areaFiltro() || undefined
    }).subscribe({
      next: data => { this.rows.set(data); this.loading.set(false); },
      error: ()   => { this.rows.set([]);  this.loading.set(false); }
    });
  }

  exportarCSV(): void {
    const filas = this.filasFiltradas();
    if (!filas.length) return;
    const headers = this.visibleCols.map(c => this.colLabel(c));
    const rowsCsv = filas.map(r =>
      this.visibleCols.map(c => `"${(r[c] ?? '').toString().replace(/"/g, '""')}"`)
        .join(',')
    );
    const csv = [headers.join(','), ...rowsCsv].join('\n');
    const a = document.createElement('a');
    a.href = 'data:text/csv;charset=utf-8,\uFEFF' + encodeURIComponent(csv);
    a.download = `reporte_asistencia_${this.fechaInicio()}_${this.fechaFin()}.csv`;
    a.click();
  }
}
