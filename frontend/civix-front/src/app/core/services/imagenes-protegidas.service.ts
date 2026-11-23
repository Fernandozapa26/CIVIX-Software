import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, shareReplay } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

/**
 * Descarga las fotos de los avances desde GET /api/avances/imagenes/{nombre}.
 *
 * Las fotos ya no son públicas: la API exige el token. Una etiqueta <img src="...">
 * no puede enviar el encabezado Authorization, así que la foto se pide con HttpClient
 * (el interceptor agrega el token) y se muestra con una URL local "blob:".
 *
 * Las URLs se guardan en caché por sesión: al cambiar de usuario se liberan.
 */
@Injectable({ providedIn: 'root' })
export class ImagenesProtegidasService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  private readonly cache = new Map<string, Observable<string | null>>();
  private readonly urlsCreadas: string[] = [];
  private tokenDeLaCache: string | null = null;

  /** URL lista para usar en src/href, o null mientras carga o si no se pudo obtener. */
  obtenerUrl(ruta: string | null | undefined): Observable<string | null> {
    if (!ruta) {
      return of(null);
    }

    const token = this.authService.obtenerToken();
    if (token !== this.tokenDeLaCache) {
      this.limpiar();
      this.tokenDeLaCache = token;
    }

    let url$ = this.cache.get(ruta);
    if (!url$) {
      url$ = this.http.get(this.urlApi(ruta), { responseType: 'blob' }).pipe(
        map((blob) => {
          const url = URL.createObjectURL(blob);
          this.urlsCreadas.push(url);
          return url;
        }),
        catchError(() => {
          // No se guarda el fallo: la próxima vez se vuelve a intentar.
          this.cache.delete(ruta);
          return of(null);
        }),
        shareReplay(1),
      );
      this.cache.set(ruta, url$);
    }

    return url$;
  }

  /** "/uploads/avances/{nombre}" (lo que guarda la base) → endpoint protegido de la API. */
  private urlApi(ruta: string): string {
    const nombre = ruta.split('/').pop() ?? '';
    return `${environment.apiUrl}/avances/imagenes/${encodeURIComponent(nombre)}`;
  }

  private limpiar(): void {
    this.urlsCreadas.forEach((url) => URL.revokeObjectURL(url));
    this.urlsCreadas.length = 0;
    this.cache.clear();
  }
}
