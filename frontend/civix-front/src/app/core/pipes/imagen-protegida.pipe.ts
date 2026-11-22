import { Pipe, PipeTransform, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ImagenesProtegidasService } from '../services/imagenes-protegidas.service';

/**
 * Convierte la ruta guardada de una foto ("/uploads/avances/...") en una URL que se
 * puede mostrar, descargándola con el token de la sesión.
 *
 * Uso: <img [attr.src]="ruta | imagenProtegida | async" />
 * (se usa [attr.src] para que, mientras carga, no se ponga un src vacío).
 */
@Pipe({ name: 'imagenProtegida' })
export class ImagenProtegidaPipe implements PipeTransform {
  private readonly imagenes = inject(ImagenesProtegidasService);

  transform(ruta: string | null | undefined): Observable<string | null> {
    return this.imagenes.obtenerUrl(ruta);
  }
}
