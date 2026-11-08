/** Estados de un material en una obra (dbo.ObraMateriales.Estado). */
export type EstadoMaterial = 'Pedido' | 'En bodega' | 'Retirado';

/** Estados que el usuario puede elegir; "Retirado" lo asigna el sistema. */
export const ESTADOS_SELECCIONABLES: readonly EstadoMaterial[] = ['Pedido', 'En bodega'];

/** Material del catálogo (GET /api/materiales). */
export interface Material {
  id: number;
  nombre: string;
  unidadMedida: string;
  descripcion: string | null;
  activo: boolean;
}

/** Cuerpo de POST/PUT /api/materiales. */
export interface MaterialPayload {
  nombre: string;
  unidadMedida: string;
  descripcion: string | null;
}

/** Material registrado en una obra (GET /api/obras/{obraId}/materiales). */
export interface ObraMaterial {
  id: number;
  obraId: number;
  obraNombre: string;
  materialId: number;
  materialNombre: string;
  unidadMedida: string;
  cotizacionId: number | null;
  cantidad: number;
  cantidadRetirada: number;
  cantidadDisponible: number;
  estado: EstadoMaterial;
  fechaRegistro: string;
  registradoPor: string;
  totalRetiros: number;
}

/** Cuerpo de POST /api/obras/{obraId}/materiales. */
export interface RegistrarObraMaterialPayload {
  materialId: number;
  cantidad: number;
  estado: EstadoMaterial;
  cotizacionId: number | null;
}

/** Cuerpo de PUT /api/obras/{obraId}/materiales/{id}. */
export type ActualizarObraMaterialPayload = Omit<RegistrarObraMaterialPayload, 'materialId'>;

/** Retiro de bodega (GET /api/retiros-material?obraId=). */
export interface RetiroMaterial {
  id: number;
  obraMaterialId: number;
  materialNombre: string;
  unidadMedida: string;
  obraOrigenId: number;
  obraOrigenNombre: string;
  obraDestinoId: number;
  obraDestinoNombre: string;
  cantidad: number;
  retiradoPor: string;
  registradoPor: string;
  fechaRetiro: string;
  observacion: string | null;
}

/** Cuerpo de POST /api/retiros-material. */
export interface RegistrarRetiroPayload {
  obraMaterialId: number;
  cantidad: number;
  retiradoPor: string;
  obraDestinoId: number;
  observacion: string | null;
}

/** Opción del selector de obras (GET /api/catalogos/obras). */
export interface ObraOpcion {
  id: number;
  nombre: string;
  estado: string;
  ubicacion: string | null;
}

/** Opción del selector de cotizaciones (GET /api/catalogos/cotizaciones). */
export interface CotizacionOpcion {
  id: number;
  fecha: string;
  total: number;
  estado: string;
}

/** Respuesta de las operaciones que guardan: { mensaje, datos }. */
export interface RespuestaGuardado<T> {
  mensaje: string;
  datos: T;
}
