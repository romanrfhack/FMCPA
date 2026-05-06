# Security Track HTTP Boundary Hardening Implementation Note

## Objetivo
- Endurecer el borde HTTP y la superficie de autenticacion de `Track 2`.
- Mantener el alcance minimo: sin WAF, CAPTCHA, proveedor externo de identidad, antifraude avanzado, cambios de produccion real ni CI/CD.

## Que se implemento
- `HttpBoundarySecuritySettings` para resolver y validar:
  - origins CORS
  - limites de rate limiting
- `PlatformRateLimitingPolicies` con policies reutilizables:
  - `auth-login`
  - `sensitive-admin`
- `HttpSecurityHeadersSupport` para headers HTTP de seguridad.
- Validaciones adicionales en `JwtAuthenticationSettings` fuera de `Development`.
- Rate limiting aplicado a:
  - `POST /api/auth/login`
  - grupo `/api/admin/users`

## Limites de rate limiting

| Superficie | Policy | Limite | Particion |
| --- | --- | --- | --- |
| Login | `auth-login` | `10` solicitudes por minuto | cliente remoto |
| Administracion de usuarios | `sensitive-admin` | `60` solicitudes por minuto | cliente remoto + usuario autenticado |

Al exceder el limite:
- status `429 Too Many Requests`
- header `Retry-After`
- body `application/problem+json` con titulo `Demasiados intentos.`

Los limites se configuran en:
```json
{
  "Security": {
    "RateLimiting": {
      "Login": {
        "PermitLimit": 10,
        "WindowSeconds": 60
      },
      "SensitiveAdmin": {
        "PermitLimit": 60,
        "WindowSeconds": 60
      }
    }
  }
}
```

## Headers agregados
- En todas las respuestas:
  - `X-Content-Type-Options: nosniff`
  - `Referrer-Policy: no-referrer`
  - `X-Frame-Options: DENY`
  - `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- En respuestas `/api`:
  - `Cache-Control: no-store`
  - `Pragma: no-cache`
  - `Expires: 0`

Las descargas documentales conservan sus headers seguros y el middleware no cambia el comportamiento de descarga ya endurecido.

## Reglas de CORS
- `Development`:
  - si no hay configuracion explicita, se permiten `http://localhost:4200` y `http://127.0.0.1:4200`
  - el proxy Angular local sigue siendo el flujo recomendado y no requiere abrir origins por puerto override
- Cualquier entorno:
  - se rechaza wildcard `*`
  - cada origin debe ser absoluto, `http` o `https`, sin path
- Fuera de `Development`:
  - `Cors:AllowedOrigins` debe estar configurado
  - se rechazan origins localhost/loopback

Ejemplo no-Development:
```bash
Cors__AllowedOrigins__0=https://app.example.com
```

## Development vs no Development
- En `Development` se conserva la signing key efimera si `Auth:Jwt:SigningKey` no esta configurada; se mantiene como comportamiento local tolerado.
- Fuera de `Development`:
  - `Auth:Jwt:SigningKey` es obligatoria
  - `Auth:Jwt:SigningKey` debe tener al menos 32 bytes para HS256
  - `Auth:Jwt:Issuer` y `Auth:Jwt:Audience` son obligatorios y no pueden ser los defaults locales `FMCPA.Local` / `FMCPA.Web.Local`
  - `Auth:Jwt:TokenLifetimeMinutes` debe ser mayor que cero
  - CORS no puede quedarse en localhost ni wildcard

## Decisiones tomadas
- Rate limiting local/en memoria para mantener alcance acotado y no introducir infraestructura distribuida.
- Limite de login `10/min` para reducir fuerza bruta basica sin romper desarrollo local normal.
- Limite admin `60/min` para proteger una superficie sensible sin bloquear tareas manuales comunes.
- `no-store` se aplica a toda respuesta `/api` para evitar cache accidental de login, sesion, admin, datos operativos o documentos.
- No se agrego frontend especifico para `429`; el manejo actual de errores muestra el `title` del problem response.
- No se creo migracion.

## Como validar localmente

