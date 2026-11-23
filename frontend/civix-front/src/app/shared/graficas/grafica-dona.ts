import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { SegmentoGrafica } from './graficas.models';

const RADIO = 15.9155; // circunferencia ≈ 100: cada unidad del trazo equivale a 1 %
const SEPARACION = 0.8; // hueco entre segmentos, en %

interface ArcoDona extends SegmentoGrafica {
  porcentaje: number;
  trazo: string;
  desfase: number;
}

/**
 * Gráfica de dona con leyenda.
 *
 * Uso: <cx-dona [segmentos]="segmentos" etiquetaTotal="cotizaciones" descripcion="…" />
 *
 * El total va en el centro y cada segmento muestra su cantidad y porcentaje en la leyenda.
 * Si todos los valores son 0, muestra un anillo vacío con el texto de `textoVacio`.
 */
@Component({
  selector: 'cx-dona',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'cx-dona' },
  template: `
    <div class="dona-cuerpo">
      <figure class="dona" role="img" [attr.aria-label]="textoAccesible()">
        <svg viewBox="0 0 42 42" aria-hidden="true">
          <circle class="dona__pista" cx="21" cy="21" [attr.r]="radio" />
          @for (arco of arcos(); track arco.etiqueta; let i = $index) {
            <circle
              class="dona__arco"
              cx="21"
              cy="21"
              [attr.r]="radio"
              [style.stroke]="arco.color"
              [attr.stroke-dasharray]="arco.trazo"
              [attr.stroke-dashoffset]="arco.desfase"
              [style.animation-delay.ms]="i * 120"
            />
          }
        </svg>
        <figcaption class="dona__centro" aria-hidden="true">
          <strong>{{ total() }}</strong>
          <span>{{ etiquetaTotal() }}</span>
        </figcaption>
      </figure>

      @if (total() > 0) {
        <ul class="cx-leyenda dona__leyenda">
          @for (arco of arcos(); track arco.etiqueta) {
            <li>
              <span class="cx-leyenda__punto" [style.--color]="arco.color" aria-hidden="true"></span>
              {{ arco.etiqueta }}
              <strong>{{ arco.valor }}</strong>
              <span class="dona__pct">{{ arco.porcentaje }}%</span>
            </li>
          }
        </ul>
      } @else {
        <p class="dona__vacio">{{ textoVacio() }}</p>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
      container-type: inline-size;
    }

    .dona-cuerpo {
      display: flex;
      align-items: center;
      gap: 1.5rem;
    }

    .dona {
      position: relative;
      flex-shrink: 0;
      width: var(--cx-dona-tamano, 148px);
      margin: 0;
    }

    svg {
      display: block;
      width: 100%;
      transform: rotate(-90deg);
    }

    circle {
      fill: none;
      stroke-width: 5.5;
    }

    .dona__pista {
      stroke: var(--cx-gris-suave);
    }

    .dona__arco {
      animation: dona-entrar 700ms cubic-bezier(0.2, 0, 0, 1) both;
    }

    .dona__centro {
      position: absolute;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      line-height: 1.1;

      strong {
        color: var(--cx-petroleo);
        font-family: var(--cx-fuente-mono);
        font-size: 1.75rem;
        font-weight: 600;
        letter-spacing: -0.04em;
      }

      span {
        max-width: 70%;
        color: var(--cx-texto-tenue);
        font-size: 0.7rem;
        font-weight: 600;
        text-align: center;
      }
    }

    .dona__leyenda {
      flex-direction: column;
      gap: 0.6rem;
      min-width: 0;

      li {
        display: grid;
        grid-template-columns: auto 1fr auto auto;
        gap: 0.5rem;
      }
    }

    .dona__pct {
      min-width: 2.6rem;
      color: var(--cx-texto-secundario);
      font-family: var(--cx-fuente-mono);
      font-size: 0.74rem;
      text-align: right;
    }

    .dona__vacio {
      margin: 0;
      color: var(--cx-texto-tenue);
      font-size: 0.85rem;
    }

    @keyframes dona-entrar {
      from {
        opacity: 0;
        stroke-dasharray: 0 100;
      }
    }

    /* En tarjetas angostas la leyenda pasa debajo de la dona */
    @container (max-width: 360px) {
      .dona-cuerpo {
        flex-direction: column;
        align-items: stretch;
        gap: 1.1rem;
      }

      .dona {
        align-self: center;
      }
    }
  `,
})
export class GraficaDona {
  readonly segmentos = input.required<SegmentoGrafica[]>();
  /** Texto bajo el total, ej. "obras". */
  readonly etiquetaTotal = input('');
  readonly textoVacio = input('Sin datos para mostrar.');

  protected readonly radio = RADIO;

  protected readonly total = computed(() =>
    this.segmentos().reduce((suma, s) => suma + Math.max(0, s.valor), 0),
  );

  protected readonly arcos = computed<ArcoDona[]>(() => {
    const total = this.total();
    const visibles = this.segmentos().filter((s) => s.valor > 0);
    const hueco = visibles.length > 1 ? SEPARACION : 0;
    let acumulado = 0;

    return this.segmentos().map((segmento) => {
      const porcentaje = total > 0 ? (Math.max(0, segmento.valor) / total) * 100 : 0;
      const largo = Math.max(0, porcentaje - hueco);
      const arco: ArcoDona = {
        ...segmento,
        porcentaje: Math.round(porcentaje),
        trazo: `${largo} ${100 - largo}`,
        // stroke-dashoffset positivo retrocede el trazo: se usa negativo para avanzar
        desfase: -acumulado,
      };
      acumulado += porcentaje;
      return arco;
    });
  });

  protected readonly textoAccesible = computed(() => {
    if (this.total() === 0) {
      return this.textoVacio();
    }
    const partes = this.arcos().map((a) => `${a.etiqueta}: ${a.valor} (${a.porcentaje}%)`);
    return `Total ${this.total()} ${this.etiquetaTotal()}. ${partes.join(', ')}.`;
  });
}
