# Track 3 - Document Rules Registry Implementation Note

## Que se implemento
- Se agrego `DocumentRuleRegistry` como fuente canónica en codigo para reglas documentales minimas.
- Se agrego `GET /api/documents/rules` como consulta de solo lectura para diagnostico y trazabilidad.
- `GET /api/documents/completeness/by-entity`, `GET /api/documents/pending` y `GET /api/documents/requirements/by-entity` consumen el registry para resolver `ruleCode`, clases requeridas, cardinalidad minima, mensajes de faltante y remediacion minima.
- Las respuestas de completitud y pendientes ahora incluyen `ruleCode`, `requiredDocumentClassCodes` y `minimumRequiredCount`.
- La UI `/documents` y los paneles de documentos relacionados muestran mejor contexto de regla incumplida.

## Reglas centralizadas
| RuleCode | ModuleCode | EntityType | Documento requerido |
| --- | --- | --- | --- |
| `MARKET_TENANT_CERTIFICATE_REQUIRED` | `MARKETS` | `MARKET_TENANT` | Minimo `1` documento activo `CERTIFICATE` en cédulas de locatario |
| `DONATION_APPLICATION_EVIDENCE_REQUIRED` | `DONATARIAS` | `DONATION_APPLICATION` | Minimo `1` evidencia activa en cualquier clase documental soportada |
| `FEDERATION_DONATION_APPLICATION_EVIDENCE_REQUIRED` | `FEDERATION` | `FEDERATION_DONATION_APPLICATION` | Minimo `1` evidencia activa en cualquier clase documental soportada |

## Decisiones tomadas
- El registry vive en codigo y no en base de datos para evitar edicion dinamica prematura.
- No se agrego migracion; la evaluacion sigue calculada en lectura sobre `StoredDocument` y entidades origen.
- El registry centraliza codigos, clases, cardinalidad, mensajes y remediacion; las queries por entidad siguen acotadas al modelo relacional actual porque las evidencias de Donatarias y Federacion cuelgan de entidades hijas.
- `GET /api/documents/rules` devuelve solo reglas de modulos que el usuario autenticado puede leer.

## Entidades cubiertas
- `MarketTenant`
- `DonationApplication`
- `FederationDonationApplication`

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
   curl -s "http://127.0.0.1:5080/api/documents/rules" -H "Authorization: Bearer ${TOKEN}"
   curl -s "http://127.0.0.1:5080/api/documents/completeness/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId=${TENANT_ID}" -H "Authorization: Bearer ${TOKEN}"
   curl -s "http://127.0.0.1:5080/api/documents/pending?take=20" -H "Authorization: Bearer ${TOKEN}"
   ```

## Que quedo fuera
- Edicion dinamica de reglas.
- Compliance documental avanzado.
- Motor de reglas generico.
- Workflow documental.
- Versionado, backup real, storage externo, OCR y clasificacion automatica.
