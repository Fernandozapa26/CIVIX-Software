import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter } from 'rxjs';

import { RolCivix, ROLES, ROLES_GESTION, ROLES_TODOS } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';

interface MenuItem {
  label: string;
  icon: string;
  route: string;
  /** Texto corto que ayuda a encontrar el módulo en el buscador del encabezado. */
  descripcion: string;
  // Roles que pueden ver la opción (deben coincidir con data.roles en app.routes.ts)
  roles: readonly RolCivix[];
}

interface Usuario {
  nombre: string;
  rol: string;
}

interface Miga {
  label: string;
  link: string | null;
}

/** Clave de localStorage donde se recuerda si el sidebar está contraído. */
const CLAVE_SIDEBAR = 'civix.sidebar.contraido';

/**
 * Estructura de la aplicación con sesión iniciada:
 * - Sidebar con navegación filtrada por rol. En escritorio se puede contraer y
 *   en pantallas pequeñas se abre como panel deslizable.
 * - Encabezado con migas de pan, buscador de módulos, notificaciones y perfil.
 */
@Component({
  selector: 'app-main-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'cerrarPaneles()',
  },
})
export class MainLayout {
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  // Opciones de navegación
  private readonly todasLasOpciones: MenuItem[] = [
    {
      label: 'Inicio',
      icon: 'bi-grid-1x2',
      route: '/app/inicio',
      descripcion: 'Resumen y estado general',
      roles: ROLES_TODOS,
    },
    {
      label: 'Obras',
      icon: 'bi-buildings',
      route: '/app/obras',
      descripcion: 'Portafolio de proyectos',
      roles: ROLES_TODOS,
    },
    {
      label: 'Cotizaciones',
      icon: 'bi-file-earmark-text',
      route: '/app/cotizaciones',
      descripcion: 'Presupuestos y aprobaciones',
      roles: ROLES_GESTION,
    },
    {
      label: 'Materiales',
      icon: 'bi-box-seam',
      route: '/app/materiales',
      descripcion: 'Inventario y retiros',
      roles: ROLES_GESTION,
    },
    {
      label: 'Avances',
      icon: 'bi-graph-up-arrow',
      route: '/app/avances',
      descripcion: 'Seguimiento de obra',
      roles: ROLES_TODOS,
    },
    {
      label: 'Usuarios',
      icon: 'bi-people',
      route: '/app/usuarios',
      descripcion: 'Clientes e ingenieros',
      roles: [ROLES.administrador],
    },
  ];

  // Solo las opciones permitidas para los roles del usuario con sesión
  readonly menuItems = computed(() =>
    this.todasLasOpciones.filter((item) => this.authService.tieneAlgunRol(item.roles)),
  );

  // Usuario con sesión iniciada (viene del token guardado por AuthService)
  readonly usuario = computed<Usuario | null>(() => {
    const actual = this.authService.usuario();

    if (!actual) {
      return null;
    }

    return {
      nombre: actual.nombre,
      rol: actual.roles.join(', ') + (this.authService.esSoloLectura() ? ' · Solo consulta' : ''),
    };
  });

  // Título de la página activa y ubicación actual
  readonly tituloPagina = signal('Inicio');
  private readonly urlActual = signal(this.router.url);

  readonly migas = computed<Miga[]>(() => {
    const url = this.urlActual().split('?')[0];
    const modulo = this.todasLasOpciones.find((item) => url.startsWith(item.route));
    const migas: Miga[] = [{ label: 'CIVIX', link: '/app/inicio' }];

    if (modulo) {
      const esPaginaDelModulo = url === modulo.route;
      migas.push({ label: modulo.label, link: esPaginaDelModulo ? null : modulo.route });

      if (!esPaginaDelModulo && this.tituloPagina() !== modulo.label) {
        migas.push({ label: this.tituloPagina(), link: null });
      }
    } else {
      migas.push({ label: this.tituloPagina(), link: null });
    }

    return migas;
  });

  // Estado del sidebar
  readonly contraido = signal(leerPreferenciaSidebar());
  readonly menuMovilAbierto = signal(false);

  // Buscador de módulos del encabezado
  readonly textoBusqueda = signal('');
  readonly busquedaAbierta = signal(false);
  readonly resultadosBusqueda = computed(() => {
    const texto = normalizar(this.textoBusqueda());
    const opciones = this.menuItems();

    if (!texto) {
      return opciones;
    }

    return opciones.filter(
      (item) =>
        normalizar(item.label).includes(texto) || normalizar(item.descripcion).includes(texto),
    );
  });

  constructor() {
    this.actualizarTitulo();

    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => {
        this.actualizarTitulo();
        this.urlActual.set(this.router.url);
        this.menuMovilAbierto.set(false);
      });
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
      .map((parte) => parte.charAt(0).toUpperCase())
      .join('');
  }

  alternarSidebar(): void {
    this.contraido.update((valor) => !valor);

    try {
      localStorage.setItem(CLAVE_SIDEBAR, this.contraido() ? '1' : '0');
    } catch {
      // Sin acceso a localStorage la preferencia dura solo esta sesión.
    }
  }

  abrirMenuMovil(): void {
    this.menuMovilAbierto.set(true);
  }

  cerrarPaneles(): void {
    this.menuMovilAbierto.set(false);
    this.busquedaAbierta.set(false);
  }

  alEscribirBusqueda(valor: string): void {
    this.textoBusqueda.set(valor);
    this.busquedaAbierta.set(true);
  }

  /** Al perder el foco se espera un instante para que el clic en un resultado alcance a registrarse. */
  alSalirBusqueda(): void {
    setTimeout(() => this.busquedaAbierta.set(false), 150);
  }

  irAlPrimerResultado(): void {
    const primero = this.resultadosBusqueda()[0];

    if (primero) {
      this.irA(primero.route);
    }
  }

  irA(ruta: string): void {
    this.textoBusqueda.set('');
    this.busquedaAbierta.set(false);
    void this.router.navigateByUrl(ruta);
  }

  cerrarSesion(): void {
    this.authService.logout();
  }
}

function leerPreferenciaSidebar(): boolean {
  try {
    return localStorage.getItem(CLAVE_SIDEBAR) === '1';
  } catch {
    return false;
  }
}

function normalizar(texto: string): string {
  return texto
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '');
}
