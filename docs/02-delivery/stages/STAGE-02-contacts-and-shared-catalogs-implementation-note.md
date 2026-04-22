# STAGE-02 Contacts and Shared Catalogs - Nota de implementacion

## Resumen
- Se implementaron entidades compartidas neutrales para contactos y catalogos reutilizables.
- Se agregaron configuraciones EF Core, `DbSet`, semilla base y la migracion `Stage02ContactsAndSharedCatalogs`.
- Se expusieron endpoints REST minimos para contactos, tipos de comision, tipos de evidencia y estatus por modulo.
- Se reemplazo el placeholder de Contactos por una vista funcional y se agregaron pantallas Angular para los catalogos compartidos de STAGE-02.

## Que se implemento

### Backend
- Entidades compartidas:
  - `Contact`
  - `ContactType`
  - `ContactParticipation`
  - `CommissionType`
  - `EvidenceType`
  - `ModuleStatusCatalogEntry`
- Persistencia:
  - `PlatformDbContext` actualizado con `DbSet` compartidos
  - configuraciones EF Core en `Persistence/Configurations/Shared`
  - semilla base para `ContactType`, `CommissionType`, `EvidenceType` y estatus `ACTIVE`/`CLOSED` por modulo
  - migracion `20260420210625_Stage02ContactsAndSharedCatalogs`
- API:
  - `GET /api/contact-types`
  - `GET /api/contacts`
  - `POST /api/contacts`
  - `GET /api/commission-types`
  - `POST /api/commission-types`
  - `GET /api/evidence-types`
  - `POST /api/evidence-types`
  - `GET /api/module-statuses`
  - `POST /api/module-statuses`

### Frontend
- Servicio HTTP `SharedCatalogsService`
- Modelos tipados para contratos de contactos y catalogos compartidos
- Pantalla funcional `Contactos`
- Pantalla funcional `Tipos de comision`
- Pantalla funcional `Tipos de evidencia`
- Pantalla funcional `Estatus por modulo`
- Integracion de nuevas rutas y navegacion dentro del shell existente

## Decisiones tecnicas aplicadas en esta implementacion
- `ContactParticipation` se dejo generico con `moduleCode`, `contextType` y `contextKey` para no acoplar STAGE-02 a entidades de negocio aun no aprobadas.
- La semilla de estatus por modulo se limito a `ACTIVE` y `CLOSED` como base reusable inicial.
- `PlatformDbContextFactory` se ajusto para respetar `ConnectionStrings__PlatformDatabase` y permitir migraciones contra bases locales temporales sin editar `appsettings`.
- La validacion end to end se realizo con una instancia temporal local de SQL Server en Docker publicada en `127.0.0.1:14333`.

## Que quedo fuera
- Relaciones fuertes de `ContactParticipation` hacia entidades de negocio.
- CRUDs funcionales de Mercados, Donatarias, Financieras, Federacion o Comisiones de negocio.
- Dashboard funcional y vistas operativas de negocio.
- Autenticacion y autorizacion.

## Validacion local ejecutada

### Comandos
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
dotnet ef migrations add Stage02ContactsAndSharedCatalogs --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations --no-build --verbose
docker start bigsmile-sql
set +H; docker exec bigsmile-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "BigSmile_dev_Passw0rd!" -Q "SELECT 1 AS Ready;"
/bin/bash -lc 'set +H; ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet ef database update --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --no-build'
/bin/bash -lc 'set +H; ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet run --no-build --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --urls http://127.0.0.1:5080'
curl -s http://127.0.0.1:5080/health
curl -s http://127.0.0.1:5080/api/contact-types
curl -s http://127.0.0.1:5080/api/commission-types
curl -s http://127.0.0.1:5080/api/evidence-types
curl -s http://127.0.0.1:5080/api/module-statuses
curl -s -X POST http://127.0.0.1:5080/api/contacts -H 'Content-Type: application/json' -d '{"name":"Ana Prueba Stage 02","contactTypeId":1,"organizationOrDependency":"Operacion interna","roleTitle":"Enlace operativo","mobilePhone":"5550001000","whatsAppPhone":"5550001000","email":"ana.stage02@example.com","notes":"Registro de validacion tecnica de STAGE-02."}'
curl -s -X POST http://127.0.0.1:5080/api/commission-types -H 'Content-Type: application/json' -d '{"code":"FIELD_SUPPORT","name":"Soporte en campo","description":"Tipo agregado durante la validacion local de STAGE-02.","sortOrder":10}'
curl -s -X POST http://127.0.0.1:5080/api/module-statuses -H 'Content-Type: application/json' -d '{"moduleCode":"CONTACTS","moduleName":"Contactos","statusCode":"ON_HOLD","statusName":"En espera","description":"Estado agregado durante la validacion local de STAGE-02.","sortOrder":3,"isClosed":false,"alertsEnabledByDefault":false}'
curl -s http://127.0.0.1:5080/api/contacts
curl -s 'http://127.0.0.1:5080/api/module-statuses?moduleCode=CONTACTS'
npm run build
npm run start -- --host 127.0.0.1 --port 4200
curl -s http://127.0.0.1:4200/
```

### Resultado
- `dotnet restore`: correcto.
- `dotnet build`: correcto, sin warnings ni errores.
- `dotnet ef migrations add`: correcto; migracion generada con `--no-build`.
- `docker start bigsmile-sql`: correcto.
- `sqlcmd SELECT 1`: correcto; SQL Server listo.
- `dotnet ef database update`: correcto usando la cadena temporal a `127.0.0.1:14333`.
- `dotnet run`: correcto; API escuchando en `http://127.0.0.1:5080`.
- `GET /health`: correcto; estado `Healthy`.
- `GET /api/contact-types`, `/api/commission-types`, `/api/evidence-types`, `/api/module-statuses`: correctos con datos sembrados.
- `POST /api/contacts`: correcto; contacto de validacion creado.
- `POST /api/commission-types`: correcto; tipo `FIELD_SUPPORT` creado.
- `POST /api/module-statuses`: correcto; estatus `CONTACTS/ON_HOLD` creado.
- `GET /api/contacts`: correcto; devuelve el contacto creado.
- `GET /api/module-statuses?moduleCode=CONTACTS`: correcto; devuelve `ACTIVE`, `CLOSED` y `ON_HOLD`.
- `npm run build`: correcto.
- `npm run start -- --host 127.0.0.1 --port 4200`: correcto; servidor local arriba.
- `curl -s http://127.0.0.1:4200/`: correcto; HTML base servido por Angular.

## Pendientes transferibles
- Aprobacion formal de STAGE-02.
- Definir el patron canonico de vinculacion de `ContactParticipation` con entidades reales de negocio.
- Definir si el entorno local estable del equipo usara `1433`, `14333` u otra convension para SQL Server antes de abrir STAGE-03.
