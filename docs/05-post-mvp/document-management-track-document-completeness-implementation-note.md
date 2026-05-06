# Track 3 - Document Completeness Implementation Note

## Que se implemento
- Se agrego una evaluacion minima de completitud documental calculada sobre `StoredDocument`.
- Se agrego `GET /api/documents/completeness/by-entity` para evaluar una entidad puntual.
- Se agrego `GET /api/documents/pending` para listar entidades incompletas filtradas por permisos de lectura de modulo.
- La UI `/documents` muestra una seccion simple de pendientes documentales.
- El panel reusable de documentos relacionados muestra estado completo/incompleto en Mercados, Donatarias y Federacion.

## Reglas minimas definidas
- `MarketTenant` esta completo si tiene al menos un `StoredDocument` activo asociado con:
  - `ModuleCode = MARKETS`
  - `EntityType = MARKET_TENANT`
  - `DocumentAreaCode = MARKETS_TENANT_CERTIFICATES`
  - `DocumentClassCode = CERTIFICATE`
- `DonationApplication` esta completa si tiene al menos una evidencia con `StoredDocument` activo en `DONATIONS_APPLICATION_EVIDENCES`.
- `FederationDonationApplication` esta completa si tiene al menos una evidencia con `StoredDocument` activo en `FEDERATION_APPLICATION_EVIDENCES`.
- Documentos `ARCHIVED` no cuentan como cobertura de completitud en esta etapa.

## Entidades cubiertas
- `MarketTenant`
- `DonationApplication`
- `FederationDonationApplication`

## Decisiones tomadas
- No se agrego migracion ni metadata persistida nueva; la completitud se calcula al consultar.
- La completitud es una senal operativa, no una declaracion de cumplimiento legal.
- La cola de pendientes usa permisos por modulo ya existentes y no abre permisos por documento individual.
- No se auditan lecturas de completitud porque no hay una accion de negocio nueva ni cambio de estado.

## Como validarlo localmente
1. Levantar el backend local y autenticar un usuario con permisos de lectura documental.
2. Crear o usar un locatario con cedula y otro sin `StoredDocument` activo de tipo `CERTIFICATE`.
3. Consultar:
   ```bash
   curl -s "http://127.0.0.1:5080/api/documents/completeness/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId=${TENANT_ID}" -H "Authorization: Bearer ${TOKEN}"
   curl -s "http://127.0.0.1:5080/api/documents/pending?moduleCode=MARKETS&take=20" -H "Authorization: Bearer ${TOKEN}"
   ```
4. Repetir para:
   ```bash
   curl -s "http://127.0.0.1:5080/api/documents/completeness/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId=${DONATION_APPLICATION_ID}" -H "Authorization: Bearer ${TOKEN}"
   curl -s "http://127.0.0.1:5080/api/documents/completeness/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId=${FEDERATION_APPLICATION_ID}" -H "Authorization: Bearer ${TOKEN}"
   ```
5. En frontend, revisar `/documents` para pendientes y las secciones de documentos relacionados en Mercados, Donatarias y Federacion.

## Que quedo fuera
- Cumplimiento documental avanzado.
- Reglas legales o matrices documentales finas.
- OCR, clasificacion automatica o validacion semantica del contenido.
- Versionado, backup real, storage externo, borrado automatico o plataforma documental completa.
