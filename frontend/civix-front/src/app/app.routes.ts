import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/guards/auth.guard';
import { ROLES_GESTION, ROLES_TODOS } from './core/models/auth.models';

export const routes: Routes = [
  {
    // Pantallas públicas (login y registro) dentro de la pantalla dividida de Figma
    path: '',
    loadComponent: () => import('./layouts/auth-layout/auth-layout').then((m) => m.AuthLayout),
    canActivate: [guestGuard],
    children: [
      {
        path: 'login',
        title: 'Iniciar sesión',
        loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
      },
      {
        path: 'registro',
        title: 'Crear cuenta',
        loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
      },
    ],
  },
  {
    // Componente principal con el menú (solo con sesión iniciada)
    path: '',
    loadComponent: () => import('./layouts/main-layout/main-layout').then((m) => m.MainLayout),
    canActivate: [authGuard],

    children: [
      {
        path: '',
        redirectTo: 'inicio',
        pathMatch: 'full',
      },
      {
        // Inicio: todos los roles
        path: 'inicio',
        title: 'Inicio',
        canActivate: [authGuard],
        data: { roles: ROLES_TODOS },
        loadComponent: () => import('./features/inicio/inicio').then((m) => m.Inicio),
      },
      {
        // Cotizaciones: todos los roles (el Cliente en modo consulta)
        path: 'cotizaciones',
        title: 'Cotizaciones',
        canActivate: [authGuard],
        data: { roles: ROLES_TODOS },
        loadComponent: () =>
          import('./features/cotizaciones/cotizaciones').then((m) => m.Cotizaciones),
      },
      {
        // Crear cotizaciones: solo Administrador e Ingeniero
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
        // Materiales: solo Administrador e Ingeniero
        path: 'materiales',
        title: 'Materiales',
        canActivate: [authGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () => import('./features/materiales/materiales').then((m) => m.Materiales),
      },
    ],
  },
  {
    // Si la ruta no existe, volver a Inicio (el guard envía al login si no hay sesión)
    path: '**',
    redirectTo: 'inicio',
  },
];
