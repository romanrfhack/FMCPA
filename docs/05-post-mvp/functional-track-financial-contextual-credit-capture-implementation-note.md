# Functional Track - Financial Contextual Credit Capture Implementation Note

## Que se implemento
- Se agrego captura contextual minima de credito en la pantalla de Financieras.
- El usuario puede iniciar la captura desde:
  - `financialName`
  - `institutionOrDependency`
  - `placeOrStand`
- La UI resuelve automaticamente el permiso vigente/no terminal para ese contexto y prepara el alta de credito existente con ese permiso seleccionado.
- `GET /api/financials/current-permit` ahora distingue tambien contextos que apuntan a un permiso historico o terminal:
  - `FINANCIAL_PERMIT_CURRENT_NOT_FOUND`
  - `FINANCIAL_PERMIT_NOT_CURRENT`
  - `FINANCIAL_PERMIT_TERMINAL`
  - `FINANCIAL_PERMIT_CURRENT_AMBIGUOUS`
- No se agrego migracion, tabla nueva, catalogo maestro, endpoint puente ni auditoria adicional.

## Enfoque elegido
Se eligio la opcion A: reutilizar `GET /api/financials/current-permit` y el `POST /api/financials/{permitId}/credits` existente.

La razon tecnica es que el alta de credito ya contiene la regla final de seguridad operativa: solo permite capturar sobre un permiso vigente y no terminal. Crear `POST /api/financials/credits/from-context` habria duplicado parte de esa validacion o habria necesitado delegar internamente al mismo flujo.

Con este enfoque:

1. La resolucion contextual solo encuentra o explica el permiso operable.
2. La captura real sigue pasando por el endpoint existente de alta de credito.
3. Si el permiso se renueva o cierra entre la resolucion y el submit, el `POST` bloquea con el mismo `409` documentado.

## Como se resuelve el vigente
La resolucion usa la misma combinacion operativa normalizada que la unicidad de permiso vigente:

- `financialName`
- `institutionOrDependency`
- `placeOrStand`

La normalizacion hace `trim`, colapsa espacios internos, compara sin distinguir mayusculas/minusculas y remueve diacriticos basicos. No interpreta sinonimos, alias ni abreviaturas.

La API busca primero permisos `IsCurrentVersion=true` con la misma combinacion operativa. Si existe un unico permiso vigente y no terminal, responde `200` con `permitId`, `currentRootPermitId`, periodo, estatus, secuencia de renovacion y resumen operativo.

Si no hay vigente operable:

- Si existe un permiso terminal vigente para ese contexto, responde `409 FINANCIAL_PERMIT_TERMINAL`.
- Si existe un historico de la misma cadena para ese contexto, responde `409 FINANCIAL_PERMIT_NOT_CURRENT` e incluye `currentPermitId` cuando aplica.
- Si no existe ningun permiso relacionado con ese contexto, responde `404 FINANCIAL_PERMIT_CURRENT_NOT_FOUND`.

## Como se captura sobre el vigente
La UI agrega un bloque simple `Captura contextual de credito` en Financieras.

Flujo:

1. El usuario captura financiera, dependencia/institucion y lugar/stand.
2. Usa `Capturar credito`.
3. Angular llama `GET /api/financials/current-permit`.
4. Si hay vigente, selecciona ese permiso y abre el formulario de alta de credito existente.
5. Al guardar, Angular llama el `POST /api/financials/{permitId}/credits` existente.
6. Backend vuelve a validar que el permiso siga siendo `IsCurrentVersion=true` y no terminal.

La captura no mueve creditos historicos, no reabre permisos cerrados y no crea un workflow alterno.

## Como validar localmente
Build y pruebas:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FinancialsPermitRenewalTests
cd src/frontend
npm run build
```

Stack local sugerido:

```bash
FMCPA_API_PORT=5096 FMCPA_WEB_PORT=4206 FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_AUTH_BOOTSTRAP_PASSWORD=Track5Context123 FMCPA_AUTH_JWT_SIGNING_KEY=Track5ContextSigningKeyLocalOnly1234567890 ./scripts/local/dev-up.sh
```

Validaciones HTTP esperadas con token `ADMIN`:

```bash
curl -i "http://127.0.0.1:5096/api/financials/current-permit?financialName=Financiera%20Local&institutionOrDependency=Direccion%20local&placeOrStand=Stand%202" -H "Authorization: Bearer ${ADMIN_TOKEN}"
curl -i http://127.0.0.1:5096/api/financials/${CURRENT_PERMIT_ID}/credits -H "Authorization: Bearer ${ADMIN_TOKEN}" -H 'Content-Type: application/json' -H 'X-FMCPA-Client: FMCPA-Web' -d '{"promoterName":"Promotor contexto","beneficiaryName":"Beneficiario contexto","authorizationDate":"2026-06-10","amount":1500}'
curl -i "http://127.0.0.1:5096/api/financials/current-permit?financialName=Financiera%20Local&institutionOrDependency=Direccion%20local&placeOrStand=Stand%20inexistente" -H "Authorization: Bearer ${ADMIN_TOKEN}"
```

Resultados esperados:

- Contexto con vigente: `200` en resolucion y `201` en alta de credito, con `financialPermitId` igual al permiso resuelto.
- Contexto inexistente: `404 FINANCIAL_PERMIT_CURRENT_NOT_FOUND`.
- Contexto historico: `409 FINANCIAL_PERMIT_NOT_CURRENT` con `currentPermitId` cuando exista vigente de la cadena.
- Contexto terminal: `409 FINANCIAL_PERMIT_TERMINAL`.

Validacion UI:

1. Abrir `/financials`.
2. Capturar financiera, dependencia/institucion y stand/lugar en `Captura contextual de credito`.
3. Usar `Capturar credito`.
4. Confirmar que se abre el alta existente y que al guardar aparece `Credito registrado.`.
5. Probar un contexto sin vigente y confirmar mensaje claro sin abrir formulario.

## Validacion ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln`: proyectos actualizados correctamente.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings, `0` errores.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter FinancialsPermitRenewalTests`: `3/3` pruebas aprobadas.
- `npm run build` en `src/frontend`: exitoso; queda solo warning de presupuesto CSS en `financials-page.component.ts` por `40 bytes`.
- Backend local en `5096`: `/health` respondio `200 OK`.
- Prueba runtime HTTP confirmo vigente -> credito `201`, historico -> `409 FINANCIAL_PERMIT_NOT_CURRENT`, terminal -> `409 FINANCIAL_PERMIT_TERMINAL` e inexistente -> `404 FINANCIAL_PERMIT_CURRENT_NOT_FOUND`.
- Validacion Playwright de UI confirmo captura contextual desde `/financials` y alta exitosa de credito.

## Que quedo fuera
- Catalogo maestro de financieras, dependencias o stands.
- Sinonimos, alias o normalizacion semantica.
- Endpoint puente `POST /api/financials/credits/from-context`.
- Workflow, wizard, aprobaciones o task inbox.
- Versionado contractual completo.
- Migracion o constraint SQL.
- Auditoria nueva para intentos de resolucion o capturas bloqueadas.
- Ajustes retroactivos sobre permisos historicos o terminales.
- Produccion y CI/CD.
