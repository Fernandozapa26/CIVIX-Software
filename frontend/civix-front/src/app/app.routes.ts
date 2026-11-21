import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/guards/auth.guard';
import { ROLES, ROLES_GESTION, ROLES_TODOS } from './core/models/auth.models';

export const routes: Routes = [
  // Landing pública
  {
    path: '',
    loadComponent: () => import('./features/landing/landing').then((m) => m.Landing),
  },
  // Pantallas públicas
  {
    path: 'login',
    loadComponent: () => import('./layouts/auth-layout/auth-layout').then((m) => m.AuthLayout),
    canActivate: [guestGuard],
    children: [
      {
        path: '',
        title: 'Iniciar sesión',
        loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
      },
    ],
  },
  {
    path: 'registro',
    loadComponent: () => import('./layouts/auth-layout/auth-layout').then((m) => m.AuthLayout),
    canActivate: [guestGuard],
    children: [
      {
        path: '',
        title: 'Crear cuenta',
        loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
      },
    ],
  },
  // Aplicación (requiere sesión)
  {
    path: 'app',
    loadComponent: () => import('./layouts/main-layout/main-layout').then((m) => m.MainLayout),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'inicio',
        pathMatch: 'full',
      },
      {
        path: 'inicio',
        title: 'Inicio',
        canActivate: [authGuard],
        data: { roles: ROLES_TODOS },
        loadComponent: () => import('./features/inicio/inicio').then((m) => m.Inicio),
      },
      {
        path: 'obras',
        title: 'Obras',
        canActivate: [authGuard],
        data: { roles: ROLES_TODOS },
        loadComponent: () => import('./features/obras/obras').then((m) => m.Obras),
      },
      {
        path: 'avances',
        title: 'Avances',
        canActivate: [authGuard],
        data: { roles: ROLES_TODOS },
        loadComponent: () => import('./features/avances/avances').then((m) => m.Avances),
      },
      {
        path: 'cotizaciones',
        title: 'Cotizaciones',
        canActivate: [authGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () =>
          import('./features/cotizaciones/cotizaciones').then((m) => m.Cotizaciones),
      },
      {
        path: 'cotizaciones/nueva',
        title: 'Nueva cotización',
        canActivate: [authGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () =>
          import('./features/cotizaciones/components/cotizacion-form/cotizacion-form').then(
            (m) => m.CotizacionForm,
          ),
      },
      {
        path: 'cotizaciones/:id/editar',
        title: 'Editar cotización',
        canActivate: [authGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () =>
          import('./features/cotizaciones/components/cotizacion-form/cotizacion-form').then(
            (m) => m.CotizacionForm,
          ),
      },
      {
        // Usuarios: solo Administrador
        path: 'usuarios',
        title: 'Usuarios',
        canActivate: [authGuard],
        data: { roles: [ROLES.administrador] },
        loadComponent: () => import('./features/usuarios/usuarios').then((m) => m.Usuarios),
      },
      {
        path: 'materiales',
        title: 'Materiales',
        canActivate: [authGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () => import('./features/materiales/materiales').then((m) => m.Materiales),
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];