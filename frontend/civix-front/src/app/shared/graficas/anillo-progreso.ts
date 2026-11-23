import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Radio del círculo dentro de un viewBox de 40 × 40. */
const RADIO = 16;
const CIRCUNFERENCIA = 2 * Math.PI * RADIO;

/**
 * Anillo de progreso (0 a 100 %).
 *
 * Uso: <cx-anillo [valor]="obra.avance" [tamano]="56" etiqueta="Avance de Torre 1" />
 *
 * - Amarillo mientras avanza y verde al llegar a 100 %.
 * - Se llena con una animación al aparecer (se omite con "movimiento reducido").
 * - Para lectores de pantalla es un progressbar con su valor.
 */
@Component({
  selector: 'cx-anillo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'cx-anillo',
    role: 'progressbar',
    'aria-valuemin': '0',
    'aria-valuemax': '100',
    '[attr.aria-valuenow]': 'porcentaje()',
    '[attr.aria-label]': 'etiqueta() || null',
    '[class.cx-anillo--completo]': 'porcentaje() >= 100',
    '[style.--tamano.px]': 'tamano()',
  },
  template: `
    <svg viewBox="0 0 40 40" aria-hidden="true">
      <circle class="cx-anillo__pista" cx="20" cy="20" [attr.r]="radio" />
      <circle
        class="cx-anillo__valor"
        cx="20"
        cy="20"
        [attr.r]="radio"
        [attr.stroke-dasharray]="circunferencia"
        [attr.stroke-dashoffset]="desfase()"
      />
    </svg>
    @if (mostrarValor()) {
      <span class="cx-anillo__texto" aria-hidden="true">{{ porcentaje() }}<small>%</small></span>
    }
  `,
  styles: `
    :host {
      position: relative;
      display: inline-grid;
      flex-shrink: 0;
      place-items: center;
      width: var(--tamano);
      height: var(--tamano);
    }

    svg {
      width: 100%;
      height: 100%;
      transform: rotate(-90deg);
    }

    circle {
      fill: none;
      stroke-width: 4.5;
    }

    .cx-anillo__pista {
      stroke: var(--cx-gris-suave);
    }

    .cx-anillo__valor {
      stroke: var(--cx-amarillo);
      stroke-linecap: round;
      animation: cx-anillo-llenar 1s cubic-bezier(0.2, 0, 0, 1) both;
      transition: stroke-dashoffset 600ms cubic-bezier(0.2, 0, 0, 1);
    }

    :host(.cx-anillo--completo) .cx-anillo__valor {
      stroke: var(--cx-verde);
    }

    .cx-anillo__texto {
      position: absolute;
      color: var(--cx-petroleo);
      font-family: var(--cx-fuente-mono);
      font-size: calc(var(--tamano) * 0.24);
      font-weight: 600;
      letter-spacing: -0.04em;

      small {
        font-size: 0.7em;
        opacity: 0.7;
      }
    }

    @keyframes cx-anillo-llenar {
      from {
        stroke-dashoffset: 100.53; /* circunferencia del anillo (2π · 16) */
      }
    }
  `,
})
export class AnilloProgreso {
  /** Porcentaje de 0 a 100 (se recorta si viene fuera de rango). */
  readonly valor = input.required<number>();
  readonly tamano = input(56);
  readonly mostrarValor = input(true);
  /** Texto para lectores de pantalla, ej. "Avance de Torre Norte". */
  readonly etiqueta = input('');

  protected readonly radio = RADIO;
  protected readonly circunferencia = CIRCUNFERENCIA;

  protected readonly porcentaje = computed(() =>
    Math.round(Math.min(100, Math.max(0, this.valor() || 0))),
  );

  protected readonly desfase = computed(() => CIRCUNFERENCIA * (1 - this.porcentaje() / 100));
}
