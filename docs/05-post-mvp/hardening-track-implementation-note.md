# Hardening Track Implementation Note

## Estado
- Fecha: 2026-04-20
- Track: `Track 1: Hardening y consistencia operativa`
- Resultado actual: Implementacion inicial completada, pendiente de aprobacion formal

## Que se implemento
- Se agrega la entidad transversal `AuditEvent` como bitacora operativa minima real.
- Se agrega la migracion `20260421042942_Track1HardeningAuditAndFormalClose`.
- Se registran eventos reales nuevos hacia adelante para:
  - alta de mercado
  - alta de locatario
  - alta de incidencia
  - alta de donacion
  - alta de aplicacion
  - carga de evidencia en Donatarias
  - alta de oficio
  - alta de credito
  - alta de comision por credito
  - alta de gestion
  - alta de participante
  - alta de donacion de Federacion
  - alta de aplicacion de Federacion
  - alta de evidencia de Federacion
  - alta de comision de Federacion
- Se agregan cierres formales explicitos para:
  - `Market`
  - `Donation`
  - `FinancialPermit`
  - `FederationAction`
  - `FederationDonation`
- Se ajusta `/api/bitacora` para consultar eventos reales con filtros por modulo, entidad, rango de fechas y texto.
- Se ajusta `/api/history/closed-items` para usar fecha de cierre formal cuando existe y fallback al timestamp historico heredado cuando no.
- Se actualizan las pantallas de Bitacora e Historico para mostrar eventos reales, cierres formales y fuente del timestamp historico.
- Se agregan acciones minimas de cierre formal en frontend para Mercados, Donatarias, Financieras y Federacion.

## Decisiones tomadas
- La trazabilidad nueva se resuelve con `AuditEvent`, no con event sourcing ni auditoria forense completa.
- El cierre formal se registra como evento explicito y es la fuente primaria del historico cuando existe.
- No se agregaron timestamps dedicados de cierre a todas las entidades del MVP para evitar una reescritura profunda del modelo.
- Los registros historicos previos al hardening no se reconstruyen de forma masiva; conviven con fallback documentado.

## Convivencia con la trazabilidad MVP previa
- Desde `Track 1`, la bitacora visible consulta eventos reales nuevos almacenados en `AuditEvent`.
- La trazabilidad derivada del MVP previo no se borra conceptualmente, pero deja de ser la fuente principal de la pantalla `/bitacora`.
- El historico ahora distingue dos fuentes:
  - `FORMAL_CLOSE_EVENT`: existe evento de cierre formal real
  - `LEGACY_TIMESTAMP_FALLBACK`: no existe evento formal y se usa `UpdatedUtc` o `CreatedUtc`
- La convivencia es deliberada para endurecer trazabilidad sin reprocesar todo el historico ya existente.

## Validacion local ejecutada

### Restore y build
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
npm run build
```

### Migracion
```bash
dotnet ef migrations add Track1HardeningAuditAndFormalClose --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --output-dir Persistence/Migrations --no-build --verbose
docker start bigsmile-sql
ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet ef database update --project src/backend/src/FMCPA.Infrastructure/FMCPA.Infrastructure.csproj --startup-project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --context PlatformDbContext --no-build
```

### API y validacion de cierres
```bash
ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__PlatformDatabase="Server=127.0.0.1,14333;Database=FMCPA_Development;User Id=sa;Password=BigSmile_dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True" dotnet run --no-build --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --urls http://127.0.0.1:5086
curl -s http://127.0.0.1:5086/health
curl -s -X POST http://127.0.0.1:5086/api/markets ...
curl -s -X POST http://127.0.0.1:5086/api/markets/{id}/close ...
curl -s -X POST http://127.0.0.1:5086/api/donations ...
curl -s -X POST http://127.0.0.1:5086/api/donations/{id}/close ...
curl -s -X POST http://127.0.0.1:5086/api/financials ...
curl -s -X POST http://127.0.0.1:5086/api/financials/{id}/close ...
curl -s -X POST http://127.0.0.1:5086/api/federation/actions ...
curl -s -X POST http://127.0.0.1:5086/api/federation/actions/{id}/close ...
curl -s -X POST http://127.0.0.1:5086/api/federation/donations ...
curl -s -X POST http://127.0.0.1:5086/api/federation/donations/{id}/close ...
curl -s 'http://127.0.0.1:5086/api/bitacora?q=Track1&fromDate=2026-04-20&take=20'
curl -s 'http://127.0.0.1:5086/api/history/closed-items?q=Track1'
curl -s 'http://127.0.0.1:5086/api/history/closed-items?moduleCode=MARKETS'
```

### Resultado de validacion
- Backend compila correctamente.
- Frontend compila correctamente.
- La migracion se genera y se aplica correctamente sobre `FMCPA_Development`.
- Los cinco cierres formales principales responden con `CloseRecordResponse`.
- `/api/bitacora` devuelve eventos reales nuevos de alta y cierre.
- `/api/history/closed-items` devuelve `FORMAL_CLOSE_EVENT` para registros cerrados por flujo formal en esta etapa.
- `/api/history/closed-items?moduleCode=MARKETS` confirma convivencia con un registro legado `ARCHIVED` usando `LEGACY_TIMESTAMP_FALLBACK`.

## Que quedo fuera
- Autenticacion y autorizacion.
- Politica documental transversal.
- Notificaciones reales.
- Analitica avanzada.
- Regularizacion masiva del historico previo al hardening.
- Auditoria forense completa o event sourcing.

## Riesgos y pendientes inmediatos
- Los registros historicos previos a `Track 1` seguiran sin cierre formal hasta que se decida una regularizacion puntual.
- La bitacora real mejora trazabilidad hacia adelante, pero no cubre ediciones finas ni todos los cambios de estado del pasado.
- La validacion local sigue dependiendo de SQL Server temporal en `127.0.0.1:14333`.
