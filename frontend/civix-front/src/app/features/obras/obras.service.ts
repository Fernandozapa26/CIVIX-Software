import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Obra, ObraPayload } from './obra.model';

const API_URL = 'http://localhost:5067/api/obras';

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

  actualizar(id: number, obra: ObraPayload): Observable<string> {
    return this.http.put(`${API_URL}/${id}`, obra, { responseType: 'text' });
  }

  eliminar(id: number): Observable<string> {
    return this.http.delete(`${API_URL}/${id}`, { responseType: 'text' });
  }
}