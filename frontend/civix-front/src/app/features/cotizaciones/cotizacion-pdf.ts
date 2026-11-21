import { CotizacionDetalle } from './cotizaciones.models';
import { formatearFechaCorta, formatearPesos } from './cotizaciones.service';

const COLOR_PRIMARIO: [number, number, number] = [18, 48, 58];
const COLOR_ACENTO: [number, number, number] = [245, 166, 35];
const COLOR_TEXTO_SUAVE: [number, number, number] = [91, 102, 109];

/**
 * Genera y descarga el PDF de una cotización (RF-010) con los datos guardados:
 * encabezado, cliente, proyecto, ítems con subtotales y total.
 * jsPDF se carga solo cuando se pide el PDF para no aumentar la carga inicial.
 */
export async function descargarPdfCotizacion(cotizacion: CotizacionDetalle): Promise<void> {
  const { jsPDF } = await import('jspdf');
  const { default: autoTable } = await import('jspdf-autotable');

  const doc = new jsPDF({ unit: 'mm', format: 'a4' });
  const ancho = doc.internal.pageSize.getWidth();
  const margen = 14;

  // Encabezado
  doc.setFillColor(...COLOR_PRIMARIO);
  doc.rect(0, 0, ancho, 32, 'F');
  doc.setFillColor(...COLOR_ACENTO);
  doc.rect(0, 32, ancho, 1.5, 'F');

  doc.setTextColor(255, 255, 255);
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(22);
  doc.text('CIVIX', margen, 16);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(9);
  doc.text('CONTROL DE OBRA', margen, 23);

  doc.setFont('helvetica', 'bold');
  doc.setFontSize(14);
  doc.text('COTIZACIÓN', ancho - margen, 14, { align: 'right' });
  doc.setFontSize(11);
  doc.text(cotizacion.numero, ancho - margen, 21, { align: 'right' });
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(9);
  doc.text(`Fecha: ${formatearFechaCorta(cotizacion.fecha)}`, ancho - margen, 27, {
    align: 'right',
  });

  // Datos generales
  let y = 44;
  doc.setTextColor(...COLOR_PRIMARIO);

  const filaDato = (etiqueta: string, valor: string, x: number): void => {
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8);
    doc.setTextColor(...COLOR_TEXTO_SUAVE);
    doc.text(etiqueta.toUpperCase(), x, y);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.setTextColor(...COLOR_PRIMARIO);
    doc.text(valor || '-', x, y + 5);
  };

  const columna2 = ancho / 2 + 4;

  filaDato('Cliente', cotizacion.clienteNombre ?? '', margen);
  filaDato('Proyecto', cotizacion.nombreProyecto, columna2);
  y += 13;
  filaDato('Correo del cliente', cotizacion.clienteCorreo ?? '', margen);
  filaDato('Ubicación', cotizacion.ubicacion ?? '', columna2);
  y += 13;
  filaDato('Elaborada por', cotizacion.creadoPor, margen);
  filaDato('Estado', cotizacion.estado, columna2);
  y += 13;

  if (cotizacion.descripcion) {
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8);
    doc.setTextColor(...COLOR_TEXTO_SUAVE);
    doc.text('DESCRIPCIÓN', margen, y);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.setTextColor(...COLOR_PRIMARIO);
    const lineas = doc.splitTextToSize(cotizacion.descripcion, ancho - margen * 2) as string[];
    doc.text(lineas, margen, y + 5);
    y += 5 + lineas.length * 5 + 3;
  }

  // Ítems
  autoTable(doc, {
    startY: y + 2,
    margin: { left: margen, right: margen },
    head: [['#', 'Concepto', 'Cantidad', 'Precio unitario', 'Subtotal']],
    body: cotizacion.items.map((item, i) => [
      String(i + 1),
      item.concepto,
      item.cantidad.toLocaleString('es-CO', { maximumFractionDigits: 3 }),
      formatearPesos(item.precioUnitario),
      formatearPesos(item.subtotal),
    ]),
    styles: { font: 'helvetica', fontSize: 9, cellPadding: 2.5, textColor: COLOR_PRIMARIO },
    headStyles: { fillColor: COLOR_PRIMARIO, textColor: [255, 255, 255], fontStyle: 'bold' },
    alternateRowStyles: { fillColor: [245, 243, 239] },
    columnStyles: {
      0: { cellWidth: 10, halign: 'center' },
      2: { cellWidth: 24, halign: 'right' },
      3: { cellWidth: 34, halign: 'right' },
      4: { cellWidth: 34, halign: 'right' },
    },
  });

  // Total (RF-009)
  const finalY = (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY;
  const yTotal = finalY + 10;

  doc.setDrawColor(...COLOR_ACENTO);
  doc.setLineWidth(0.8);
  doc.line(ancho - margen - 80, yTotal - 6, ancho - margen, yTotal - 6);
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(11);
  doc.setTextColor(...COLOR_PRIMARIO);
  doc.text('TOTAL', ancho - margen - 80, yTotal);
  doc.setFontSize(14);
  doc.text(formatearPesos(cotizacion.total), ancho - margen, yTotal, { align: 'right' });

  // Pie de página
  const alto = doc.internal.pageSize.getHeight();
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(8);
  doc.setTextColor(...COLOR_TEXTO_SUAVE);
  doc.text(
    `Documento generado por CIVIX el ${new Date().toLocaleString('es-CO')}`,
    margen,
    alto - 10,
  );

  doc.save(`${cotizacion.numero}.pdf`);
}
