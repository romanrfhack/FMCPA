# Security Track Authorization Regression Coverage Implementation Note

## Que se implemento
- Proyecto backend `FMCPA.Api.AuthorizationRegressionTests` agregado a `FMCPA.Backend.sln`.
- Suite xUnit con `WebApplicationFactory<Program>` y `PlatformDbContext` en memoria.
- Bootstrap local de usuarios `ADMIN`, `OPERATOR` y `READONLY` solo para pruebas.
- Tests de endpoints publicos permitidos.
- Tests de rechazo anonimo con `401` sobre superficies protegidas representativas.
- Tests de matriz por rol para lectura, escritura, cierres formales, catalogos admin y administracion de usuarios.
- Test de cambio self-service de password con validacion de password actual, password nueva invalida, cambio exitoso, invalidacion del token anterior y login con nueva password.
- Spec frontend minimo para confirmar que la navegacion del shell se filtra por permisos en sesion.
- Inventario documental `docs/05-post-mvp/security-authorization-surface-inventory.md`.

## Estrategia de pruebas elegida
- Integration tests backend ligeros con host ASP.NET Core de prueba.
- Tokens reales emitidos por `/api/auth/login`.
- DB InMemory para evitar Docker, SQL Server local y flakiness de infraestructura.
- Validacion por superficie/policy, no por cada endpoint individual.

Esta estrategia detecta regresiones de:
- Endpoint protegido que queda anonimo por error.
- Policy faltante o demasiado permisiva.
- `READONLY` escribiendo por error.
- `OPERATOR` entrando a superficies `ADMIN`.
- `ADMIN` perdiendo acceso administrativo.
- Cambio self-service que deje vivo el token anterior.

## Superficies cubiertas
- Publicas minimas: `/`, `/health`, `/api/auth/login`.
- Auth: `/api/auth/session`, `/api/auth/change-password`.
- Dashboard / History.
- Mercados read/write/formal close.
- Donatarias read/write/formal close.
- Financieras read/write/formal close.
- Federacion read/write/formal close.
- Catalogos compartidos read/admin.
- Administracion de usuarios admin-only.

## Limites de cobertura
- No se agrego E2E pesado con navegador.
- No se prueba cada endpoint individual ni cada payload funcional.
- No sustituye `smoke-mvp.sh` ni validaciones runtime con SQL Server.
- No se agregan permisos nuevos ni RBAC fino.
- No se toca produccion ni CI/CD.

## Como ejecutar localmente
Restaurar y compilar backend:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
```

Ejecutar solo la suite de regresion de autorizacion:

```bash
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --logger "console;verbosity=normal"
```

Si se toca frontend, ejecutar:

```bash
cd src/frontend
npm run build
npm test -- --watch=false
```

## Que quedo fuera
- Nuevas features de seguridad.
- RBAC ultra fino por endpoint/accion.
- Edicion manual de permisos por usuario.
- Playwright/Cypress u otra suite E2E pesada.
- Cambios de produccion o CI/CD.
