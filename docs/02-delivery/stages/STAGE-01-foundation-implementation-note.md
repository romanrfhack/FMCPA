# STAGE-01 Foundation Implementation Note

## Que se implemento
- Backend .NET 10 en `src/backend` con cuatro proyectos:
  - `FMCPA.Api`
  - `FMCPA.Domain`
  - `FMCPA.Application`
  - `FMCPA.Infrastructure`
- Configuracion base para SQL Server en `appsettings.json` y `appsettings.Development.json`.
- `DbContext` base (`PlatformDbContext`) y entidad tecnica neutral `SystemSetting`.
- Endpoint JSON de health en `/health`.
- CORS local preparado para frontend en `http://localhost:4200` y `http://127.0.0.1:4200`.
- OpenAPI habilitado solo en desarrollo.
- Frontend Angular 21 en `src/frontend` con:
  - shell base
  - routing base
  - navegacion placeholder a Dashboard, Mercados, Donatarias, Financieras, Federacion, Comisiones, Contactos y Bitacora
  - servicio minimo de connectivity check contra `/health`
  - environments base para la URL de API

## Que quedo fuera
- Cualquier entidad o logica de negocio de Mercados, Donatarias, Financieras, Federacion, Comisiones o Contactos.
- CRUDs funcionales.
- Autenticacion.
- Migraciones reales ejecutadas contra SQL Server.
- Catalogos funcionales.
- Dashboards funcionales o analitica.

## Como validar localmente
### Backend
```bash
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet restore src/backend/FMCPA.Backend.sln
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet build src/backend/FMCPA.Backend.sln
DOTNET_CLI_HOME=/tmp/dotnet-home dotnet run --no-build --project src/backend/src/FMCPA.Api/FMCPA.Api.csproj --urls http://127.0.0.1:5080
```

### Frontend
```bash
cd src/frontend
npm run build
npm run start -- --host 127.0.0.1 --port 4200
```

### Verificacion minima
```bash
curl -s http://127.0.0.1:5080/health
curl -s http://127.0.0.1:4200/
```

## Resultado esperado
- Backend compilado y escuchando en `http://127.0.0.1:5080`
- Respuesta JSON en `/health`
- Frontend compilado y servido en `http://127.0.0.1:4200`
- Shell visible con placeholders y sin logica de negocio
