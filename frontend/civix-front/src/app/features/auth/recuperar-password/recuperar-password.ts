import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { obtenerMensajeError } from '../../../core/utils/http-error';

/**
 * Paso 1 de "¿Olvidaste tu contraseña?": el usuario escribe su correo y la API
 * le envía un enlace para crear una nueva contraseña.
 * El mensaje de confirmación es el mismo exista o no la cuenta (no revela correos registrados).
 */
@Component({
  selector: 'app-recuperar-password',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './recuperar-password.html',
  styleUrl: './recuperar-password.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecuperarPassword {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly authService = inject(AuthService);

  protected readonly enviando = signal(false);
  protected readonly error = signal<string | null>(null);
  /** Mensaje de la API cuando la solicitud se procesó; mientras sea null se muestra el formulario. */
  protected readonly confirmacion = signal<string | null>(null);
  protected readonly correoEnviado = signal('');

  protected readonly form = this.fb.group({
    correo: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
  });

  protected campoInvalido(): boolean {
    const control = this.form.controls.correo;
    return control.invalid && control.touched;
  }

  protected enviar(): void {
    if (this.enviando()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const correo = this.form.controls.correo.value.trim();
    this.enviando.set(true);
    this.error.set(null);

    this.authService
      .solicitarRecuperacion(correo)
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: (mensaje) => {
          this.correoEnviado.set(correo);
          this.confirmacion.set(mensaje);
        },
        error: (error: unknown) => this.error.set(obtenerMensajeError(error)),
      });
  }

  /** Vuelve al formulario para escribir otro correo o pedir el enlace otra vez. */
  protected intentarDeNuevo(): void {
    this.confirmacion.set(null);
    this.form.reset({ correo: this.correoEnviado() });
  }
}
