/**
 * Datos de entrada de las gráficas de CIVIX.
 * Todas se dibujan con SVG o CSS propios: no se usa ninguna librería de gráficas.
 */

/** Una parte de un total (dona, barra segmentada). */
export interface SegmentoGrafica {
  etiqueta: string;
  valor: number;
  /** Color CSS, normalmente una variable de la paleta: 'var(--cx-verde)'. */
  color: string;
}

/** Un punto de una serie (columnas por semana). */
export interface PuntoGrafica {
  /** Texto corto bajo la columna, ej. "3 nov". */
  etiqueta: string;
  valor: number;
  /** Texto completo para lectores de pantalla y para el título emergente. */
  descripcion: string;
}

/** Colores fijos por estado, compartidos por todas las gráficas. */
export const COLORES_GRAFICA = {
  verde: 'var(--cx-verde)',
  amarillo: 'var(--cx-amarillo)',
  rojo: 'var(--cx-rojo)',
  pizarra: 'var(--cx-pizarra)',
  gris: 'var(--cx-texto-secundario)',
} as const;
