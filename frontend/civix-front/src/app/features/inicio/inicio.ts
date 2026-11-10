import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

import { environment } from '../../../environments/environment';
import { ROLES } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { obtenerMensajeError } from '../../core/utils/http-error';
import { formatearFechaCorta, formatearPesos } from '../cotizaciones/cotizaciones.service';
import { ResumenInicio } from './inicio.models';

/** Dirección del servidor sin "/api", para armar la URL de las fotos (/uploads/...). */
const URL_SERVIDOR = environment.apiUrl.replace(/\/api\/?$/, '');

/**
 * Pantalla de Inicio: resumen según el rol.
 * - Administrador: todo el sistema y las cotizaciones por aprobar.
 * - Ingeniero: sus obras, sus cotizaciones en borrador y los materiales de sus obras.
 * - Cliente: sus obras, el avance y las últimas fotos.
 */
@Component({
  selector: 'app-inicio',
  imports: [RouterLink],
  templateUrl: './inicio.html',
  styleUrl: './inicio.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Inicio {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  readonly resumen = signal<ResumenInicio | null>(null);
  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);

  readonly nombre = computed(() => this.auth.usuario()?.nombre.split(' ')[0] ?? '');
  readonly esAdministrador = computed(() => this.auth.roles().includes(ROLES.administrador));
  readonly esCliente = computed(() => this.auth.esSoloLectura());

  readonly saludo = computed(() => {
    const hora = new Date().getHours();
    if (hora < 12) {
      return 'Buenos días';
    }
    return hora < 19 ? 'Buenas tardes' : 'Buenas noches';
  });

  readonly fechaHoy = new Date().toLocaleDateString('es-CO', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });

  readonly subtitulo = computed(() => {
    if (this.esAdministrador()) {
      return 'Este es el estado general de todas las obras de CIVIX.';
    }
    if (this.esCliente()) {
      return 'Así van tus obras.';
    }
    return 'Este es el resumen de las obras que tienes asignadas.';
  });

  readonly formatearPesos = formatearPesos;
  readonly formatearFechaCorta = formatearFechaCorta;

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);

    this.http.get<ResumenInicio>(`${environment.apiUrl}/inicio`).subscribe({
      next: (resumen) => {
        this.resumen.set(resumen);
        this.cargando.set(false);
      },
      error: (error: unknown) => {
        this.error.set(obtenerMensajeError(error, 'No se pudo cargar el resumen.'));
        this.cargando.set(false);
      },
    });
  }

  urlImagen(ruta: string): string {
    return ruta.startsWith('http') ? ruta : `${URL_SERVIDOR}${ruta}`;
  }

  claseEstadoObra(estado: string): string {
    const valor = estado.toLowerCase();
    if (valor.includes('ejecuci')) {
      return 'chip chip-ejecucion';
    }
    if (valor.includes('finaliz')) {
      return 'chip chip-finalizada';
    }
    if (valor.includes('suspend')) {
      return 'chip chip-suspendida';
    }
    return 'chip chip-planeacion';
  }
}
