import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { AsyncPipe } from '@angular/common';

import { environment } from '../../../environments/environment';
import { ContadorDirective } from '../../core/directives/contador.directive';
import { ImagenProtegidaPipe } from '../../core/pipes/imagen-protegida.pipe';
import { ROLES } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { claseEstadoObra } from '../../core/utils/estados';
import { obtenerMensajeError } from '../../core/utils/http-error';
import { formatearFechaCorta, formatearPesos } from '../cotizaciones/cotizaciones.service';
import { AnilloProgreso } from '../../shared/graficas/anillo-progreso';
import { BarraSegmentada } from '../../shared/graficas/barra-segmentada';
import { GraficaColumnas } from '../../shared/graficas/grafica-columnas';
import { GraficaDona } from '../../shared/graficas/grafica-dona';
import {
  COLORES_GRAFICA,
  PuntoGrafica,
  SegmentoGrafica,
} from '../../shared/graficas/graficas.models';
import { ResumenInicio } from './inicio.models';


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
  imports: [
    RouterLink,
    AsyncPipe,
    ContadorDirective,
    ImagenProtegidaPipe,
    AnilloProgreso,
    BarraSegmentada,
    GraficaColumnas,
    GraficaDona,
  ],
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

  /** Obras por estado, con un color por estado (dona e indicador). */
  readonly segmentosObras = computed<SegmentoGrafica[]>(() =>
    (this.resumen()?.obrasPorEstado ?? []).map((e) => ({
      etiqueta: e.estado,
      valor: e.cantidad,
      color: colorEstadoObra(e.estado),
    })),
  );

  readonly segmentosCotizaciones = computed<SegmentoGrafica[]>(() => {
    const c = this.resumen()?.cotizaciones;
    if (!c) {
      return [];
    }
    return [
      { etiqueta: 'En borrador', valor: c.borrador, color: COLORES_GRAFICA.amarillo },
      { etiqueta: 'Aprobadas', valor: c.aprobadas, color: COLORES_GRAFICA.verde },
      { etiqueta: 'Rechazadas', valor: c.rechazadas, color: COLORES_GRAFICA.rojo },
    ];
  });

  readonly segmentosMateriales = computed<SegmentoGrafica[]>(() => {
    const m = this.resumen()?.materiales;
    if (!m) {
      return [];
    }
    return [
      { etiqueta: 'Pedidos', valor: m.pedidos, color: COLORES_GRAFICA.pizarra },
      { etiqueta: 'En bodega', valor: m.enBodega, color: COLORES_GRAFICA.amarillo },
      { etiqueta: 'Retirados', valor: m.retirados, color: COLORES_GRAFICA.gris },
    ];
  });

  /** Avances registrados en cada una de las últimas semanas. */
  readonly avancesPorSemana = computed<PuntoGrafica[]>(() =>
    (this.resumen()?.avancesPorSemana ?? []).map((s) => {
      const lunes = new Date(s.semana);
      const mes = lunes.toLocaleDateString('es-CO', { month: 'short' }).replace('.', '').slice(0, 3);
      return {
        etiqueta: `${lunes.getDate()} ${mes}`,
        valor: s.cantidad,
        descripcion: `Semana del ${lunes.toLocaleDateString('es-CO', { day: 'numeric', month: 'long' })}`,
      };
    }),
  );

  readonly totalAvancesSemanas = computed(() =>
    this.avancesPorSemana().reduce((suma, p) => suma + p.valor, 0),
  );

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

}

/** Color de cada estado de obra en las gráficas (coincide con las etiquetas .cx-chip). */
function colorEstadoObra(estado: string): string {
  const clase = claseEstadoObra(estado);

  if (clase.includes('success')) {
    return COLORES_GRAFICA.verde;
  }
  if (clase.includes('danger')) {
    return COLORES_GRAFICA.rojo;
  }
  if (clase.includes('muted')) {
    return COLORES_GRAFICA.gris;
  }
  return COLORES_GRAFICA.pizarra;
}
