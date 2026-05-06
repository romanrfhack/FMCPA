# Document Management Track - Document Context Navigation Implementation Note

## Que se implemento
- Se agrego `OriginContext` en las respuestas de `GET /api/documents` y `GET /api/documents/{documentId}`.
- Se agrego `GET /api/documents/by-entity` para consultar documentos relacionados por `moduleCode`, `entityType` y `entityId`.
- Se resolvio contexto de origen para:
  - `MARKETS` + `MARKET_TENANT`
  - `DONATARIAS` + `DONATION_APPLICATION`
  - `FEDERATION` + `FEDERATION_DONATION_APPLICATION`
- La UI `/documents` muestra origen, entidad, resumen y enlace contextual cuando el backend entrega `routeHint`.
- Mercados, Donatarias y Federacion muestran un panel minimo de documentos relacionados dentro de sus pantallas de negocio.

## Decisiones tomadas
- No se agregaron columnas ni migraciones. La relacion se resuelve con `StoredDocument.ModuleCode`, `EntityType` y `EntityId`.
- La resolucion de `displayName`, `summary` y `routeHint` queda acotada a las tres relaciones documentales existentes.
- Otros `EntityType` siguen funcionando con fallback generico: entidad tecnica, identificador corto y ruta base del modulo si existe.
- El frontend reutiliza `DocumentCatalogService` y un componente standalone `RelatedDocumentsPanelComponent` para evitar duplicar logica de descarga y listado por entidad.

## Entidades cubiertas
- Mercados:
  - Documento: cedula de locatario.
  - Origen navegable: `MarketTenant`.
  - Ruta sugerida: `/markets?marketId={marketId}&tenantId={tenantId}`.
- Donatarias:
  - Documento: evidencia de aplicacion.
  - Origen navegable: `DonationApplication`.
  - Ruta sugerida: `/donatarias?donationId={donationId}&applicationId={applicationId}`.
- Federacion:
  - Documento: evidencia de aplicacion de donacion de Federacion.
  - Origen navegable: `FederationDonationApplication`.
  - Ruta sugerida: `/federation?donationId={donationId}&applicationId={applicationId}`.

## Como se resuelve el contexto origen
- Para `MARKET_TENANT`, el backend consulta el locatario y su mercado para armar nombre, resumen y ruta.
- Para evidencias de Donatarias, el backend resuelve evidencia -> aplicacion -> donacion.
- Para evidencias de Federacion, el backend resuelve evidencia -> aplicacion -> donacion de Federacion.
- La consulta por entidad incluye evidencias asociadas a una aplicacion aunque el `StoredDocument.EntityId` apunte al registro de evidencia.

## Como validarlo localmente
1. Restaurar y compilar backend:
   ```bash
   dotnet restore src/backend/FMCPA.Backend.sln
   dotnet build src/backend/FMCPA.Backend.sln --no-restore
   ```
2. Compilar frontend:
   ```bash
   cd src/frontend
   npm run build
   ```
3. Ejecutar pruebas de regresion:
   ```bash
   dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --no-build
   ```
4. Con backend local levantado y token autenticado, validar:
   ```bash
   curl -s "http://127.0.0.1:5080/api/documents?includeArchived=true&take=20" -H "Authorization: Bearer ${TOKEN}"
   curl -s "http://127.0.0.1:5080/api/documents/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId=${TENANT_ID}&take=20" -H "Authorization: Bearer ${TOKEN}"
   curl -s "http://127.0.0.1:5080/api/documents/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId=${DONATION_APPLICATION_ID}&take=20" -H "Authorization: Bearer ${TOKEN}"
   curl -s "http://127.0.0.1:5080/api/documents/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId=${FEDERATION_APPLICATION_ID}&take=20" -H "Authorization: Bearer ${TOKEN}"
   ```
5. En frontend local, revisar:
   - `/documents`: columna/bloque de origen y enlace contextual.
   - `/markets`: documentos relacionados por locatario.
   - `/donatarias`: documentos relacionados por aplicacion seleccionada.
   - `/federation`: documentos relacionados por aplicacion seleccionada.

## Que quedo fuera
- Busqueda full-text.
- Versionado documental.
- Backup real.
- Storage externo.
- OCR o clasificacion automatica.
- Plataforma documental completa.
- Permisos por documento individual.
- Navegacion contextual para modulos sin documentos transversales aprobados.
