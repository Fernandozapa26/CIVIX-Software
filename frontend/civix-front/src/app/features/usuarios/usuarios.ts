import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { forkJoin } from 'rxjs';

import { obtenerMensajeError } from '../../core/utils/http-error';
import { UsuarioOpcion } from '../obras/obra.model';
import { ObrasService } from '../obras/obras.service';
import { UsuariosService } from './usuarios.service';

type RolListado = 'Ingeniero' | 'Cliente';
type FiltroRol = 'Todos' | RolListado;

interface UsuarioFila extends UsuarioOpcion {
  rol: RolListado;
}

/** Mismas reglas que RegisterDto en el backend: mínimo 8 caracteres, con letras y números. */
const PATRON_PASSWORD = /^(?=.*[A-Za-z])(?=.*\d).+$/;

const passwordsCoinciden: ValidatorFn = (grupo: AbstractControl): ValidationErrors | null => {
  const password = grupo.get('password')?.value as string | undefined;
  const confirmar = grupo.get('confirmar')?.value as string | undefined;
  return password === confirmar ? null : { noCoinciden: true };
};

/**
 * Administración de usuarios (solo Administrador).
 * Lista los ingenieros y clientes activos (GET /api/catalogos/usuarios?rol=...) y permite
 * crear usuarios con su rol. La API todavía no tiene endpoints para editar ni desactivar.
 */
@Component({
  selector: 'app-usuarios',
  imports: [ReactiveFormsModule],
  templateUrl: './usuarios.html',
  styleUrl: './usuarios.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'cerrarFormulario()',
  },
})
export class Usuarios implements OnInit {
  private readonly obrasService = inject(ObrasService);
  private readonly usuariosService = inject(UsuariosService);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly usuarios = signal<UsuarioFila[]>([]);
  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly mensajeExito = signal<string | null>(null);

  protected readonly filtros: readonly FiltroRol[] = ['Todos', 'Ingeniero', 'Cliente'];
  protected readonly filtro = signal<FiltroRol>('Todos');
  protected readonly busqueda = signal('');

  protected readonly mostrarFormulario = signal(false);
  protected readonly guardando = signal(false);
  protected readonly errorFormulario = signal<string | null>(null);

  protected readonly conteo = computed(() => {
    const lista = this.usuarios();
    return {
      Todos: lista.length,
      Ingeniero: lista.filter((u) => u.rol === 'Ingeniero').length,
      Cliente: lista.filter((u) => u.rol === 'Cliente').length,
    } satisfies Record<FiltroRol, number>;
  });

  protected readonly usuariosFiltrados = computed(() => {
    const filtro = this.filtro();
    const texto = this.busqueda().trim().toLowerCase();

    return this.usuarios().filter(
      (u) =>
        (filtro === 'Todos' || u.rol === filtro) &&
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
    this.cargar();
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.error.set(null);

    forkJoin({
      ingenieros: this.obrasService.listarUsuarios('Ingeniero'),
      clientes: this.obrasService.listarUsuarios('Cliente'),
    }).subscribe({
      next: ({ ingenieros, clientes }) => {
        const filas: UsuarioFila[] = [
          ...ingenieros.map((u) => ({ ...u, rol: 'Ingeniero' as const })),
          ...clientes.map((u) => ({ ...u, rol: 'Cliente' as const })),
        ].sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'));

        this.usuarios.set(filas);
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

  protected abrirFormulario(): void {
    this.form.reset({ nombre: '', correo: '', rolId: 2, password: '', confirmar: '' });
    this.errorFormulario.set(null);
    this.mostrarFormulario.set(true);
  }

  protected cerrarFormulario(): void {
    if (!this.guardando()) {
      this.mostrarFormulario.set(false);
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

    const { nombre, correo, rolId, password } = this.form.getRawValue();
    this.guardando.set(true);
    this.errorFormulario.set(null);

    this.usuariosService
      .crear({ nombre: nombre.trim(), correo: correo.trim(), password, rolId: Number(rolId) })
      .subscribe({
        next: () => {
          this.guardando.set(false);
          this.mostrarFormulario.set(false);
          this.mensajeExito.set(`Se creó el usuario ${nombre.trim()}.`);
          this.cargar();
        },
        error: (err: unknown) => {
          this.guardando.set(false);
          this.errorFormulario.set(obtenerMensajeError(err, 'No se pudo crear el usuario.'));
        },
      });
  }
}
