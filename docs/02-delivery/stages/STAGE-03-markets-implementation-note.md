# STAGE-03 Markets - Nota de implementacion

## Resumen
- Se implemento el primer modulo de negocio real del sistema: Mercados.
- El modulo incluye mercados, locatarios, incidencias, cédula digitalizada con upload real, vigencias y alertas minimas.
- Se reutilizo la base de contactos y de estatus compartidos sin invadir Donatarias, Financieras o Federacion.

## Que se implemento

### Backend
- Entidades del modulo:
  - `Market`
  - `MarketTenant`
  - `MarketIssue`
- Persistencia:
  - configuraciones EF Core del modulo Mercados
  - `DbSet` del modulo en `PlatformDbContext`
  - migracion `Stage03Markets`
- Estatus reutilizables:
  - `ModuleStatusCatalogEntry` incorpora `ContextCode` y `ContextName`
  - estatus sembrados para `MARKETS / MARKET`
  - estatus sembrados para `MARKETS / MARKET_ISSUE`
- API minima:
  - `GET /api/markets`
  - `POST /api/markets`
  - `GET /api/markets/{marketId}`
  - `GET /api/markets/{marketId}/tenants`
  - `POST /api/markets/{marketId}/tenants`
  - `GET /api/markets/{marketId}/issues`
  - `POST /api/markets/{marketId}/issues`
  - `GET /api/markets/alerts/tenants`
  - `GET /api/markets/tenants/{tenantId}/cedula`
- Soporte de cédula digitalizada:
  - upload real multipart
  - almacenamiento local configurable
  - metadatos en base de datos
  - descarga por endpoint dedicado

### Frontend
- Servicio HTTP `MarketsService`
- Modelos tipados del modulo Mercados
- Pantalla funcional de Mercados con:
  - listado de mercados
  - alta de mercado
  - detalle de mercado
  - alta y listado de locatarios
  - upload de cédula
  - indicadores visuales de vigencia
  - alta y listado de incidencias
  - filtro por estatus y por alertas activas

## Decisiones tecnicas aplicadas
- `Contact` se reutiliza como vinculo opcional para secretario general y locatario, pero el modulo guarda snapshot local para mantener trazabilidad historica.
- `ModuleStatusCatalogEntry` se amplio con contexto reusable para distinguir estatus de Mercado y estatus de Incidencia dentro del mismo modulo.
- La cédula digitalizada se resolvio con almacenamiento local configurable bajo `App_Data/markets/tenant-certificates`, sin crear aun un sistema global de documentos.
- El endpoint multipart de alta de locatario se dejo con `DisableAntiforgery()` para esta etapa, porque no existe todavia autenticacion completa ni formularios protegidos de navegador.

## Que quedo fuera
- Donatarias, Financieras, Federacion, Dashboard funcional global y Comisiones de negocio.
- Edicion o eliminacion de mercados, locatarios e incidencias.
- Sistema transversal de documentos para todos los modulos.
- Politica final de alertas de incidencias mas alla del soporte minimo de estatus.
- Autenticacion y autorizacion.

## Validacion local ejecutada

