# Document Management Track - Contextual Remediation Implementation Note

## Objetivo
Permitir que faltantes documentales minimos se resuelvan desde el contexto de la entidad o desde la navegacion de pendientes, reutilizando `StoredDocument`, reglas documentales, uploads endurecidos y permisos por modulo.

## Implementado
- `MarketTenant` ahora puede cargar/reemplazar su cédula desde `POST /api/markets/tenants/{tenantId}/cedula`.
- El endpoint de Mercados valida archivo con la politica documental existente y actualiza la referencia operativa del locatario; si ya existe una cédula transversal, la etapa posterior de reemplazo conserva trazabilidad archivando el documento previo y creando un vigente enlazado.
- `StoredDocumentSupport.FindStoredDocumentAsync` selecciona primero documentos `ACTIVE` y luego el mas reciente, para convivir con documentos archivados existentes sin romper descarga.
- `related-documents-panel` agrega upload contextual cuando el requisito esta incompleto y el usuario tiene permiso de escritura del modulo.
- Donatarias y Federacion reutilizan sus endpoints existentes de evidencia por aplicacion desde el mismo panel.
- `/documents` conserva la cola de pendientes y muestra navegacion de remediacion hacia el origen cuando existe `routeHint`.

## Entidades cubiertas
- `MARKETS` / `MarketTenant`: requiere `CERTIFICATE`; se remedia cargando cédula/certificado.
- `DONATARIAS` / `DonationApplication`: requiere evidencia; se remedia cargando evidencia con tipo de evidencia.
- `FEDERATION` / `FederationDonationApplication`: requiere evidencia; se remedia cargando evidencia con tipo de evidencia.

## Decisiones
- No se creo un endpoint generico de upload documental transversal.
- La remediacion reutiliza los uploads de negocio existentes para conservar validaciones, clasificacion, auditoria y permisos ya aprobados.
- Solo se agrego un endpoint puente para Mercados porque antes la cédula solo podia cargarse al crear locatario.
- La cédula de Mercados no abre versionado completo; cuando hay sustitucion, queda una relacion minima entre documento previo y vigente mediante la trazabilidad de reemplazo documental.
- La accion solo se muestra a usuarios con permiso de escritura del modulo; la seguridad real queda en backend.
- Esta subetapa no agrego migracion; la trazabilidad posterior de reemplazo si agrega campos especificos en `StoredDocument`.

## Validacion local
Comandos base:
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
npm run build --prefix src/frontend
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter "Contextual_market_tenant_certificate_remediation_completes_requirement_and_requires_write_permission"
```

Validacion runtime sugerida:
1. Levantar backend local en `Development` con SQL Server local y migraciones aplicadas.
2. Crear o usar un `MarketTenant`, una `DonationApplication` y una `FederationDonationApplication` incompletas.
3. Consultar `GET /api/documents/requirements/by-entity?...` y confirmar `INCOMPLETE`.
4. Remediar:
   - Mercados: `POST /api/markets/tenants/{tenantId}/cedula` con `certificateFile`.
   - Donatarias: `POST /api/donations/applications/{applicationId}/evidences`.
   - Federacion: `POST /api/federation/applications/{applicationId}/evidences`.
5. Volver a consultar requisitos, completitud y pending queue; el faltante debe desaparecer cuando era el unico requisito pendiente.
6. Confirmar `403` para `READONLY` en uploads/remediacion.

## Fuera de alcance
- Workflow documental complejo.
- Bandeja de tareas o aprobaciones.
- Versionado documental.
- Borrado fisico o limpieza automatica de archivos reemplazados.
- OCR, clasificacion automatica avanzada, compliance legal y storage externo.
