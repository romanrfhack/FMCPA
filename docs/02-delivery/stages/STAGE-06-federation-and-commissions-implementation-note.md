# STAGE-06 Federation and Commissions Implementation Note

## Resumen
- Se implemento el modulo Federacion con `FederationAction` para gestiones, `FederationDonation` como maestro, `FederationDonationApplication` como detalle multiple, `FederationDonationApplicationCommission` para comision por aplicacion y `FederationDonationApplicationEvidence` para evidencia por aplicacion.
- Se agrego soporte de participantes internos y externos reutilizando `Contact` con snapshot historico de nombre, organizacion y cargo.
- La evidencia quedo con upload real y descarga real mediante storage local configurable acotado al modulo.
- Las donaciones cerradas permanecen visibles en historico, pero no aparecen en alertas activas.

## Que se implemento
- Entidades backend:
  - `FederationAction`
  - `FederationActionParticipant`
  - `FederationDonation`
  - `FederationDonationApplication`
  - `FederationDonationApplicationCommission`
  - `FederationDonationApplicationEvidence`
- Persistencia:
  - `DbSet` y configuraciones EF Core del modulo
  - migracion `Stage06FederationAndCommissions`
  - seeds de estatus reutilizables por contexto:
    - `FEDERATION_ACTION`: `IN_PROCESS`, `FOLLOW_UP_PENDING`, `CONCLUDED`, `CLOSED`
    - `FEDERATION_DONATION`: `NOT_APPLIED`, `PARTIALLY_APPLIED`, `APPLIED`, `CLOSED`
    - `FEDERATION_DONATION_APPLICATION`: `PARTIALLY_APPLIED`, `APPLIED`, `CLOSED`
- API minima:
  - `GET /api/federation/alerts`
  - `GET /api/federation/actions`
  - `POST /api/federation/actions`
  - `GET /api/federation/actions/{actionId}`
  - `GET /api/federation/actions/{actionId}/participants`
  - `POST /api/federation/actions/{actionId}/participants`
  - `GET /api/federation/donations`
  - `POST /api/federation/donations`
  - `GET /api/federation/donations/{donationId}`
  - `GET /api/federation/donations/{donationId}/applications`
  - `POST /api/federation/donations/{donationId}/applications`
  - `GET /api/federation/applications/{applicationId}/commissions`
  - `POST /api/federation/applications/{applicationId}/commissions`
  - `GET /api/federation/applications/{applicationId}/evidences`
  - `POST /api/federation/applications/{applicationId}/evidences`
  - `GET /api/federation/applications/evidences/{evidenceId}/download`
- Frontend Angular:
  - listado y alta de gestiones
  - captura y consulta de participantes internos y externos
  - listado y alta de donaciones de Federacion
  - detalle de donacion con porcentaje aplicado visible
  - alta y listado de aplicaciones
  - alta y listado de comisiones por aplicacion
  - alta y listado de evidencias por aplicacion
  - filtros simples por estatus y por alertas activas

## Decisiones tomadas
- STAGE-06 mantiene entidades propias de Federacion para no mezclar prematuramente este contexto con Donatarias ni con Financieras.
- Los participantes de gestion usan `Contact` como referencia, pero `FederationActionParticipant` conserva snapshot visible para trazabilidad historica.
- La comision se modela a nivel de aplicacion de donacion de Federacion y reutiliza `CommissionType` y `RecipientCategory`.
- La evidencia reutiliza `EvidenceType`, pero el storage sigue siendo acotado a Federacion bajo `App_Data/federation/application-evidences`.

## Validacion local ejecutada

