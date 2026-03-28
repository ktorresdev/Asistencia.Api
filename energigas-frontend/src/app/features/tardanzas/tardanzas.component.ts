import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RrhhService } from '../../core/services/rrhh.service';
import { TardanzaReporte } from '../../core/models/rrhh.models';

type Filtro = 'ALL' | 'TARD' | 'PUNT' | 'FALTA' | 'SIN';

@Component({
  selector: 'app-tardanzas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
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
        <button class="btn btn-primary btn-sm" (click)="cargar()">Buscar</button>
        @if (loading()) { <span class="spin"></span> }
      </div>

      <!-- Búsqueda y área -->
      <div class="tb-group" style="flex:1;min-width:0">
        <div class="srch-wrap">
          <span class="srch-ico">⌕</span>
          <input class="srch" placeholder="Nombre, DNI…"
            [ngModel]="busqueda()" (ngModelChange)="busqueda.set($event); pagina.set(0)" />
        </div>
        <select class="sel" style="max-width:175px;flex:1 1 140px"
          [ngModel]="areaSeleccionada()"
          (ngModelChange)="areaSeleccionada.set($event); pagina.set(0)">
          <option value="">Todas las áreas</option>
          @for (a of areas(); track a) { <option [value]="a">{{ a }}</option> }
        </select>
      </div>

      <!-- Pills de filtro -->
      <div class="tb-pills">
        <button class="pill" [class.on]="filtro() === 'ALL'"   (click)="setFiltro('ALL')">Todos</button>
        <button class="pill" [class.on]="filtro() === 'TARD'"  (click)="setFiltro('TARD')">Tardanza</button>
        <button class="pill" [class.on]="filtro() === 'PUNT'"  (click)="setFiltro('PUNT')">Puntual</button>
        <button class="pill" [class.on]="filtro() === 'FALTA'" (click)="setFiltro('FALTA')">Falta</button>
        <button class="pill" [class.on]="filtro() === 'SIN'"   (click)="setFiltro('SIN')">Sin marcar</button>
        <button class="btn btn-sm" (click)="exportarCSV()" [disabled]="!filasFiltradas().length">↓ CSV</button>
      </div>
    </div>

    <!-- STATS -->
    <div class="stats-row">
      <div class="stat">
        <div class="stat-lbl">TOTAL</div>
        <div class="stat-val">{{ filasFiltradas().length }}</div>
        <div class="stat-sub">registros</div>
      </div>
      <div class="stat">
        <div class="stat-lbl">PUNTUALES</div>
        <div class="stat-val" style="color:var(--grn)">{{ countPuntuales() }}</div>
        <div class="stat-sub">a tiempo</div>
      </div>
      <div class="stat">
        <div class="stat-lbl">CON TARDANZA</div>
        <div class="stat-val" style="color:var(--red)">{{ countTardanza() }}</div>
        <div class="stat-sub">registros</div>
      </div>
      <div class="stat">
        <div class="stat-lbl">FALTAS</div>
        <div class="stat-val" style="color:var(--red)">{{ countFaltas() }}</div>
        <div class="stat-sub">confirmadas</div>
      </div>
    </div>

    <!-- TABLE -->
    <div class="grid-outer">
      <div class="tbl-wrap">
      <table class="tbl">
        <thead>
          <tr>
            <th>Trabajador</th>
            <th>DNI</th>
            <th>Área</th>
            <th>Fecha</th>
            <th>Hora Turno</th>
            <th>Hora Marcación</th>
            <th>Tardanza</th>
          </tr>
        </thead>
        <tbody>
          @if (loading()) {
            <tr class="load-row"><td colspan="7"><span class="spin"></span></td></tr>
          } @else if (!datos().length) {
            <tr class="load-row"><td colspan="7">Selecciona un rango y presiona "Buscar"</td></tr>
          } @else if (!filasFiltradas().length) {
            <tr class="load-row"><td colspan="7">Sin resultados para el filtro aplicado</td></tr>
          } @else {
            @for (row of paginaActual(); track row.dni + row.fecha) {
              <tr>
                <td style="font-weight:600">{{ row.nombre }}</td>
                <td style="font-family:var(--mono);font-size:11px;color:var(--txt2)">{{ row.dni }}</td>
                <td><span class="w-sede">{{ row.area ?? '—' }}</span></td>
                <td style="font-family:var(--mono);font-size:11px">{{ row.fecha }}</td>
                <td style="font-family:var(--mono);font-size:12px">{{ row.hora_Turno }}</td>
                <td style="font-family:var(--mono);font-weight:600" [style.color]="colorMarcacion(row)">
                  {{ row.hora_Marcacion }}
                </td>
                <td>
                  <span class="ec" [ngClass]="badgeClass(row)">{{ badgeTexto(row) }}</span>
                </td>
              </tr>
            }
          }
        </tbody>
      </table>
      </div>
    </div>

    <!-- PAGINACION -->
    @if (totalPaginas() > 1) {
      <div class="pag-bar">
        <button class="btn btn-sm" [disabled]="pagina() === 0" (click)="pagina.set(pagina() - 1)">‹ Ant.</button>
        <span style="font-size:12px;color:var(--txt2)">
          Página {{ pagina() + 1 }} de {{ totalPaginas() }}
          · {{ paginaActual().length }} de {{ filasFiltradas().length }} registros
        </span>
        <button class="btn btn-sm" [disabled]="pagina() === totalPaginas() - 1" (click)="pagina.set(pagina() + 1)">Sig. ›</button>
      </div>
    }
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
export class TardanzasComponent implements OnInit {
  private svc = inject(RrhhService);

  fechaInicio      = signal(new Date().toISOString().split('T')[0]);
  fechaFin         = signal(new Date().toISOString().split('T')[0]);
  busqueda         = signal('');
  areaSeleccionada = signal('');
  filtro           = signal<Filtro>('ALL');
  datos            = signal<TardanzaReporte[]>([]);
  loading          = signal(false);
  pagina           = signal(0);

  readonly PAGE_SIZE = 50;

  areas = computed(() =>
    [...new Set(this.datos().map(x => x.area ?? 'Sin área'))].sort()
  );

  filasFiltradas = computed(() => {
    const q    = this.busqueda().toLowerCase();
    const area = this.areaSeleccionada();
    const fil  = this.filtro();
    return this.datos().filter(x => {
      if (q && !(x.nombre.toLowerCase().includes(q) || (x.dni ?? '').includes(q))) return false;
      if (area && (x.area ?? 'Sin área') !== area) return false;
      const esFalta = (x.estado ?? '').toUpperCase() === 'FALTA';
      if (fil === 'TARD'  && x.minutos_Late <= 0)                                       return false;
      if (fil === 'PUNT'  && (x.minutos_Late > 0 || x.hora_Marcacion === '--:--'))      return false;
      if (fil === 'FALTA' && !esFalta)                                                  return false;
      if (fil === 'SIN'   && x.hora_Marcacion !== '--:--')                              return false;
      return true;
    });
  });

  totalPaginas = computed(() => Math.max(1, Math.ceil(this.filasFiltradas().length / this.PAGE_SIZE)));

  paginaActual = computed(() => {
    const start = this.pagina() * this.PAGE_SIZE;
    return this.filasFiltradas().slice(start, start + this.PAGE_SIZE);
  });

  countPuntuales = computed(() => this.filasFiltradas().filter(x => x.minutos_Late === 0 && x.hora_Marcacion !== '--:--' && (x.estado ?? '').toUpperCase() !== 'FALTA').length);
  countTardanza  = computed(() => this.filasFiltradas().filter(x => x.minutos_Late > 0).length);
  countFaltas    = computed(() => this.filasFiltradas().filter(x => (x.estado ?? '').toUpperCase() === 'FALTA').length);

  ngOnInit(): void { /* no carga automático — espera que el user presione Buscar */ }

  setFiltro(f: Filtro): void { this.filtro.set(f); this.pagina.set(0); }

  cargar(): void {
    const ini = this.fechaInicio();
    const fin = this.fechaFin();
    if (!ini || !fin) return;
    this.loading.set(true);
    this.pagina.set(0);
    this.svc.getTardanzas(ini, fin).subscribe({
      next: data => { this.datos.set(data); this.loading.set(false); },
      error: ()   => { this.datos.set([]);   this.loading.set(false); }
    });
  }

  badgeClass(row: TardanzaReporte): string {
    if (row.minutos_Late > 0)                         return 'ec-err';
    if ((row.estado ?? '').toUpperCase() === 'FALTA') return 'ec-falta';
    if (row.hora_Marcacion === '--:--')               return 'ec-warn';
    return 'ec-ok';
  }

  badgeTexto(row: TardanzaReporte): string {
    if (row.minutos_Late > 0)                         return row.tiempo_Tardanza_Texto;
    if ((row.estado ?? '').toUpperCase() === 'FALTA') return 'Falta';
    if (row.hora_Marcacion === '--:--')               return 'Sin marcar';
    return 'Puntual';
  }

  colorMarcacion(row: TardanzaReporte): string {
    if (row.hora_Marcacion === '--:--')  return 'var(--txt3)';
    if (row.minutos_Late > 0)            return 'var(--red)';
    return 'var(--grn)';
  }

  exportarCSV(): void {
    const filas = this.filasFiltradas();
    if (!filas.length) return;
    const headers = ['DNI', 'Nombre', 'Área', 'Fecha', 'Hora Turno', 'Hora Marcación', 'Estado', 'Tardanza'];
    const rows = filas.map(x =>
      [x.dni, x.nombre, x.area, x.fecha, x.hora_Turno, x.hora_Marcacion, x.estado ?? '', x.tiempo_Tardanza_Texto]
        .map(v => `"${(v ?? '').toString().replace(/"/g, '""')}"`)
        .join(',')
    );
    const csv = [headers.join(','), ...rows].join('\n');
    const a = document.createElement('a');
    a.href = 'data:text/csv;charset=utf-8,\uFEFF' + encodeURIComponent(csv);
    a.download = `tardanzas_${this.fechaInicio()}_${this.fechaFin()}.csv`;
    a.click();
  }
}
