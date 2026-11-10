import { Component, computed, inject, signal } from '@angular/core';

import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import { filter } from 'rxjs';

import { RolCivix, ROLES, ROLES_GESTION, ROLES_TODOS } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';

interface MenuItem {
  label: string;
  icon: string;
  route: string;
  // Roles que pueden ver la opción (deben coincidir con data.roles en app.routes.ts)
  roles: readonly RolCivix[];
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
  private authService = inject(AuthService);

  // Opciones de navegación
  private readonly todasLasOpciones: MenuItem[] = [
    {
      label: 'Inicio',
      icon: 'bi-house-door',
      route: '/app/inicio',
      roles: ROLES_TODOS
    },
    {
      label: 'Obras',
      icon: 'bi-buildings',
      route: '/app/obras',
      roles: ROLES_TODOS
    },
    {
      label: 'Cotizaciones',
      icon: 'bi-file-earmark-text',
      route: '/app/cotizaciones',
      roles: ROLES_GESTION
    },
    {
      label: 'Materiales',
      icon: 'bi-box-seam',
      route: '/app/materiales',
      roles: ROLES_GESTION
    },
    {
      label: 'Avances',
      icon: 'bi-graph-up-arrow',
      route: '/app/avances',
      roles: ROLES_TODOS
    },
    {
      label: 'Usuarios',
      icon: 'bi-people',
      route: '/usuarios',
      roles: [ROLES.administrador]
    }
  ];

  // Solo las opciones permitidas para los roles del usuario con sesión
  readonly menuItems = computed(() =>
    this.todasLasOpciones.filter(item => this.authService.tieneAlgunRol(item.roles))
  );

  // Usuario con sesión iniciada (viene del token guardado por AuthService)
  readonly usuario = computed<Usuario | null>(() => {
    const actual = this.authService.usuario();

    if (!actual) {
      return null;
    }

    return {
      nombre: actual.nombre,
      rol: actual.roles.join(', ') + (this.authService.esSoloLectura() ? ' · Solo consulta' : '')
    };
  });

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

  cerrarSesion(): void {
    this.authService.logout();
  }
}
