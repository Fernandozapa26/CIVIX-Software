/**
 * Configuración del frontend.
 * apiUrl debe coincidir con el puerto del perfil "http" de launchSettings.json del backend.
 * mostrarCuentasPrueba controla las tarjetas de acceso rápido del login (desactivar en producción).
 */
export const environment = {
  apiUrl: 'http://localhost:5067/api',
  mostrarCuentasPrueba: true,
};
