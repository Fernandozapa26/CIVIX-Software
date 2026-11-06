# Módulo de Autenticación, Registro y Control de Acceso por Roles

Este documento explica cómo funciona el módulo de seguridad de CIVIX: qué hace cada archivo, cómo funciona por dentro y cómo se conecta con el resto del sistema.

## 1. Visión general del flujo

```text
[Login Angular] --POST /api/auth/login--> [AuthController] --> [AuthService]
                                                                  |  1. Busca el usuario por correo (EF Core -> dbo.Usuarios)
                                                                  |  2. Verifica la contraseña con PasswordHasher (PBKDF2)
                                                                  |  3. Lee sus roles (dbo.UsuariosRoles + dbo.Roles)
                                                                  |  4. Firma un token JWT con id, nombre, correo y roles
[AuthService Angular] <------ { token, expiraEn, nombre, roles } -+
   | guarda la sesión en localStorage y en un signal
   v
[authInterceptor] agrega "Authorization: Bearer <token>" a cada petición a la API
[authGuard]       bloquea rutas según la sesión y data.roles
[API]             [Authorize(Roles = ...)] valida el token y el rol en cada endpoint
```

**Regla de oro:** el frontend oculta botones y rutas para dar una buena experiencia, pero quien realmente protege los datos es el backend. Por eso cada controlador lleva `[Authorize]`.

### Matriz de permisos actual

| Recurso | Administrador | Ingeniero | Cliente |
|---|---|---|---|
| Login / Registro | ✔ | ✔ | ✔ (el registro público crea Clientes) |
| Crear usuarios con rol | ✔ (enviando `rolId` con su token) | ✘ | ✘ |
| Cotizaciones (consultar) | ✔ | ✔ | ✔ |
| Materiales: consultar en la API | ✔ | ✔ | ✔ |
| Materiales: crear, editar o desactivar | ✔ | ✔ | ✘ |
| Pantalla de Materiales en Angular | ✔ | ✔ | ✘ |
| Obras | Todas | Solo las asignadas (ObraUsuarios) | Solo las asignadas, en modo consulta |

---

## 2. Decisiones técnicas

| Decisión | Elección | Motivo |
|---|---|---|
| Mecanismo de autenticación | **JWT (Bearer token)** | La API no guarda sesiones (stateless) y Angular solo tiene que enviar un encabezado. |
| Hash de contraseñas | **`PasswordHasher<T>` de ASP.NET Core** (PBKDF2 + HMAC-SHA512, 100.000 iteraciones, sal aleatoria) | Viene incluido en .NET y no requiere otro paquete. Es el mismo algoritmo de ASP.NET Core Identity, pero sin las tablas de Identity: así se conserva el esquema propio (`Usuarios`, `Roles`, `UsuariosRoles`). Si los parámetros de seguridad cambian en el futuro, el hash se actualiza solo en el siguiente login. |
| ASP.NET Core Identity completo | **No se usa** | Exigiría reemplazar las tablas de `001_creacion_inicial.sql` por las de Identity (`AspNetUsers`, etc.). |
| Rol en el registro público | **Siempre Cliente** | Si se aceptara `rolId` de cualquier persona, cualquiera podría registrarse como Administrador. |
| Esquema de base de datos | **Script SQL como fuente de verdad** | EF Core solo se mapea a las tablas existentes; no se usan migraciones para estas tablas. |

**Alternativa evaluada:** BCrypt (paquete `BCrypt.Net-Next`). También es seguro, pero exige un paquete externo y no aporta ventajas frente a PBKDF2 para este proyecto.

---

## 3. Backend (`backend/civix-backend/`)

### 3.1 Modelos (`Models/`)

**`Usuario.cs`**: representa una fila de `dbo.Usuarios`.
- Guarda `PasswordHash`, nunca la contraseña.
- Tiene dos colecciones de navegación:
  - `UsuariosRoles`: sus roles.
  - `ObrasAsignadas`: sus obras, según `ObraUsuarios`.
- EF Core usa estas colecciones cuando se escribe `.Include(u => u.UsuariosRoles).ThenInclude(ur => ur.Rol)`.

