# Track 3 - Document Requirements In Context Implementation Note

## Que se implemento
- Se agrego `GET /api/documents/requirements/by-entity` para consultar el requisito documental aplicable a una entidad.
- La respuesta incluye `ruleCode`, si aplica regla, clases requeridas, minimo requerido, conteo actual, estado `COMPLETE`/`INCOMPLETE`, motivo de faltante y remediacion minima.
- El panel reusable de documentos relacionados ahora muestra requisito, estado, conteo actual/minimo y remediacion en Mercados, Donatarias y Federacion.
- La cola de pendientes en `/documents` tambien muestra la remediacion declarada por la regla.

## Entidades cubiertas
- `MarketTenant`
- `DonationApplication`
- `FederationDonationApplication`

## Como se resuelve el contexto
- La API reutiliza `DocumentRuleRegistry` como fuente canónica de regla.
- La evaluacion usa la misma logica de completitud calculada sobre `StoredDocument` activo y entidades origen.
- `OriginContext` conserva el contexto ya existente de modulo, entidad, nombre/resumen y `routeHint`.
- La remediacion vive en la regla documentada, no en reglas duplicadas de frontend.

## Como validar localmente
1. Compilar backend:
   ```bash
   dotnet restore src/backend/FMCPA.Backend.sln
   dotnet build src/backend/FMCPA.Backend.sln --no-restore
   ```
2. Compilar frontend:
   ```bash
   cd src/frontend
   npm run build
   ```
3. Ejecutar pruebas afectadas:
   ```bash
   dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter Document_completeness_by_entity_and_pending_list_cover_key_entities
   dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build
   ```
4. Con backend local autenticado, validar:
   ```bash
   curl -s "http://127.0.0.1:5080/api/documents/requirements/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId=${TENANT_ID}" -H "Authorization: Bearer ${TOKEN}"
   curl -s "http://127.0.0.1:5080/api/documents/requirements/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId=${DONATION_APPLICATION_ID}" -H "Authorization: Bearer ${TOKEN}"
   curl -s "http://127.0.0.1:5080/api/documents/requirements/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId=${FEDERATION_APPLICATION_ID}" -H "Authorization: Bearer ${TOKEN}"
   ```
5. En frontend, abrir una entidad con panel de documentos relacionados en Mercados, Donatarias o Federacion y verificar el bloque de requisito documental.

## Que quedo fuera
- Workflow documental complejo.
- Edicion dinamica de reglas.
- Aprobaciones o tareas documentales.
- Compliance avanzado.
- Versionado, backup real, storage externo, OCR y clasificacion automatica.
