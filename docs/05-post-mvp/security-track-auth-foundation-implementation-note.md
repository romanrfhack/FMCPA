# Security Track Auth Foundation Implementation Note

## Objetivo
- Abrir `Track 2` de Seguridad dejando una base minima de autenticacion transversal para backend y frontend, sin abrir todavia autorizacion fina por roles ni rehacer el MVP.

## Que se implemento

### Backend
- Se introduce `ApplicationUser` como entidad tecnica minima para autenticacion local.
- Se agrega la migracion `Track2SecurityAuthFoundation` para persistir usuarios locales.
- La API incorpora `JWT bearer` con validacion de issuer, audience, firma y expiracion.
- Se agregan:
  - `POST /api/auth/login`
  - `GET /api/auth/session`
- Toda la superficie `/api` ahora exige autenticacion salvo `/api/auth/login`.
- `health` y la raiz del servicio permanecen abiertos.
- Las descargas sensibles de Mercados, Donatarias y Federacion quedan protegidas por autenticacion.

### Bootstrap local
- El usuario bootstrap local se aprovisiona solo en `Development`.
- El aprovisionamiento depende de configuracion local:
  - `Auth__Bootstrap__UserName`
  - `Auth__Bootstrap__DisplayName`
  - `Auth__Bootstrap__Password`
- No se versionan contrasenas reales en el repo.
- Si `Auth__Jwt__SigningKey` no se define en `Development`, el backend usa una llave efimera por arranque.

### Frontend Angular
- Se agrega una pantalla de login simple.
- Se agrega `AuthService` con almacenamiento minimo de sesion en `sessionStorage`.
- Se agrega interceptor para enviar el bearer en requests autenticados.
- Se agrega guard para proteger rutas principales y redirigir a `/login`.
- Se agrega logout minimo.
- El shell principal solo se expone con sesion valida.
- Las descargas sensibles dejan de usar URLs abiertas y pasan a `HttpClient` autenticado.

### Flujo local y smoke
- `.env.local.example` y `scripts/local/common.sh` ahora documentan y exponen la configuracion local de auth.
- `smoke.sh` valida:
  - health
  - `401` sin token
  - login local
  - `/api/auth/session`
  - acceso autenticado a endpoints protegidos
- `smoke-mvp.sh` ahora inicia con login real y mantiene autenticado el recorrido funcional minimo.

## Decisiones tecnicas tomadas
- Se uso `JWT bearer` en vez de cookies o proveedor externo para mantener una base minima, estandar y compatible con la API actual.
- Se uso una entidad local `ApplicationUser` y hash `PBKDF2-SHA256` para evitar introducir una plataforma completa de identidad antes de tiempo.
- Se protegió toda la superficie `/api` en lugar de solo `POST`, porque dejar lecturas operativas abiertas debilitaba el objetivo de proteger realmente la app.
- El usuario bootstrap se limita a `Development` y depende de variables locales para no versionar secretos reales.
- El frontend guarda la sesion en `sessionStorage` para mantener el alcance minimo y evitar persistencia mas larga de la necesaria en esta etapa.

## Como usarlo localmente

### Variables minimas
```bash
FMCPA_AUTH_BOOTSTRAP_USER_NAME=admin
FMCPA_AUTH_BOOTSTRAP_DISPLAY_NAME=Administrador local
FMCPA_AUTH_BOOTSTRAP_PASSWORD=LocalAuth123!Aa
```

### Arranque local minimo
```bash
./scripts/local/doctor.sh
./scripts/local/dev-up.sh
```

### Login manual por API
```bash
curl -s http://127.0.0.1:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<password-local>"}'
```

### Obtener sesion actual
```bash
TOKEN="<token-devuelto-por-login>"
curl -s http://127.0.0.1:5080/api/auth/session \
  -H "Authorization: Bearer ${TOKEN}"
```

### Probar un endpoint protegido
```bash
curl -s http://127.0.0.1:5080/api/dashboard/summary \
  -H "Authorization: Bearer ${TOKEN}"
```

## Como se valido localmente
- `dotnet restore src/backend/FMCPA.Backend.sln`
- `dotnet build src/backend/FMCPA.Backend.sln`
- `npm run build` en `src/frontend`
- `dotnet tool restore`
- Generacion de migracion `Track2SecurityAuthFoundation`
- Aplicacion de migraciones sobre una base local aislada
- `smoke.sh` autenticado
- `smoke-mvp.sh` autenticado
- `POST /api/auth/login` real
- `GET /api/auth/session` real con bearer
- `GET /api/dashboard/summary` real sin token y con bearer
- Prueba puntual del guard Angular con `vitest`

## Validaciones manuales relevantes
- Sin token, `GET /api/dashboard/summary` devuelve `401 Unauthorized`.
- Con login correcto, `POST /api/auth/login` devuelve `accessToken`, `tokenType`, `expiresAtUtc` y `user`.
- Con bearer valido, `GET /api/auth/session` devuelve el usuario autenticado actual.
- Con bearer valido, `GET /api/dashboard/summary` devuelve datos protegidos de la base local.

## Que quedo explicitamente fuera
- RBAC completo por modulo o accion.
- Politicas finas por claims.
- Gestion completa de usuarios.
- Refresh tokens o revocacion central.
- Integracion con proveedores externos de identidad.
- Cambios de produccion o CI/CD.