**`Rol.cs`**: representa `dbo.Roles`. Los registros 1 (Administrador), 2 (Ingeniero) y 3 (Cliente) los inserta `001_creacion_inicial.sql`.

**`UsuarioRol.cs`**: tabla puente `dbo.UsuariosRoles`, con llave compuesta `(UsuarioId, RolId)`.
- Permite que un usuario tenga varios roles.
- El token incluye todos sus roles.

**`ObraUsuario.cs`**: tabla `dbo.ObraUsuarios`, con llave `(ObraId, UsuarioId)` y campo `Activo`. Es la base de la regla "el Ingeniero y el Cliente solo ven sus obras".
- Por ahora no tiene navegación a `Obra`, porque ese modelo aún no existe en el backend.
- Cuando se cree `Models/Obra.cs`, se agrega `public Obra Obra { get; set; }` y su configuración en el DbContext.

**`RolesSistema.cs`**: constantes con los nombres e ids de los roles.
- `RolesSistema.Gestion` = `"Administrador,Ingeniero"`.
- `RolesSistema.Todos` = los tres roles.
- Se usan en `[Authorize(Roles = RolesSistema.Gestion)]`. Si mañana cambia un nombre, se corrige en un solo lugar.

### 3.2 DTOs (`DTOs/Auth/`)

Los DTO definen exactamente qué datos entran y salen de la API, sin exponer la entidad `Usuario` (que contiene el hash).

**`LoginDto.cs`**: `Correo` y `Password`. Tiene validaciones `[Required]` y `[EmailAddress]`. Como el controlador lleva `[ApiController]`, si fallan, ASP.NET responde **400** automáticamente sin entrar al método.

**`RegisterDto.cs`**: `Nombre`, `Correo`, `Password` y `RolId` (opcional).
- La contraseña exige mínimo 8 caracteres, con letras y números. Angular aplica la misma regla en `register.ts`.
- `RolId` solo se respeta si quien llama es Administrador.

**`AuthResponseDto.cs`**: respuesta de login y registro.
- Campos: `Token`, `ExpiraEn` (UTC), `UsuarioId`, `Nombre`, `Correo` y `Roles`.
- ASP.NET la serializa en camelCase (`token`, `expiraEn`…), que es lo que espera la interfaz `AuthResponse` de Angular.

**`UsuarioActualDto.cs`**: respuesta de `GET /api/auth/me`.
- Incluye `SoloLectura`, `TodasLasObras` y `ObrasPermitidas`.
- Sirve para que una pantalla sepa a qué obras tiene acceso el usuario.

### 3.3 Configuración (`Configuration/JwtOptions.cs`)

Clase que se llena con la sección `"Jwt"` de `appsettings.json`: `Issuer`, `Audience`, `ExpiracionMinutos` y `Key`.
- **La `Key` no se sube a Git.** Va en `appsettings.Development.json` (ya está en `.gitignore`), en User Secrets o en la variable de entorno `Jwt__Key`.
- En desarrollo, si no se configura, `Program.cs` genera una clave temporal. Los tokens dejan de servir cada vez que se reinicia la API, y el usuario simplemente vuelve a iniciar sesión.
- En producción la API no arranca sin clave.

### 3.4 `Services/AuthService.cs`

Contiene toda la lógica de autenticación. Se registra como `Scoped`, igual que `MaterialesService`, porque usa el `DbContext`.

**`LoginAsync(LoginDto)`**
1. Normaliza el correo (quita espacios y lo pasa a minúsculas).
2. Busca el usuario con sus roles: `Include(UsuariosRoles).ThenInclude(Rol)`.
3. Si el correo no existe, igual calcula un hash. Así la respuesta tarda lo mismo y un atacante no puede averiguar qué correos están registrados midiendo tiempos.
4. Verifica la contraseña con `IPasswordHasher<Usuario>.VerifyHashedPassword`. Si en la base hay un valor que no es un hash válido (por ejemplo, una contraseña escrita en texto plano a mano), lo trata como contraseña incorrecta y no lanza un error 500.
5. Si el usuario está inactivo (`Activo = 0`), lo rechaza con el mismo mensaje genérico.
6. Si el resultado es `SuccessRehashNeeded`, regenera el hash con los parámetros actuales.
7. Devuelve `AuthResponseDto` con el token. Si algo falla devuelve `null`, y el controlador responde **401**.

