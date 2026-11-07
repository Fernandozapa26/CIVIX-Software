import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { obtenerMensajeError } from '../../../../core/utils/http-error';
import {
  CotizacionOpcion,
  ESTADOS_SELECCIONABLES,
  EstadoMaterial,
  Material,
  ObraMaterial,
  ObraOpcion,
} from '../../materiales.models';
import {
  formatearCantidad,
  formatearFecha,
  formatearMoneda,
  MaterialesService,
} from '../../materiales.service';

type Panel = 'ninguno' | 'registrar' | 'editar' | 'retirar';
type FiltroEstado = EstadoMaterial | 'Todos';

/**
 * Materiales de una obra:
 * - RF-007: consulta con cantidades y estado.
 * - RF-006: registro (y edición) de materiales en la obra.
 * - Retiros parciales de material en bodega.
 */
@Component({
  selector: 'app-materiales-obra',
  imports: [ReactiveFormsModule],
  templateUrl: './materiales-obra.html',
  styleUrl: './materiales-obra.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MaterialesObra {
  private readonly materialesService = inject(MaterialesService);
  private readonly fb = inject(FormBuilder);

  readonly obra = input.required<ObraOpcion>();
  readonly obras = input.required<ObraOpcion[]>();
  readonly retiroRegistrado = output<void>();

  readonly estadosSeleccionables = ESTADOS_SELECCIONABLES;
  readonly filtros: readonly FiltroEstado[] = ['Todos', 'Pedido', 'En bodega', 'Retirado'];

  readonly materiales = signal<ObraMaterial[]>([]);
  readonly catalogo = signal<Material[]>([]);
  readonly cotizaciones = signal<CotizacionOpcion[]>([]);

  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensajeExito = signal<string | null>(null);

  readonly panel = signal<Panel>('ninguno');
  readonly seleccionado = signal<ObraMaterial | null>(null);
  readonly guardando = signal(false);
  readonly errorFormulario = signal<string | null>(null);
  readonly confirmarEliminarId = signal<number | null>(null);

  readonly filtroEstado = signal<FiltroEstado>('Todos');

  readonly materialesActivos = computed(() => this.catalogo().filter((m) => m.activo));

  readonly materialesFiltrados = computed(() => {
    const filtro = this.filtroEstado();
    const lista = this.materiales();
    return filtro === 'Todos' ? lista : lista.filter((m) => m.estado === filtro);
  });

  readonly resumen = computed(() => {
    const lista = this.materiales();
    return {
      total: lista.length,
      pedido: lista.filter((m) => m.estado === 'Pedido').length,
      enBodega: lista.filter((m) => m.estado === 'En bodega').length,
      retirado: lista.filter((m) => m.estado === 'Retirado').length,
    };
  });

  readonly formMaterial = this.fb.group({
    materialId: this.fb.control<number | null>(null, Validators.required),
    cantidad: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.001)]),
    estado: this.fb.nonNullable.control<EstadoMaterial>('Pedido', Validators.required),
    cotizacionId: this.fb.control<number | null>(null),
  });

  readonly formRetiro = this.fb.group({
    cantidad: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.001)]),
    retiradoPor: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(150)]),
    obraDestinoId: this.fb.control<number | null>(null, Validators.required),
    observacion: this.fb.nonNullable.control('', Validators.maxLength(500)),
  });

  readonly formatearCantidad = formatearCantidad;
  readonly formatearFecha = formatearFecha;

  constructor() {
    // Recarga la tabla cada vez que cambia la obra seleccionada.
    effect(() => {
      const obraId = this.obra().id;
      untracked(() => {
        this.cerrarPanel();
        this.filtroEstado.set('Todos');
        this.mensajeExito.set(null);
        this.cargar(obraId);
      });
    });

    this.cargarSelectores();
  }

  // ---------- Carga de datos ----------

  cargar(obraId = this.obra().id): void {
    this.cargando.set(true);
    this.error.set(null);

    this.materialesService.listarPorObra(obraId).subscribe({
      next: (lista) => {
        this.materiales.set(lista);
        this.cargando.set(false);
      },
      error: (error: unknown) => {
        this.error.set(
          obtenerMensajeError(error, 'No se pudieron cargar los materiales de la obra.'),
        );
        this.cargando.set(false);
      },
    });
  }

  private cargarSelectores(): void {
    this.materialesService.listarCatalogo().subscribe({
      next: (lista) => this.catalogo.set(lista),
      error: () => this.catalogo.set([]),
    });

    this.materialesService.listarCotizaciones().subscribe({
      next: (lista) => this.cotizaciones.set(lista),
      error: () => this.cotizaciones.set([]),
    });
  }

  // ---------- Paneles ----------

  abrirRegistro(): void {
    this.prepararPanel('registrar', null);
    this.formMaterial.reset({
      materialId: null,
      cantidad: null,
      estado: 'Pedido',
      cotizacionId: null,
    });
    this.formMaterial.controls.materialId.enable();
  }

  abrirEdicion(item: ObraMaterial): void {
    this.prepararPanel('editar', item);
    this.formMaterial.reset({
      materialId: item.materialId,
      cantidad: item.cantidad,
      estado: item.estado === 'Retirado' ? 'En bodega' : item.estado,
      cotizacionId: item.cotizacionId,
    });
    // El material no se cambia en la edición: se elimina y se registra de nuevo.
    this.formMaterial.controls.materialId.disable();
  }

  abrirRetiro(item: ObraMaterial): void {
    this.prepararPanel('retirar', item);
    this.formRetiro.reset({
      cantidad: null,
      retiradoPor: '',
      obraDestinoId: this.obra().id,
      observacion: '',
    });
    this.formRetiro.controls.cantidad.setValidators([
      Validators.required,
      Validators.min(0.001),
      Validators.max(item.cantidadDisponible),
    ]);
    this.formRetiro.controls.cantidad.updateValueAndValidity();
  }

  cerrarPanel(): void {
    this.panel.set('ninguno');
    this.seleccionado.set(null);
    this.errorFormulario.set(null);
  }

  private prepararPanel(panel: Panel, item: ObraMaterial | null): void {
    this.panel.set(panel);
    this.seleccionado.set(item);
    this.errorFormulario.set(null);
    this.mensajeExito.set(null);
    this.confirmarEliminarId.set(null);
  }

  // ---------- Acciones ----------

  guardarMaterial(): void {
    if (this.formMaterial.invalid) {
      this.formMaterial.markAllAsTouched();
      return;
    }

    const valores = this.formMaterial.getRawValue();
    const datos = {
      cantidad: Number(valores.cantidad),
      estado: valores.estado,
      cotizacionId: valores.cotizacionId,
    };
    const obraId = this.obra().id;
    const item = this.seleccionado();

    const peticion =
      this.panel() === 'editar' && item
        ? this.materialesService.actualizarEnObra(obraId, item.id, datos)
        : this.materialesService.registrarEnObra(obraId, {
            ...datos,
            materialId: Number(valores.materialId),
          });

    this.guardando.set(true);
    this.errorFormulario.set(null);

    peticion.subscribe({
      next: (guardado) => {
        this.guardando.set(false);
        this.mensajeExito.set(
          this.panel() === 'editar'
            ? `Se actualizó ${guardado.materialNombre}.`
            : `Se registró ${guardado.materialNombre} en la obra.`,
        );
        this.cerrarPanel();
        this.cargar();
      },
      error: (error: unknown) => {
        this.guardando.set(false);
        this.errorFormulario.set(obtenerMensajeError(error, 'No se pudo guardar el material.'));
      },
    });
  }

  guardarRetiro(): void {
    const item = this.seleccionado();

    if (!item) {
      return;
    }

    if (this.formRetiro.invalid) {
      this.formRetiro.markAllAsTouched();
      return;
    }

    const valores = this.formRetiro.getRawValue();

    this.guardando.set(true);
    this.errorFormulario.set(null);

    this.materialesService
      .registrarRetiro({
        obraMaterialId: item.id,
        cantidad: Number(valores.cantidad),
        retiradoPor: valores.retiradoPor.trim(),
        obraDestinoId: Number(valores.obraDestinoId),
        observacion: valores.observacion.trim() || null,
      })
      .subscribe({
        next: () => {
          this.guardando.set(false);
          this.mensajeExito.set(
            `Retiro registrado: ${formatearCantidad(Number(valores.cantidad))} ${item.unidadMedida} de ${item.materialNombre}.`,
          );
          this.cerrarPanel();
          this.cargar();
          this.retiroRegistrado.emit();
        },
        error: (error: unknown) => {
          this.guardando.set(false);
          this.errorFormulario.set(obtenerMensajeError(error, 'No se pudo registrar el retiro.'));
        },
      });
  }

  /** Atajo: pasa un material de "Pedido" a "En bodega" sin abrir el formulario. */
  marcarEnBodega(item: ObraMaterial): void {
    this.guardando.set(true);
    this.mensajeExito.set(null);
    this.error.set(null);

    this.materialesService
      .actualizarEnObra(this.obra().id, item.id, {
        cantidad: item.cantidad,
        estado: 'En bodega',
        cotizacionId: item.cotizacionId,
      })
      .subscribe({
        next: () => {
          this.guardando.set(false);
          this.mensajeExito.set(`${item.materialNombre} quedó en bodega.`);
          this.cargar();
        },
        error: (error: unknown) => {
          this.guardando.set(false);
          this.error.set(obtenerMensajeError(error, 'No se pudo cambiar el estado.'));
        },
      });
  }

  pedirConfirmacionEliminar(item: ObraMaterial): void {
    this.cerrarPanel();
    this.confirmarEliminarId.set(item.id);
  }

  eliminar(item: ObraMaterial): void {
    this.guardando.set(true);
    this.mensajeExito.set(null);
    this.error.set(null);

    this.materialesService.eliminarDeObra(this.obra().id, item.id).subscribe({
      next: () => {
        this.guardando.set(false);
        this.confirmarEliminarId.set(null);
        this.mensajeExito.set(`${item.materialNombre} se quitó de la obra.`);
        this.cargar();
      },
      error: (error: unknown) => {
        this.guardando.set(false);
        this.confirmarEliminarId.set(null);
        this.error.set(obtenerMensajeError(error, 'No se pudo eliminar el material.'));
      },
    });
  }

  // ---------- Ayudas para la plantilla ----------

  claseEstado(estado: EstadoMaterial): string {
    switch (estado) {
      case 'Pedido':
        return 'estado estado-pedido';
      case 'En bodega':
        return 'estado estado-bodega';
      default:
        return 'estado estado-retirado';
    }
  }

  textoCotizacion(c: CotizacionOpcion): string {
    return `N.º ${c.id} · ${formatearFecha(c.fecha, false)} · ${formatearMoneda(c.total)}`;
  }

  unidadMaterial(materialId: number | null): string {
    return this.catalogo().find((m) => m.id === materialId)?.unidadMedida ?? '';
  }

  invalido(control: { invalid: boolean; touched: boolean }): boolean {
    return control.invalid && control.touched;
  }
}
