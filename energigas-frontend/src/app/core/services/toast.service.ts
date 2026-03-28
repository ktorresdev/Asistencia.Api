import { Injectable, signal } from '@angular/core';

export type ToastType = 'ok' | 'err' | 'warn' | 'info';

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  toasts = signal<Toast[]>([]);
  private counter = 0;

  show(message: string, type: ToastType = 'info', duration = 3500): void {
    const id = ++this.counter;
    this.toasts.update(ts => [...ts, { id, message, type }]);
    setTimeout(() => this.dismiss(id), duration);
  }

  ok(msg: string) { this.show(msg, 'ok'); }
  err(msg: string) { this.show(msg, 'err', 5000); }
  warn(msg: string) { this.show(msg, 'warn', 4000); }

  /** Extrae y muestra el mensaje más claro posible de un HttpErrorResponse */
  errHttp(err: any, fallback = 'Error inesperado'): void {
    this.err(this.extractHttpMsg(err, fallback));
  }

  extractHttpMsg(err: any, fallback = 'Error inesperado'): string {
    const e = err?.error;
    if (!e) return err?.message ?? fallback;

    // Respuesta texto plano
    if (typeof e === 'string' && e.trim()) return e.trim();

    // Campos de mensaje directos
    if (e.mensaje) return e.mensaje;
    if (e.message) return e.message;

    // Detail de ProblemDetails (suele tener el mensaje del SP/SQL)
    if (e.detail) return e.detail;

    // Errores de validación de modelo ASP.NET Core (400)
    if (e.errors && typeof e.errors === 'object') {
      const msgs: string[] = [];
      for (const key of Object.keys(e.errors)) {
        const arr: string[] = e.errors[key];
        if (!Array.isArray(arr)) continue;
        arr.forEach(m => {
          // Filtrar mensajes técnicos internos de model binding
          if (!m.includes('JSON value could not') && m !== 'The request field is required.')
            msgs.push(m);
        });
      }
      if (msgs.length) return msgs.join(' · ');
      if (e.title) return e.title;
    }

    // ProblemDetails genérico
    if (e.title) return e.title;

    return fallback;
  }

  dismiss(id: number): void {
    this.toasts.update(ts => ts.filter(t => t.id !== id));
  }
}
