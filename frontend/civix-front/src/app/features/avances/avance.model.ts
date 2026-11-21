export interface Avance {
  id: number;
  obraId: number;
  registradoPorUsuarioId?: number;
  /** Nombre de quien registró el avance (lo envía GET /api/avances). */
  registradoPorNombre?: string | null;
  fecha: string;
  descripcion: string;
  porcentajeAvance?: number | null;
  imagenes: string[];
}

export type AvancePayload = Omit<Avance, 'id' | 'registradoPorNombre'>;
