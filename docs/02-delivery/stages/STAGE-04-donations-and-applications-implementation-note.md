# STAGE-04 Donations and Applications Implementation Note

## Resumen
- Se implemento el modulo Donatarias con `Donation` como maestro y `DonationApplication` como detalle multiple.
- Se agrego soporte de evidencias por aplicacion con upload real de archivos y descarga por endpoint.
- El porcentaje aplicado del maestro se calcula con base en la suma de montos aplicados respecto al monto total de la donacion.
- Las donaciones cerradas permanecen visibles en historico, pero no aparecen en alertas activas.

## Que se implemento
- Entidades backend:
  - `Donation`
  - `DonationApplication`
  - `DonationApplicationEvidence`
- Persistencia:
  - `DbSet` y configuraciones EF Core del modulo
  - migracion `Stage04DonationsAndApplications`
  - seeds de estatus reutilizables por contexto:
    - `DONATION`: `NOT_APPLIED`, `PARTIALLY_APPLIED`, `APPLIED`, `CLOSED`
    - `DONATION_APPLICATION`: `PARTIALLY_APPLIED`, `APPLIED`, `CLOSED`
- API minima:
  - `GET /api/donations`
  - `POST /api/donations`
  - `GET /api/donations/{id}`
  - `GET /api/donations/{id}/progress`
  - `GET /api/donations/{id}/applications`
  - `POST /api/donations/{id}/applications`
  - `GET /api/donations/alerts`
  - `GET /api/donations/applications/{applicationId}/evidences`
  - `POST /api/donations/applications/{applicationId}/evidences`
  - `GET /api/donations/applications/evidences/{evidenceId}/download`
- Frontend Angular:
  - listado de donaciones
  - alta de donacion
  - detalle de donacion
  - alta de multiples aplicaciones
  - listado de aplicaciones
  - porcentaje aplicado visible
  - alta de evidencia por aplicacion
  - consulta y descarga de evidencias
  - filtros por estatus y por alertas activas

## Decisiones tomadas
- La donacion maestra queda separada de sus aplicaciones; no se uso un modelo plano.
- El estatus maestro no cerrado se recalcula automaticamente con base en lo aplicado:
  - `NOT_APPLIED` si el acumulado es `0`
  - `PARTIALLY_APPLIED` si el acumulado es mayor a `0` y menor al monto base
  - `APPLIED` si el acumulado alcanza el monto base
