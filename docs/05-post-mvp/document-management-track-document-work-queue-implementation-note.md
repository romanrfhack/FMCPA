# Track 3 - Document Work Queue Implementation Note

## Objetivo
Implementar una bandeja operativa documental unificada para que el usuario pueda revisar en una sola superficie las senales documentales que requieren atencion, sin abrir workflow, asignaciones, aprobaciones, compliance avanzado, backup real ni storage externo.

## Que se implemento
- Endpoint autenticado `GET /api/documents/work-queue`.
- Contratos `DocumentWorkQueueResponse` y `DocumentWorkQueueItemResponse`.
- Vista Angular `/documents/work-queue`.
- Metodo `DocumentCatalogService.listWorkQueue`.
- Guardrail de superficie protegido en `security-authorization-surface-guardrails.json`.
- Prueba de regresion que valida consolidacion de completitud, integridad y retencion.

## Fuentes consolidadas
La bandeja se calcula en lectura a partir de fuentes existentes:
- pendientes de completitud de `GET /api/documents/pending`;
- documentos cuyo estado de integridad no es `VALID`;
- documentos que entran a revision de retencion (`REVIEW_DUE` o `EXPIRED_RETENTION`).

No se crea tabla de tareas ni estado persistido nuevo para la bandeja.

## Modelo de item
Cada item incluye:
- `workItemType`;
- `severityCode`;
- `moduleCode`;
- `entityType`;
- `entityId`;
- `documentId` cuando aplica;
- `title` y `summary`;
- `reasonCode`;
- `currentStatusCode`;
- `originContext`;
- `routeHint`;
- `documentDetailUrl` cuando aplica;
- `remediationHint` cuando existe.

## Severidades
Convencion minima:
- `HIGH`: `COMPLETENESS_PENDING` y `DOCUMENT_INTEGRITY_ISSUE`.
- `MEDIUM`: `RETENTION_REVIEW` con `EXPIRED_RETENTION`.
- `LOW`: `RETENTION_REVIEW` con `REVIEW_DUE`.

## Filtros
El endpoint soporta:
- `moduleCode`;
- `workItemType`;
- `severityCode`;
- `skip`;
- `take`.

Los valores soportados de `workItemType` son:
- `COMPLETENESS_PENDING`;
- `DOCUMENT_INTEGRITY_ISSUE`;
- `RETENTION_REVIEW`.

## Autorizacion
- El endpoint requiere usuario autenticado.
- La API filtra por permisos efectivos de lectura documental del modulo: `MARKETS_READ`, `DONATIONS_READ` y `FEDERATION_READ`.
- Las acciones de remediacion siguen estando fuera de la bandeja y dependen de los permisos de escritura del modulo correspondiente.
- La bandeja no introduce permisos por documento individual.

## Frontend
La ruta `/documents/work-queue` muestra:
- filtros por modulo, tipo y severidad;
- badges de severidad/tipo/modulo;
- origen y motivo del item;
- enlace al contexto de remediacion o a la bandeja de revision cuando aplica.

## Como validar localmente
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter "DocumentCatalogTests"
cd src/frontend
npm run build
```

Validaciones API sugeridas con backend local y token autenticado:
```bash
curl -s "http://127.0.0.1:5080/api/documents/work-queue?take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/work-queue?workItemType=DOCUMENT_INTEGRITY_ISSUE&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/work-queue?severityCode=HIGH&take=20" -H "Authorization: Bearer ${TOKEN}"
curl -s "http://127.0.0.1:5080/api/documents/work-queue?moduleCode=MARKETS&take=20" -H "Authorization: Bearer ${TOKEN}"
```

## Fuera de alcance
- Workflow documental.
- Asignaciones, owners, SLA o estados de tarea.
- Acciones masivas.
- Aprobaciones.
- Compliance documental avanzado.
- Versionado completo.
- Backup real.
- Storage externo.
- OCR o clasificacion automatica.
