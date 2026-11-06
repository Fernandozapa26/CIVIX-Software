/** Roles tal como existen en dbo.Roles y viajan en el token. */
export type RolCivix = 'Administrador' | 'Ingeniero' | 'Cliente';

export const ROLES = {
  administrador: 'Administrador',
  ingeniero: 'Ingeniero',
  cliente: 'Cliente',
} as const satisfies Record<string, RolCivix>;

/** Grupos de roles reutilizables en la configuración de rutas. */
export const ROLES_GESTION: readonly RolCivix[] = [ROLES.administrador, ROLES.ingeniero];
export const ROLES_TODOS: readonly RolCivix[] = [ROLES.administrador, ROLES.ingeniero, ROLES.cliente];

/** Cuerpo de POST /api/auth/login (equivale a LoginDto en el backend). */
export interface LoginRequest {
  correo: string;
  password: string;
}

/** Cuerpo de POST /api/auth/register (equivale a RegisterDto en el backend). */
export interface RegisterRequest {
  nombre: string;
  correo: string;
  password: string;
  rolId?: number;
}

/** Respuesta de login y registro (equivale a AuthResponseDto en el backend). */
export interface AuthResponse {
  token: string;
  /** Fecha ISO en UTC en la que vence el token. */
  expiraEn: string;
  usuarioId: number;
  nombre: string;
  correo: string;
  roles: RolCivix[];
}

/** Datos del usuario que las pantallas necesitan (sin el token). */
export interface UsuarioSesion {
  usuarioId: number;
  nombre: string;
  correo: string;
  roles: RolCivix[];
}
