import { DestroyRef, Directive, ElementRef, effect, inject, input, untracked } from '@angular/core';

/**
 * Anima un número desde 0 hasta su valor cuando aparece en pantalla.
 *
 * Uso: <span [cxContador]="total" cxContadorSufijo="%"></span>
 *
 * - Muestra un decimal solo si el valor no es entero.
 * - Con "movimiento reducido" activado en el sistema, muestra el valor final de inmediato.
 * - El número final queda siempre en el texto, así que los lectores de pantalla leen el valor real.
 */
@Directive({
  selector: '[cxContador]',
})
export class ContadorDirective {
  readonly cxContador = input.required<number>();
  readonly cxContadorSufijo = input('');

  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef);
  private cuadro = 0;

  constructor() {
    effect(() => {
      const destino = this.cxContador();
      const sufijo = this.cxContadorSufijo();
      untracked(() => this.animar(destino, sufijo));
    });

    inject(DestroyRef).onDestroy(() => cancelAnimationFrame(this.cuadro));
  }

  private animar(destino: number, sufijo: string): void {
    cancelAnimationFrame(this.cuadro);

    const decimales = Number.isInteger(destino) ? 0 : 1;
    const formatear = (valor: number): string =>
      valor.toLocaleString('es-CO', {
        minimumFractionDigits: decimales,
        maximumFractionDigits: decimales,
      }) + sufijo;

    const reducirMovimiento =
      typeof window.matchMedia === 'function' &&
      window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (reducirMovimiento || !Number.isFinite(destino) || destino === 0) {
      this.mostrar(formatear(Number.isFinite(destino) ? destino : 0));
      return;
    }

    const duracion = 900;
    const inicio = performance.now();

    const paso = (ahora: number): void => {
      const progreso = Math.min((ahora - inicio) / duracion, 1);
      // Desaceleración suave al final (ease-out cúbico)
      const suavizado = 1 - Math.pow(1 - progreso, 3);
      this.mostrar(formatear(destino * suavizado));

      if (progreso < 1) {
        this.cuadro = requestAnimationFrame(paso);
      } else {
        this.mostrar(formatear(destino));
      }
    };

    this.cuadro = requestAnimationFrame(paso);
  }

  private mostrar(texto: string): void {
    this.elemento.nativeElement.textContent = texto;
  }
}
