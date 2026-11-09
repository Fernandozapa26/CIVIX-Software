import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { CurrencyPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { AuthService } from '../../core/services/auth.service';
import { obtenerMensajeError } from '../../core/utils/http-error';
import { ObrasService } from './obras.service';
import { Obra, ObraPayload, UsuarioOpcion } from './obra.model';

@Component({
  selector: 'app-obras',
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './obras.html',
  styleUrl: './obras.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Obras implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(ObrasService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  /** El Cliente solo consulta: no ve los botones de crear, editar ni eliminar. */
  protected readonly puedeGestionar = computed(() => !this.auth.esSoloLectura());

  protected readonly clientes = signal<UsuarioOpcion[]>([]);
  protected readonly ingenieros = signal<UsuarioOpcion[]>([]);

  protected readonly obras = signal<Obra[]>([]);
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editandoId = signal<number | null>(null);
  protected readonly mostrarFormulario = signal(false);
  protected readonly busqueda = signal('');
  protected readonly filtro = signal('Todas');

  protected readonly obrasFiltradas = computed(() => {
    const term = this.busqueda().trim().toLowerCase();
    const filtro = this.filtro();
    return this.obras().filter((o) => {
      const matchFiltro =
        filtro === 'Todas' ||
        (filtro === 'En ejecución' && o.estado === 'En ejecución') ||
        (filtro === 'En planeación' && o.estado === 'En planeación') ||
        (filtro === 'Finalizada' && o.estado === 'Finalizada');
      const matchBusqueda =
        term === '' ||
        o.nombre.toLowerCase().includes(term) ||
        (o.clienteNombre ?? o.cliente ?? '').toLowerCase().includes(term) ||
        (o.responsableNombre ?? '').toLowerCase().includes(term) ||
        (o.ubicacion ?? '').toLowerCase().includes(term);
      return matchFiltro && matchBusqueda;
    });
  });

  protected readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    clienteId: [0, [Validators.required, Validators.min(1)]],
    responsableId: [0, [Validators.required, Validators.min(1)]],
    descripcion: [''],
    ubicacion: ['', Validators.required],
    fechaInicio: ['', Validators.required],
    fechaFinEstimada: [''],
    estado: ['En planeación', Validators.required],
    presupuesto: [0, [Validators.required, Validators.min(0)]],
    avance: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    personal: [0, [Validators.required, Validators.min(0)]],
  });

  ngOnInit(): void {
    this.cargar();
    if (this.puedeGestionar()) {
      this.cargarUsuarios();
    }
    this.route.queryParamMap.subscribe((params) => {
      if (params.get('nueva') === '1') {
        this.nuevaObra();
      }
    });
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.service.listar().subscribe({
      next: (data) => {
        this.obras.set(data);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar las obras. Verifica que la API esté en ejecución.');
        this.cargando.set(false);
      },
    });
  }

  /** Clientes e ingenieros disponibles para asignar a la obra. */
  private cargarUsuarios(): void {
    this.service.listarUsuarios('Cliente').subscribe({
      next: (lista) => this.clientes.set(lista),
      error: () => this.clientes.set([]),
    });
    this.service.listarUsuarios('Ingeniero').subscribe({
      next: (lista) => this.ingenieros.set(lista),
      error: () => this.ingenieros.set([]),
    });
  }

  protected nuevaObra(): void {
    this.editandoId.set(null);
    this.form.reset({
      nombre: '',
      clienteId: 0,
      responsableId: 0,
      descripcion: '',
      ubicacion: '',
      fechaInicio: '',
      fechaFinEstimada: '',
      estado: 'En planeación',
      presupuesto: 0,
      avance: 0,
      personal: 0,
    });
    this.mostrarFormulario.set(true);
  }

  protected editar(obra: Obra): void {
    this.editandoId.set(obra.id);
    this.form.reset({
      nombre: obra.nombre,
      clienteId: obra.clienteId ?? 0,
      responsableId: obra.responsableId ?? 0,
      descripcion: obra.descripcion ?? '',
      ubicacion: obra.ubicacion ?? '',
      fechaInicio: obra.fechaInicio?.slice(0, 10) ?? '',
      fechaFinEstimada: obra.fechaFinEstimada?.slice(0, 10) ?? '',
      estado: obra.estado,
      presupuesto: obra.presupuesto,
      avance: obra.avance,
      personal: obra.personal,
    });
    this.mostrarFormulario.set(true);
  }

  protected cancelar(): void {
    this.mostrarFormulario.set(false);
    this.editandoId.set(null);
    this.router.navigate(['/app/obras']);
  }

  protected guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload: ObraPayload = {
      nombre: value.nombre,
      clienteId: Number(value.clienteId),
      responsableId: Number(value.responsableId),
      descripcion: value.descripcion || null,
      ubicacion: value.ubicacion || null,
      fechaInicio: value.fechaInicio || null,
      fechaFinEstimada: value.fechaFinEstimada || null,
      estado: value.estado,
      presupuesto: value.presupuesto,
      avance: value.avance,
      personal: value.personal,
    };

    const id = this.editandoId();
    const request$ = (
      id === null
        ? this.service.crear(payload)
        : this.service.actualizar(id, payload)
    ) as Observable<unknown>;

    request$.subscribe({
      next: () => {
        this.mostrarFormulario.set(false);
        this.editandoId.set(null);
        this.router.navigate(['/app/obras']);
        this.cargar();
      },
      error: (err: unknown) => this.error.set(obtenerMensajeError(err, 'No se pudo guardar la obra.')),
    });
  }

  protected slug(estado: string): string {
    return estado
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/\s+/g, '-');
  }

  protected estadoLabel(estado: string): string {
    const map: Record<string, string> = {
      'En ejecución': '● En ejecución',
      'En planeación': '● En planeación',
      'Finalizada': '● Finalizada',
      'Planificada': '● Planificada',
      'Pausada': '● Pausada',
    };
    return map[estado] ?? `● ${estado}`;
  }

  protected eliminar(obra: Obra): void {
    if (!confirm(`¿Eliminar la obra "${obra.nombre}"?`)) return;
    this.service.eliminar(obra.id).subscribe({
      next: () => this.cargar(),
      error: (err: unknown) => this.error.set(obtenerMensajeError(err, 'No se pudo eliminar la obra.')),
    });
  }
}