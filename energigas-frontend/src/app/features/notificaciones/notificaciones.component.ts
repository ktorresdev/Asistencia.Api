import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RrhhService } from '../../core/services/rrhh.service';
import { Notificacion } from '../../core/models/rrhh.models';

@Component({
  selector: 'app-notificaciones',
  standalone: true,
  imports: [CommonModule],
  template: `
    <ng-container>
      <div class="toolbar">
        <span style="font-weight:600;font-size:13px">Notificaciones</span>
        <span style="flex:1"></span>
        @if (loading()) { <span class="spin"></span> }
      </div>
      <div class="grid-outer" style="overflow-y:auto">
        @if (!notificaciones().length && !loading()) {
          <div style="padding:60px;text-align:center;color:var(--txt3)">Sin notificaciones</div>
        }
        @for (n of notificaciones(); track n.id) {
          <div style="padding:14px 20px;border-bottom:1px solid var(--brd);display:flex;gap:12px;cursor:pointer"
            [style.opacity]="n.leida ? '.5' : '1'"
            (click)="marcar(n)">
            <div style="width:8px;height:8px;border-radius:50%;background:var(--acc);margin-top:5px;flex-shrink:0"
              [style.visibility]="n.leida ? 'hidden' : 'visible'"></div>
            <div>
              <div style="font-size:13px;font-weight:600">{{ n.titulo }}</div>
              <div style="font-size:12px;color:var(--txt2);margin-top:3px">{{ n.mensaje }}</div>
              <div style="font-size:10px;color:var(--txt3);font-family:var(--mono);margin-top:4px">
                {{ n.fechaCreacion | date:'dd/MM/yyyy HH:mm' }}
              </div>
            </div>
          </div>
        }
      </div>
    </ng-container>
  `,
  styles: [`:host{display:flex;flex-direction:column;flex:1;min-height:0;overflow:hidden;}`]
})
export class NotificacionesComponent implements OnInit {
  private rrhhService = inject(RrhhService);
  notificaciones = signal<Notificacion[]>([]);
  loading = signal(false);

  ngOnInit(): void {
    this.loading.set(true);
    this.rrhhService.getNotificaciones().subscribe({
      next: n => { this.notificaciones.set(n); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  marcar(n: Notificacion): void {
    if (n.leida) return;
    this.rrhhService.marcarLeida(n.id).subscribe(() => {
      this.notificaciones.update(ns => ns.map(x => x.id === n.id ? { ...x, leida: true } : x));
    });
  }
}
