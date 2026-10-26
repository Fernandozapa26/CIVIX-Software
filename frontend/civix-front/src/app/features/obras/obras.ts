import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ObrasService } from './obras.service';
import { Obra, ObraPayload } from './obra.model';

@Component({
  selector: 'app-obras',
  imports: [ReactiveFormsModule, DatePipe, CurrencyPipe],
  templateUrl: './obras.html',
  styleUrl: './obras.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Obras {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(ObrasService);

  protected readonly obras = signal<Obra[]>([]);
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editandoId = signal<number | null>(null);
  protected readonly mostrarFormulario = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(120)]],
    descripcion: [''],
    ubicacion: ['', Validators.required],
    fechaInicio: ['', Validators.required],
    fechaFinEstimada: [''],
    estado: ['Planificada', Validators.required],
    presupuesto: [0, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    this.cargar();
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

  protected nuevaObra(): void {
    this.editandoId.set(null);
    this.form.reset({
      nombre: '',
      descripcion: '',
      ubicacion: '',
      fechaInicio: '',
      fechaFinEstimada: '',
      estado: 'Planificada',
      presupuesto: 0,
    });
    this.mostrarFormulario.set(true);
  }

  protected editar(obra: Obra): void {
    this.editandoId.set(obra.id);
    this.form.reset({
      nombre: obra.nombre,
      descripcion: obra.descripcion ?? '',
      ubicacion: obra.ubicacion,
      fechaInicio: obra.fechaInicio?.slice(0, 10) ?? '',
      fechaFinEstimada: obra.fechaFinEstimada?.slice(0, 10) ?? '',
      estado: obra.estado,
      presupuesto: obra.presupuesto,
    });
    this.mostrarFormulario.set(true);
  }

  protected cancelar(): void {
    this.mostrarFormulario.set(false);
    this.editandoId.set(null);
  }

  protected guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload: ObraPayload = {
      nombre: value.nombre,
      descripcion: value.descripcion || null,
      ubicacion: value.ubicacion,
      fechaInicio: value.fechaInicio,
      fechaFinEstimada: value.fechaFinEstimada || null,
      estado: value.estado,
      presupuesto: value.presupuesto,
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
        this.cargar();
      },
      error: () => this.error.set('No se pudo guardar la obra.'),
    });
  }

  protected eliminar(obra: Obra): void {
    if (!confirm(`¿Eliminar la obra "${obra.nombre}"?`)) return;
    this.service.eliminar(obra.id).subscribe({
      next: () => this.cargar(),
      error: () => this.error.set('No se pudo eliminar la obra.'),
    });
  }
}