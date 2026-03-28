import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="login-wrap">
      <div class="login-card">
        <div class="login-logo">
          <img class="logo-light" src="assets/img/logofondoclaro.ico" alt="Energigas" />
          <img class="logo-dark"  src="assets/img/logofondooscuro.ico" alt="Energigas" />
        </div>
        <div class="login-sub">Control de Asistencia · v3</div>

        @if (error()) {
          <div class="err-box">{{ error() }}</div>
        }

        <form (ngSubmit)="onSubmit()">
          <div class="fld" style="margin-bottom:12px">
            <label class="fld-lbl">Usuario</label>
            <input class="inp" type="text" [(ngModel)]="username" name="username"
              placeholder="usuario" autocomplete="username" />
          </div>
          <div class="fld" style="margin-bottom:20px">
            <label class="fld-lbl">Contraseña</label>
            <input class="inp" type="password" [(ngModel)]="password" name="password"
              placeholder="••••••••" autocomplete="current-password" />
          </div>
          <button class="btn btn-primary" type="submit" style="width:100%" [disabled]="loading()">
            {{ loading() ? 'Ingresando...' : 'Ingresar' }}
          </button>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .login-wrap {
      display: flex; align-items: center; justify-content: center;
      min-height: 100vh; padding: 24px;
      background: radial-gradient(ellipse at 60% 40%, rgba(79,142,247,.06) 0%, transparent 70%),
                  radial-gradient(ellipse at 20% 80%, rgba(124,58,237,.04) 0%, transparent 60%),
                  var(--bg);
    }
    .login-card {
      background: var(--surf); border: 1px solid var(--brd2); border-radius: 16px;
      padding: 40px 36px; width: 100%; max-width: 400px; box-shadow: 0 8px 32px rgba(0,0,0,.12);
      animation: cardIn .4s cubic-bezier(.34,1.1,.64,1) both;
    }
    @keyframes cardIn {
      from { opacity: 0; transform: scale(.94) translateY(12px); }
      to   { opacity: 1; transform: scale(1) translateY(0); }
    }
    .login-logo {
      display: flex; justify-content: center; align-items: center;
      margin-bottom: 6px;
      animation: fadeUp .35s ease .1s both;
    }
    .login-logo img {
      height: 64px; width: auto; max-width: 220px; display: block;
    }
    .login-logo .logo-light { display: none; }
    .login-logo .logo-dark  { display: block; }
    :host-context([data-theme="light"]) .login-logo .logo-light { display: block; }
    :host-context([data-theme="light"]) .login-logo .logo-dark  { display: none; }
    .login-logo em {
      font-style: normal;
      background: linear-gradient(90deg, var(--acc), var(--pur));
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }
    .login-sub { text-align: center; color: var(--txt2); font-size: 13px; margin-bottom: 28px; animation: fadeUp .35s ease .15s both; }
    .err-box {
      background: var(--red-bg); color: var(--red); border: 1px solid rgba(248,113,113,.3);
      border-radius: 8px; padding: 10px 14px; font-size: 12px; margin-bottom: 14px;
      animation: shake .3s ease;
    }
    @keyframes shake {
      0%, 100% { transform: translateX(0); }
      20%       { transform: translateX(-5px); }
      40%       { transform: translateX(5px); }
      60%       { transform: translateX(-4px); }
      80%       { transform: translateX(4px); }
    }
  `]
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private themeService = inject(ThemeService); // ensure theme is initialized

  username = '';
  password = '';
  loading = signal(false);
  error = signal('');

  onSubmit(): void {
    if (!this.username || !this.password) {
      this.error.set('Ingresa usuario y contraseña.');
      return;
    }
    this.loading.set(true);
    this.error.set('');
    this.auth.login({ username: this.username, password: this.password }).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: err => {
        this.error.set(err.status === 401 ? 'Credenciales incorrectas.' : 'Error de conexión.');
        this.loading.set(false);
      }
    });
  }
}
