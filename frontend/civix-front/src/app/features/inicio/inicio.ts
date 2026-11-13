import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

import { environment } from '../../../environments/environment';
import { ContadorDirective } from '../../core/directives/contador.directive';
import { ROLES } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { claseEstadoObra } from '../../core/utils/estados';
import { obtenerMensajeError } from '../../core/utils/http-error';
import { formatearFechaCorta, formatearPesos } from '../cotizaciones/cotizaciones.service';
import { ResumenInicio } from './inicio.models';

/** Dirección del servidor sin "/api", para armar la URL de las fotos (/uploads/...). */
const URL_SERVIDOR = environment.apiUrl.replace(/\/api\/?$/, '');

/** Elemento de la línea de "Actividad reciente" (avances y cotizaciones reales). */
interface Actividad {
  clave: string;
  tipo: 'avance' | 'cotizacion';
  fecha: string;
  titulo: string;
  detalle: string;
  extra: string | null;
  imagen: string | null;
  enlace: string;
}

/**
 * Pantalla de Inicio: resumen según el rol.
 * - Administrador: todo el sistema y las cotizaciones por aprobar.
 * - Ingeniero: sus obras, sus cotizaciones en borrador y los materiales de sus obras.
 * - Cliente: sus obras, el avance y las últimas fotos.
 */
@Component({
  selector: 'app-inicio',
  imports: [RouterLink, ContadorDirective],
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

  /** Obras en ejecución según el conteo por estado que entrega la API. */
  readonly obrasEnEjecucion = computed(() => {
    const r = this.resumen();
    if (!r) {
      return 0;
    }
    return r.obrasPorEstado
      .filter((e) => e.estado.toLowerCase().includes('ejecuci'))
      .reduce((total, e) => total + e.cantidad, 0);
  });

  /** Frase corta con el estado general, armada solo con datos reales. */
  readonly estadoGeneral = computed(() => {
    const r = this.resumen();
    if (!r) {
      return '';
    }

    const partes = [
      `${r.obrasTotal} ${r.obrasTotal === 1 ? 'obra' : 'obras'}`,
      `${this.obrasEnEjecucion()} en ejecución`,
    ];

    if (r.cotizaciones && r.cotizaciones.borrador > 0) {
      partes.push(
        `${r.cotizaciones.borrador} ${r.cotizaciones.borrador === 1 ? 'cotización' : 'cotizaciones'} ${
          this.esAdministrador() ? 'por aprobar' : 'en borrador'
        }`,
      );
    }

    return partes.join(' · ');
  });

  /** Avances y cotizaciones pendientes ordenados del más reciente al más antiguo. */
  readonly actividad = computed<Actividad[]>(() => {
    const r = this.resumen();
    if (!r) {
      return [];
    }

    const avances: Actividad[] = r.avancesRecientes.map((avance) => ({
      clave: `avance-${avance.id}`,
      tipo: 'avance',
      fecha: avance.fecha,
      titulo: avance.obraNombre,
      detalle: avance.descripcion,
      extra: avance.porcentajeAvance !== null ? `${avance.porcentajeAvance}% de avance` : null,
      imagen: avance.imagen,
      enlace: '/app/avances',
    }));

    const cotizaciones: Actividad[] = (r.cotizaciones?.pendientes ?? []).map((c) => ({
      clave: `cotizacion-${c.id}`,
      tipo: 'cotizacion',
      fecha: c.fecha,
      titulo: `Cotización ${c.numero}`,
      detalle: c.nombreProyecto,
      extra: formatearPesos(c.total),
      imagen: null,
      enlace: '/app/cotizaciones',
    }));

    return [...avances, ...cotizaciones]
      .sort((a, b) => (a.fecha < b.fecha ? 1 : a.fecha > b.fecha ? -1 : 0))
      .slice(0, 8);
  });

  readonly formatearPesos = formatearPesos;
  readonly formatearFechaCorta = formatearFechaCorta;
  readonly claseEstadoObra = claseEstadoObra;

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

  /** Porcentaje (0-100) de una parte respecto al total, para las barras del resumen. */
  porcentaje(parte: number, total: number): number {
    return total > 0 ? Math.round((parte / total) * 100) : 0;
  }
}
