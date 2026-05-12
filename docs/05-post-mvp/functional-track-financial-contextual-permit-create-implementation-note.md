# Functional Track - Financial Contextual Permit Create Implementation Note

## Contexto
- Fecha: 2026-05-11
- Track: `Track 5: Evolucion funcional posterior`
- Subetapa: alta contextual prellenada para Financieras.
- Alcance: preparar el alta de un oficio desde `CREATE_NEW_PERMIT` sin workflow complejo, sin catalogo maestro, sin endpoint nuevo y sin migracion.

## Que se implemento
- La UI de Financieras muestra `Preparar alta de oficio` solo cuando la resolucion contextual devuelve `suggestionCode=CREATE_NEW_PERMIT`.
- La accion copia el contexto operativo al formulario actual de alta de oficio:
  - `financialName`
  - `institutionOrDependency`
  - `placeOrStand`
- El formulario muestra una nota de alta contextual para distinguirlo de una renovacion y aclarar que no hay permiso vigente ni antecedente.
- La creacion real sigue usando `POST /api/financials`.
- Se agrego una prueba de regresion que valida `CREATE_NEW_PERMIT`, alta real por el endpoint existente, resolucion posterior `USE_CURRENT_PERMIT` y bloqueo de duplicado por unicidad operativa.

## Como se prellena el contexto
- `GET /api/financials/current-permit` sigue siendo la fuente de decision contextual.
- Cuando la respuesta no trae `currentPermit` ni `lastKnownPermit` y el `suggestionCode` es `CREATE_NEW_PERMIT`, la UI habilita `Preparar alta de oficio`.
- La accion solo actualiza estado local de Angular y el formulario existente; no persiste borradores ni llama a una API de preparacion.

## Campos manuales
- `validFrom`
- `validTo`
- `schedule`
- `negotiatedTerms`
- `notes` cuando aplique
- `statusCatalogEntryId`

## Validacion local
Stack local usado para la validacion manual:
- API: `http://127.0.0.1:5112`
- Frontend Angular: `http://127.0.0.1:4200`
- SQL Server local: contenedor `fmcpa-sql-context-renewal`, puerto `14346`, base `FMCPA_ContextRenewalDraft_20260511`

Comandos ejecutados:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter FinancialsPermitRenewalTests

cd src/frontend
npm run build
npm run start -- --host 127.0.0.1 --port 4200 --proxy-config /tmp/fmcpa-angular-proxy.4200.5112.json
node /tmp/fmcpa-contextual-permit-create-http.js
NODE_PATH=/home/romanrfhack/.npm/_npx/420ff84f11983ee5/node_modules /home/romanrfhack/.npm/_npx/420ff84f11983ee5/node_modules/.bin/playwright test -c /tmp/fmcpa-contextual-permit-create-playwright.config.js --output /tmp/fmcpa-contextual-permit-create-ui-output
```

Resultado de la validacion automatica:
- `dotnet restore`: exitoso.
- `dotnet build`: exitoso, `0 Warning(s)`, `0 Error(s)`.
- `FinancialsPermitRenewalTests`: exitoso, 5 pruebas aprobadas.
- `npm run build`: exitoso.
- HTTP runtime: exitoso con cabecera `X-FMCPA-Client: FMCPA-Web`.
- Playwright UI: exitoso, 1 prueba aprobada en 5.3 s.

## Validacion manual por HTTP/UI
Flujo recomendado:

1. Levantar SQL Server local, aplicar migraciones y ejecutar backend.
2. Consultar `GET /api/financials/current-permit` con una financiera, dependencia/institucion y stand/lugar sin vigente ni antecedente.
3. Confirmar `suggestionCode=CREATE_NEW_PERMIT`.
4. En Angular, usar `Preparar alta de oficio` y confirmar que el alta queda prellenada con los tres campos del contexto.
5. Completar vigencia, horario, terminos, observaciones y estatus.
6. Registrar el oficio con la accion existente `Registrar oficio`.
7. Consultar nuevamente `GET /api/financials/current-permit` con el mismo contexto y confirmar `suggestionCode=USE_CURRENT_PERMIT`.
8. Intentar crear un segundo oficio con la misma combinacion para confirmar que backend conserva `409 FINANCIAL_PERMIT_ACTIVE_CONFLICT`.

Resultado HTTP observado:

```json
{
  "financialName": "Financiera Alta HTTP 199033",
  "initialSuggestion": "CREATE_NEW_PERMIT",
  "createdPermitId": "7b1f0a0b-d2ab-486a-8a65-b1589f582afc",
  "finalSuggestion": "USE_CURRENT_PERMIT",
  "finalCurrentPermitId": "7b1f0a0b-d2ab-486a-8a65-b1589f582afc",
  "duplicateStatus": 409,
  "duplicateReasonCode": "FINANCIAL_PERMIT_ACTIVE_CONFLICT"
}
```

Resultado UI observado:
- La resolucion contextual mostro `Preparar alta de oficio` para `CREATE_NEW_PERMIT`.
- El formulario de alta quedo prellenado con financiera, dependencia/institucion y stand/lugar.
- La creacion se confirmo con `Registrar oficio`.
- Una nueva resolucion con el mismo contexto mostro `USE_CURRENT_PERMIT`.

## Fuera de alcance
- No se crea endpoint nuevo.
- No se crea entidad draft persistida.
- No se agrega migracion.
- No se audita la preparacion de UI.
- No se abre catalogo maestro de financieras, dependencias o stands.
- No se agrega workflow, aprobacion multinivel, task inbox ni versionado contractual completo.
- No se toca produccion ni CI/CD.
