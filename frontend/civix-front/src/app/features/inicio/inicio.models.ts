/** Respuesta de GET /api/inicio. Cotizaciones y Materiales vienen en null para el Cliente. */
export interface ResumenInicio {
  obrasTotal: number;
  avancePromedio: number;
  obrasPorEstado: { estado: string; cantidad: number }[];
  obras: ObraInicio[];
  avancesRecientes: AvanceInicio[];
  cotizaciones: CotizacionesInicio | null;
  materiales: MaterialesInicio | null;
}

export interface ObraInicio {
  id: number;
  nombre: string;
  estado: string;
  ubicacion: string | null;
  avance: number;
}

export interface AvanceInicio {
  id: number;
  obraId: number;
  obraNombre: string;
  fecha: string;
  descripcion: string;
  porcentajeAvance: number | null;
  imagen: string | null;
}

export interface CotizacionesInicio {
  borrador: number;
  aprobadas: number;
  rechazadas: number;
  valorEnBorrador: number;
  pendientes: {
    id: number;
    numero: string;
    nombreProyecto: string;
    clienteNombre: string | null;
    fecha: string;
    total: number;
  }[];
}

export interface MaterialesInicio {
  pedidos: number;
  enBodega: number;
  retirados: number;
  retirosUltimos7Dias: number;
}
