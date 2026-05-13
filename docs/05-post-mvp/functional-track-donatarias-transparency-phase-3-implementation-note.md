# Donatarias Transparencia - Fase 3 implementation note

Fecha: 2026-05-12

## Objetivo

Fortalecer la transparencia documental de Donatarias con un semaforo agregado por donacion, calculado desde la capa documental real del sistema, sin abrir checklist legal avanzado, aprobacion humana, schema nuevo, migraciones ni permisos nuevos.

## Que se implemento

- Endpoint de lectura `GET /api/donations/{donationId}/documentary-status`.
- Contratos `DonationDocumentaryStatusResponse` y `DonationApplicationDocumentaryStatusResponse`.
- Pruebas backend para donacion sin aplicaciones, aplicacion sin evidencia, evidencia archivada que no cuenta como activa, donacion completa, rechazo anonimo y cobertura de autorizacion por `DONATIONS_READ`.
- Integracion frontend en `/donatarias`:
  - Resumen con estado documental agregado, aplicaciones completas/pendientes y documentos activos.
  - Aplicaciones / distribucion con evidencia basada en documentos activos y faltantes documentales.
  - Evidencias con bloque agregado de estado documental.
  - Reporte preliminar con seccion "Estado documental" y faltantes por aplicacion.
  - Panel de cierre formal con advertencia de evidencia minima pendiente.
- Servicio frontend `getDonationDocumentaryStatus`.
- Modelos frontend para el agregado documental.

## Como se calcula el semaforo documental

El endpoint reutiliza `DocumentRuleRegistry` para la regla `DONATARIAS / DONATION_APPLICATION`.

Para cada aplicacion:

- Busca evidencias operativas `DonationApplicationEvidence`.
- Cuenta documentos `StoredDocument` asociados a esas evidencias.
- Solo cuentan documentos:
  - del modulo `DONATARIAS`,
  - del area `DonationsApplicationEvidences`,
  - con entidad cubierta `DONATION_APPLICATION_EVIDENCE`,
  - con clase requerida por la regla documental,
  - con `StatusCode = ACTIVE`,
  - sin `SupersededByDocumentId`.

Estados agregados:

- `NO_APPLICATIONS`: no hay aplicaciones que comprobar.
- `EVIDENCE_PENDING`: al menos una aplicacion no tiene evidencia minima activa.
- `MINIMUM_EVIDENCE_COMPLETE`: todas las aplicaciones tienen evidencia minima activa.

## Limites de la validacion documental

- La senal valida presencia documental minima, no suficiencia legal.
- No valida contenido, calidad, folio, firmas, facturas ni comprobacion contable.
- No abre aprobacion humana ni checklist por tipo de evidencia.
- Evidencia heredada fuera de `StoredDocument` puede quedar fuera del conteo hasta una regularizacion documental aprobada.
- `CLOSED` sigue siendo cierre operativo y no implica aplicacion financiera ni comprobacion documental completa.

## Como validar localmente

Validacion minima:

- `dotnet restore src/backend/FMCPA.Backend.sln`
- `dotnet build src/backend/FMCPA.Backend.sln`
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter "FullyQualifiedName~DonationDocumentaryStatusTests|FullyQualifiedName~AuthorizationRegressionTests"`
- `npm run build` en `src/frontend`
- `npm test -- --watch=false` en `src/frontend`
- Validacion UI con tres donaciones:
  - sin aplicaciones,
  - evidencia parcial,
  - evidencia completa.
- `git diff --check`

## Validacion ejecutada

- `dotnet restore src/backend/FMCPA.Backend.sln`: OK.
- `dotnet build src/backend/FMCPA.Backend.sln`: OK, 0 warnings, 0 errors.
- `dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter "FullyQualifiedName~DonationDocumentaryStatusTests|FullyQualifiedName~AuthorizationRegressionTests"`: OK, `258/258`.
- `npm run build`: OK, con warning preexistente de budget CSS del componente Donatarias.
- `npm test -- --watch=false`: OK, `22/22`.
- Validacion local con stack aislado:
  - SQL container `fmcpa-sql-donatarias-phase3`.
  - Base `FMCPA_DonatariasPhase3Validation`.
  - API `http://127.0.0.1:5109`.
  - Frontend `http://127.0.0.1:4221`.
- Datos de prueba:
  - Donacion sin aplicaciones: `NO_APPLICATIONS`.
  - Donacion con una aplicacion con evidencia activa y una sin evidencia: `EVIDENCE_PENDING`.
  - Donacion con todas las aplicaciones con evidencia activa: `MINIMUM_EVIDENCE_COMPLETE`.
- Playwright local: OK para tabs, encabezado, query params `donationId/applicationId`, semaforo agregado, aplicaciones completas/pendientes, Evidencias, Reporte preliminar y advertencia de cierre formal.
- `git diff --check`: OK.
- `git diff -- src/backend/src/FMCPA.Infrastructure/Persistence/Migrations`: sin cambios.

## Quedo fuera

- Migraciones y cambios de schema.
- Permisos nuevos.
- Checklist legal/documental avanzado.
- Aprobacion documental humana.
- Validacion de suficiencia juridica o contable.
- Reporte formal exportable.
- PDF, CSV o vista imprimible nueva.
- Timeline funcional consolidado.
- Regularizacion documental heredada fuera de `StoredDocument`.