**`RegistrarAsync(RegisterDto, bool solicitadoPorAdministrador)`**
1. Si el correo ya existe, devuelve un conflicto (**409**).
2. Decide el rol: `RolId` solo si lo pide un Administrador; en cualquier otro caso, Cliente (3).
3. Crea el `Usuario`, calcula el hash y agrega `UsuarioRol`. EF Core inserta las dos filas en una sola transacción con `SaveChangesAsync`.
4. Si dos personas se registran con el mismo correo al mismo tiempo, el índice `UNIQUE` de SQL Server lanza los errores 2601/2627. Se capturan y se devuelve 409, no un error 500.

**`GenerarToken`**: crea el JWT con `JsonWebTokenHandler`, firmado con HMAC-SHA256. Claims incluidos:
- `sub`: id del usuario.
- `email`: correo.
- `name`: nombre.
- `role`: uno por cada rol.
- `jti`: identificador único del token.

**`ResultadoRegistro`**: un record pequeño que permite devolver "éxito con datos" o "error con mensaje" sin usar excepciones para el flujo normal.

### 3.5 `Services/AccesoObrasService.cs`

Centraliza las reglas de acceso por obra para que los módulos de Obras, Avances y Cotizaciones no las repitan.
- `ObtenerObrasPermitidasAsync(User)`: devuelve `null` si el usuario es Administrador (acceso a todas). Si no, devuelve los `ObraId` de `ObraUsuarios` con `Activo = 1`.
- `PuedeAccederAObraAsync(User, obraId)`: comprobación puntual, útil en `GET /api/obras/{id}`.
- `EsSoloLectura(User)`: es `true` cuando el usuario solo tiene el rol Cliente.

**Cómo usarlo en el futuro `ObrasController`:**

```csharp
[HttpGet]
[Authorize(Roles = RolesSistema.Todos)]
public async Task<IActionResult> Listar()
{
    var permitidas = await _accesoObras.ObtenerObrasPermitidasAsync(User);
    var consulta = _context.Obras.AsQueryable();
    if (permitidas is not null)
        consulta = consulta.Where(o => permitidas.Contains(o.Id));
    return Ok(await consulta.ToListAsync());
}
```

**Cotizaciones del Cliente:** la tabla `Cotizaciones` también tiene `ClienteId`. Cuando se implemente el servicio real de cotizaciones, un Cliente debe ver las cotizaciones donde `ClienteId` sea su id, además de las de sus obras.

### 3.6 `Controllers/AuthController.cs`

| Endpoint | Acceso | Respuestas |
|---|---|---|
| `POST /api/auth/login` | Público | 200 `AuthResponseDto` · 400 validación · 401 credenciales inválidas |
| `POST /api/auth/register` | Público (con token de Administrador permite elegir rol) | 201 `AuthResponseDto` · 400 · 409 correo repetido |
| `GET /api/auth/me` | Cualquier usuario autenticado | 200 `UsuarioActualDto` · 401 |

Aunque `register` es `[AllowAnonymous]`, el middleware de autenticación igual lee el token si viene en la petición. Por eso `User.IsInRole("Administrador")` funciona dentro del método.

### 3.7 Archivos modificados

**`Data/CivixDbContext.cs`**
- Se agregaron los DbSets `Usuarios`, `Roles`, `UsuariosRoles` y `ObraUsuarios`.
- En `OnModelCreating` se mapea cada uno a su tabla real (`ToTable("Usuarios", "dbo")`), con las llaves compuestas, las longitudes de las columnas y las relaciones, usando los mismos nombres de FK del script.
- No crea tablas: solo describe las que ya existen.

