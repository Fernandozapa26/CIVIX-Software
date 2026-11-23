import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { SegmentoGrafica } from './graficas.models';

/**
 * Mini-gráfica de una sola barra dividida en partes (para los indicadores).
 *
 * Uso: <cx-barra-segmentada [segmentos]="partes" descripcion="Obras por estado" />
 *
 * Cada parte ocupa el ancho proporcional a su valor. Sin datos, se ve la pista vacía.
 */
@Component({
  selector: 'cx-barra-segmentada',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'cx-barra-segmentada',
    role: 'img',
    '[attr.aria-label]': 'textoAccesible()',
  },
  template: `
    @for (parte of partes(); track parte.etiqueta; let i = $index) {
      <span
        class="parte"
        [style.width.%]="parte.ancho"
        [style.background]="parte.color"
        [style.animation-delay.ms]="i * 90"
        [title]="parte.etiqueta + ': ' + parte.valor"
      ></span>
    }
  `,
  styles: `
    :host {
      display: flex;
      gap: 2px;
      height: 6px;
      overflow: hidden;
      border-radius: 999px;
      background: var(--cx-gris-suave);
    }

    .parte {
      display: block;
      height: 100%;
      transform-origin: left center;
      animation: parte-llenar 700ms cubic-bezier(0.2, 0, 0, 1) both;
    }

    @keyframes parte-llenar {
      from {
        transform: scaleX(0);
      }
    }
  `,
})
export class BarraSegmentada {
  readonly segmentos = input.required<SegmentoGrafica[]>();
  readonly descripcion = input('');

  private readonly total = computed(() =>
    this.segmentos().reduce((suma, s) => suma + Math.max(0, s.valor), 0),
  );

  protected readonly partes = computed(() => {
    const total = this.total();
    return this.segmentos()
      .filter((s) => s.valor > 0)
      .map((s) => ({ ...s, ancho: total > 0 ? (s.valor / total) * 100 : 0 }));
  });

  protected readonly textoAccesible = computed(() => {
    const detalle = this.segmentos()
      .map((s) => `${s.etiqueta}: ${s.valor}`)
      .join(', ');
    return `${this.descripcion()}. ${detalle}.`;
  });
}
