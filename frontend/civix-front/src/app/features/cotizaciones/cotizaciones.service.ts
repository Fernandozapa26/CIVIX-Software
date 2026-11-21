import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AprobarCotizacionPayload,
  CotizacionDetalle,
  CotizacionResumen,
  EstadoCotizacion,
  GuardarCotizacionPayload,
  RespuestaCotizacion,
} from './cotizaciones.models';

/** Acceso al API de Cotizaciones. */
@Injectable({ providedIn: 'root' })
export class CotizacionesService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/cotizaciones`;

  /** Sin estado: todas menos las anuladas. */
  listar(estado: EstadoCotizacion | null): Observable<CotizacionResumen[]> {
    const params = estado ? new HttpParams().set('estado', estado) : undefined;
    return this.http.get<CotizacionResumen[]>(this.api, { params });
  }

  obtener(id: number): Observable<CotizacionDetalle> {
    return this.http.get<CotizacionDetalle>(`${this.api}/${id}`);
  }

  crear(datos: GuardarCotizacionPayload): Observable<CotizacionDetalle> {
    return this.http.post<RespuestaCotizacion>(this.api, datos).pipe(map((r) => r.datos));
  }

  actualizar(id: number, datos: GuardarCotizacionPayload): Observable<CotizacionDetalle> {
    return this.http.put<RespuestaCotizacion>(`${this.api}/${id}`, datos).pipe(map((r) => r.datos));
  }

  /** Borrado lógico: la cotización pasa a "Anulada". */
  anular(id: number): Observable<CotizacionDetalle> {
    return this.http.delete<RespuestaCotizacion>(`${this.api}/${id}`).pipe(map((r) => r.datos));
  }

  /** Solo Administrador. Crea la obra automáticamente. */
  aprobar(id: number, datos: AprobarCotizacionPayload): Observable<CotizacionDetalle> {
    return this.http
      .post<RespuestaCotizacion>(`${this.api}/${id}/aprobar`, datos)
      .pipe(map((r) => r.datos));
  }

  /** Solo Administrador. */
  rechazar(id: number): Observable<CotizacionDetalle> {
    return this.http
      .post<RespuestaCotizacion>(`${this.api}/${id}/rechazar`, {})
      .pipe(map((r) => r.datos));
  }
}

/** Pesos colombianos sin decimales: $ 1.234.567 */
export function formatearPesos(valor: number): string {
  return valor.toLocaleString('es-CO', {
    style: 'currency',
    currency: 'COP',
    maximumFractionDigits: 0,
  });
}

/** Fecha ISO (aaaa-mm-dd o con hora) como dd/mm/aaaa. */
export function formatearFechaCorta(iso: string | null): string {
  if (!iso) {
    return '';
  }

  const [anio, mes, dia] = iso.slice(0, 10).split('-');
  return `${dia}/${mes}/${anio}`;
}
