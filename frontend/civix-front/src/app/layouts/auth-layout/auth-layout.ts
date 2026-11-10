import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

/**
 * Pantalla dividida del prototipo de Figma:
 * izquierda, panel de marca CIVIX; derecha, el formulario (login o registro) vía <router-outlet>.
 */
@Component({
  selector: 'app-auth-layout',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './auth-layout.html',
  styleUrl: './auth-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthLayout {}
