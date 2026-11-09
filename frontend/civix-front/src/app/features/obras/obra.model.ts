export interface Obra {
  id: number;
  nombre: string;
  descripcion?: string | null;
  /** Nombre del cliente guardado en la obra (texto). */
  cliente?: string | null;
  ubicacion?: string | null;
  fechaInicio?: string | null;
  fechaFinEstimada?: string | null;
  estado: string;
  presupuesto: number;
  avance: number;
  personal: number;
  /** Usuario con rol Cliente asignado a la obra. */
  clienteId?: number | null;
  clienteNombre?: string | null;
  /** Ingeniero responsable asignado a la obra. */
  responsableId?: number | null;
  responsableNombre?: string | null;
}

/** Cuerpo de POST/PUT /api/obras. */
export interface ObraPayload {
  nombre: string;
  descripcion: string | null;
  ubicacion: string | null;
  fechaInicio: string | null;
  fechaFinEstimada: string | null;
  estado: string;
  presupuesto: number;
  avance: number;
  personal: number;
  clienteId: number;
  responsableId: number;
}

/** Usuario para los selectores de cliente y responsable. */
export interface UsuarioOpcion {
  id: number;
  nombre: string;
  correo: string;
}
