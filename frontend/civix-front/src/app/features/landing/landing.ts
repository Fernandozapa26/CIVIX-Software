import { ChangeDetectionStrategy, Component, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

export const CORREO_CONTACTO = "civix@correo.com";

@Component({
  selector: 'app-landing',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Landing {
  protected readonly correoContacto = CORREO_CONTACTO;
  protected readonly anioActual = new Date().getFullYear();

  private readonly fb = new FormBuilder();

  protected readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.minLength(2)]],
    correo: ['', [Validators.required, Validators.email]],
    mensaje: ['', [Validators.required, Validators.minLength(10)]],
  });

  protected enviarContacto(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { nombre, correo, mensaje } = this.form.getRawValue();
    const asunto = encodeURIComponent(`Contacto CIVIX - ${nombre}`);
    const cuerpo = encodeURIComponent(
      `Nombre: ${nombre}\nCorreo: ${correo}\n\nMensaje:\n${mensaje}`,
    );
    window.location.href = `mailto:${this.correoContacto}?subject=${asunto}&body=${cuerpo}`;
  }

  protected irA(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}