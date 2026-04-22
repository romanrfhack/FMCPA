# STAGE-05 Financials and Credits Implementation Note

## Resumen
- Se implemento el modulo Financieras con `FinancialPermit` como registro de oficio o autorizacion, `FinancialCredit` como credito individual y `FinancialCreditCommission` como comision por credito.
- Se agrego soporte de vigencia y alertas basicas para permisos por vencer, vencidos o en renovacion.
- Se permitio registrar multiples comisiones por credito reutilizando `CommissionType`, sin abrir aun una vista transversal global de comisiones.
- Los oficios cerrados permanecen visibles en historico, pero no aparecen en alertas activas.

## Que se implemento
- Entidades backend:
  - `FinancialPermit`
  - `FinancialCredit`
  - `FinancialCreditCommission`
- Persistencia:
  - `DbSet` y configuraciones EF Core del modulo
  - migracion `Stage05FinancialsAndCredits`
  - seeds de estatus reutilizables por contexto:
    - `FINANCIAL_PERMIT`: `ACCEPTED`, `REJECTED`, `IN_PROCESS`, `RENEW`, `CLOSED`
- API minima:
  - `GET /api/financials`
  - `POST /api/financials`
  - `GET /api/financials/{permitId}`
  - `GET /api/financials/alerts/permits`
  - `GET /api/financials/{permitId}/credits`
  - `POST /api/financials/{permitId}/credits`
  - `GET /api/financials/credits/{creditId}/commissions`
  - `POST /api/financials/credits/{creditId}/commissions`
- Frontend Angular:
  - listado de oficios o autorizaciones
  - alta de oficio o autorizacion
  - filtro por estatus y por alertas activas
  - indicador visual de vigencia
  - alta de credito individual
  - listado de creditos por oficio
  - alta de comisiones por credito
  - listado de comisiones por credito

## Decisiones tomadas
- STAGE-05 no abre aun una entidad compartida `FinancialInstitution`; `FinancialPermit` conserva snapshot de financiera, institucion y stand.
- Promotor, beneficiario y destinatario de comision pueden vincularse a `Contact`, pero cada registro conserva snapshot local de nombres y telefonos visibles.
- Las comisiones se mantienen acotadas al credito individual y se distinguen con `RecipientCategory`:
  - `COMPANY`
  - `THIRD_PARTY`
  - `OTHER_PARTICIPANT`
- No se abrio aun un consolidado transversal de comisiones ni se mezclo Financieras con Federacion.

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
dotnet ef migrations add Stage05FinancialsAndCredits --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations --no-build --verbose
/bin/bash -lc 'set +H; ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet ef database update --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --no-build'
```

Resultado:
- migracion generada correctamente: `20260421004641_Stage05FinancialsAndCredits.cs`
- migracion aplicada correctamente sobre `FMCPA_Development` en `127.0.0.1:14333`
- un `database update` posterior confirmo que la base ya estaba al dia

### Ejecucion local y pruebas de API
```bash
docker start bigsmile-sql
set +H; docker exec bigsmile-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "BigSmile_dev_Passw0rd!" -Q "SELECT 1 AS Ready;"
/bin/bash -lc 'set +H; ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet run --no-build --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --urls http://127.0.0.1:5082'
curl -s http://127.0.0.1:5082/health
curl -s 'http://127.0.0.1:5082/api/module-statuses?moduleCode=FINANCIALS&contextCode=FINANCIAL_PERMIT'
curl -s http://127.0.0.1:5082/api/commission-types
curl -s http://127.0.0.1:5082/api/financials
curl -s 'http://127.0.0.1:5082/api/financials?alertsOnly=true'
curl -s http://127.0.0.1:5082/api/financials/alerts/permits
curl -s http://127.0.0.1:5082/api/financials/8e5b5abf-518f-4724-a0ca-08972def4aeb
curl -s http://127.0.0.1:5082/api/financials/8e5b5abf-518f-4724-a0ca-08972def4aeb/credits
curl -s http://127.0.0.1:5082/api/financials/credits/3b2f6923-cce6-433e-a36c-53e260ce29db/commissions
```

Resultado:
- `health`: correcto
- catalogo `FINANCIAL_PERMIT`: correcto
- `GET /api/financials` devuelve tres oficios de validacion:
  - uno `ACCEPTED` con alerta `DUE_SOON`
  - uno `RENEW` con alerta `RENEWAL`
  - uno `CLOSED` con alerta `ALERTS_DISABLED`
- `GET /api/financials?alertsOnly=true` y `GET /api/financials/alerts/permits` excluyen el oficio cerrado
- el detalle del oficio `8e5b5abf-518f-4724-a0ca-08972def4aeb` devuelve un credito individual con `commissionCount = 3`
- el endpoint de comisiones devuelve las tres categorias validadas:
  - `COMPANY`
  - `THIRD_PARTY`
  - `OTHER_PARTICIPANT`

### Frontend
```bash
npm run start -- --host 127.0.0.1 --port 4200
curl -s http://127.0.0.1:4200/
```

Resultado:
- `ng serve` correcto en `http://127.0.0.1:4200/`
- el shell Angular responde correctamente y deja disponible la pantalla funcional de Financieras

## Como validar localmente
- Levantar un SQL Server local accesible en `127.0.0.1:14333` o ajustar `ConnectionStrings__PlatformDatabase`.
- Ejecutar restore, build y migracion con los comandos anteriores.
- Levantar la API y verificar:
  - `GET /health`
  - `GET /api/module-statuses?moduleCode=FINANCIALS&contextCode=FINANCIAL_PERMIT`
  - `GET /api/financials`
  - `GET /api/financials?alertsOnly=true`
- Levantar Angular con `npm run start -- --host 127.0.0.1 --port 4200` y revisar la ruta de Financieras.

## Que quedo explicitamente fuera
- Federacion
- Dashboard global funcional
- vista transversal global de comisiones
- workflow formal de renovacion de oficios
- catalogo compartido de financieras
- autenticacion y autorizacion completas

## Pendientes transferibles
- Confirmar si mas adelante se requiere un catalogo compartido de financieras o si el snapshot por oficio sera suficiente.
- Evaluar un flujo formal de renovacion y versionado de oficios si la operacion lo necesita.
- Abrir STAGE-06 sin romper los contratos ya establecidos de Financieras ni mezclar aun comisiones transversales.
