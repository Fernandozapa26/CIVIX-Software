export interface Avance {
  id: number;
  obraId: number;
  registradoPorUsuarioId?: number;
  fecha: string;
  descripcion: string;
  porcentajeAvance?: number | null;
  imagenes: string[];
}

export type AvancePayload = Omit<Avance, 'id'>;
