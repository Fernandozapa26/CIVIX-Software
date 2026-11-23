import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { PuntoGrafica } from './graficas.models';

/**
 * Gráfica de columnas sobre una cuadrícula de plano.
 *
 * Uso: <cx-columnas [puntos]="avancesPorSemana" titulo="Avances por semana" />
 *
 * - La última columna (el periodo actual) se resalta en amarillo.
 * - Cada columna muestra su valor encima y su etiqueta debajo.
 * - Las columnas crecen desde abajo al aparecer (se omite con "movimiento reducido").
 */
@Component({
  selector: 'cx-columnas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'cx-columnas' },
  template: `
    <figure class="columnas" role="img" [attr.aria-label]="textoAccesible()">
      <div class="columnas__area" aria-hidden="true">
        @for (punto of puntos(); track punto.etiqueta; let i = $index; let ultimo = $last) {
          <div class="columnas__item" [title]="punto.descripcion">
            <span class="columnas__valor" [class.columnas__valor--cero]="punto.valor === 0">
              {{ punto.valor }}
            </span>
            <span
              class="columnas__barra"
              [class.columnas__barra--actual]="ultimo"
              [style.height.%]="altura(punto.valor)"
              [style.animation-delay.ms]="i * 60"
            ></span>
          </div>
        }
      </div>
      <div class="columnas__ejes" aria-hidden="true">
        @for (punto of puntos(); track punto.etiqueta; let ultimo = $last) {
          <span [class.columnas__eje--actual]="ultimo">{{ punto.etiqueta }}</span>
        }
      </div>
    </figure>
  `,
  styles: `
    :host {
      display: block;
    }

    .columnas {
      margin: 0;
    }

    .columnas__area {
      display: flex;
      align-items: flex-end;
      gap: 0.6rem;
      height: var(--cx-columnas-alto, 170px);
      padding: 0 0.25rem;
      border-bottom: 1px solid var(--cx-borde-fuerte);
      // Líneas guía horizontales cada 25 %
      background-image: linear-gradient(var(--cx-borde) 1px, transparent 1px);
      background-size: 100% 25%;
    }

    .columnas__item {
      display: flex;
      flex: 1;
      flex-direction: column;
      align-items: center;
      justify-content: flex-end;
      height: 100%;
      min-width: 0;
    }

    .columnas__valor {
      margin-bottom: 0.3rem;
      color: var(--cx-petroleo);
      font-family: var(--cx-fuente-mono);
      font-size: 0.75rem;
      font-weight: 600;
    }

    .columnas__valor--cero {
      color: var(--cx-texto-secundario);
      font-weight: 400;
    }

    .columnas__barra {
      display: block;
      width: min(100%, 34px);
      min-height: 3px;
      border-radius: 5px 5px 0 0;
      background: var(--cx-pizarra);
      opacity: 0.85;
      transform-origin: bottom;
      animation: columnas-crecer 700ms cubic-bezier(0.2, 0, 0, 1) both;
    }

    .columnas__barra--actual {
      background: var(--cx-amarillo);
      opacity: 1;
    }

    .columnas__ejes {
      display: flex;
      gap: 0.6rem;
      padding: 0.45rem 0.25rem 0;

      span {
        flex: 1;
        min-width: 0;
        color: var(--cx-texto-secundario);
        font-family: var(--cx-fuente-mono);
        font-size: 0.66rem;
        line-height: 1.25;
        text-align: center;
        /* En columnas angostas el mes baja a una segunda línea */
        overflow-wrap: anywhere;
      }

      .columnas__eje--actual {
        color: var(--cx-amarillo-texto);
        font-weight: 600;
      }
    }

    @keyframes columnas-crecer {
      from {
        transform: scaleY(0);
      }
    }
  `,
})
export class GraficaColumnas {
  readonly puntos = input.required<PuntoGrafica[]>();
  /** Nombre de la serie para lectores de pantalla, ej. "Avances registrados por semana". */
  readonly titulo = input('');

  private readonly maximo = computed(() => Math.max(1, ...this.puntos().map((p) => p.valor)));

  protected altura(valor: number): number {
    // Se deja un 18 % libre arriba para el número de la columna más alta
    return (Math.max(0, valor) / this.maximo()) * 82;
  }

  protected readonly textoAccesible = computed(() => {
    const detalle = this.puntos()
      .map((p) => `${p.descripcion}: ${p.valor}`)
      .join(', ');
    return `${this.titulo()}. ${detalle}.`;
  });
}
