import { Injectable, signal, computed } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class WeekService {
  weekOffset = signal<number>(0);

  weekDates = computed<Date[]>(() => this.getWeekDates(this.weekOffset()));

  weekLabel = computed<string>(() => {
    const dates = this.weekDates();
    const start = dates[0];
    const end = dates[6];
    const fmt = (d: Date) =>
      d.toLocaleDateString('es-PE', { day: '2-digit', month: 'short' }).replace('.', '');
    return `${fmt(start)} – ${fmt(end)} ${end.getFullYear()}`;
  });

  prev(): void { this.weekOffset.update(n => n - 1); }
  next(): void { this.weekOffset.update(n => n + 1); }
  goToday(): void { this.weekOffset.set(0); }

  getWeekDates(offset: number): Date[] {
    const now = new Date();
    const dow = now.getDay();
    const monday = new Date(now);
    monday.setDate(now.getDate() - ((dow + 6) % 7) + offset * 7);
    return Array.from({ length: 7 }, (_, i) => {
      const d = new Date(monday);
      d.setDate(monday.getDate() + i);
      return d;
    });
  }

  toISO(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
