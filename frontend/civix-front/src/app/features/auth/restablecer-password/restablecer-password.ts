import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { obtenerMensajeError } from '../../../core/utils/http-error';

/** Mismas reglas que el registro y el backend: mínimo 8 caracteres, con letras y números. */
const PATRON_PASSWORD = /^(?=.*[A-Za-z])(?=.*\d).+$/;

const passwordsCoinciden: ValidatorFn = (grupo: AbstractControl): ValidationErrors | null => {
  const password = grupo.get('password')?.value as string | undefined;
  const confirmar = grupo.get('confirmar')?.value as string | undefined;
  return password === confirmar ? null : { noCoinciden: true };
};

type Estado = 'validando' | 'formulario' | 'invalido' | 'listo';

/**
 * Paso 2 de "¿Olvidaste tu contraseña?": se abre desde el enlace del correo
 * (/restablecer-password?token=...). Primero comprueba que el enlace siga vigente
 * y después permite escribir la nueva contraseña.
 */
@Component({
  selector: 'app-restablecer-password',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './restablecer-password.html',
  styleUrl: './restablecer-password.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RestablecerPassword implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  private token = '';

  protected readonly estado = signal<Estado>('validando');
  protected readonly enviando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly mensajeInvalido = signal(
    'El enlace no es válido o ya venció. Solicita uno nuevo.',
  );
  protected readonly mensajeListo = signal('');

  protected readonly form = this.fb.group(
    {
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.maxLength(100),
          Validators.pattern(PATRON_PASSWORD),
        ],
      ],
      confirmar: ['', [Validators.required]],
    },
    { validators: passwordsCoinciden },
  );

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token')?.trim() ?? '';

    if (!this.token) {
      this.estado.set('invalido');
      return;
    }

    this.authService.validarTokenRecuperacion(this.token).subscribe({
      next: () => this.estado.set('formulario'),
      error: (error: unknown) => {
        this.mensajeInvalido.set(obtenerMensajeError(error, this.mensajeInvalido()));
        this.estado.set('invalido');
      },
    });
  }

  protected campoInvalido(campo: 'password' | 'confirmar'): boolean {
    const control = this.form.controls[campo];
    const invalido = control.invalid || (campo === 'confirmar' && this.form.hasError('noCoinciden'));
    return invalido && control.touched;
  }

  protected mensajeCampo(campo: 'password' | 'confirmar'): string {
    const control = this.form.controls[campo];

    if (control.hasError('required')) {
      return 'Este campo es obligatorio.';
    }

    return campo === 'password'
      ? 'Usa al menos 8 caracteres, con letras y números.'
      : 'Las contraseñas no coinciden.';
  }

  protected enviar(): void {
    if (this.enviando()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.enviando.set(true);
    this.error.set(null);

    this.authService
      .restablecerPassword(this.token, this.form.controls.password.value)
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: (mensaje) => {
          this.mensajeListo.set(mensaje);
          this.estado.set('listo');
        },
        error: (error: unknown) => this.error.set(obtenerMensajeError(error)),
      });
  }
}
