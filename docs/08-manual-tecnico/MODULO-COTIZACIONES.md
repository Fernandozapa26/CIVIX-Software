# Módulo de Cotizaciones

Requisitos: **RF-008** (crear cotización con ítems), **RF-009** (calcular el total) y **RF-010** (generar el PDF).
Además incluye lo asignado al equipo: editar, anular (borrado lógico), visibilidad por rol y creación automática de la obra al aprobar.

## Reglas

| Acción | Administrador | Ingeniero | Cliente |
|---|---|---|---|
| Ver cotizaciones | Todas | Solo las que creó | No tiene acceso |
| Crear | Sí | Sí | No |
| Editar (solo en Borrador) | Sí | Solo las suyas | No |
| Anular (Borrador o Rechazada) | Sí | Solo las suyas | No |
| Aprobar o rechazar | Sí | No | No |
| Descargar PDF | Sí | Solo las suyas | No |

- **Estados:** Borrador → Aprobada | Rechazada. Anular es el borrado lógico: `Estado = Anulada` y `Activo = 0`. Una cotización aprobada no se puede anular porque ya tiene obra.
- **Número automático** por año: `COT-2026-0001`, `COT-2026-0002`…
- **Total (RF-009):** se calcula en el formulario mientras se escribe y el backend lo vuelve a calcular al guardar, sumando los subtotales redondeados a 2 decimales, igual que la columna calculada `CotizacionDetalles.Subtotal`.
- **Al aprobar** se crea la obra con:
  - nombre (editable), descripción y ubicación de la cotización;
  - presupuesto igual al total y estado *En planeación*;
  - el cliente de la cotización y el ingeniero responsable elegido, asignados en `ObraUsuarios`, para que los dos vean la obra.

  La cotización queda ligada a la obra (`ObraId`).

## Base de datos

Ejecutar `database/scripts/006_cotizaciones.sql` después de `001` y `005`. El script agrega `NombreProyecto`, `Descripcion`, `Ubicacion`, `FechaDecision`, `DecididaPorUsuarioId` y la restricción de estados.

## API

| Método | Ruta | Uso |
|---|---|---|
| GET | `/api/cotizaciones?estado=` | Listado. Sin `estado` devuelve todas menos las anuladas |
| GET | `/api/cotizaciones/{id}` | Cotización con sus ítems (para editar y para el PDF) |
| POST | `/api/cotizaciones` | Crear (RF-008, RF-009) |
| PUT | `/api/cotizaciones/{id}` | Editar (solo en Borrador) |
| DELETE | `/api/cotizaciones/{id}` | Anular (borrado lógico) |
| POST | `/api/cotizaciones/{id}/aprobar` | Aprobar y crear la obra (solo Administrador) |
| POST | `/api/cotizaciones/{id}/rechazar` | Rechazar (solo Administrador) |
| GET | `/api/catalogos/usuarios?rol=Cliente` | Clientes para el formulario |

## PDF (RF-010)

Lo genera el navegador con `jspdf` y `jspdf-autotable` (`cotizacion-pdf.ts`), usando los datos guardados que devuelve `GET /api/cotizaciones/{id}`. Incluye encabezado, número, fecha, cliente, proyecto, ubicación, quién la elaboró, estado, descripción, la tabla de ítems y el total.
Después de bajar estos cambios hay que ejecutar `npm install` en `frontend/civix-front`.