### Build
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
npm run build --prefix src/frontend
```

### Arranque aislado usado
```bash
FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-httpboundary-14339 \
FMCPA_SQL_PORT=14339 \
FMCPA_DB_NAME=FMCPA_HttpBoundaryHardening_20260505 \
FMCPA_API_PORT=5096 \
FMCPA_WEB_PORT=4205 \
FMCPA_STORAGE_ROOT=/tmp/fmcpa-http-boundary-storage \
FMCPA_AUTH_BOOTSTRAP_PASSWORD='LocalAdmin123!Aa' \
FMCPA_AUTH_OPERATOR_PASSWORD='LocalOperator123!Aa' \
FMCPA_AUTH_READONLY_PASSWORD='LocalReadonly123!Aa' \
FMCPA_AUTH_JWT_SIGNING_KEY='HttpBoundaryHardeningLocalSigningKey20260505OnlyForValidation1234567890' \
./scripts/local/dev-up.sh --no-backend --no-frontend
```

Luego levantar API en `Development` con la misma configuracion y `--urls http://127.0.0.1:5096`.

### Verificaciones HTTP
```bash
curl -i http://127.0.0.1:5096/health

curl -i http://127.0.0.1:5096/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"LocalAdmin123!Aa"}'

curl -i http://127.0.0.1:5096/api/auth/session \
  -H "Authorization: Bearer ${TOKEN}"

curl -i -X OPTIONS http://127.0.0.1:5096/api/auth/login \
  -H 'Origin: http://localhost:4200' \
  -H 'Access-Control-Request-Method: POST' \
  -H 'Access-Control-Request-Headers: content-type'

curl -i -X OPTIONS http://127.0.0.1:5096/api/auth/login \
  -H 'Origin: http://evil.example' \
  -H 'Access-Control-Request-Method: POST' \
  -H 'Access-Control-Request-Headers: content-type'
```

Para forzar rate limit, ejecutar mas de `10` `POST /api/auth/login` en menos de un minuto desde el mismo cliente.

### Fail-fast no-Development
```bash
ASPNETCORE_ENVIRONMENT=Production \
dotnet run --no-launch-profile --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj \
  --urls http://127.0.0.1:5998
```

Debe fallar si falta `Auth:Jwt:SigningKey`.

Tambien debe fallar con signing key corta:
```bash
ASPNETCORE_ENVIRONMENT=Production \
Auth__Jwt__Issuer='https://api.example.local' \
Auth__Jwt__Audience='https://app.example.local' \
Auth__Jwt__SigningKey='short' \
Cors__AllowedOrigins__0='https://app.example.local' \
dotnet run --no-launch-profile --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj \
  --urls http://127.0.0.1:5998
```

Y debe fallar si conserva CORS localhost fuera de `Development`.

## Validacion ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln` -> exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln` -> exitoso, `0 Warning(s)`, `0 Error(s)`.
- `npm run build` en `src/frontend` -> exitoso.
- `dotnet tool restore` -> `dotnet-ef` `10.0.6` restaurado durante `dev-up`.
- Migraciones aplicadas sobre `FMCPA_HttpBoundaryHardening_20260505`; no se genero migracion nueva.
- API `Development` ejecutada en `http://127.0.0.1:5096`.
- Resultado runtime:
  - `GET /health`: `200`
  - `POST /api/auth/login` valido: `200`
  - `GET /api/auth/session`: `200`
  - headers de seguridad en sesion: `OK`
  - headers de cache `/api`: `OK`
  - headers de cache en login: `OK`
  - CORS `Origin: http://localhost:4200`: `204` con `Access-Control-Allow-Origin`
  - CORS `Origin: http://evil.example`: `204` sin `Access-Control-Allow-Origin`
  - intentos de login despues del login normal: `401 401 401 401 401 401 401 401 401 429`
  - `429` incluye `Retry-After: 60`
  - proxy Angular local: `/` `200` y `/health` `200`
  - Production sin signing key: falla con `Auth:Jwt:SigningKey is required outside Development.`
  - Production con signing key corta: falla con `Auth:Jwt:SigningKey must be at least 32 bytes for HS256.`
  - Production con CORS localhost: falla con `Cors:AllowedOrigins cannot use localhost origins outside Development.`

## Que quedo fuera
- WAF.
- CAPTCHA.
- Proveedor externo de identidad.
- Antifraude avanzado.
- Rate limiting distribuido o persistente.
- Bloqueo por usuario/IP a largo plazo.
- Cambios de produccion real.
- Cambios de CI/CD.
