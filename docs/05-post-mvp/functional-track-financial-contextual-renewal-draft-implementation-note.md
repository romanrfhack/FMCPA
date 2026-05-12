# Functional Track - Financial Contextual Renewal Draft Implementation Note

## Contexto
- Fecha: 2026-05-11
- Track: `Track 5: Evolucion funcional posterior`
- Subetapa: renovacion contextual prellenada para Financieras.
- Alcance: preparar una renovacion desde `RENEW_LAST_PERMIT` sin workflow complejo, sin aprobaciones, sin catalogo maestro, sin versionado contractual completo y sin entidad draft persistida.

## Que se implemento
- Se agrego `GET /api/financials/{permitId}/renewal-draft` como consulta de solo lectura.
- La respuesta incluye el permiso fuente, el permiso vigente objetivo de renovacion, la raiz de cadena, datos operativos prellenados, periodo anterior, fechas nuevas sugeridas, estatus conservado y campos que el usuario debe confirmar.
- Angular agrega la accion `Preparar renovacion` cuando `GET /api/financials/current-permit` devuelve `suggestionCode=RENEW_LAST_PERMIT`.
- La UI carga el draft, prellena el formulario simple de renovacion y confirma usando el endpoint real existente `POST /api/financials/{permitId}/renew`.
- La regresion de autorizacion cubre el nuevo endpoint como lectura para `ADMIN`, `OPERATOR` y `READONLY`.

## Como se prepara la renovacion
- La resolucion contextual sigue siendo la fuente que decide si aplica `RENEW_LAST_PERMIT`; no se duplican las reglas de busqueda contextual.
- El frontend toma `lastKnownPermit.permitId` de la resolucion y consulta `/renewal-draft`.
- Si el permiso fuente ya es historico, el draft prellena desde ese ultimo permiso aplicable, pero devuelve `renewalTargetPermitId` con el permiso vigente de la cadena.
- La renovacion real se confirma contra `renewalTargetPermitId`, manteniendo la regla existente de renovar solo sobre el vigente de la cadena.
- Las fechas sugeridas se calculan desde la mayor `ValidTo` conocida entre el permiso fuente y el vigente de la cadena, para evitar sugerir una renovacion cronologicamente anterior a la cadena actual.

## Que se prellena
- `financialName`
- `institutionOrDependency`
- `placeOrStand`
- `schedule`
- `negotiatedTerms`
- `notes` cuando existe

## Que debe confirmarse o editarse
- `newStartDate`
- `newEndDate`
- `status` se muestra como estatus conservado del permiso vigente objetivo; no se abre edicion de estatus en esta subetapa.
- El usuario puede ajustar lugar/stand, horario, terminos negociados y observaciones antes de confirmar.

## Validacion local
Comandos ejecutados:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter FinancialsPermitRenewalTests

cd src/frontend
npm run build
```

Resultado:
- `dotnet restore`: exitoso.
- `dotnet build`: exitoso, `0 Warning(s)`, `0 Error(s)`.
- `FinancialsPermitRenewalTests`: exitoso, `4/4`.
- `npm run build`: exitoso.

## Validacion manual por HTTP/UI
Stack local usado para la validacion runtime:

```bash
FMCPA_SQL_PORT=14346 \
FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-context-renewal \
FMCPA_DB_NAME=FMCPA_ContextRenewalDraft_20260511 \
FMCPA_API_PORT=5112 \
FMCPA_WEB_PORT=4216 \
FMCPA_AUTH_BOOTSTRAP_PASSWORD=AdminLocal123 \
FMCPA_AUTH_OPERATOR_PASSWORD=OperatorLocal123 \
FMCPA_AUTH_READONLY_PASSWORD=ReadonlyLocal123 \
./scripts/local/dev-up.sh
```

La API se valido localmente en `http://127.0.0.1:5112` contra SQL Server local `fmcpa-sql-context-renewal`, base `FMCPA_ContextRenewalDraft_20260511`. Para la prueba UI se sirvio Angular en `http://127.0.0.1:4200` con proxy local hacia la API `5112`.

Resultado HTTP real:
- Se creo un permiso base de Financieras con stand original.
- Se renovo hacia un stand temporal para dejar el stand original como antecedente historico.
- `GET /api/financials/current-permit` con el stand original devolvio `suggestionCode=RENEW_LAST_PERMIT`.
- `GET /api/financials/{lastKnownPermit.permitId}/renewal-draft` devolvio `sourcePermitId` del antecedente, `renewalTargetPermitId` del vigente de la cadena, `newStartDate=2026-10-01`, `newEndDate=2026-10-31` y `fieldsToConfirm=["newStartDate","newEndDate","status"]`.
- `POST /api/financials/{renewalTargetPermitId}/renew` creo la secuencia `2`.
- La cadena quedo con secuencias `0, 1, 2` y una resolucion posterior del mismo contexto devolvio `USE_CURRENT_PERMIT` apuntando al nuevo vigente.

Resultado UI real:
- La prueba Playwright temporal abrio `/financials`, provoco `RENEW_LAST_PERMIT`, mostro `Preparar renovacion`, cargo el formulario con `Stand UI Contextual`, `08:00-13:00`, `2026-10-01`, `2026-10-31` y `Terminos UI originales`, y confirmo `Crear renovacion`.
- Resultado: `1 passed`.
- Evidencia visual temporal: `/tmp/fmcpa-renewal-draft-ui.png`.

Flujo recomendado para repetir:

1. Levantar base local y API con la convencion local.
2. Crear un permiso base de Financieras para una financiera/dependencia/stand.
3. Renovarlo hacia otro stand para dejar el stand original como antecedente historico.
4. Consultar `GET /api/financials/current-permit` con el stand original y confirmar `suggestionCode=RENEW_LAST_PERMIT`.
5. Consultar `GET /api/financials/{lastKnownPermit.permitId}/renewal-draft`.
6. Confirmar que el draft prellena lugar/stand, horario, terminos y observaciones del antecedente, pero que `renewalTargetPermitId` apunta al vigente de la cadena.
7. Ejecutar `POST /api/financials/{renewalTargetPermitId}/renew` con las fechas y datos del draft.
8. Consultar `GET /api/financials/{sourcePermitId}/renewal-chain` y confirmar que la cadena queda con nueva secuencia vigente.
9. En Angular, entrar a Financieras, provocar la misma resolucion contextual y usar `Preparar renovacion`; validar que el formulario se muestra prellenado y que `Crear renovacion` usa el endpoint real.

## Fuera de alcance
- No se crea entidad draft persistida.
- No se agrega migracion.
- No se audita la preparacion de lectura.
- No se abre catalogo maestro de financieras, dependencias o stands.
- No se agrega workflow, aprobacion multinivel, task inbox ni versionado contractual completo.
- No se toca produccion ni CI/CD.
