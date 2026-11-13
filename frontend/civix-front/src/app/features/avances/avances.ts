import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { Observable, firstValueFrom } from 'rxjs';
import { Avance, AvancePayload } from './avance.model';
import { AvancesService } from './avances.service';
import { ObrasService } from '../obras/obras.service';
import { AuthService } from '../../core/services/auth.service';
import { Obra } from '../obras/obra.model';
import { environment } from '../../../environments/environment';
import { claseEstadoObra } from '../../core/utils/estados';

/** Dirección del servidor sin "/api", para armar la URL de las fotos (/uploads/...). */
const URL_SERVIDOR = environment.apiUrl.replace(/\/api\/?$/, '');

@Component({
  selector: 'app-avances',
  imports: [ReactiveFormsModule],
  templateUrl: './avances.html',
  styleUrl: './avances.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'cerrarVentanas()',
  },
})
export class Avances implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(AvancesService);
  private readonly obrasService = inject(ObrasService);
  private readonly auth = inject(AuthService);

  /** El Cliente solo consulta: no ve los botones de crear, editar ni eliminar. */
  protected readonly puedeGestionar = computed(() => !this.auth.esSoloLectura());

  protected readonly avances = signal<Avance[]>([]);
  protected readonly obras = signal<Obra[]>([]);
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editandoId = signal<number | null>(null);
  protected readonly mostrarFormulario = signal(false);
  protected readonly obraSeleccionada = signal<number | null>(null);
  protected readonly subiendo = signal(false);
  protected readonly imagenes = signal<string[]>([]);

  protected readonly avanceAEliminar = signal<Avance | null>(null);
  protected readonly eliminando = signal(false);
  protected readonly guardando = signal(false);

  protected readonly claseEstadoObra = claseEstadoObra;

  /** Obra elegida en el selector (para mostrar su estado y avance actual). */
  protected readonly obraActual = computed(() => {
    const id = this.obraSeleccionada();
    return id ? (this.obras().find((o) => o.id === id) ?? null) : null;
  });

  /** Cantidad de fotos registradas en los avances visibles. */
  protected readonly totalFotos = computed(() =>
    this.avancesFiltrados().reduce((total, a) => total + a.imagenes.length, 0),
  );

  protected readonly avancesFiltrados = computed(() => {
    const id = this.obraSeleccionada();
    const base = id ? this.avances().filter((a) => a.obraId === id) : this.avances();
    return [...base].sort((a, b) => {
      if (a.fecha !== b.fecha) return a.fecha < b.fecha ? 1 : -1;
      return b.id - a.id;
    });
  });

  protected readonly form = this.fb.nonNullable.group({
    obraId: [0, [Validators.required, Validators.min(1)]],
    fecha: ['', Validators.required],
    descripcion: ['', [Validators.required, Validators.maxLength(1500)]],
    porcentajeAvance: [0, [Validators.min(0), Validators.max(100)]],
  });

  ngOnInit(): void {
    this.cargarObras();
    this.cargar();
  }

  protected cargarObras(): void {
    this.obrasService.listar().subscribe({
      next: (data: Obra[]) => this.obras.set(data),
      error: () => {},
    });
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    const id = this.obraSeleccionada();
    this.service.listar(id ?? undefined).subscribe({
      next: (data: Avance[]) => {
        this.avances.set(data);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar los avances. Verifica que la API esté en ejecución.');
        this.cargando.set(false);
      },
    });
  }

  protected seleccionarObra(id: number | null): void {
    this.obraSeleccionada.set(id);
    this.cargar();
  }

  protected nuevaAvance(): void {
    this.editandoId.set(null);
    this.imagenes.set([]);
    this.form.reset({
      obraId: this.obraSeleccionada() ?? 0,
      fecha: (() => {
        const hoy = new Date();
        const y = hoy.getFullYear();
        const m = String(hoy.getMonth() + 1).padStart(2, '0');
        const d = String(hoy.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
      })(),
      descripcion: '',
      porcentajeAvance: 0,
    });
    this.mostrarFormulario.set(true);
  }

  protected editar(avance: Avance): void {
    this.editandoId.set(avance.id);
    this.imagenes.set([...avance.imagenes]);
    this.form.reset({
      obraId: avance.obraId,
      fecha: avance.fecha?.slice(0, 10) ?? '',
      descripcion: avance.descripcion,
      porcentajeAvance: avance.porcentajeAvance ?? 0,
    });
    this.mostrarFormulario.set(true);
  }

  protected cancelar(): void {
    this.mostrarFormulario.set(false);
    this.editandoId.set(null);
    this.imagenes.set([]);
  }

  protected onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const files = Array.from(input.files);
    this.subiendo.set(true);
    const uploads = files.map((f) => firstValueFrom(this.service.subirImagen(f)));
    Promise.all(uploads)
      .then((results: Array<{ url: string } | undefined>) => {
        const urls = results.map((r) => r?.url).filter((u): u is string => !!u);
        this.imagenes.update((prev) => [...prev, ...urls]);
        this.subiendo.set(false);
      })
      .catch(() => {
        this.error.set('No se pudieron subir algunas imágenes.');
        this.subiendo.set(false);
      });
  }

  protected quitarImagen(url: string): void {
    this.imagenes.update((prev) => prev.filter((u) => u !== url));
  }

  protected guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload: AvancePayload = {
      obraId: value.obraId,
      fecha: value.fecha,
      descripcion: value.descripcion,
      porcentajeAvance: value.porcentajeAvance,
      imagenes: this.imagenes(),
    };

    const id = this.editandoId();
    const request$ = (
      id === null
        ? this.service.crear(payload)
        : this.service.actualizar(id, payload)
    ) as Observable<unknown>;

    this.guardando.set(true);

    request$.subscribe({
      next: () => {
        this.guardando.set(false);
        this.mostrarFormulario.set(false);
        this.editandoId.set(null);
        this.imagenes.set([]);
        this.cargar();
      },
      error: () => {
        this.guardando.set(false);
        this.error.set('No se pudo guardar el avance.');
      },
    });
  }

  /** Abre la ventana de confirmación antes de eliminar. */
  protected eliminar(avance: Avance): void {
    this.avanceAEliminar.set(avance);
  }

  protected confirmarEliminacion(): void {
    const avance = this.avanceAEliminar();
    if (!avance) return;

    this.eliminando.set(true);
    this.service.eliminar(avance.id).subscribe({
      next: () => {
        this.eliminando.set(false);
        this.avanceAEliminar.set(null);
        this.cargar();
      },
      error: () => {
        this.eliminando.set(false);
        this.avanceAEliminar.set(null);
        this.error.set('No se pudo eliminar el avance.');
      },
    });
  }

  /** Escape cierra la ventana abierta (primero la confirmación, luego el formulario). */
  protected cerrarVentanas(): void {
    if (this.avanceAEliminar()) {
      this.avanceAEliminar.set(null);
    } else if (this.mostrarFormulario()) {
      this.cancelar();
    }
  }

  protected invalido(control: AbstractControl): boolean {
    return control.invalid && control.touched;
  }

  protected iniciales(nombre: string): string {
    return nombre
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((parte) => parte.charAt(0).toUpperCase())
      .join('');
  }

  protected urlImagen(ruta: string): string {
    return ruta.startsWith('http') ? ruta : `${URL_SERVIDOR}${ruta}`;
  }

  /** Día y mes por separado para el marcador de la línea de tiempo. */
  protected partesFecha(fecha: string): { dia: string; mes: string; anio: string } {
    const [y, m, d] = (fecha ?? '').split('T')[0].split('-');
    if (!y || !m || !d) return { dia: '', mes: '', anio: '' };
    const date = new Date(Number(y), Number(m) - 1, Number(d));
    return {
      dia: d,
      mes: date.toLocaleDateString('es-CO', { month: 'short' }).replace('.', ''),
      anio: y,
    };
  }

  protected formatearFecha(fecha: string): string {
    if (!fecha) return '';
    const parte = fecha.split('T')[0];
    const [y, m, d] = parte.split('-');
    if (!y || !m || !d) return fecha;
    const date = new Date(Number(y), Number(m) - 1, Number(d));
    return date.toLocaleDateString('es-CO', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  protected nombreObra(id: number): string {
    return this.obras().find((o) => o.id === id)?.nombre ?? `Obra #${id}`;
  }
}