### Comandos
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
npm run build
dotnet ef migrations add Stage03Markets --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations --no-build --verbose
docker start bigsmile-sql
set +H; docker exec bigsmile-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "BigSmile_dev_Passw0rd!" -Q "SELECT 1 AS Ready;"
/bin/bash -lc 'set +H; ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet ef database update --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --no-build'
/bin/bash -lc 'set +H; ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet run --no-build --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --urls http://127.0.0.1:5080'
curl -s 'http://127.0.0.1:5080/api/module-statuses?moduleCode=MARKETS&contextCode=MARKET'
curl -s 'http://127.0.0.1:5080/api/module-statuses?moduleCode=MARKETS&contextCode=MARKET_ISSUE'
curl -s -X POST http://127.0.0.1:5080/api/contacts -H 'Content-Type: application/json' -d '{"name":"Jorge Mercado Stage03","contactTypeId":2,"organizationOrDependency":"Mercados","roleTitle":"Secretario general","mobilePhone":"5553001000","whatsAppPhone":"5553001000","email":"jorge.market.stage03@example.com","notes":"Contacto reutilizable para validacion de Mercados en STAGE-03."}'
curl -s -X POST http://127.0.0.1:5080/api/contacts -H 'Content-Type: application/json' -d '{"name":"Lucia Locataria Stage03","contactTypeId":2,"organizationOrDependency":"Mercados","roleTitle":"Locataria","mobilePhone":"5553002000","whatsAppPhone":"5553002000","email":"lucia.locataria.stage03@example.com","notes":"Contacto reutilizable para locatario de validacion en STAGE-03."}'
curl -s -X POST http://127.0.0.1:5080/api/markets -H 'Content-Type: application/json' -d '{"name":"Mercado Validacion Activo Stage03","borough":"Cuauhtemoc","statusCatalogEntryId":1001,"secretaryGeneralContactId":"ebba5c60-a8f4-4828-bd2d-914a6815f1ee","secretaryGeneralName":"Jorge Mercado Stage03","notes":"Mercado activo creado para validacion end to end de STAGE-03."}'
curl -s -X POST http://127.0.0.1:5080/api/markets -H 'Content-Type: application/json' -d '{"name":"Mercado Validacion Archivado Stage03","borough":"Iztapalapa","statusCatalogEntryId":1004,"secretaryGeneralContactId":"ebba5c60-a8f4-4828-bd2d-914a6815f1ee","secretaryGeneralName":"Jorge Mercado Stage03","notes":"Mercado archivado creado para validar supresion de alertas en STAGE-03."}'
printf '%s\n' 'STAGE-03 market tenant certificate validation' > /tmp/stage03-market-tenant-certificate.pdf
curl -s -X POST http://127.0.0.1:5080/api/markets/284a8286-529f-4c5c-8daf-d4f82fc3a992/tenants -F 'contactId=8569a7a5-748e-450a-814b-235f88befc7e' -F 'tenantName=Lucia Locataria Stage03' -F 'certificateNumber=CED-3001' -F 'certificateValidityTo=2026-04-28' -F 'businessLine=Abarrotes' -F 'mobilePhone=5553002000' -F 'whatsAppPhone=5553002000' -F 'email=lucia.locataria.stage03@example.com' -F 'notes=Locatario de validacion activo para STAGE-03.' -F 'certificateFile=@/tmp/stage03-market-tenant-certificate.pdf;type=application/pdf'
curl -s -X POST http://127.0.0.1:5080/api/markets/a77499cb-41a7-44d6-a07b-fb1c4710be24/tenants -F 'tenantName=Locatario Archivado Stage03' -F 'certificateNumber=CED-3999' -F 'certificateValidityTo=2026-04-25' -F 'businessLine=Comida' -F 'mobilePhone=5553999000' -F 'whatsAppPhone=5553999000' -F 'email=locatario.archivado.stage03@example.com' -F 'notes=Locatario de validacion en mercado archivado.' -F 'certificateFile=@/tmp/stage03-market-tenant-certificate.pdf;type=application/pdf'
curl -s -X POST http://127.0.0.1:5080/api/markets/284a8286-529f-4c5c-8daf-d4f82fc3a992/issues -H 'Content-Type: application/json' -d '{"issueType":"Mejora de infraestructura","description":"Solicitud de mejora en instalacion electrica del mercado.","issueDate":"2026-04-20","advanceSummary":"Se levanto revision inicial y se programo seguimiento.","statusCatalogEntryId":1102,"followUpOrResolution":"Seguimiento acordado con administracion local.","finalSatisfaction":null}'
curl -s http://127.0.0.1:5080/api/markets
curl -s 'http://127.0.0.1:5080/api/markets?alertsOnly=true'
curl -s http://127.0.0.1:5080/api/markets/284a8286-529f-4c5c-8daf-d4f82fc3a992
curl -s http://127.0.0.1:5080/api/markets/284a8286-529f-4c5c-8daf-d4f82fc3a992/tenants
curl -s http://127.0.0.1:5080/api/markets/284a8286-529f-4c5c-8daf-d4f82fc3a992/issues
curl -s http://127.0.0.1:5080/api/markets/alerts/tenants
curl -s http://127.0.0.1:5080/api/markets/tenants/a8b8a7c9-2a10-4938-8aac-dc2f8382d850/cedula
npm run start -- --host 127.0.0.1 --port 4200
curl -s http://127.0.0.1:4200/
```

### Resultado
- `dotnet restore`: correcto.
- `dotnet build`: correcto, sin warnings ni errores.
- `npm run build`: correcto.
- `dotnet ef migrations add`: correcto; migracion `Stage03Markets` generada.
- `docker start bigsmile-sql`: correcto.
- `sqlcmd SELECT 1`: correcto; SQL Server listo.
- `dotnet ef database update`: correcto sobre `FMCPA_Development` en `127.0.0.1:14333`.
- `dotnet run`: correcto; API escuchando en `http://127.0.0.1:5080`.
- `GET` de estatus de mercado e incidencia: correcto; devuelve semillas `MARKET` y `MARKET_ISSUE`.
- `POST` de contactos compartidos: correcto.
- `POST` de mercados activo y archivado: correcto.
- `POST` de locatario con upload real de cédula sobre mercado activo: correcto; archivo almacenado y metadata registrada.
- `POST` de locatario sobre mercado archivado: correcto; respuesta con `alertsSuppressed=true`.
- `POST` de incidencia: correcto.
- `GET /api/markets`: correcto; muestra el mercado activo con `activeTenantAlertsCount=1` y el archivado con `activeTenantAlertsCount=0`.
- `GET /api/markets?alertsOnly=true`: correcto; devuelve solo el mercado activo.
- `GET` de detalle, locatarios, incidencias y alertas: correctos.
- `GET` de descarga de cédula: correcto; devuelve el contenido subido.
- `npm run start -- --host 127.0.0.1 --port 4200`: correcto; servidor Angular arriba.
- `curl -s http://127.0.0.1:4200/`: correcto; HTML base servido por Angular.

## Pendientes transferibles
- Aprobacion formal de STAGE-03.
- Definir si la estrategia de archivos de Mercados se conservara como patron local para modulos siguientes o si migrara a una capacidad transversal.
- Confirmar el tratamiento futuro de `ContactParticipation` y de los estatus terminales de incidencias cuando existan tableros o alertas de negocio.
