/**
 * Etiquetas visuales de estado compartidas por todos los módulos.
 * Devuelven las clases globales .cx-chip definidas en styles.scss.
 */

function normalizar(texto: string): string {
  return texto
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '');
}

/** Estados de obra: En ejecución, En planeación, Planificada, Pausada, Suspendida, Finalizada. */
export function claseEstadoObra(estado: string): string {
  const valor = normalizar(estado);

  if (valor.includes('ejecuci')) {
    return 'cx-chip cx-chip--success';
  }
  if (valor.includes('finaliz')) {
    return 'cx-chip cx-chip--muted';
  }
  if (valor.includes('pausa') || valor.includes('suspend')) {
    return 'cx-chip cx-chip--danger';
  }
  return 'cx-chip cx-chip--info';
}

/** Estados de cotización: Borrador, Aprobada, Rechazada, Anulada. */
export function claseEstadoCotizacion(estado: string): string {
  const valor = normalizar(estado);

  if (valor.includes('aprobad')) {
    return 'cx-chip cx-chip--success';
  }
  if (valor.includes('rechazad')) {
    return 'cx-chip cx-chip--danger';
  }
  if (valor.includes('anulad')) {
    return 'cx-chip cx-chip--muted';
  }
  return 'cx-chip cx-chip--warning';
}
