import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login').then((m) => m.Login) },
  {
    path: '',
    loadComponent: () => import('./pages/home/home').then((m) => m.Home),
    canActivate: [authGuard],
  },
  {
    path: 'settings/connections',
    loadComponent: () => import('./pages/settings/connections/connections').then((m) => m.Connections),
    canActivate: [authGuard],
  },
  {
    path: 'admin/sync-overview',
    loadComponent: () => import('./pages/admin/sync-overview/sync-overview').then((m) => m.SyncOverview),
    canActivate: [authGuard],
  },
  { path: '**', redirectTo: '' },
];
