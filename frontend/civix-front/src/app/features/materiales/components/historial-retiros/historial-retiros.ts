import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';

import { obtenerMensajeError } from '../../../../core/utils/http-error';
import { ObraOpcion, RetiroMaterial } from '../../materiales.models';
import { formatearCantidad, formatearFecha, MaterialesService } from '../../materiales.service';

/** Historial de retiros de una obra: lo que salió de su bodega y lo que llegó desde otras. */
@Component({
  selector: 'app-historial-retiros',
  templateUrl: './historial-retiros.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistorialRetiros {
  private readonly materialesService = inject(MaterialesService);

  readonly obra = input.required<ObraOpcion>();
  /** Cambia cuando se registra un retiro en otra pestaña, para recargar. */
  readonly version = input(0);

  readonly retiros = signal<RetiroMaterial[]>([]);
  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);
  readonly busqueda = signal('');

  readonly retirosFiltrados = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();

    if (!texto) {
      return this.retiros();
    }

    return this.retiros().filter((r) =>
      [r.materialNombre, r.retiradoPor, r.registradoPor, r.obraDestinoNombre, r.obraOrigenNombre]
        .join(' ')
        .toLowerCase()
        .includes(texto),
    );
  });

  readonly formatearCantidad = formatearCantidad;
  readonly formatearFecha = formatearFecha;

  constructor() {
    effect(() => {
      const obraId = this.obra().id;
      this.version();
      untracked(() => this.cargar(obraId));
    });
  }

  cargar(obraId = this.obra().id): void {
    this.cargando.set(true);
    this.error.set(null);

    this.materialesService.listarRetiros(obraId).subscribe({
      next: (lista) => {
        this.retiros.set(lista);
        this.cargando.set(false);
      },
      error: (error: unknown) => {
        this.error.set(obtenerMensajeError(error, 'No se pudo cargar el historial de retiros.'));
        this.cargando.set(false);
      },
    });
  }

  /** "Salida" si el material salió de la bodega de esta obra hacia otra. */
  esSalidaHaciaOtraObra(retiro: RetiroMaterial): boolean {
    return retiro.obraOrigenId === this.obra().id && retiro.obraDestinoId !== this.obra().id;
  }

  esEntradaDesdeOtraObra(retiro: RetiroMaterial): boolean {
    return retiro.obraDestinoId === this.obra().id && retiro.obraOrigenId !== this.obra().id;
  }
}