- En esta etapa, el alta inicial del maestro solo permite `NOT_APPLIED` o `CLOSED`.
- El responsable de una aplicacion puede vincularse a `Contact`, pero la aplicacion conserva snapshot de `ResponsibleName`.
- La evidencia reutiliza `EvidenceType`, pero el storage sigue siendo acotado a Donatarias.
- Se bloqueo registrar aplicaciones por encima del monto remanente para evitar porcentajes mayores a `100`.

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
dotnet ef migrations add Stage04DonationsAndApplications --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations --no-build --verbose
/bin/bash -lc 'set +H; ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet ef database update --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --no-build'
```

Resultado:
- migracion generada correctamente: `20260421000650_Stage04DonationsAndApplications.cs`
- migracion aplicada correctamente sobre `FMCPA_Development` en `127.0.0.1:14333`

Nota:
- el primer `database update` fallo por `PendingModelChangesWarning` porque el binario aun no reflejaba la migracion recien creada; se recompilo backend y se repitio con exito

### Ejecucion local y pruebas de API
```bash
docker start bigsmile-sql
set +H; docker exec bigsmile-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "BigSmile_dev_Passw0rd!" -Q "SELECT 1 AS Ready;"
/bin/bash -lc 'set +H; ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet run --no-build --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --urls http://127.0.0.1:5081'
curl -s http://127.0.0.1:5081/health
curl -s 'http://127.0.0.1:5081/api/module-statuses?moduleCode=DONATARIAS&contextCode=DONATION'
curl -s 'http://127.0.0.1:5081/api/module-statuses?moduleCode=DONATARIAS&contextCode=DONATION_APPLICATION'
curl -s -X POST http://127.0.0.1:5081/api/contacts -H 'Content-Type: application/json' -d '{"name":"Marta Responsable Stage04","contactTypeId":1,"organizationOrDependency":"Donatarias Validacion","roleTitle":"Responsable de aplicacion","mobilePhone":"5554001000","whatsAppPhone":"5554001000","email":"marta.responsable.stage04@example.com","notes":"Contacto reutilizable para validar STAGE-04."}'
curl -s -X POST http://127.0.0.1:5081/api/donations -H 'Content-Type: application/json' -d '{"donorEntityName":"Donacion Validacion Pendiente Stage04","donationDate":"2026-04-20","donationType":"Efectivo","baseAmount":15000.00,"reference":"DON-STAGE04-001","notes":"Donacion pendiente para validar alertas de no aplicada.","statusCatalogEntryId":1201}'
curl -s -X POST http://127.0.0.1:5081/api/donations -H 'Content-Type: application/json' -d '{"donorEntityName":"Donacion Validacion Parcial Stage04","donationDate":"2026-04-20","donationType":"Donativo en efectivo","baseAmount":20000.00,"reference":"DON-STAGE04-002","notes":"Donacion con multiples aplicaciones para validar porcentaje.","statusCatalogEntryId":1201}'
curl -s -X POST http://127.0.0.1:5081/api/donations -H 'Content-Type: application/json' -d '{"donorEntityName":"Donacion Validacion Cerrada Stage04","donationDate":"2026-04-20","donationType":"Especie","baseAmount":9000.00,"reference":"DON-STAGE04-003","notes":"Donacion cerrada para validar exclusion de alertas.","statusCatalogEntryId":1204}'
curl -s -X POST http://127.0.0.1:5081/api/donations/12d3066c-dc0d-4747-9cf8-f95decd81e39/applications -H 'Content-Type: application/json' -d '{"beneficiaryName":"Beneficiario Stage04 A","responsibleContactId":"a5b2834c-3155-44a9-9936-660fed81cca8","responsibleName":"Marta Responsable Stage04","applicationDate":"2026-04-20","appliedAmount":5000.00,"statusCatalogEntryId":1301,"verificationDetails":"Aplicacion parcial inicial validada en sitio.","closingDetails":null}'
curl -s -X POST http://127.0.0.1:5081/api/donations/12d3066c-dc0d-4747-9cf8-f95decd81e39/applications -H 'Content-Type: application/json' -d '{"beneficiaryName":"Beneficiario Stage04 B","responsibleContactId":"a5b2834c-3155-44a9-9936-660fed81cca8","responsibleName":"Marta Responsable Stage04","applicationDate":"2026-04-21","appliedAmount":7000.00,"statusCatalogEntryId":1302,"verificationDetails":"Segunda aplicacion con detalle documental.","closingDetails":null}'
printf '%s\n' 'STAGE-04 donation evidence validation' > /tmp/stage04-donation-evidence.pdf
curl -s -X POST http://127.0.0.1:5081/api/donations/applications/9820e627-dcb5-4ee1-981d-225e2ec35045/evidences -F 'evidenceTypeId=4' -F 'description=Documento soporte de la aplicacion Stage04.' -F 'file=@/tmp/stage04-donation-evidence.pdf;type=application/pdf'
curl -s http://127.0.0.1:5081/api/donations
curl -s 'http://127.0.0.1:5081/api/donations?alertsOnly=true'
curl -s http://127.0.0.1:5081/api/donations/12d3066c-dc0d-4747-9cf8-f95decd81e39/progress
curl -s http://127.0.0.1:5081/api/donations/alerts
curl -s http://127.0.0.1:5081/api/donations/12d3066c-dc0d-4747-9cf8-f95decd81e39
curl -s http://127.0.0.1:5081/api/donations/12d3066c-dc0d-4747-9cf8-f95decd81e39/applications
curl -s http://127.0.0.1:5081/api/donations/applications/9820e627-dcb5-4ee1-981d-225e2ec35045/evidences
curl -s http://127.0.0.1:5081/api/donations/applications/evidences/d95b884b-ccd2-44d0-aba9-6d5da5f22676/download
```

Resultado:
- `health`: correcto
- catalogos `DONATION` y `DONATION_APPLICATION`: correctos
- se crean tres donaciones de validacion:
  - una `NOT_APPLIED`
  - una `PARTIALLY_APPLIED` con dos aplicaciones
  - una `CLOSED`
- la donacion parcial queda con:
  - `appliedAmountTotal = 12000`
  - `remainingAmount = 8000`
  - `appliedPercentage = 60`
  - `applicationCount = 2`
  - `evidenceCount = 1`
- `GET /api/donations?alertsOnly=true` devuelve solo la donacion pendiente y la parcial; la cerrada no aparece
- `GET /api/donations/alerts` devuelve solo alertas `NOT_APPLIED` y `PARTIALLY_APPLIED`
- la evidencia se carga y descarga correctamente

### Frontend
```bash
npm run start -- --host 127.0.0.1 --port 4200
curl -s http://127.0.0.1:4200/
```

Resultado:
- `ng serve` correcto en `http://127.0.0.1:4200/`
- el shell Angular responde correctamente

## Que quedo explicitamente fuera
- Flujos de edicion o cierre posterior de donaciones y aplicaciones
- vista transversal de comisiones
- Financieras
- Federacion
- Dashboard global funcional
- sistema documental transversal entre modulos
- autenticacion y autorizacion completas

## Pendientes transferibles
- Definir flujo dedicado de cierre/actualizacion del maestro si se requiere operacion posterior a la alta inicial
- Confirmar estrategia estable de almacenamiento local de evidencias antes de abrir otros modulos con adjuntos
- Abrir STAGE-05 sin tocar contratos ya establecidos de Donatarias
