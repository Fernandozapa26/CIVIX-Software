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
        title: 'Iniciar sesión | CIVIX',
        loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
      },
      {
        path: 'registro',
        title: 'Crear cuenta | CIVIX',
        loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
      },
    ],
  },
  {
    // Componente principal que contiene el menú (solo con sesión iniciada)
    path: '',
    loadComponent: () => import('./layouts/main-layout/main-layout').then((m) => m.MainLayout),
    canActivate: [authGuard],

    // Páginas que se mostrarán dentro del menú
    children: [
      {
        // Al entrar a la raíz, abrir Cotizaciones
        path: '',
        redirectTo: 'cotizaciones',
        pathMatch: 'full',
      },
      {
        // Cotizaciones: todos los roles (el Cliente en modo consulta)
        path: 'cotizaciones',
        title: 'Cotizaciones | CIVIX',
        canActivate: [authGuard],
        data: { roles: ROLES_TODOS },
        loadComponent: () =>
          import('./features/cotizaciones/cotizaciones').then((m) => m.Cotizaciones),
      },
      {
        // Materiales: solo Administrador e Ingeniero
        path: 'materiales',
        title: 'Materiales | CIVIX',
        canActivate: [authGuard],
        data: { roles: ROLES_GESTION },
        loadComponent: () => import('./features/materiales/materiales').then((m) => m.Materiales),
      },
    ],
  },
  {
    // Si la ruta no existe, volver a Cotizaciones (el guard envía al login si no hay sesión)
    path: '**',
    redirectTo: 'cotizaciones',
  },
];
