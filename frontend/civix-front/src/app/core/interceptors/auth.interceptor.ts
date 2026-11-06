import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

/**
 * Se ejecuta en cada petición HTTP:
 * 1. Si la petición va hacia la API de CIVIX y hay sesión, agrega "Authorization: Bearer <token>".
 * 2. Si la API responde 401 a una petición que llevaba token, la sesión venció: se cierra y se vuelve al login.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const token = req.url.startsWith(environment.apiUrl) ? authService.obtenerToken() : null;

  const peticion = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(peticion).pipe(
    catchError((error: unknown) => {
      if (token && error instanceof HttpErrorResponse && error.status === 401) {
        authService.logout('expirada');
      }

      return throwError(() => error);
    }),
  );
};
