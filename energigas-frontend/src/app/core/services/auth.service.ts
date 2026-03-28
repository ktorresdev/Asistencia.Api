import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, CurrentUser, RefreshTokenRequest } from '../models/auth.models';

const TOKEN_KEY = 'eg_token';
const REFRESH_KEY = 'eg_refresh';
const USER_KEY = 'eg_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = environment.apiUrl;

  currentUser = signal<CurrentUser | null>(this.loadUser());
  isAuthenticated = signal<boolean>(!!this.getToken());

  constructor(private http: HttpClient, private router: Router) {}

  login(req: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.api}/api/Auth/login`, req).pipe(
      tap(res => {
        // Backend returns "accessToken", not "token"
        localStorage.setItem(TOKEN_KEY, res.accessToken);
        localStorage.setItem(REFRESH_KEY, res.refreshToken);

        const user: CurrentUser = {
          username: res.user?.username ?? req.username,
          role: res.role ?? res.user?.role ?? '',
          sucursalId: res.trabajador?.sucursalId,
          trabajadorId: res.trabajador?.id,
          personaNombre: res.persona?.apellidosNombres
        };
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        this.currentUser.set(user);
        this.isAuthenticated.set(true);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this.router.navigate(['/login']);
  }

  refreshToken(): Observable<LoginResponse> {
    // Backend RefreshRequest record only requires { refreshToken }
    const body: RefreshTokenRequest = { refreshToken: this.getRefreshToken() ?? '' };
    return this.http.post<LoginResponse>(`${this.api}/api/Auth/refresh`, body).pipe(
      tap(res => {
        localStorage.setItem(TOKEN_KEY, res.accessToken);
        localStorage.setItem(REFRESH_KEY, res.refreshToken);
      })
    );
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  private loadUser(): CurrentUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try { return JSON.parse(raw); } catch { return null; }
  }

  hasRole(...roles: string[]): boolean {
    const role = (this.currentUser()?.role ?? '').toUpperCase();
    return roles.some(r => r.toUpperCase() === role);
  }

  isAdmin(): boolean {
    return this.hasRole('ADMIN', 'SUPERADMIN');
  }

  isSuperAdmin(): boolean {
    return this.hasRole('SUPERADMIN');
  }

  isSupervisor(): boolean {
    return this.hasRole('SUPERVISOR');
  }

  isAdminOrSupervisor(): boolean {
    return this.hasRole('ADMIN', 'SUPERADMIN', 'SUPERVISOR');
  }
}
