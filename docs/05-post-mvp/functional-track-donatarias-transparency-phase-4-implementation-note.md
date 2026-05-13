# Donatarias Transparencia Fase 4 - Reporte de transparencia por donacion

Fecha: 2026-05-12

## Objetivo

Convertir el tab `Reporte de transparencia` en una vista real y presentable por donacion, consolidando informacion financiera, documental y operativa sin abrir exportacion formal, folio, firma, aprobacion documental humana ni validacion legal avanzada.

## Implementado

- Endpoint de lectura `GET /api/donations/{donationId}/transparency-report`.
- Proteccion con la policy existente de Donatarias lectura (`DONATIONS_READ`).
- Contratos de reporte en `DonationsContracts.cs`.
- Calculo de reporte en `DonationsEndpoints.cs` sin persistir snapshot ni crear migracion.
- Reutilizacion del agregado documental de Fase 3 (`BuildDonationDocumentaryStatusAsync`) para evitar reglas contradictorias.
- Tab `Reporte de transparencia` en `/donatarias` conectado al endpoint nuevo.
- Pruebas backend para readiness, evidencia pendiente, evidencia completa, ausencia de rutas internas y acceso anonimo.

## Estructura del reporte

El endpoint devuelve:

- Datos base: `donationId`, donante, fecha, tipo, referencia, notas y `reportGeneratedUtc`.
- `financialSummary`: monto recibido, aplicado, saldo, porcentaje aplicado y numero de aplicaciones.
- `operationalStatus`: estatus actual de donacion, indicador terminal, etiqueta financiera y etiqueta operativa.
- `documentarySummary`: estado documental agregado, aplicaciones completas/pendientes y bandera de evidencia minima completa.
- `applications`: beneficiario, fecha, responsable, monto, porcentaje, estatus, detalles, conteos documentales, faltante y evidencias.
- `evidences`: tipo, nombre original, descripcion, fecha de carga y `downloadUrl` protegido.
- `presentationReadiness`: `READY`, `PARTIAL` o `NOT_READY` con razones.
- `scopeNotes`: notas visibles de alcance documental y operativo.

## Readiness operativo preliminar

- `READY`: saldo pendiente en cero, al menos una aplicacion y `documentaryStatusCode = MINIMUM_EVIDENCE_COMPLETE`.
- `PARTIAL`: existen aplicaciones, pero hay saldo pendiente o evidencia documental pendiente.
- `NOT_READY`: no existen aplicaciones registradas.

Este criterio es solo operativo preliminar. No representa aprobacion legal, fiscal, contable ni documental humana.

## Limites del reporte

- No genera PDF, CSV ni vista imprimible oficial.
- No crea folio, versionamiento, firma ni snapshot persistido.
- No cambia schema, migraciones, permisos ni significado de estatus.
- No abre checklist legal avanzado ni aprobacion documental humana.
- No expone `StoredRelativePath` ni rutas fisicas internas.
- La evidencia minima registrada acredita presencia documental en el sistema. No sustituye revision legal, fiscal o contable.

## Validacion local

Validacion ejecutada en la sesion:

- `dotnet restore src/backend/FMCPA.Backend.sln`
- `dotnet build src/backend/FMCPA.Backend.sln` sin warnings ni errores.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj`: `267/267`.
- `npm run build` exitoso con advertencia existente de presupuesto CSS del componente Donatarias.
- `npm test -- --watch=false`: `22/22`.
- Playwright headless con API mockeada para donacion sin aplicaciones, donacion parcial con evidencia pendiente y donacion completa con evidencia.
- `git diff --check` sin observaciones.
- Sin archivos nuevos en `src/backend/src/FMCPA.Infrastructure/Persistence/Migrations`.

Comandos minimos:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter DonationTransparencyReportTests
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj
cd src/frontend
npm run build
npm test -- --watch=false
cd ../..
git diff --check
```

Escenarios funcionales:

- Donacion sin aplicaciones: `NOT_READY`, sin aplicaciones ni evidencias.
- Donacion parcial con aplicacion y evidencia pendiente: `PARTIAL`, saldo pendiente y faltante documental.
- Donacion 100% aplicada con evidencia minima activa: `READY`, saldo cero y evidencia listada con descarga protegida.

## Fuera de alcance

- Exportacion PDF/CSV.
- Reporte oficial firmado.
- Folio o versionamiento de reporte.
- Aprobacion documental humana.
- Checklist legal/documental avanzado.
- Validacion legal, fiscal o contable.
- Cambios de permisos, schema o migraciones.
