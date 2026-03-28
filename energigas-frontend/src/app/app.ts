import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `<router-outlet />`,
  styles: [`:host { display: block; height: 100%; }`]
})
export class App implements OnInit {
  private themeService = inject(ThemeService);

  ngOnInit(): void {
    const splash = document.getElementById('eg-splash');
    if (splash) {
      splash.classList.add('out');
      setTimeout(() => splash.remove(), 500);
    }
  }
}
