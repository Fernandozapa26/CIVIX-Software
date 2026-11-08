# Módulo de Materiales

Requisitos: **RF-006** (registrar materiales asociados a una obra) y **RF-007** (consultar materiales por obra).
Actores: Administrador e Ingeniero. El Ingeniero solo ve las obras que tiene asignadas en `dbo.ObraUsuarios`.

## Reglas acordadas

- Cada material registrado en una obra tiene un estado: **Pedido**, **En bodega** o **Retirado**.
- Al registrar o editar se elige *Pedido* o *En bodega*. *Retirado* lo pone el sistema cuando no queda nada disponible.
- La cotización asociada es opcional.
- Solo se retira material que está *En bodega* y nunca más de lo disponible. Los retiros pueden ser parciales.
- Cada retiro guarda quién recogió el material (nombre escrito a mano), para qué obra, la cantidad, la fecha, una observación y el usuario de CIVIX que lo registró.
- Un material con retiros no se puede quitar de la obra ni volver a *Pedido*.

## Base de datos

Ejecutar `database/scripts/004_materiales_estados_retiros.sql` **después** de `001_creacion_inicial.sql`.
El script se puede ejecutar más de una vez y hace lo siguiente:

- Agrega `Estado` y `CotizacionId` a `dbo.ObraMateriales`.
- Crea la tabla `dbo.RetirosMaterial`.
- Si las tablas están vacías, inserta una obra y cuatro materiales de prueba.

## API

| Método | Ruta | Uso |
|---|---|---|
| GET | `/api/catalogos/obras` | Obras a las que el usuario tiene acceso |
| GET | `/api/catalogos/cotizaciones` | Cotizaciones para asociar |
| GET | `/api/obras/{obraId}/materiales` | RF-007: materiales de la obra con cantidad retirada y disponible |
| POST | `/api/obras/{obraId}/materiales` | RF-006: registrar `{ materialId, cantidad, estado, cotizacionId }` |
| PUT | `/api/obras/{obraId}/materiales/{id}` | Editar `{ cantidad, estado, cotizacionId }` |
| DELETE | `/api/obras/{obraId}/materiales/{id}` | Quitar de la obra (solo si no tiene retiros) |
| GET | `/api/retiros-material?obraId=` | Historial de retiros de la obra (origen o destino) |
| POST | `/api/retiros-material` | Registrar retiro `{ obraMaterialId, cantidad, retiradoPor, obraDestinoId, observacion }` |
| GET/POST/PUT/DELETE | `/api/materiales` | Catálogo de materiales (ya existía) |

La lista de obras está en `/api/catalogos/obras` y no en `/api/obras` para no chocar con el módulo de Obras.

## Frontend

`frontend/civix-front/src/app/features/materiales/`

- `materiales.ts`: página con pestañas y selector de obra.
- `components/materiales-obra`: tabla por obra, registro, edición, cambio a *En bodega*, retiro y eliminación.
- `components/historial-retiros`: historial con búsqueda.
- `components/catalogo-materiales`: crear, editar y desactivar materiales.
- `materiales.service.ts` y `materiales.models.ts`: llamadas al API y tipos.

## Nota de integración

`Models/Obra.cs` se creó siguiendo las columnas reales de `dbo.Obras`. La rama de Juan Paniagua tiene otro `Obra.cs` con campos que no existen en la BD (Cliente, Presupuesto, Avance y Personal). Al fusionar hay que quedarse con una sola versión que coincida con la tabla.
