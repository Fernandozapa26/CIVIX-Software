import { Routes } from '@angular/router';

export const routes: Routes = [
      {
    // Componente principal que contiene el menú
    path: '',
    loadComponent: () =>
      import('./layouts/main-layout/main-layout')
        .then(m => m.MainLayout),

    // Páginas que se mostrarán dentro del menú
    children: [
      {
        // Al entrar a la raíz, abrir Cotizaciones
        path: '',
        redirectTo: 'cotizaciones',
        pathMatch: 'full'
      },
      {
        // Ruta del módulo Cotizaciones
        path: 'cotizaciones',
        loadComponent: () =>
          import('./features/cotizaciones/cotizaciones')
            .then(m => m.Cotizaciones)
      },
      {
        // Ruta del módulo Materiales
        path: 'materiales',
        loadComponent: () =>
          import('./features/materiales/materiales')
            .then(m => m.Materiales)
      }
    ]
  },
  {
    // Si la ruta no existe, volver a Cotizaciones
    path: '**',
    redirectTo: 'cotizaciones'
  }
];
