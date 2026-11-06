import { HttpErrorResponse } from '@angular/common/http';

const MENSAJE_POR_DEFECTO = 'Ocurrió un error inesperado. Intenta de nuevo.';

/**
 * Convierte un error HTTP en un mensaje legible para el usuario.
 * Entiende las dos formas de error del backend:
 *  - { mensaje: "..." } que devuelven los controladores de CIVIX.
 *  - { errors: { Campo: ["..."] } } que genera [ApiController] al fallar las validaciones.
 */
export function obtenerMensajeError(error: unknown, porDefecto = MENSAJE_POR_DEFECTO): string {
  if (!(error instanceof HttpErrorResponse)) {
    return porDefecto;
  }

  if (error.status === 0) {
    return 'No se pudo conectar con el servidor. Verifica que la API esté en ejecución.';
  }

  const cuerpo: unknown = error.error;

  if (cuerpo !== null && typeof cuerpo === 'object') {
    const { mensaje, errors } = cuerpo as { mensaje?: unknown; errors?: unknown };

    if (typeof mensaje === 'string' && mensaje.length > 0) {
      return mensaje;
    }

    if (errors !== null && typeof errors === 'object') {
      const primero = Object.values(errors as Record<string, unknown>)
        .flat()
        .find((valor): valor is string => typeof valor === 'string');

      if (primero) {
        return primero;
      }
    }
  }

  return porDefecto;
}
