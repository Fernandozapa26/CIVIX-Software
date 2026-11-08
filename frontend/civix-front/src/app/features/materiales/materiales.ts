import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import { obtenerMensajeError } from '../../core/utils/http-error';
import { CatalogoMateriales } from './components/catalogo-materiales/catalogo-materiales';
import { HistorialRetiros } from './components/historial-retiros/historial-retiros';
import { MaterialesObra } from './components/materiales-obra/materiales-obra';
import { ObraOpcion } from './materiales.models';
import { MaterialesService } from './materiales.service';

type Pestana = 'obra' | 'retiros' | 'catalogo';

interface OpcionPestana {
  id: Pestana;
  etiqueta: string;
  icono: string;
}

/**
 * Página de Materiales (RF-006 y RF-007).
 * - "Materiales por obra": registrar, consultar, cambiar estado y retirar.
 * - "Historial de retiros": quién retiró, cuánto y para qué obra.
 * - "Catálogo": materiales disponibles para asociar a las obras.
 */
@Component({
  selector: 'app-materiales',
  imports: [MaterialesObra, HistorialRetiros, CatalogoMateriales],
  templateUrl: './materiales.html',
  styleUrl: './materiales.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Materiales {
  private readonly materialesService = inject(MaterialesService);

  readonly pestanas: readonly OpcionPestana[] = [
    { id: 'obra', etiqueta: 'Materiales por obra', icono: 'bi-buildings' },
    { id: 'retiros', etiqueta: 'Historial de retiros', icono: 'bi-box-arrow-up-right' },
    { id: 'catalogo', etiqueta: 'Catálogo', icono: 'bi-journal-text' },
  ];

  readonly pestanaActiva = signal<Pestana>('obra');

  readonly obras = signal<ObraOpcion[]>([]);
  readonly obraSeleccionadaId = signal<number | null>(null);
  readonly cargandoObras = signal(true);
  readonly errorObras = signal<string | null>(null);

  /** Se incrementa cuando se registra un retiro para que el historial se recargue. */
  readonly versionRetiros = signal(0);

  readonly obraSeleccionada = computed(
    () => this.obras().find((o) => o.id === this.obraSeleccionadaId()) ?? null,
  );

  constructor() {
    this.cargarObras();
  }

  cargarObras(): void {
    this.cargandoObras.set(true);
    this.errorObras.set(null);

    this.materialesService.listarObras().subscribe({
      next: (obras) => {
        this.obras.set(obras);
        this.cargandoObras.set(false);

        if (!obras.some((o) => o.id === this.obraSeleccionadaId())) {
          this.obraSeleccionadaId.set(obras[0]?.id ?? null);
        }
      },
      error: (error: unknown) => {
        this.errorObras.set(obtenerMensajeError(error, 'No se pudieron cargar las obras.'));
        this.cargandoObras.set(false);
      },
    });
  }

  seleccionarObra(valor: string): void {
    const id = Number(valor);
    this.obraSeleccionadaId.set(Number.isNaN(id) ? null : id);
  }

  cambiarPestana(pestana: Pestana): void {
    this.pestanaActiva.set(pestana);
  }

  alRegistrarRetiro(): void {
    this.versionRetiros.update((v) => v + 1);
  }
}
