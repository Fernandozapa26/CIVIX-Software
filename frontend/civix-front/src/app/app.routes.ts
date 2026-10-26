import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layouts/main-layout/main-layout')
        .then(m => m.MainLayout),
    children: [
      { path: '', redirectTo: 'obras', pathMatch: 'full' },
      {
        path: 'obras',
        loadComponent: () =>
          import('./features/obras/obras')
            .then(m => m.Obras),
      },
      {
        path: 'obras/nueva',
        loadComponent: () =>
          import('./features/obras/obras')
            .then(m => m.Obras),
        data: { nueva: true },
      },
      {
        path: 'resumen',
        loadComponent: () =>
          import('./features/placeholder/placeholder')
            .then(m => m.Placeholder),
        data: { title: 'Resumen' },
      },
      {
        path: 'personal',
        loadComponent: () =>
          import('./features/placeholder/placeholder')
            .then(m => m.Placeholder),
        data: { title: 'Personal' },
      },
      {
        path: 'presupuesto',
        loadComponent: () =>
          import('./features/placeholder/placeholder')
            .then(m => m.Placeholder),
        data: { title: 'Presupuesto' },
      },
      {
        path: 'inventario',
        loadComponent: () =>
          import('./features/placeholder/placeholder')
            .then(m => m.Placeholder),
        data: { title: 'Inventario' },
      },
      {
        path: 'reportes',
        loadComponent: () =>
          import('./features/placeholder/placeholder')
            .then(m => m.Placeholder),
        data: { title: 'Reportes' },
      },
      {
        path: 'configuracion',
        loadComponent: () =>
          import('./features/placeholder/placeholder')
            .then(m => m.Placeholder),
        data: { title: 'Configuración' },
      },
      {
        path: 'cotizaciones',
        loadComponent: () =>
          import('./features/cotizaciones/cotizaciones')
            .then(m => m.Cotizaciones),
      },
      {
        path: 'materiales',
        loadComponent: () =>
          import('./features/materiales/materiales')
            .then(m => m.Materiales),
      },
    ],
  },
  { path: '**', redirectTo: 'obras' },
];