**`Program.cs`**: se agregaron, en este orden:
1. Lectura y validación de `JwtOptions`, con la clave temporal en desarrollo.
2. Registro de `PasswordHasher<Usuario>` (Singleton, porque no guarda estado), `AuthService` y `AccesoObrasService` (Scoped).
3. `AddAuthentication().AddJwtBearer(...)` para validar emisor, audiencia, firma y vencimiento.
   - `MapInboundClaims = false` conserva los nombres cortos de los claims.
   - `RoleClaimType = "role"` hace que `[Authorize(Roles = ...)]` lea el claim correcto.
4. Política CORS para `http://localhost:4200`, configurable en `Cors:OrigenesPermitidos`.
5. Pipeline: `UseCors` → `UseAuthentication` → `UseAuthorization`. **El orden importa:** la autenticación debe ir antes que la autorización.

**`civix-backend.csproj`**: se agregó el paquete `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.13, la misma línea de versión que `Microsoft.AspNetCore.OpenApi`.

**`appsettings.json`**: se agregaron las secciones `Jwt` (sin la clave) y `Cors`.

**`MaterialesController.cs` y `CotizacionesController.cs`**: ahora exigen token.
- Ambos controladores: `[Authorize(Roles = RolesSistema.Todos)]` a nivel de clase.
- En Materiales, `POST`, `PUT` y `DELETE` llevan además `[Authorize(Roles = RolesSistema.Gestion)]`.
- ⚠️ Cualquier prueba con Postman o con el archivo `.http` necesita el token. Ver `civix-backend.http`.

**`civix-backend.http`**: ejemplos de login, `/me`, registro y materiales. Toma el token automáticamente de la respuesta del login (`{{login.response.body.$.token}}`).

---

## 4. Base de datos

**`database/scripts/002_datos_prueba_auth.sql`**: se ejecuta después de `001_creacion_inicial.sql`. Es idempotente: se puede ejecutar varias veces sin duplicar datos.
- Crea 3 usuarios de prueba con contraseñas ya hasheadas en el formato de `PasswordHasher` v3.
- Les asigna su rol.
- Crea la "Obra demostración CIVIX" y asigna al Ingeniero y al Cliente para probar el acceso por obra.

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | admin@civix.co | Admin123 |
| Ingeniero | ingeniero@civix.co | Ingeniero123 |
| Cliente | cliente@civix.co | Cliente123 |

⚠️ Solo para desarrollo. No ejecutar en producción.

---

## 5. Frontend (`frontend/civix-front/src/`)

### 5.1 `environments/environment.ts`
- `apiUrl`: URL base de la API (`http://localhost:5067/api`, perfil **http** de `launchSettings.json`).
- `mostrarCuentasPrueba`: muestra u oculta las tarjetas de acceso rápido. Ponerlo en `false` antes de una entrega real.

### 5.2 `app/core/models/auth.models.ts`
- Interfaces que reflejan los DTO del backend: `LoginRequest`, `RegisterRequest`, `AuthResponse` y `UsuarioSesion`.
- El tipo `RolCivix`.
- Los grupos `ROLES_GESTION` y `ROLES_TODOS`, que se usan en las rutas.

Si el backend cambia un DTO, este es el archivo que hay que actualizar.

### 5.3 `app/core/services/auth.service.ts`
Servicio singleton (`providedIn: 'root'`) que es dueño de la sesión.
- **Estado con signals:** `sesion` es privado y de él se derivan, con `computed()`, `usuario`, `estaAutenticado`, `roles` y `esSoloLectura`. Cualquier plantilla que los lea se actualiza sola al iniciar o cerrar sesión.
- **Persistencia:** al iniciar, `leerSesionGuardada()` recupera la sesión de `localStorage` y valida su forma. Si el JSON está dañado, lo descarta.
- **`login()` y `register()`:** llaman a la API y, con `tap`, guardan la respuesta.
- **`sesionVigente()`:** comprueba la fecha `expiraEn` y que haya al menos un rol. Si la sesión venció, la limpia.
- **`logout('expirada')`:** lleva al login con `?sesionExpirada=1` para mostrar el aviso.
- **`tieneAlgunRol([...])`:** lo usan el guard y el menú.
- **`esSoloLectura`:** las futuras pantallas lo usan para ocultar los botones de crear y editar al Cliente, con `@if (!auth.esSoloLectura())`.