### Restore y build
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
npm run build
```

Resultado:
- `dotnet restore`: correcto
- `dotnet build`: correcto, `0 Warning(s)` y `0 Error(s)`
- `npm run build`: correcto

### Migracion
```bash
dotnet ef migrations add Stage06FederationAndCommissions --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations --no-build --verbose
/bin/bash -lc 'set +H; ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet ef database update --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --no-build'
```

Resultado:
- migracion generada correctamente: `20260421014120_Stage06FederationAndCommissions.cs`
- migracion aplicada correctamente sobre `FMCPA_Development` en `127.0.0.1:14333`

### Ejecucion local y pruebas de API
```bash
docker start bigsmile-sql
set +H; docker exec bigsmile-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "BigSmile_dev_Passw0rd!" -Q "SELECT 1 AS Ready;"
/bin/bash -lc 'set +H; ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet run --no-build --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --urls http://127.0.0.1:5083'
curl -s http://127.0.0.1:5083/health
curl -s 'http://127.0.0.1:5083/api/module-statuses?moduleCode=FEDERATION&contextCode=FEDERATION_ACTION'
curl -s 'http://127.0.0.1:5083/api/module-statuses?moduleCode=FEDERATION&contextCode=FEDERATION_DONATION'
curl -s 'http://127.0.0.1:5083/api/module-statuses?moduleCode=FEDERATION&contextCode=FEDERATION_DONATION_APPLICATION'
curl -s http://127.0.0.1:5083/api/federation/actions
curl -s 'http://127.0.0.1:5083/api/federation/actions?alertsOnly=true'
curl -s http://127.0.0.1:5083/api/federation/actions/f616ee37-1a9b-420c-8af9-758cc4ff9b3a
curl -s http://127.0.0.1:5083/api/federation/actions/f616ee37-1a9b-420c-8af9-758cc4ff9b3a/participants
curl -s http://127.0.0.1:5083/api/federation/alerts
curl -s http://127.0.0.1:5083/api/federation/donations
curl -s 'http://127.0.0.1:5083/api/federation/donations?alertsOnly=true'
curl -s http://127.0.0.1:5083/api/federation/donations/2ba77e19-4f1d-438e-852e-3ca02299d47d
curl -s http://127.0.0.1:5083/api/federation/donations/2ba77e19-4f1d-438e-852e-3ca02299d47d/applications
curl -s http://127.0.0.1:5083/api/federation/applications/db3e1df0-6fd4-4200-ac5a-a139c5bafcf1/commissions
curl -s http://127.0.0.1:5083/api/federation/applications/db3e1df0-6fd4-4200-ac5a-a139c5bafcf1/evidences
curl -s http://127.0.0.1:5083/api/federation/applications/evidences/f6e6a027-a478-48dd-a40a-0c7add83a575/download
```

Resultado:
- `health`: correcto
- catalogos `FEDERATION_ACTION`, `FEDERATION_DONATION` y `FEDERATION_DONATION_APPLICATION`: correctos
- `GET /api/federation/actions` devuelve una gestion activa `IN_PROCESS` y una gestion `CLOSED`
- `GET /api/federation/actions?alertsOnly=true` excluye la gestion cerrada
- el detalle de la gestion activa devuelve dos participantes:
  - uno interno
  - uno externo
- `GET /api/federation/alerts` devuelve:
  - la gestion activa en alertas
  - una donacion `NOT_APPLIED`
  - una donacion `PARTIALLY_APPLIED`
- `GET /api/federation/donations` devuelve tres donaciones de validacion:
  - una `NOT_APPLIED`
  - una `PARTIALLY_APPLIED`
  - una `CLOSED`
- `GET /api/federation/donations?alertsOnly=true` excluye la donacion cerrada
- la donacion parcial queda con:
  - `applicationCount = 2`
  - `appliedAmountTotal = 5500`
  - `remainingAmount = 4500`
  - `appliedPercentage = 55`
  - `commissionCount = 1`
  - `evidenceCount = 1`
- el endpoint de comisiones devuelve una comision asociada a la aplicacion seleccionada
- la evidencia se carga y descarga correctamente

### Frontend
```bash
npm run start -- --host 127.0.0.1 --port 4200
curl -s http://127.0.0.1:4200/
```

Resultado:
- `ng serve` correcto en `http://127.0.0.1:4200/`
- el shell Angular responde correctamente y deja disponible la pantalla funcional de Federacion

## Como validar localmente
- Levantar un SQL Server local accesible en `127.0.0.1:14333` o ajustar `ConnectionStrings__PlatformDatabase`.
- Ejecutar restore, build y migracion con los comandos anteriores.
- Levantar la API y verificar:
  - `GET /health`
  - `GET /api/federation/alerts`
  - `GET /api/federation/actions`
  - `GET /api/federation/donations`
- Levantar Angular con `npm run start -- --host 127.0.0.1 --port 4200` y revisar la ruta de Federacion.

## Que quedo explicitamente fuera
- Dashboard global final
- vista transversal global definitiva de comisiones
- automatizaciones avanzadas o notificaciones reales
- autenticacion y autorizacion completas
- unificacion transversal del modelo de donaciones entre Donatarias y Federacion

## Pendientes transferibles
- Definir en STAGE-07 el alcance exacto del historico, cierre y consolidado transversal de comisiones.
- Confirmar si el limite actual entre Donatarias y Federacion se conserva o si requiere una vista historica comun.
- Mantener la evidencia como storage local por modulo hasta que exista una decision formal de almacenamiento transversal.
