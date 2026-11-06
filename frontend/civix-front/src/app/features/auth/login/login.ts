import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AuthService, RUTA_INICIO } from '../../../core/services/auth.service';
import { obtenerMensajeError } from '../../../core/utils/http-error';

interface CuentaPrueba {
  rol: string;
  icono: 'escudo' | 'casco' | 'usuario';
  correo: string;
  password: string;
}

/** Cuentas creadas por database/scripts/002_datos_prueba_auth.sql. */
const CUENTAS_PRUEBA: readonly CuentaPrueba[] = [
  { rol: 'Administrador', icono: 'escudo', correo: 'admin@civix.co', password: 'Admin123' },
  { rol: 'Ingeniero', icono: 'casco', correo: 'ingeniero@civix.co', password: 'Ingeniero123' },
  { rol: 'Cliente', icono: 'usuario', correo: 'cliente@civix.co', password: 'Cliente123' },
];

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly cuentasPrueba = environment.mostrarCuentasPrueba ? CUENTAS_PRUEBA : [];
  protected readonly sesionExpirada = this.route.snapshot.queryParamMap.has('sesionExpirada');
  protected readonly enviando = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.group({
    correo: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  /** Acceso rápido: llena el formulario con la cuenta de prueba e inicia sesión. */
  protected usarCuenta(cuenta: CuentaPrueba): void {
    this.form.setValue({ correo: cuenta.correo, password: cuenta.password });
    this.enviar();
  }

  protected campoInvalido(campo: 'correo' | 'password'): boolean {
    const control = this.form.controls[campo];
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

    this.enviando.set(true);
    this.error.set(null);

    this.authService
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => {
          if (!this.authService.sesionVigente()) {
            this.error.set('Tu usuario no tiene un rol asignado. Contacta al administrador.');
            return;
          }

          void this.router.navigateByUrl(this.obtenerRutaDestino());
        },
        error: (error: unknown) => {
          this.error.set(obtenerMensajeError(error));
        },
      });
  }

  /** Vuelve a la página que el usuario intentó abrir antes de iniciar sesión, si es interna. */
  private obtenerRutaDestino(): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    return returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : RUTA_INICIO;
  }
}
