import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Avance, AvancePayload } from './avance.model';

const API_URL = 'http://localhost:5067/api/avances';

@Injectable({ providedIn: 'root' })
export class AvancesService {
  private readonly http = inject(HttpClient);

  listar(obraId?: number): Observable<Avance[]> {
    const url = obraId ? `${API_URL}?obraId=${obraId}` : API_URL;
    return this.http.get<Avance[]>(url);
  }

  obtener(id: number): Observable<Avance> {
    return this.http.get<Avance>(`${API_URL}/${id}`);
  }

  crear(avance: AvancePayload): Observable<Avance> {
    return this.http.post<Avance>(API_URL, avance);
  }

  actualizar(id: number, avance: AvancePayload): Observable<string> {
    return this.http.put(`${API_URL}/${id}`, avance, { responseType: 'text' });
  }

  eliminar(id: number): Observable<string> {
    return this.http.delete(`${API_URL}/${id}`, { responseType: 'text' });
  }

  subirImagen(archivo: File): Observable<{ url: string }> {
    const form = new FormData();
    form.append('archivo', archivo);
    return this.http.post<{ url: string }>(`${API_URL}/imagenes`, form);
  }
}
