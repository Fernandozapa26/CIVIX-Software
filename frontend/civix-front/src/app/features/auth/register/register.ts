import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService, RUTA_INICIO } from '../../../core/services/auth.service';
import { obtenerMensajeError } from '../../../core/utils/http-error';

/** Mismas reglas que RegisterDto en el backend: mínimo 8 caracteres, con letras y números. */
const PATRON_PASSWORD = /^(?=.*[A-Za-z])(?=.*\d).+$/;

/** Valida que "password" y "confirmar" sean iguales. */
const passwordsCoinciden: ValidatorFn = (grupo: AbstractControl): ValidationErrors | null => {
  const password = grupo.get('password')?.value as string | undefined;
  const confirmar = grupo.get('confirmar')?.value as string | undefined;
  return password === confirmar ? null : { noCoinciden: true };
};

type CampoRegistro = 'nombre' | 'correo' | 'password' | 'confirmar';

/**
 * Registro público. El backend asigna siempre el rol Cliente a estas cuentas;
 * solo un Administrador puede crear Ingenieros u otros Administradores.
 */
@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Register {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly enviando = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.group(
    {
      nombre: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
      correo: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
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

  protected campoInvalido(campo: CampoRegistro): boolean {
    const control = this.form.controls[campo];
    const invalido = control.invalid || (campo === 'confirmar' && this.form.hasError('noCoinciden'));
    return invalido && control.touched;
  }

  /** Mensaje del primer error del campo, o cadena vacía si no hay error. */
  protected mensajeCampo(campo: CampoRegistro): string {
    const control = this.form.controls[campo];

    if (control.hasError('required')) {
      return 'Este campo es obligatorio.';
    }

    switch (campo) {
      case 'nombre':
        return 'El nombre debe tener entre 3 y 150 caracteres.';
      case 'correo':
        return 'El correo no tiene un formato válido.';
      case 'password':
        return 'Usa al menos 8 caracteres, con letras y números.';
      case 'confirmar':
        return 'Las contraseñas no coinciden.';
    }
  }

  protected enviar(): void {
    if (this.enviando()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { nombre, correo, password } = this.form.getRawValue();

    this.enviando.set(true);
    this.error.set(null);

    this.authService
      .register({ nombre: nombre.trim(), correo: correo.trim(), password })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => {
          void this.router.navigateByUrl(RUTA_INICIO);
        },
        error: (error: unknown) => {
          this.error.set(obtenerMensajeError(error));
        },
      });
  }
}
