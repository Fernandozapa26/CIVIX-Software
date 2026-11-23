import { CuentaPrueba } from '../app/core/models/auth.models';

/**
 * Configuración del frontend para DESARROLLO (ng serve).
 * apiUrl debe coincidir con el puerto del perfil "http" de launchSettings.json del backend.
 *
 * cuentasPrueba: tarjetas de acceso rápido del login. Son las cuentas que crea
 * database/scripts/002_datos_prueba_auth.sql. Solo existen en este archivo:
 * el build de producción usa environment.prod.ts, que no las incluye, así que
 * las contraseñas no llegan al JavaScript publicado.
 */
export const environment: { apiUrl: string; cuentasPrueba: readonly CuentaPrueba[] } = {
  apiUrl: 'http://localhost:5067/api',
  cuentasPrueba: [
    { rol: 'Administrador', icono: 'escudo', correo: 'admin@civix.co', password: 'Admin123' },
    { rol: 'Ingeniero', icono: 'casco', correo: 'ingeniero@civix.co', password: 'Ingeniero123' },
    { rol: 'Cliente', icono: 'usuario', correo: 'cliente@civix.co', password: 'Cliente123' },
  ],
};
