# Track 3 - Resumen ejecutivo documental transversal

## Objetivo
- Agregar una vista ejecutiva rapida del estado documental del sistema sin abrir BI, analitica pesada, exportaciones complejas ni plataforma documental avanzada.

## Que se implemento
- Endpoint autenticado `GET /api/documents/summary`.
- Contratos de respuesta para resumen general, desglose por modulo, desglose por clase documental y categorias de work queue.
- Calculo de KPIs sobre `StoredDocument` y sobre la composicion existente de `GET /api/documents/work-queue`.
- Seccion de resumen ejecutivo en `/documents` con KPIs principales, desglose por modulo y acceso a `/documents/work-queue`.
- Cobertura de regresion backend para validar KPIs, permisos de modulo y exclusion de modulos no mapeados.
- Inventario/guardrail de superficie protegido actualizado para el nuevo endpoint.

## Metricas incluidas
- `totalDocuments`
- `activeDocuments`
- `archivedDocuments`
- `integrityIssuesCount`
- `incompleteEntitiesCount`
- `reviewDueCount`
- `expiredRetentionCount`
- `modules[]` con totales y senales principales por `moduleCode`.
- `documentClasses[]` con totales por `documentClassCode`.
- `workQueueCategories[]` agrupado por `workItemType`, `reasonCode` y `severityCode`.

## Decisiones tomadas
- El resumen se calcula en lectura; no se persisten metricas ni snapshots.
- La fuente de verdad sigue siendo `StoredDocument`, completitud, integridad y review/work queue existentes.
- No se crea migracion porque no hay cambios de schema.
- No se agregan graficas ni exportaciones; la UI solo muestra una superficie ejecutiva simple.

## Permisos por modulo
- El endpoint usa los permisos documentales transversales existentes:
  - `MARKETS_READ`
  - `DONATIONS_READ`
  - `FEDERATION_READ`
- Un usuario solo ve totales y desgloses de modulos que puede leer.
- Documentos con `ModuleCode` no mapeado en la autorizacion documental transversal quedan fuera del resumen hasta que se documente su permiso.

## Validacion local
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
npm run build
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build --filter DocumentCatalogTests
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build
npm test -- --watch=false
```

Validacion manual opcional con backend local:
```bash
curl -s "http://127.0.0.1:5080/api/documents/summary" -H "Authorization: Bearer ${TOKEN}"
```

La respuesta debe incluir KPIs principales, `modules`, `documentClasses` y `workQueueCategories`, sin exponer rutas fisicas ni datos de modulos no permitidos.

## Que quedo fuera
- BI o analitica historica.
- Exportaciones complejas.
- Snapshots persistidos de metricas.
- Graficas avanzadas.
- Compliance documental avanzado.
- Backup real, storage externo, OCR y clasificacion automatica.
