export interface Obra {
  id: number;
  nombre: string;
  descripcion?: string | null;
  ubicacion: string;
  fechaInicio: string;
  fechaFinEstimada?: string | null;
  estado: string;
  presupuesto: number;
}

export type ObraPayload = Omit<Obra, 'id'>;