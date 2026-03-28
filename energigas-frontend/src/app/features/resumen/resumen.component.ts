import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RrhhService } from '../../core/services/rrhh.service';
import { ToastService } from '../../core/services/toast.service';
import { ResumenDiario, SucursalCentro } from '../../core/models/rrhh.models';

@Component({
  selector: 'app-resumen',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <ng-container>
      <div class="toolbar">
        <div class="fld-lbl" style="margin-right:4px">Fecha:</div>
        <input type="date" class="inp" style="max-width:160px" [ngModel]="fecha()" (ngModelChange)="fecha.set($event)" />
        <select class="sel" style="max-width:180px" [ngModel]="sedeId()" (ngModelChange)="sedeId.set(+$event)">
          <option [value]="0">Todas las sedes</option>
          @for (s of sedes(); track s.id) { <option [value]="s.id">{{ s.nombreSucursal }}</option> }
        </select>
        <button class="btn btn-primary btn-sm" (click)="load()">Cargar</button>
        @if (loading()) { <span class="spin"></span> }
      </div>
      <div class="grid-outer">
        @if (!resumen().length && !loading()) {
          <div style="padding:40px;text-align:center;color:var(--txt3)">Sin datos para la fecha seleccionada</div>
        }
        <div class="tbl-wrap">
        <table class="tbl">
          <thead>
            <tr>
              <th>Trabajador</th>
              <th>DNI</th>
              <th>Tipo resumen</th>
              <th>H. Teóricas</th>
              <th>H. Reales</th>
              <th>Tardanza</th>
              <th>H. Extra</th>
            </tr>
          </thead>
          <tbody>
            @for (r of resumen(); track r.trabajadorId) {
              <tr>
                <td style="font-weight:600">{{ r.trabajadorNombre }}</td>
                <td style="font-family:var(--mono);font-size:11px;color:var(--txt2)">{{ r.dni }}</td>
                <td>
                  <span class="ec" [class]="getResumenCls(r.tipoResumen)">{{ r.tipoResumen }}</span>
                </td>
                <td style="font-family:var(--mono)">{{ r.horasTeoricas ?? '—' }}</td>
                <td style="font-family:var(--mono)">{{ r.horasReales ?? '—' }}</td>
                <td style="font-family:var(--mono);color:var(--amb)">
                  {{ r.minutosTardanza ? r.minutosTardanza + ' min' : '—' }}
                </td>
                <td><span *ngIf="r.esHoraExtra" class="ec ec-grn">SÍ</span></td>
              </tr>
            }
          </tbody>
        </table>
        </div>
      </div>
    </ng-container>
  `,
  styles: [`:host { display:flex;flex-direction:column;flex:1;min-height:0;overflow:hidden; }`]
})
export class ResumenComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  toast = inject(ToastService);

  resumen = signal<ResumenDiario[]>([]);
  sedes = signal<SucursalCentro[]>([]);
  fecha = signal(new Date().toISOString().split('T')[0]);
  sedeId = signal(0);
  loading = signal(false);

  ngOnInit(): void {
    this.rrhhService.getSucursales().subscribe(s => this.sedes.set(s));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.rrhhService.getResumenDiario(this.fecha(), this.sedeId() || undefined).subscribe({
      next: r => { this.resumen.set(r); this.loading.set(false); },
      error: () => { this.resumen.set([]); this.loading.set(false); }
    });
  }

  getResumenCls(tipo: string): string {
    const m: Record<string, string> = {
      ASISTIO: 'ec-ok', FALTA: 'ec-err', TARDANZA: 'ec-warn',
      VACACIONES: 'ec-info', DESCANSO: 'ec-gray'
    };
    return m[tipo] ?? 'ec-gray';
  }
}
