import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  RolCivix,
  ROLES,
  UsuarioSesion,
} from '../models/auth.models';

const CLAVE_SESION = 'civix.sesion';

/** Ruta a la que se envía al usuario después de iniciar sesión. Todos los roles pueden verla. */
export const RUTA_INICIO = '/inicio';

/**
 * Maneja la sesión del usuario:
 * - Llama a /api/auth/login y /api/auth/register.
 * - Guarda la respuesta en localStorage para sobrevivir a recargas de página.
 * - Expone el estado con signals para que el menú y los guards reaccionen solos.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  private readonly sesion = signal<AuthResponse | null>(leerSesionGuardada());

  /** Usuario actual o null si no hay sesión. */
  readonly usuario = computed<UsuarioSesion | null>(() => {
    const sesion = this.sesion();

    if (!sesion) {
      return null;
    }

    const { usuarioId, nombre, correo, roles } = sesion;
    return { usuarioId, nombre, correo, roles };
  });

  readonly estaAutenticado = computed(() => this.sesion() !== null);

  readonly roles = computed<readonly RolCivix[]>(() => this.sesion()?.roles ?? []);

  /** El Cliente solo consulta: las pantallas usan esto para ocultar botones de crear/editar. */
  readonly esSoloLectura = computed(() => {
    const roles = this.roles();
    return (
      roles.includes(ROLES.cliente) &&
      !roles.includes(ROLES.administrador) &&
      !roles.includes(ROLES.ingeniero)
    );
  });

  login(datos: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, datos)
      .pipe(tap((respuesta) => this.guardarSesion(respuesta)));
  }

  register(datos: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, datos)
      .pipe(tap((respuesta) => this.guardarSesion(respuesta)));
  }

  /** Cierra la sesión y vuelve al login. Si la sesión venció, el login muestra un aviso. */
  logout(motivo: 'manual' | 'expirada' = 'manual'): void {
    this.limpiarSesion();
    void this.router.navigate(['/login'], {
      queryParams: motivo === 'expirada' ? { sesionExpirada: 1 } : {},
    });
  }

  /** Token vigente para adjuntar a las peticiones, o null si no hay sesión válida. */
  obtenerToken(): string | null {
    return this.sesionVigente() ? (this.sesion()?.token ?? null) : null;
  }

  /**
   * Comprueba que exista sesión, que el token no haya vencido y que el usuario tenga roles.
   * Si la sesión ya no sirve, la borra.
   */
  sesionVigente(): boolean {
    const sesion = this.sesion();

    if (!sesion) {
      return false;
    }

    const vigente = Date.parse(sesion.expiraEn) > Date.now() && sesion.roles.length > 0;

    if (!vigente) {
      this.limpiarSesion();
    }

    return vigente;
  }

  /** True si el usuario tiene al menos uno de los roles indicados (o si la lista está vacía). */
  tieneAlgunRol(rolesPermitidos: readonly RolCivix[]): boolean {
    return rolesPermitidos.length === 0 || this.roles().some((rol) => rolesPermitidos.includes(rol));
  }

  private guardarSesion(respuesta: AuthResponse): void {
    this.sesion.set(respuesta);

    try {
      localStorage.setItem(CLAVE_SESION, JSON.stringify(respuesta));
    } catch {
      // Navegación privada o almacenamiento lleno: la sesión vive solo en memoria.
    }
  }

  private limpiarSesion(): void {
    this.sesion.set(null);

    try {
      localStorage.removeItem(CLAVE_SESION);
    } catch {
      // Sin acceso a localStorage no hay nada que borrar.
    }
  }
}

/** Recupera la sesión guardada y descarta datos dañados o con forma inesperada. */
function leerSesionGuardada(): AuthResponse | null {
  try {
    const texto = localStorage.getItem(CLAVE_SESION);

    if (!texto) {
      return null;
    }

    const datos: unknown = JSON.parse(texto);

    if (esAuthResponse(datos)) {
      return datos;
    }
  } catch {
    // JSON inválido o localStorage no disponible.
  }

  return null;
}

function esAuthResponse(valor: unknown): valor is AuthResponse {
  if (valor === null || typeof valor !== 'object') {
    return false;
  }

  const datos = valor as Partial<Record<keyof AuthResponse, unknown>>;

  return (
    typeof datos.token === 'string' &&
    typeof datos.expiraEn === 'string' &&
    typeof datos.usuarioId === 'number' &&
    typeof datos.nombre === 'string' &&
    typeof datos.correo === 'string' &&
    Array.isArray(datos.roles)
  );
}
