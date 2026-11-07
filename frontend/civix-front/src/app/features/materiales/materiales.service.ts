import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ActualizarObraMaterialPayload,
  CotizacionOpcion,
  Material,
  MaterialPayload,
  ObraMaterial,
  ObraOpcion,
  RegistrarObraMaterialPayload,
  RegistrarRetiroPayload,
  RespuestaGuardado,
  RetiroMaterial,
} from './materiales.models';

/** Acceso al API de Materiales: catálogo, materiales por obra y retiros. */
@Injectable({ providedIn: 'root' })
export class MaterialesService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  // ---------- Selectores ----------

  listarObras(): Observable<ObraOpcion[]> {
    return this.http.get<ObraOpcion[]>(`${this.api}/catalogos/obras`);
  }

  listarCotizaciones(): Observable<CotizacionOpcion[]> {
    return this.http.get<CotizacionOpcion[]>(`${this.api}/catalogos/cotizaciones`);
  }

  // ---------- Catálogo de materiales ----------

  listarCatalogo(): Observable<Material[]> {
    return this.http.get<Material[]>(`${this.api}/materiales`);
  }

  crearMaterial(datos: MaterialPayload): Observable<Material> {
    return this.http
      .post<RespuestaGuardado<Material>>(`${this.api}/materiales`, datos)
      .pipe(map((r) => r.datos));
  }

  actualizarMaterial(id: number, datos: MaterialPayload): Observable<Material> {
    return this.http
      .put<RespuestaGuardado<Material>>(`${this.api}/materiales/${id}`, datos)
      .pipe(map((r) => r.datos));
  }

  desactivarMaterial(id: number): Observable<void> {
    return this.http.delete(`${this.api}/materiales/${id}`).pipe(map(() => undefined));
  }

  // ---------- Materiales por obra (RF-006 / RF-007) ----------

  listarPorObra(obraId: number): Observable<ObraMaterial[]> {
    return this.http.get<ObraMaterial[]>(`${this.api}/obras/${obraId}/materiales`);
  }

  registrarEnObra(obraId: number, datos: RegistrarObraMaterialPayload): Observable<ObraMaterial> {
    return this.http
      .post<RespuestaGuardado<ObraMaterial>>(`${this.api}/obras/${obraId}/materiales`, datos)
      .pipe(map((r) => r.datos));
  }

  actualizarEnObra(
    obraId: number,
    id: number,
    datos: ActualizarObraMaterialPayload,
  ): Observable<ObraMaterial> {
    return this.http
      .put<RespuestaGuardado<ObraMaterial>>(`${this.api}/obras/${obraId}/materiales/${id}`, datos)
      .pipe(map((r) => r.datos));
  }

  eliminarDeObra(obraId: number, id: number): Observable<void> {
    return this.http
      .delete(`${this.api}/obras/${obraId}/materiales/${id}`)
      .pipe(map(() => undefined));
  }

  // ---------- Retiros ----------

  listarRetiros(obraId: number): Observable<RetiroMaterial[]> {
    const params = new HttpParams().set('obraId', obraId);
    return this.http.get<RetiroMaterial[]>(`${this.api}/retiros-material`, { params });
  }

  registrarRetiro(datos: RegistrarRetiroPayload): Observable<ObraMaterial> {
    return this.http
      .post<RespuestaGuardado<ObraMaterial>>(`${this.api}/retiros-material`, datos)
      .pipe(map((r) => r.datos));
  }
}

/** Formatea cantidades con hasta 3 decimales al estilo colombiano (1.234,5). */
export function formatearCantidad(valor: number): string {
  return valor.toLocaleString('es-CO', { maximumFractionDigits: 3 });
}

/** Formatea pesos colombianos. */
export function formatearMoneda(valor: number): string {
  return valor.toLocaleString('es-CO', {
    style: 'currency',
    currency: 'COP',
    maximumFractionDigits: 0,
  });
}

/** Formatea una fecha ISO como dd/mm/aaaa hh:mm. */
export function formatearFecha(iso: string, conHora = true): string {
  const fecha = new Date(iso);

  if (Number.isNaN(fecha.getTime())) {
    return iso;
  }

  return fecha.toLocaleString('es-CO', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    ...(conHora ? { hour: '2-digit', minute: '2-digit' } : {}),
  });
}
