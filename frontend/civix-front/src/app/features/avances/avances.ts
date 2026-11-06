import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';

import { Observable, firstValueFrom } from 'rxjs';
import { Avance, AvancePayload } from './avance.model';
import { AvancesService } from './avances.service';
import { ObrasService } from '../obras/obras.service';
import { Obra } from '../obras/obra.model';

@Component({
  selector: 'app-avances',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './avances.html',
  styleUrl: './avances.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Avances implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(AvancesService);
  private readonly obrasService = inject(ObrasService);

  protected readonly avances = signal<Avance[]>([]);
  protected readonly obras = signal<Obra[]>([]);
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editandoId = signal<number | null>(null);
  protected readonly mostrarFormulario = signal(false);
  protected readonly obraSeleccionada = signal<number | null>(null);
  protected readonly subiendo = signal(false);
  protected readonly imagenes = signal<string[]>([]);

  protected readonly avancesFiltrados = computed(() => {
    const id = this.obraSeleccionada();
    if (!id) return this.avances();
    return this.avances().filter((a) => a.obraId === id);
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
      fecha: new Date().toISOString().slice(0, 10),
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
      registradoPorUsuarioId: 1,
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
        this.imagenes.set([]);
        this.cargar();
      },
      error: () => this.error.set('No se pudo guardar el avance.'),
    });
  }

  protected eliminar(avance: Avance): void {
    if (!confirm('¿Eliminar este avance?')) return;
    this.service.eliminar(avance.id).subscribe({
      next: () => this.cargar(),
      error: () => this.error.set('No se pudo eliminar el avance.'),
    });
  }

  protected nombreObra(id: number): string {
    return this.obras().find((o) => o.id === id)?.nombre ?? `Obra #${id}`;
  }
}
