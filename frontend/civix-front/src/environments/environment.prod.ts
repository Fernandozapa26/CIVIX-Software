import { CuentaPrueba } from '../app/core/models/auth.models';

/**
 * Configuración del frontend para PRODUCCIÓN (ng build).
 * Reemplaza a environment.ts gracias a "fileReplacements" en angular.json.
 *
 * apiUrl: cambiar por la dirección real de la API al desplegar.
 * cuentasPrueba: siempre vacío en producción (no se muestran ni se publican contraseñas).
 */
export const environment: { apiUrl: string; cuentasPrueba: readonly CuentaPrueba[] } = {
  apiUrl: 'http://localhost:5067/api',
  cuentasPrueba: [],
};
