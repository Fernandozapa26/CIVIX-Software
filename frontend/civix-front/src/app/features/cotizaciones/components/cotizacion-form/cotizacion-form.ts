import { Component, computed, inject, signal } from '@angular/core';
import {FormArray, FormBuilder,ReactiveFormsModule,Validators} from '@angular/forms';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-cotizacion-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './cotizacion-form.html',
  styleUrl: './cotizacion-form.scss'
})
export class CotizacionForm {

  private fb = inject(FormBuilder);

  readonly enviado = signal(false);

  readonly formulario = this.fb.group({
    clienteId: [null as number | null, Validators.required],
    descripcion: ['', [Validators.required, Validators.maxLength(500)]],
    fecha: ['', Validators.required],
    detalles: this.fb.array([this.crearItem()])
  });

  get detalles(): FormArray {
    return this.formulario.controls.detalles;
  }

  crearItem() {
    return this.fb.group({
      descripcion: ['', Validators.required],
      cantidad: [1, [Validators.required, Validators.min(0.01)]],
      valorUnitario: [0, [Validators.required, Validators.min(0)]]
    });
  }

  agregarItem(): void {
    this.detalles.push(this.crearItem());
  }

  eliminarItem(index: number): void {
    if (this.detalles.length > 1) {
      this.detalles.removeAt(index);
    }
  }

  calcularSubtotal(index: number): number {
    const item = this.detalles.at(index);

    const cantidad = Number(item.get('cantidad')?.value ?? 0);
    const valorUnitario = Number(item.get('valorUnitario')?.value ?? 0);

    return cantidad * valorUnitario;
  }

  calcularTotal(): number {
    return this.detalles.controls.reduce((total, item) => {
      const cantidad = Number(item.get('cantidad')?.value ?? 0);
      const valorUnitario = Number(item.get('valorUnitario')?.value ?? 0);

      return total + cantidad * valorUnitario;
    }, 0);
  }

  guardar(): void {
    this.enviado.set(true);

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const cotizacion = {
      ...this.formulario.getRawValue(),
      total: this.calcularTotal()
    };

    console.log('Cotización validada:', cotizacion);
  }
}