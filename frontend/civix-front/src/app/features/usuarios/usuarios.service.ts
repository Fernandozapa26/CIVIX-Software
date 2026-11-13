import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { RegisterRequest, RolCivix } from '../../core/models/auth.models';

/** Usuario tal como lo devuelve GET /api/usuarios (solo Administrador). */
export interface UsuarioAdmin {
  id: number;
  nombre: string;
  correo: string;
  rolId: number | null;
  roles: RolCivix[];
  activo: boolean;
  fechaCreacion: string;
  obrasAsignadas: number;
}

/** Cuerpo de PUT /api/usuarios/{id}. password null = conservar la actual. */
export interface ActualizarUsuarioRequest {
  nombre: string;
  correo: string;
  rolId: number;
  activo: boolean;
  password: string | null;
}

/**
 * Administración de usuarios:
 * - Listar, editar, desactivar y reactivar: /api/usuarios (solo Administrador).
 * - Crear: POST /api/auth/register con el token del Administrador (la API respeta el rolId).
 *   Se llama directamente (no con AuthService.register) para NO reemplazar la sesión del
 *   Administrador por la del usuario recién creado.
 */
@Injectable({ providedIn: 'root' })
export class UsuariosService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/usuarios`;

  listar(): Observable<UsuarioAdmin[]> {
    return this.http.get<UsuarioAdmin[]>(this.api);
  }

  crear(datos: RegisterRequest): Observable<void> {
    return this.http
      .post<unknown>(`${environment.apiUrl}/auth/register`, datos)
      .pipe(map(() => undefined));
  }

  actualizar(id: number, datos: ActualizarUsuarioRequest): Observable<UsuarioAdmin> {
    return this.http.put<UsuarioAdmin>(`${this.api}/${id}`, datos);
  }

  desactivar(id: number): Observable<UsuarioAdmin> {
    return this.http.delete<UsuarioAdmin>(`${this.api}/${id}`);
  }

  reactivar(id: number): Observable<UsuarioAdmin> {
    return this.http.post<UsuarioAdmin>(`${this.api}/${id}/reactivar`, {});
  }
}
