import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';

import { ROLES } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { obtenerMensajeError } from '../../core/utils/http-error';
import { UsuarioOpcion } from '../obras/obra.model';
import { ObrasService } from '../obras/obras.service';
import { descargarPdfCotizacion } from './cotizacion-pdf';
import { CotizacionResumen, EstadoCotizacion } from './cotizaciones.models';
import { CotizacionesService, formatearFechaCorta, formatearPesos } from './cotizaciones.service';

type Filtro = EstadoCotizacion | 'Activas';
type Confirmacion = { id: number; accion: 'anular' | 'rechazar' } | null;

/**
 * Listado de cotizaciones.
 * - Administrador: ve todas; aprueba (crea la obra) y rechaza.
 * - Ingeniero: ve, edita y anula solo las que creó.
 * - Todos: descargan el PDF (RF-010).
 */
@Component({
  selector: 'app-cotizaciones',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './cotizaciones.html',
  styleUrl: './cotizaciones.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Cotizaciones {
  private readonly cotizacionesService = inject(CotizacionesService);
  private readonly obrasService = inject(ObrasService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  readonly filtros: readonly Filtro[] = ['Activas', 'Borrador', 'Aprobada', 'Rechazada', 'Anulada'];

  readonly esAdministrador = computed(() => this.auth.roles().includes(ROLES.administrador));

  readonly cotizaciones = signal<CotizacionResumen[]>([]);
  readonly filtro = signal<Filtro>('Activas');
  readonly busqueda = signal('');
  readonly cargando = signal(false);
  readonly procesandoId = signal<number | null>(null);
  readonly error = signal<string | null>(null);
  readonly mensajeExito = signal<string | null>(null);
  readonly confirmacion = signal<Confirmacion>(null);

  /** Cotización que se está aprobando (muestra el panel para crear la obra). */
  readonly aprobando = signal<CotizacionResumen | null>(null);
  readonly ingenieros = signal<UsuarioOpcion[]>([]);
  readonly errorAprobacion = signal<string | null>(null);

  readonly formAprobar = this.fb.nonNullable.group({
    nombreObra: ['', [Validators.required, Validators.maxLength(150)]],
    responsableId: [0, [Validators.required, Validators.min(1)]],
    fechaInicio: [''],
    fechaFinEstimada: [''],
  });

  private readonly guardada = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('guardada'))),
  );

  readonly cotizacionesFiltradas = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();
    if (!texto) {
      return this.cotizaciones();
    }
    return this.cotizaciones().filter((c) =>
      `${c.numero} ${c.nombreProyecto} ${c.clienteNombre ?? ''} ${c.creadoPor}`
        .toLowerCase()
        .includes(texto),
    );
  });

  readonly totalFiltrado = computed(() =>
    this.cotizacionesFiltradas().reduce((suma, c) => suma + c.total, 0),
  );

  readonly formatearPesos = formatearPesos;
  readonly formatearFechaCorta = formatearFechaCorta;

  constructor() {
    const numero = this.guardada();
    if (numero) {
      this.mensajeExito.set(`Se guardó la cotización ${numero}.`);
    }

    this.cargar();

    if (this.esAdministrador()) {
      this.obrasService.listarUsuarios('Ingeniero').subscribe({
        next: (lista) => this.ingenieros.set(lista),
        error: () => this.ingenieros.set([]),
      });
    }
  }

  cargar(): void {
    const filtro = this.filtro();
    this.cargando.set(true);
    this.error.set(null);

    this.cotizacionesService.listar(filtro === 'Activas' ? null : filtro).subscribe({
      next: (lista) => {
        this.cotizaciones.set(lista);
        this.cargando.set(false);
      },
      error: (error: unknown) => {
        this.error.set(obtenerMensajeError(error, 'No se pudieron cargar las cotizaciones.'));
        this.cargando.set(false);
      },
    });
  }

  cambiarFiltro(filtro: Filtro): void {
    this.filtro.set(filtro);
    this.cerrarAprobacion();
    this.confirmacion.set(null);
    this.cargar();
  }

  // ---------- PDF (RF-010) ----------

  descargarPdf(cotizacion: CotizacionResumen): void {
    this.procesandoId.set(cotizacion.id);
    this.error.set(null);

    this.cotizacionesService.obtener(cotizacion.id).subscribe({
      next: async (detalle) => {
        try {
          await descargarPdfCotizacion(detalle);
        } catch {
          this.error.set(
            'No se pudo generar el PDF. Verifica que estén instaladas las dependencias (npm install).',
          );
        } finally {
          this.procesandoId.set(null);
        }
      },
      error: (error: unknown) => {
        this.procesandoId.set(null);
        this.error.set(obtenerMensajeError(error, 'No se pudo cargar la cotización.'));
      },
    });
  }

  // ---------- Anular y rechazar ----------

  pedirConfirmacion(cotizacion: CotizacionResumen, accion: 'anular' | 'rechazar'): void {
    this.cerrarAprobacion();
    this.confirmacion.set({ id: cotizacion.id, accion });
  }

  confirmar(cotizacion: CotizacionResumen): void {
    const confirmacion = this.confirmacion();
    if (!confirmacion) {
      return;
    }

    const peticion =
      confirmacion.accion === 'anular'
        ? this.cotizacionesService.anular(cotizacion.id)
        : this.cotizacionesService.rechazar(cotizacion.id);

    this.procesandoId.set(cotizacion.id);
    this.error.set(null);
    this.mensajeExito.set(null);

    peticion.subscribe({
      next: () => {
        this.procesandoId.set(null);
        this.confirmacion.set(null);
        this.mensajeExito.set(
          confirmacion.accion === 'anular'
            ? `La cotización ${cotizacion.numero} quedó anulada.`
            : `La cotización ${cotizacion.numero} fue rechazada.`,
        );
        this.cargar();
      },
      error: (error: unknown) => {
        this.procesandoId.set(null);
        this.confirmacion.set(null);
        this.error.set(obtenerMensajeError(error, 'No se pudo completar la acción.'));
      },
    });
  }

  // ---------- Aprobar (crea la obra) ----------

  abrirAprobacion(cotizacion: CotizacionResumen): void {
    this.confirmacion.set(null);
    this.errorAprobacion.set(null);
    this.mensajeExito.set(null);

    // Si la creó un ingeniero, se propone como responsable.
    const creador = this.ingenieros().find((i) => i.id === cotizacion.creadoPorUsuarioId);

    this.formAprobar.reset({
      nombreObra: cotizacion.nombreProyecto,
      responsableId: creador?.id ?? 0,
      fechaInicio: '',
      fechaFinEstimada: '',
    });
    this.aprobando.set(cotizacion);
  }

  cerrarAprobacion(): void {
    this.aprobando.set(null);
    this.errorAprobacion.set(null);
  }

  aprobar(): void {
    const cotizacion = this.aprobando();
    if (!cotizacion) {
      return;
    }

    if (this.formAprobar.invalid) {
      this.formAprobar.markAllAsTouched();
      return;
    }

    const valores = this.formAprobar.getRawValue();

    this.procesandoId.set(cotizacion.id);
    this.errorAprobacion.set(null);

    this.cotizacionesService
      .aprobar(cotizacion.id, {
        responsableId: Number(valores.responsableId),
        nombreObra: valores.nombreObra.trim() || null,
        fechaInicio: valores.fechaInicio || null,
        fechaFinEstimada: valores.fechaFinEstimada || null,
      })
      .subscribe({
        next: (aprobada) => {
          this.procesandoId.set(null);
          this.cerrarAprobacion();
          this.mensajeExito.set(
            `La cotización ${aprobada.numero} fue aprobada y se creó la obra "${aprobada.obraNombre}".`,
          );
          this.cargar();
        },
        error: (error: unknown) => {
          this.procesandoId.set(null);
          this.errorAprobacion.set(obtenerMensajeError(error, 'No se pudo aprobar la cotización.'));
        },
      });
  }

  // ---------- Ayudas para la plantilla ----------

  claseEstado(estado: EstadoCotizacion): string {
    return `estado estado-${estado.toLowerCase()}`;
  }

  invalido(control: { invalid: boolean; touched: boolean }): boolean {
    return control.invalid && control.touched;
  }
}