### 5.4 `app/core/interceptors/auth.interceptor.ts`
Interceptor funcional, registrado en `app.config.ts` con `withInterceptors`.
1. Si la URL empieza con `environment.apiUrl`, agrega `Authorization: Bearer <token>`. El token nunca se envía a otros dominios.
2. Si la API responde **401** a una petición que llevaba token, significa que venció: cierra la sesión y vuelve al login. No actúa sobre el 401 del propio login, porque esa petición no lleva token.

### 5.5 `app/core/guards/auth.guard.ts`
- **`authGuard`:**
  - Sin sesión: redirige a `/login?returnUrl=/ruta-pedida`.
  - Con sesión pero sin el rol que exige `data.roles`: redirige a `/cotizaciones`, que todos los roles pueden ver.
- **`guestGuard`:** si ya hay sesión, no deja volver a `/login` ni a `/registro`.

### 5.6 `app/core/utils/http-error.ts`
`obtenerMensajeError(error)` convierte cualquier error HTTP en un texto para el usuario. Entiende tres casos:
- `{ mensaje }`, que devuelven los controladores de CIVIX.
- `{ errors: {...} }`, que devuelve la validación automática de `[ApiController]`.
- El estado 0, que significa que la API está apagada o que CORS la bloqueó.

### 5.7 `app/layouts/auth-layout/`
Pantalla dividida del prototipo de Figma.
- A la izquierda: logo CIVIX, la etiqueta "Gestión integral de construcción", el titular y el pie, sobre un **degradado oscuro** con una retícula tenue de plano arquitectónico. Más adelante se puede reemplazar por la foto de obra cambiando el `background` de `.hero` en el SCSS.
- A la derecha: `<router-outlet />`, donde se carga login o registro.
- Por debajo de 960 px el panel se reduce a una franja con el logo.

### 5.8 `app/features/auth/login/`
- **`login.ts`**
  - Formulario reactivo tipado (`NonNullableFormBuilder`) con validadores `required` y `email`.
  - Signals `enviando` y `error`.
  - `usarCuenta()`: al pulsar una tarjeta de prueba, llena el formulario e inicia sesión de inmediato.
  - Después de iniciar sesión navega al `returnUrl` (solo si es una ruta interna) o a `/cotizaciones`.
- **`login.html`**
  - Etiqueta "Acceso seguro", título, campos con iconos y botón naranja.
  - Enlace al registro y bloque "Cuentas de prueba" con las 3 tarjetas.
  - Usa `@if`, `@for` y `@switch`, sin directivas `*ngIf`.
  - Los errores se anuncian con `role="alert"` y los campos inválidos llevan `aria-invalid`.
- **`login.scss`:** estilos de las tarjetas de prueba. Importa los estilos compartidos de `_auth-form.scss`.

### 5.9 `app/features/auth/register/`
- Formulario con nombre, correo, contraseña y confirmación.
- `passwordsCoinciden` es un validador de grupo que compara las dos contraseñas.
- Las reglas son las mismas del backend: mínimo 8 caracteres, con letras y números.
- Al registrarse, el usuario queda con sesión iniciada como **Cliente**.

### 5.10 `app/features/auth/_auth-form.scss`
Parcial de Sass que comparten login y registro: tipografía del encabezado, campos, botón, alertas y enlace inferior. Para cambiar el aspecto de ambos formularios, se edita este archivo.

### 5.11 Archivos modificados
- **`app.routes.ts`:**
  - `/login` y `/registro` son rutas propias que cargan `AuthLayout` con `guestGuard`. **No deben usar `path: ''`**, porque competirían con la ruta raíz del layout principal y dejarían la pantalla en blanco al abrir `localhost:4200`.
  - La ruta `''` usa `MainLayout` y `authGuard`. Sus hijas (`inicio`, `cotizaciones`, `cotizaciones/nueva`, `materiales`) declaran `data: { roles: [...] }`.
  - Después del login se entra a `/inicio` (`RUTA_INICIO` en `auth.service.ts`).
