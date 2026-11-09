import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Obra, ObraPayload, UsuarioOpcion } from './obra.model';

const API_URL = `${environment.apiUrl}/obras`;

@Injectable({ providedIn: 'root' })
export class ObrasService {
  private readonly http = inject(HttpClient);

  listar(): Observable<Obra[]> {
    return this.http.get<Obra[]>(API_URL);
  }

  obtener(id: number): Observable<Obra> {
    return this.http.get<Obra>(`${API_URL}/${id}`);
  }

  crear(obra: ObraPayload): Observable<Obra> {
    return this.http.post<Obra>(API_URL, obra);
  }

  actualizar(id: number, obra: ObraPayload): Observable<Obra> {
    return this.http.put<Obra>(`${API_URL}/${id}`, obra);
  }

  eliminar(id: number): Observable<string> {
    return this.http.delete(`${API_URL}/${id}`, { responseType: 'text' });
  }

  /** Usuarios activos con rol Cliente o Ingeniero (selectores del formulario). */
  listarUsuarios(rol: 'Cliente' | 'Ingeniero'): Observable<UsuarioOpcion[]> {
    const params = new HttpParams().set('rol', rol);
    return this.http.get<UsuarioOpcion[]>(`${environment.apiUrl}/catalogos/usuarios`, { params });
  }
}
