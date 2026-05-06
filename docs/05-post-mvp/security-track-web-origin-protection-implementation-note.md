# Security Track Web Origin Protection Implementation Note

## Que se implemento
- Middleware `UseWebOriginProtection` en el backend para metodos inseguros bajo `/api`.
- Configuracion minima `Security:WebOriginProtection` con:
  - `ClientHeaderName = X-FMCPA-Client`
  - `ClientHeaderValue = FMCPA-Web`
- Interceptor Angular centralizado que agrega `X-FMCPA-Client: FMCPA-Web` a metodos inseguros `/api`.
- Pruebas backend `WebOriginProtectionTests`.
- Prueba frontend del interceptor para confirmar el header en requests inseguras.

## Modelo elegido
La proteccion aplica solo como capa pragmatica de borde web:
- no usa cookies;
- no cambia JWT;
- no introduce antiforgery MVC;
- no reemplaza autenticacion, autorizacion ni CORS.

Regla backend:
- aplica a `POST`, `PUT`, `PATCH` y `DELETE` bajo `/api`;
- no aplica a `GET`, `HEAD` ni `OPTIONS`;
- si existe `Origin` o `Referer`, el origen debe estar en `Cors:AllowedOrigins`;
- si la request trae senales browser (`Origin`, `Referer` o `Sec-Fetch-*`), debe incluir `X-FMCPA-Client: FMCPA-Web`;
- scripts no-browser sin senales browser siguen soportados para validacion local.

## Superficies cubiertas
Por regla de metodo/ruta, quedan cubiertas:
- `POST /api/auth/login`;
- mutaciones funcionales de modulos;
- uploads documentales;
- cierres formales;
- administracion de usuarios;
- reset/desbloqueo/cambio de password;
- futuras mutaciones `/api` mientras pasen por el middleware.

## Compatibilidad con frontend actual
Angular usa el interceptor existente de auth. El cambio se resolvio de forma centralizada:
- requests `GET` no reciben el header;
- requests inseguras `/api` reciben `X-FMCPA-Client: FMCPA-Web`;
- login tambien recibe el header, pero no recibe bearer;
- uploads `FormData` no requieren tocar componentes individuales.

El proxy local de `run-frontend.sh` se mantiene compatible. `run-backend.sh` exporta origins locales derivados de `FMCPA_WEB_PORT` cuando no existen `Cors__AllowedOrigins__0/1`, para que el origin efectivo del frontend local siga aceptado aun con puerto override.

## Respuestas de error
- `400 Bad Request`: request browser sensible sin header de cliente web esperado.
- `403 Forbidden`: `Origin` o `Referer` presente pero no permitido.

Ambas respuestas usan `application/problem+json` y no exponen detalle sensible.

## Como validarlo localmente
Comandos de build/test:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FullyQualifiedName~WebOriginProtectionTests --logger "console;verbosity=minimal"
npm run build
npm test -- --watch=false
```

Validacion manual con API local:

```bash
curl -i http://127.0.0.1:5080/api/auth/login \
  -H 'Origin: http://127.0.0.1:4200' \
  -H 'X-FMCPA-Client: FMCPA-Web' \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<password-local>"}'

curl -i http://127.0.0.1:5080/api/auth/login \
  -H 'Origin: http://127.0.0.1:4200' \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<password-local>"}'

curl -i http://127.0.0.1:5080/api/auth/login \
  -H 'Origin: https://unexpected.example.test' \
  -H 'X-FMCPA-Client: FMCPA-Web' \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<password-local>"}'
```

Resultados esperados:
- primer comando: pasa a la logica normal de login;
- segundo comando: `400`;
- tercer comando: `403`.

## Que quedo fuera
- No se migro a cookies.
- No se implemento antiforgery MVC tradicional.
- No se agrego WAF, CAPTCHA, MFA ni antifraude avanzado.
- No se cambio el modelo JWT ni la validacion de permisos.
- No se tocaron produccion ni CI/CD.
- No protege contra XSS; si un atacante ejecuta JavaScript dentro del origin permitido, esta capa no es suficiente.