- **`app.config.ts`:** `provideHttpClient(withFetch(), withInterceptors([authInterceptor]))`.
- **`app.html`:** se eliminó la página de bienvenida de Angular. Antes se mostraba encima de todas las rutas; ahora el archivo solo tiene `<router-outlet />`.
- **`layouts/main-layout/*`:**
  - Muestra el nombre y el rol del usuario, la marca "Solo consulta" para el Cliente y el botón **Cerrar sesión**.
  - El enlace a Materiales solo aparece para el Administrador y el Ingeniero.
  - Se agregaron estilos básicos a la barra lateral, que no tenía.
- **`index.html`:** `lang="es"`, título de la pestaña y la fuente Manrope de Google Fonts.
- **`styles.scss`:** colores de marca como variables CSS (`--civix-naranja`, `--civix-tinta`…), que usan todos los componentes.
  - El naranja de Figma (`#f5a623`) se usa en botones y fondos.
  - Para textos pequeños sobre fondo claro se usa `#a15c00`, porque el naranja original no cumple el contraste mínimo de WCAG AA.
- **`angular.json`:** el aviso de tamaño de estilos por componente pasó de 4 kB a 6 kB, porque el login supera un poco los 4 kB. El límite de error de 8 kB no cambia.
- **`app.spec.ts` y `main-layout.spec.ts`:** se actualizaron para los cambios.
  - `app.spec.ts` ya no busca el título "Hello, civix-front".
  - `main-layout.spec.ts` ahora incluye los proveedores de HttpClient y Router, que necesita `AuthService`.

---

## 6. Puesta en marcha

```powershell
# 1. Base de datos: en SSMS, ejecutar database/scripts/002_datos_prueba_auth.sql

# 2. Backend
cd backend/civix-backend
dotnet restore           # descarga el paquete JwtBearer
# (Opcional, recomendado) clave fija para que los tokens sobrevivan a reinicios.
# En appsettings.Development.json (ignorado por Git):
#   "Jwt": { "Key": "una-clave-larga-de-al-menos-32-caracteres-aleatorios" }
dotnet run --launch-profile http     # API en http://localhost:5067

# 3. Frontend (otra terminal)
cd frontend/civix-front
npm install
npm start                # http://localhost:4200 -> redirige a /login
```

## 7. Pruebas manuales sugeridas

1. Abrir `http://localhost:4200`. Debe redirigir a `/login`.
2. Pulsar la tarjeta **Administrador**. Debe entrar a Cotizaciones y ver Materiales en el menú.
3. Cerrar sesión y entrar como **Cliente**. No debe aparecer Materiales. Escribir `/materiales` en la barra de direcciones debe llevar de vuelta a Cotizaciones.
4. Intentar entrar con una contraseña incorrecta. Debe aparecer "Correo o contraseña incorrectos.".
5. Registrar una cuenta nueva. Debe quedar con sesión iniciada y el rol Cliente.
6. Registrar otra vez el mismo correo. Debe aparecer "Ya existe una cuenta registrada con ese correo.".
7. Con la API detenida, intentar iniciar sesión. Debe aparecer "No se pudo conectar con el servidor…".
8. En `civix-backend.http`, hacer `GET /api/materiales` sin token. Debe responder **401**.

## 8. Pendientes para módulos siguientes
- Crear `Models/Obra.cs` y `ObrasController`, aplicando `AccesoObrasService` en cada consulta.
- En Cotizaciones: filtrar por `ClienteId` y por obras permitidas cuando se reemplacen los datos de ejemplo.
- En las pantallas: usar `authService.esSoloLectura()` para ocultar las acciones de edición al Cliente.
- Crear una pantalla de administración de usuarios, que usaría `POST /api/auth/register` con `rolId` y el token del Administrador.
- Reemplazar el degradado del panel izquierdo por la foto de Figma cuando esté disponible.
