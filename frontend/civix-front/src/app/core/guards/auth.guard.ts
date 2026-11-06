import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { RolCivix } from '../models/auth.models';
import { AuthService, RUTA_INICIO } from '../services/auth.service';

/**
 * Protege las rutas privadas.
 * - Sin sesión vigente: redirige a /login y recuerda la ruta pedida (returnUrl).
 * - Con sesión, pero sin el rol que la ruta exige en data.roles: redirige a la ruta de inicio.
 *
 * Uso en app.routes.ts:
 *   { path: 'materiales', canActivate: [authGuard], data: { roles: ROLES_GESTION }, ... }
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.sesionVigente()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }

  const rolesPermitidos = (route.data['roles'] as readonly RolCivix[] | undefined) ?? [];

  if (authService.tieneAlgunRol(rolesPermitidos)) {
    return true;
  }

  return router.createUrlTree([RUTA_INICIO]);
};

/** Evita que un usuario con sesión activa vuelva a ver el login o el registro. */
export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.sesionVigente() ? router.createUrlTree([RUTA_INICIO]) : true;
};
