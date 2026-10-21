# Organización sugerida de Angular
Una vez creado `frontend/civix-web` con `ng new`, organizar `src/app` así:

- `core/auth/`, `core/guards/`, `core/interceptors/`, `core/services/`
- `shared/components/`, `shared/directives/`, `shared/pipes/`
- `features/usuarios/`, `features/obras/`, `features/materiales/`
- `features/cotizaciones/`, `features/avances/`
- `layouts/`
Cada feature podrá contener `pages/`, `components/`, `models/`, `services/`
y rutas. Generar archivos con Angular CLI conforme se implementen historias.
