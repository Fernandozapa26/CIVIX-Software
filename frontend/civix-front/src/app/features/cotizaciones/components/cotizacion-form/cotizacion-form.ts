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
import { toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { obtenerMensajeError } from '../../../../core/utils/http-error';
import { UsuarioOpcion } from '../../../obras/obra.model';
import { ObrasService } from '../../../obras/obras.service';
import { CotizacionDetalle, GuardarCotizacionPayload } from '../../cotizaciones.models';
import { CotizacionesService, formatearPesos } from '../../cotizaciones.service';

/**
 * Crear (RF-008) y editar cotizaciones con ítems; el total se calcula en vivo (RF-009)
 * y el backend lo vuelve a calcular al guardar.
 * Rutas: /app/cotizaciones/nueva y /app/cotizaciones/:id/editar
 */
@Component({
  selector: 'app-cotizacion-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './cotizacion-form.html',
  styleUrl: './cotizacion-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CotizacionForm {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly cotizacionesService = inject(CotizacionesService);
  private readonly obrasService = inject(ObrasService);

  /** Parámetro :id de la ruta (vacío al crear). */
  readonly id = input<string>();

  readonly esEdicion = computed(() => !!this.id());

  readonly clientes = signal<UsuarioOpcion[]>([]);
  readonly cotizacion = signal<CotizacionDetalle | null>(null);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal<string | null>(null);
  /** True si la cotización que se intenta editar ya no está en Borrador. */
  readonly bloqueada = signal(false);

  readonly formulario = this.fb.nonNullable.group({
    nombreProyecto: ['', [Validators.required, Validators.maxLength(150)]],
    clienteId: [0, [Validators.required, Validators.min(1)]],
    fecha: [hoy(), Validators.required],
    ubicacion: ['', Validators.maxLength(250)],
    descripcion: ['', Validators.maxLength(1000)],
    items: this.fb.array([this.crearItem()]),
  });

  /** Valores de los ítems como signal para recalcular el total en vivo. */
  private readonly valoresItems = toSignal(
    this.formulario.controls.items.valueChanges as Observable<ValorItem[]>,
    { initialValue: this.formulario.controls.items.getRawValue() as ValorItem[] },
  );

  readonly total = computed(() =>
    this.valoresItems().reduce(
      (suma, item) =>
        suma + redondear(Number(item.cantidad ?? 0) * Number(item.precioUnitario ?? 0)),
      0,
    ),
  );

  readonly formatearPesos = formatearPesos;

  constructor() {
    this.obrasService.listarUsuarios('Cliente').subscribe({
      next: (lista) => this.clientes.set(lista),
      error: () => this.clientes.set([]),
    });

    effect(() => {
      const id = Number(this.id());
      if (id) {
        untracked(() => this.cargar(id));
      }
    });
  }

  get items(): FormArray {
    return this.formulario.controls.items;
  }

  crearItem(concepto = '', cantidad = 1, precioUnitario = 0) {
    return this.fb.nonNullable.group({
      concepto: [concepto, [Validators.required, Validators.maxLength(250)]],
      cantidad: [cantidad, [Validators.required, Validators.min(0.001)]],
      precioUnitario: [precioUnitario, [Validators.required, Validators.min(0)]],
    });
  }

  agregarItem(): void {
    this.items.push(this.crearItem());
  }

  eliminarItem(index: number): void {
    if (this.items.length > 1) {
      this.items.removeAt(index);
    }
  }

  subtotal(index: number): number {
    const item = this.valoresItems()[index];
    return item ? redondear(Number(item.cantidad ?? 0) * Number(item.precioUnitario ?? 0)) : 0;
  }

  invalido(control: { invalid: boolean; touched: boolean } | null): boolean {
    return !!control && control.invalid && control.touched;
  }

  private cargar(id: number): void {
    this.cargando.set(true);
    this.error.set(null);

    this.cotizacionesService.obtener(id).subscribe({
      next: (cotizacion) => {
        this.cotizacion.set(cotizacion);
        this.bloqueada.set(cotizacion.estado !== 'Borrador');

        this.items.clear();
        for (const item of cotizacion.items) {
          this.items.push(this.crearItem(item.concepto, item.cantidad, item.precioUnitario));
        }
        if (this.items.length === 0) {
          this.items.push(this.crearItem());
        }

        this.formulario.patchValue({
          nombreProyecto: cotizacion.nombreProyecto,
          clienteId: cotizacion.clienteId ?? 0,
          fecha: cotizacion.fecha.slice(0, 10),
          ubicacion: cotizacion.ubicacion ?? '',
          descripcion: cotizacion.descripcion ?? '',
        });

        if (this.bloqueada()) {
          this.formulario.disable();
        }

        this.cargando.set(false);
      },
      error: (error: unknown) => {
        this.error.set(obtenerMensajeError(error, 'No se pudo cargar la cotización.'));
        this.cargando.set(false);
      },
    });
  }

  guardar(): void {
    if (this.bloqueada()) {
      return;
    }

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.error.set('Revisa los campos marcados en rojo.');
      return;
    }

    const valores = this.formulario.getRawValue();
    const datos: GuardarCotizacionPayload = {
      nombreProyecto: valores.nombreProyecto.trim(),
      clienteId: Number(valores.clienteId),
      fecha: valores.fecha || null,
      ubicacion: valores.ubicacion.trim() || null,
      descripcion: valores.descripcion.trim() || null,
      items: valores.items.map((item) => ({
        concepto: item.concepto.trim(),
        cantidad: Number(item.cantidad),
        precioUnitario: Number(item.precioUnitario),
      })),
    };

    const id = Number(this.id());
    const peticion = id
      ? this.cotizacionesService.actualizar(id, datos)
      : this.cotizacionesService.crear(datos);

    this.guardando.set(true);
    this.error.set(null);

    peticion.subscribe({
      next: (guardada) => {
        this.guardando.set(false);
        this.router.navigate(['/app/cotizaciones'], {
          queryParams: { guardada: guardada.numero },
        });
      },
      error: (error: unknown) => {
        this.guardando.set(false);
        this.error.set(obtenerMensajeError(error, 'No se pudo guardar la cotización.'));
      },
    });
  }
}

interface ValorItem {
  concepto?: string;
  cantidad?: number;
  precioUnitario?: number;
}

function hoy(): string {
  const fecha = new Date();
  const mes = String(fecha.getMonth() + 1).padStart(2, '0');
  const dia = String(fecha.getDate()).padStart(2, '0');
  return `${fecha.getFullYear()}-${mes}-${dia}`;
}

/** Redondeo a 2 decimales, igual que el subtotal de la base de datos. */
function redondear(valor: number): number {
  return Math.round((valor + Number.EPSILON) * 100) / 100;
}
