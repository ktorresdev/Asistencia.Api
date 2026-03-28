import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  {
    path: '',
    loadComponent: () => import('./layout/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'horario', loadComponent: () => import('./features/horario/horario.component').then(m => m.HorarioComponent) },
      { path: 'programacion', loadComponent: () => import('./features/programacion/programacion.component').then(m => m.ProgramacionComponent) },
      { path: 'coberturas', loadComponent: () => import('./features/coberturas/coberturas.component').then(m => m.CoberturasComponent) },
      { path: 'ausencias', loadComponent: () => import('./features/ausencias/ausencias.component').then(m => m.AusenciasComponent) },
      { path: 'trabajadores', loadComponent: () => import('./features/trabajadores/trabajadores.component').then(m => m.TrabajadoresComponent) },
      { path: 'resumen', loadComponent: () => import('./features/resumen/resumen.component').then(m => m.ResumenComponent) },
      { path: 'notificaciones', loadComponent: () => import('./features/notificaciones/notificaciones.component').then(m => m.NotificacionesComponent) },
      { path: 'tardanzas', loadComponent: () => import('./features/tardanzas/tardanzas.component').then(m => m.TardanzasComponent) },
      { path: 'reportes', loadComponent: () => import('./features/reportes/reportes.component').then(m => m.ReportesComponent) },
      { path: 'maestros/sucursales', loadComponent: () => import('./features/maestros/sucursales/sucursales.component').then(m => m.SucursalesComponent) },
      { path: 'maestros/tipo-turno', loadComponent: () => import('./features/maestros/tipo-turno/tipo-turno.component').then(m => m.TipoTurnoComponent) },
      { path: 'maestros/turnos', loadComponent: () => import('./features/maestros/turnos/turnos.component').then(m => m.TurnosComponent) },
      { path: 'maestros/horarios-turno', loadComponent: () => import('./features/maestros/horarios-turno/horarios-turno.component').then(m => m.HorariosTurnoComponent) },
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
