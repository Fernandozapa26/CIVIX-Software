# Pantalla de Inicio

Es el resumen que ve cada usuario al entrar a la aplicación (`/app/inicio`). Los datos salen de `GET /api/inicio` y respetan los permisos por obra (`AccesoObrasService`).

| Bloque | Administrador | Ingeniero | Cliente |
|---|---|---|---|
| Total de obras y avance promedio | Todas | Sus obras asignadas | Sus obras |
| Obras recientes con barra de avance | Todas | Las suyas | Las suyas |
| Últimos avances con foto | Todos | De sus obras | De sus obras |
| Cotizaciones en borrador | Por aprobar (todas) | Las que creó | No |
| Materiales: pedidos, en bodega y retiros de la semana | Todos | De sus obras | No |
| Accesos rápidos | Sí | Sí | No |

Las cotizaciones anuladas no se cuentan.
