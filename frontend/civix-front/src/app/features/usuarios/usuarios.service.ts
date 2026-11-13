import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { RegisterRequest } from '../../core/models/auth.models';

/**
 * Operaciones de administración de usuarios con los endpoints que ya existen en la API:
 * - Crear: POST /api/auth/register. Con el token de un Administrador, la API respeta el rolId.
 *   Se llama directamente (no con AuthService.register) para NO reemplazar la sesión del
 *   Administrador por la del usuario recién creado.
 */
@Injectable({ providedIn: 'root' })
export class UsuariosService {
  private readonly http = inject(HttpClient);

  crear(datos: RegisterRequest): Observable<void> {
    return this.http
      .post<unknown>(`${environment.apiUrl}/auth/register`, datos)
      .pipe(map(() => undefined));
  }
}
