import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layouts/main-layout/main-layout')
        .then(m => m.MainLayout),

    children: [
      {
        path: '',
        redirectTo: 'inicio',
        pathMatch: 'full'
      },
      {
        path: 'inicio',
        title: 'Inicio',
        loadComponent: () =>
          import('./features/inicio/inicio')
            .then(m => m.Inicio)
      },
      {
        path: 'cotizaciones',
        title: 'Cotizaciones',
        loadComponent: () =>
          import('./features/cotizaciones/cotizaciones')
            .then(m => m.Cotizaciones)
      },
      {
        path: 'cotizaciones/nueva',
        title: 'Nueva cotización',
        loadComponent: () =>
          import('./features/cotizaciones/components/cotizacion-form/cotizacion-form')
            .then(m => m.CotizacionForm)
      },
      {
        path: 'materiales',
        title: 'Materiales',
        loadComponent: () =>
          import('./features/materiales/materiales')
            .then(m => m.Materiales)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'inicio'
  }
];