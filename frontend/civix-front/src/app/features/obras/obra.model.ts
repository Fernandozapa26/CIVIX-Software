export interface Obra {
  id: number;
  nombre: string;
  descripcion?: string | null;
  cliente: string;
  ubicacion: string;
  fechaInicio: string;
  fechaFinEstimada?: string | null;
  estado: string;
  presupuesto: number;
  avance: number;
  personal: number;
}

export type ObraPayload = Omit<Obra, 'id'>;