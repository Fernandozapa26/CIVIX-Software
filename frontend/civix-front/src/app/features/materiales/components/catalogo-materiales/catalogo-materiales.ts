import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { obtenerMensajeError } from '../../../../core/utils/http-error';
import { Material } from '../../materiales.models';
import { MaterialesService } from '../../materiales.service';

/** Unidades sugeridas; el campo admite cualquier texto. */
const UNIDADES_SUGERIDAS = [
  'Unidad',
  'Bulto',
  'kg',
  'Tonelada',
  'm',
  'm²',
  'm³',
  'Litro',
  'Galón',
  'Caja',
  'Rollo',
  'Varilla',
];

/** Catálogo de materiales (usa el API existente /api/materiales). */
@Component({
  selector: 'app-catalogo-materiales',
  imports: [ReactiveFormsModule],
  templateUrl: './catalogo-materiales.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogoMateriales {
  private readonly materialesService = inject(MaterialesService);
  private readonly fb = inject(FormBuilder);

  readonly unidades = UNIDADES_SUGERIDAS;

  readonly materiales = signal<Material[]>([]);
  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensajeExito = signal<string | null>(null);

  readonly mostrarFormulario = signal(false);
  readonly editando = signal<Material | null>(null);
  readonly guardando = signal(false);
  readonly errorFormulario = signal<string | null>(null);
  readonly confirmarDesactivarId = signal<number | null>(null);

  readonly busqueda = signal('');
  readonly mostrarInactivos = signal(false);

  readonly materialesFiltrados = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();

    return this.materiales()
      .filter((m) => this.mostrarInactivos() || m.activo)
      .filter(
        (m) =>
          !texto ||
          `${m.nombre} ${m.unidadMedida} ${m.descripcion ?? ''}`.toLowerCase().includes(texto),
      );
  });

  readonly formulario = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    unidadMedida: ['', [Validators.required, Validators.maxLength(30)]],
    descripcion: ['', Validators.maxLength(500)],
  });

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);

    this.materialesService.listarCatalogo().subscribe({
      next: (lista) => {
        this.materiales.set([...lista].sort((a, b) => a.nombre.localeCompare(b.nombre)));
        this.cargando.set(false);
      },
      error: (error: unknown) => {
        this.error.set(obtenerMensajeError(error, 'No se pudo cargar el catálogo.'));
        this.cargando.set(false);
      },
    });
  }

  abrirNuevo(): void {
    this.editando.set(null);
    this.formulario.reset({ nombre: '', unidadMedida: '', descripcion: '' });
    this.abrirFormulario();
  }

  abrirEdicion(material: Material): void {
    this.editando.set(material);
    this.formulario.reset({
      nombre: material.nombre,
      unidadMedida: material.unidadMedida,
      descripcion: material.descripcion ?? '',
    });
    this.abrirFormulario();
  }

  cerrarFormulario(): void {
    this.mostrarFormulario.set(false);
    this.editando.set(null);
    this.errorFormulario.set(null);
  }

  guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const valores = this.formulario.getRawValue();
    const datos = {
      nombre: valores.nombre.trim(),
      unidadMedida: valores.unidadMedida.trim(),
      descripcion: valores.descripcion.trim() || null,
    };
    const material = this.editando();

    const peticion = material
      ? this.materialesService.actualizarMaterial(material.id, datos)
      : this.materialesService.crearMaterial(datos);

    this.guardando.set(true);
    this.errorFormulario.set(null);

    peticion.subscribe({
      next: (guardado) => {
        this.guardando.set(false);
        this.mensajeExito.set(
          material ? `Se actualizó ${guardado.nombre}.` : `Se creó ${guardado.nombre}.`,
        );
        this.cerrarFormulario();
        this.cargar();
      },
      error: (error: unknown) => {
        this.guardando.set(false);
        this.errorFormulario.set(obtenerMensajeError(error, 'No se pudo guardar el material.'));
      },
    });
  }

  desactivar(material: Material): void {
    this.guardando.set(true);
    this.error.set(null);

    this.materialesService.desactivarMaterial(material.id).subscribe({
      next: () => {
        this.guardando.set(false);
        this.confirmarDesactivarId.set(null);
        this.mensajeExito.set(`${material.nombre} se desactivó.`);
        this.cargar();
      },
      error: (error: unknown) => {
        this.guardando.set(false);
        this.confirmarDesactivarId.set(null);
        this.error.set(obtenerMensajeError(error, 'No se pudo desactivar el material.'));
      },
    });
  }

  invalido(control: { invalid: boolean; touched: boolean }): boolean {
    return control.invalid && control.touched;
  }

  private abrirFormulario(): void {
    this.mostrarFormulario.set(true);
    this.errorFormulario.set(null);
    this.mensajeExito.set(null);
    this.confirmarDesactivarId.set(null);
  }
}
