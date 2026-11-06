import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { ROLES_GESTION } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-main-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainLayout {
  private readonly authService = inject(AuthService);

  protected readonly usuario = this.authService.usuario;
  protected readonly esSoloLectura = this.authService.esSoloLectura;

  /** El enlace de Materiales solo se muestra a Administrador e Ingeniero (igual que la ruta). */
  protected readonly puedeVerMateriales = computed(() =>
    this.authService.tieneAlgunRol(ROLES_GESTION),
  );

  protected readonly rolesTexto = computed(() => this.usuario()?.roles.join(', ') ?? '');

  protected cerrarSesion(): void {
    this.authService.logout();
  }
}
