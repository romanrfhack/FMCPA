# Functional Track - Financial Current Permit Resolution Implementation Note

## Que se implemento
- Se agrego `GET /api/financials/current-permit` para resolver el permiso vigente de Financieras por contexto operativo.
- La consulta acepta `financialName`, `institutionOrDependency` y `placeOrStand`.
- En la subetapa posterior de captura contextual, la misma consulta se amplio para distinguir contexto historico o terminal antes de preparar el alta de credito.
- La respuesta exitosa incluye:
  - `permitId`
  - `currentRootPermitId`
  - `renewalSequence`
  - financiera, dependencia/institucion y lugar/stand
  - vigencia `validFrom` / `validTo`
  - horario
  - estatus
  - `daysUntilExpiration`
  - `alertState`
  - resumen operativo
- La UI de Financieras agrega accion minima `Buscar vigente` en el alta de oficio, reutilizando los tres campos operativos ya capturados.
- Si se encuentra vigente, la UI permite ir al permiso encontrado sin abrir wizard ni flujo nuevo.
- No se creo migracion ni auditoria de consulta.

## Como se resuelve el vigente
La regla exacta de resolucion es:

1. Buscar permisos con `IsCurrentVersion = true`.
2. Excluir permisos terminales segun la misma regla de estatus ya usada por operaciones bloqueadas.
3. Comparar la combinacion operativa normalizada:
   - `financialName`
   - `institutionOrDependency`
   - `placeOrStand`
4. Si no hay coincidencia vigente operable pero el contexto apunta a un permiso terminal vigente, responder `409` con `reasonCode = FINANCIAL_PERMIT_TERMINAL`.
5. Si no hay coincidencia vigente operable pero el contexto apunta a un permiso historico, responder `409` con `reasonCode = FINANCIAL_PERMIT_NOT_CURRENT` y `currentPermitId` cuando exista vigente de la cadena.
6. Si no hay ningun permiso relacionado, responder `404` con `reasonCode = FINANCIAL_PERMIT_CURRENT_NOT_FOUND`.
7. Si hay una coincidencia vigente operable, responder `200` con el contexto minimo del permiso vigente.
8. Si hay mas de una coincidencia vigente operable, responder `409` con `reasonCode = FINANCIAL_PERMIT_CURRENT_AMBIGUOUS` y lista minima de coincidencias.

La ambiguedad no deberia generarse por API despues de la unicidad operativa minima, pero queda cubierta para datos heredados, cargas directas o carreras residuales.

## Normalizacion operativa
La comparacion reutiliza la misma normalizacion de la validacion de conflicto activo:

- `Trim`.
- Colapso de espacios internos.
- Comparacion case-insensitive mediante mayusculas invariantes.
- Remocion basica de diacriticos.

No se interpretan sinonimos, alias, abreviaturas ni cambios semanticos. Eso queda fuera hasta que exista un catalogo maestro aprobado.

## Como validar localmente
1. Ejecutar build backend:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
```

2. Ejecutar prueba focal:

```bash
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --filter "Financial_current_permit_resolution_uses_operational_key_and_current_chain"
```

3. Ejecutar build frontend:

```bash
cd src/frontend
npm run build
```

4. Con backend local levantado, validar:

```bash
curl -i "http://127.0.0.1:5080/api/financials/current-permit?financialName=Financiera%20Local&institutionOrDependency=Direccion%20local&placeOrStand=Stand%202" -H "Authorization: Bearer ${ADMIN_TOKEN}"
curl -i "http://127.0.0.1:5080/api/financials/current-permit?financialName=Financiera%20Local&institutionOrDependency=Direccion%20local&placeOrStand=Stand%20inexistente" -H "Authorization: Bearer ${ADMIN_TOKEN}"
```

El primer caso debe devolver `200` con `permitId` vigente. El segundo debe devolver `404 FINANCIAL_PERMIT_CURRENT_NOT_FOUND`. Si el contexto apunta a un permiso historico o terminal, debe devolver `409` con `FINANCIAL_PERMIT_NOT_CURRENT` o `FINANCIAL_PERMIT_TERMINAL`.

5. En la UI `/financials`, capturar financiera, dependencia/institucion y lugar/stand en el alta de oficio y usar `Buscar vigente`.

## Que quedo fuera
- Catalogo maestro de financieras, dependencias o stands.
- Normalizacion por alias, sinonimos o equivalencias semanticas.
- Workflow de busqueda/correccion.
- Workflow complejo o endpoint puente de captura directa desde contexto.
- Versionado contractual completo.
- Migracion o constraint SQL.
- Auditoria de consultas de resolucion.
