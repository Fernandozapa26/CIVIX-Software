import { Component, inject, signal } from '@angular/core';

import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import { filter } from 'rxjs';

interface MenuItem {
  label: string;
  icon: string;
  route: string;
}

interface Usuario {
  nombre: string;
  rol: string;
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss'
})
export class MainLayout {

  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);

  // Opciones de navegación
  readonly menuItems: MenuItem[] = [
    {
      label: 'Inicio',
      icon: 'bi-house-door',
      route: '/inicio'
    },
    {
      label: 'Obras',
      icon: 'bi-buildings',
      route: '/obras'
    },
    {
      label: 'Cotizaciones',
      icon: 'bi-file-earmark-text',
      route: '/cotizaciones'
    },
    {
      label: 'Materiales',
      icon: 'bi-box-seam',
      route: '/materiales'
    },
    {
      label: 'Avances',
      icon: 'bi-graph-up-arrow',
      route: '/avances'
    },
    {
      label: 'Usuarios',
      icon: 'bi-people',
      route: '/usuarios'
    }
  ];

  // Datos temporales hasta conectar autenticación
  readonly usuario = signal<Usuario | null>(null);

  // Título de la página activa
  readonly tituloPagina = signal('Inicio');

  constructor() {
    this.actualizarTitulo();

    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => this.actualizarTitulo());
  }

  private actualizarTitulo(): void {
    let ruta = this.activatedRoute;

    while (ruta.firstChild) {
      ruta = ruta.firstChild;
    }
    const titulo = ruta.snapshot?.title;

    this.tituloPagina.set(titulo ?? 'Inicio');
  }

  obtenerIniciales(nombre: string): string {
    return nombre
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map(parte => parte.charAt(0).toUpperCase())
      .join('');
  }
}