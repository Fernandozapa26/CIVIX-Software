import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';

import { RolCivix } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { obtenerMensajeError } from '../../core/utils/http-error';
import { UsuarioAdmin, UsuariosService } from './usuarios.service';

type FiltroRol = 'Todos' | RolCivix;

/** Mismas reglas que el backend: mínimo 8 caracteres, con letras y números. */
const PATRON_PASSWORD = /^(?=.*[A-Za-z])(?=.*\d).+$/;

const ROL_POR_ID: Record<number, RolCivix> = { 1: 'Administrador', 2: 'Ingeniero', 3: 'Cliente' };

const passwordsCoinciden: ValidatorFn = (grupo: AbstractControl): ValidationErrors | null => {
  const password = (grupo.get('password')?.value as string | undefined) ?? '';
  const confirmar = (grupo.get('confirmar')?.value as string | undefined) ?? '';
  return password === confirmar ? null : { noCoinciden: true };
};

/**
 * Administración de usuarios (solo Administrador).
 * Lista todos los usuarios con su rol, estado, fecha de registro y obras asignadas;
 * permite crear, editar, desactivar y reactivar cuentas.
 */
@Component({
  selector: 'app-usuarios',
  imports: [ReactiveFormsModule],
  templateUrl: './usuarios.html',
  styleUrl: './usuarios.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'cerrarVentanas()',
  },
})
export class Usuarios implements OnInit {
  private readonly usuariosService = inject(UsuariosService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly usuarios = signal<UsuarioAdmin[]>([]);
  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly mensajeExito = signal<string | null>(null);

  protected readonly filtros: readonly FiltroRol[] = ['Todos', 'Administrador', 'Ingeniero', 'Cliente'];
  protected readonly filtro = signal<FiltroRol>('Todos');
  protected readonly busqueda = signal('');
  protected readonly mostrarInactivos = signal(true);

  /** null = formulario cerrado; 0 = nuevo usuario; > 0 = id del usuario que se edita. */
  protected readonly editandoId = signal<number | null>(null);
  protected readonly guardando = signal(false);
  protected readonly errorFormulario = signal<string | null>(null);

  protected readonly usuarioADesactivar = signal<UsuarioAdmin | null>(null);
  protected readonly procesandoId = signal<number | null>(null);

  protected readonly miId = computed(() => this.auth.usuario()?.usuarioId ?? 0);

  protected readonly conteo = computed(() => {
    const lista = this.usuarios();
    const contar = (rol: RolCivix) => lista.filter((u) => u.roles.includes(rol)).length;
    return {
      Todos: lista.length,
      Administrador: contar('Administrador'),
      Ingeniero: contar('Ingeniero'),
      Cliente: contar('Cliente'),
    } satisfies Record<FiltroRol, number>;
  });

  protected readonly totalInactivos = computed(() => this.usuarios().filter((u) => !u.activo).length);

  protected readonly usuariosFiltrados = computed(() => {
    const filtro = this.filtro();
    const texto = this.busqueda().trim().toLowerCase();
    const inactivos = this.mostrarInactivos();

    return this.usuarios().filter(
      (u) =>
        (filtro === 'Todos' || u.roles.includes(filtro)) &&
        (inactivos || u.activo) &&
        (texto === '' ||
          u.nombre.toLowerCase().includes(texto) ||
          u.correo.toLowerCase().includes(texto)),
    );
  });

  protected readonly form = this.fb.group(
    {
      nombre: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
      correo: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      rolId: [2, [Validators.required]],
      activo: [true],
      password: [''],
      confirmar: [''],
    },
    { validators: passwordsCoinciden },
  );

  ngOnInit(): void {
    this.cargar();
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.error.set(null);

    this.usuariosService.listar().subscribe({
      next: (lista) => {
        this.usuarios.set(lista);
        this.cargando.set(false);
      },
      error: (err: unknown) => {
        this.error.set(obtenerMensajeError(err, 'No se pudieron cargar los usuarios.'));
        this.cargando.set(false);
      },
    });
  }

  protected iniciales(nombre: string): string {
    return nombre
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((parte) => parte.charAt(0).toUpperCase())
      .join('');
  }

  protected rolPrincipal(usuario: UsuarioAdmin): RolCivix | null {
    return usuario.roles[0] ?? (usuario.rolId ? ROL_POR_ID[usuario.rolId] : null);
  }

  protected claseRol(rol: RolCivix | null): string {
    switch (rol) {
      case 'Administrador':
        return 'cx-chip cx-chip--danger';
      case 'Ingeniero':
        return 'cx-chip cx-chip--info';
      case 'Cliente':
        return 'cx-chip cx-chip--warning';
      default:
        return 'cx-chip cx-chip--muted';
    }
  }

  protected formatearFecha(fecha: string): string {
    const valor = new Date(fecha);
    return Number.isNaN(valor.getTime())
      ? '—'
      : valor.toLocaleDateString('es-CO', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  // ---------- Formulario (crear / editar) ----------

  protected esEdicion(): boolean {
    return (this.editandoId() ?? 0) > 0;
  }

  protected abrirNuevo(): void {
    this.prepararFormulario(true);
    this.form.reset({ nombre: '', correo: '', rolId: 2, activo: true, password: '', confirmar: '' });
    this.editandoId.set(0);
  }

  protected abrirEdicion(usuario: UsuarioAdmin): void {
    this.prepararFormulario(false);
    this.form.reset({
      nombre: usuario.nombre,
      correo: usuario.correo,
      rolId: usuario.rolId ?? 2,
      activo: usuario.activo,
      password: '',
      confirmar: '',
    });
    this.editandoId.set(usuario.id);
  }

  /** Al crear, la contraseña es obligatoria; al editar es opcional (vacía = no cambiar). */
  private prepararFormulario(passwordObligatoria: boolean): void {
    const password = this.form.controls.password;
    const reglas = [Validators.minLength(8), Validators.maxLength(100), Validators.pattern(PATRON_PASSWORD)];
    password.setValidators(passwordObligatoria ? [Validators.required, ...reglas] : reglas);
    this.form.controls.confirmar.setValidators(passwordObligatoria ? [Validators.required] : []);
    password.updateValueAndValidity();
    this.form.controls.confirmar.updateValueAndValidity();
    this.errorFormulario.set(null);
  }

  protected cerrarFormulario(): void {
    if (!this.guardando()) {
      this.editandoId.set(null);
    }
  }

  protected cerrarVentanas(): void {
    if (this.usuarioADesactivar()) {
      this.usuarioADesactivar.set(null);
    } else {
      this.cerrarFormulario();
    }
  }

  protected invalido(control: AbstractControl): boolean {
    return control.invalid && control.touched;
  }

  protected confirmacionInvalida(): boolean {
    const control = this.form.controls.confirmar;
    return (control.invalid || this.form.hasError('noCoinciden')) && control.touched;
  }

  protected guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { nombre, correo, rolId, activo, password } = this.form.getRawValue();
    const id = this.editandoId() ?? 0;

    this.guardando.set(true);
    this.errorFormulario.set(null);

    const peticion =
      id > 0
        ? this.usuariosService.actualizar(id, {
            nombre: nombre.trim(),
            correo: correo.trim(),
            rolId: Number(rolId),
            activo,
            password: password ? password : null,
          })
        : this.usuariosService.crear({
            nombre: nombre.trim(),
            correo: correo.trim(),
            password,
            rolId: Number(rolId),
          });

    peticion.subscribe({
      next: () => {
        this.guardando.set(false);
        this.editandoId.set(null);
        this.mensajeExito.set(
          id > 0 ? `Se actualizó el usuario ${nombre.trim()}.` : `Se creó el usuario ${nombre.trim()}.`,
        );
        this.cargar();
      },
      error: (err: unknown) => {
        this.guardando.set(false);
        this.errorFormulario.set(obtenerMensajeError(err, 'No se pudo guardar el usuario.'));
      },
    });
  }

  // ---------- Activar / desactivar ----------

  protected pedirDesactivar(usuario: UsuarioAdmin): void {
    this.usuarioADesactivar.set(usuario);
  }

  protected confirmarDesactivar(): void {
    const usuario = this.usuarioADesactivar();
    if (!usuario) return;

    this.cambiarEstado(usuario, false);
  }

  protected reactivar(usuario: UsuarioAdmin): void {
    this.cambiarEstado(usuario, true);
  }

  private cambiarEstado(usuario: UsuarioAdmin, activo: boolean): void {
    this.procesandoId.set(usuario.id);
    const peticion = activo
      ? this.usuariosService.reactivar(usuario.id)
      : this.usuariosService.desactivar(usuario.id);

    peticion.subscribe({
      next: (actualizado) => {
        this.procesandoId.set(null);
        this.usuarioADesactivar.set(null);
        this.usuarios.update((lista) => lista.map((u) => (u.id === actualizado.id ? actualizado : u)));
        this.mensajeExito.set(
          activo ? `${usuario.nombre} puede volver a iniciar sesión.` : `${usuario.nombre} quedó inactivo.`,
        );
      },
      error: (err: unknown) => {
        this.procesandoId.set(null);
        this.usuarioADesactivar.set(null);
        this.error.set(obtenerMensajeError(err, 'No se pudo cambiar el estado del usuario.'));
      },
    });
  }
}
