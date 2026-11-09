/** Estados de una cotización (dbo.Cotizaciones.Estado). */
export type EstadoCotizacion = 'Borrador' | 'Aprobada' | 'Rechazada' | 'Anulada';

/** Fila del listado (GET /api/cotizaciones). */
export interface CotizacionResumen {
  id: number;
  numero: string;
  fecha: string;
  nombreProyecto: string;
  clienteId: number | null;
  clienteNombre: string | null;
  creadoPorUsuarioId: number;
  creadoPor: string;
  estado: EstadoCotizacion;
  activo: boolean;
  total: number;
  cantidadItems: number;
  obraId: number | null;
  obraNombre: string | null;
}

export interface ItemCotizacion {
  id: number;
  concepto: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
}

/** Cotización completa (GET /api/cotizaciones/{id}). */
export interface CotizacionDetalle extends CotizacionResumen {
  clienteCorreo: string | null;
  ubicacion: string | null;
  descripcion: string | null;
  fechaDecision: string | null;
  decididaPor: string | null;
  items: ItemCotizacion[];
}

/** Cuerpo de POST y PUT /api/cotizaciones. */
export interface GuardarCotizacionPayload {
  nombreProyecto: string;
  clienteId: number;
  fecha: string | null;
  ubicacion: string | null;
  descripcion: string | null;
  items: { concepto: string; cantidad: number; precioUnitario: number }[];
}

/** Cuerpo de POST /api/cotizaciones/{id}/aprobar. */
export interface AprobarCotizacionPayload {
  responsableId: number;
  nombreObra: string | null;
  fechaInicio: string | null;
  fechaFinEstimada: string | null;
}

/** Respuesta de las operaciones que guardan: { mensaje, datos }. */
export interface RespuestaCotizacion {
  mensaje: string;
  datos: CotizacionDetalle;
}
