# Donatarias Transparencia - Nota de implementacion Fase 2

Fecha: 2026-05-12

## Alcance implementado

La Fase 2 refuerza la lectura financiera de `/donatarias` usando datos actuales ya expuestos por los DTOs existentes. El cambio se hizo solo en frontend, sin modificar backend, contratos API, schema, permisos ni migraciones.

Se implemento:

- KPIs de lista cargada/filtrada: donaciones visibles, total recibido visible, total aplicado visible, saldo pendiente visible, porcentaje aplicado visible, abiertas, cerradas y con saldo pendiente.
- Copy explicito indicando que esos KPIs son de la lista cargada/filtrada y no un resumen global server-side.
- Saldo pendiente destacado en el encabezado y en el resumen de la donacion seleccionada.
- Mensajes financieros:
  - `Aun existe recurso pendiente de aplicar.`
  - `El recurso registrado ya fue aplicado financieramente.`
  - `La donacion fue cerrada operativamente con saldo pendiente.`
- Diferenciacion reforzada entre `Aplicada financieramente` y `Cerrada` operativamente.
- Orden visual de aplicaciones por fecha ascendente, fecha descendente, monto mayor y evidencia pendiente.
- Tabla de aplicaciones con saldo restante despues de cada aplicacion, porcentaje del total recibido, indicador de peso relativo y estado documental simple.
- Indicador de aplicacion significativa cuando representa al menos 25% del total recibido.
- Criterio operativo preliminar `Lista para presentar: Si/Parcial/No`.
- Reporte preliminar enriquecido con distribucion financiera, saldo pendiente, lista para presentar y faltantes basicos por aplicacion.

## Calculos en frontend

Los KPIs de lista visible se calculan sobre `donations()`:

- `totalReceived = sum(baseAmount)`
- `totalApplied = sum(appliedAmountTotal)`
- `pendingBalance = sum(remainingAmount)`
- `appliedPercentage = totalApplied / totalReceived`
- `openCount = statusIsClosed === false`
- `closedCount = statusIsClosed === true`
- `pendingBalanceCount = remainingAmount > 0`

La distribucion por aplicacion se calcula sobre `selectedDonation().applications`:

- `% del total recibido = appliedAmount / baseAmount`
- `saldo restante = baseAmount - suma(appliedAmount)` hasta la aplicacion actual en el orden visual seleccionado.
- `peso relativo = Parte significativa` cuando la aplicacion representa al menos 25% del total recibido.
- `faltantes basicos` usa `evidenceCount <= 0` y ausencia de `verificationDetails`.

El criterio `Lista para presentar` es operativo preliminar:

- `Si`: saldo pendiente `0` y todas las aplicaciones tienen evidencia minima.
- `Parcial`: tiene aplicaciones y al menos una evidencia, pero conserva saldo pendiente o faltantes.
- `No`: no tiene aplicaciones o no tiene evidencia.

## Limites de los KPIs calculados en cliente

- Los KPIs de lista no son globales si la API entrega una lista filtrada o parcial.
- No reemplazan un endpoint agregado server-side ni un reporte oficial.
- El saldo restante por aplicacion depende del orden visual seleccionado; no se persiste como asiento contable.
- `evidenceCount` y presencia de detalle son senales minimas operativas, no suficiencia legal o contable.
- `CLOSED` no implica aplicacion financiera completa; se mantiene como estatus operativo separado.

## Datos actuales reutilizados

- `DonationSummary`
- `DonationDetail`
- `DonationApplication`
- `DonationApplicationEvidence`
- `DonationsService`
- `baseAmount`
- `appliedAmountTotal`
- `remainingAmount`
- `appliedPercentage`
- `applicationCount`
- `evidenceCount`
- `applications`
- `statusIsClosed`
- `verificationDetails`

## Fuera de alcance

No se implemento:

- Backend nuevo.
- Endpoint agregado de resumen global.
- Endpoint de reporte de transparencia.
- Migraciones.
- Cambios de permisos.
- Exportacion PDF o CSV.
- Vista imprimible formal.
- Snapshot, folio o versionamiento de reporte.
- Validacion legal, fiscal o contable de evidencias.
- Checklist documental avanzado.

## Como validar localmente

Validacion automatizada:

```bash
cd src/frontend
npm run build
npm test -- --watch=false
cd ../..
dotnet build src/backend/FMCPA.Backend.sln --no-restore
git diff -- src/backend
git diff --check
```

Validacion manual sugerida:

1. Abrir `/donatarias`.
2. Confirmar que aparece el bloque `Lista cargada / filtrada`.
3. Aplicar filtros en el tab `Donaciones` y confirmar que los KPIs visibles cambian con la lista cargada.
4. Seleccionar una donacion parcialmente aplicada y confirmar saldo pendiente destacado.
5. Seleccionar una donacion 100% aplicada no cerrada y confirmar que se muestra aplicada financieramente, no cerrada.
6. Seleccionar una donacion cerrada con saldo pendiente, si existe, y confirmar la advertencia operativa.
7. Abrir `Aplicaciones / distribucion` y probar orden por fecha, monto y evidencia pendiente.
8. Confirmar que cada aplicacion muestra porcentaje del total recibido y saldo restante.
9. Confirmar que una aplicacion sin evidencia queda marcada como evidencia pendiente.
10. Confirmar que `Lista para presentar` aparece como criterio operativo preliminar.
11. Abrir `Reporte de transparencia` y confirmar distribucion, saldo pendiente, faltantes y nota de alcance.
12. Probar `/donatarias?donationId=<id>&applicationId=<id>` con ids existentes.

## Riesgos y pendientes

- El componente Donatarias conserva warning de presupuesto CSS en build; no bloquea, pero conviene extraer estilos si el presupuesto se vuelve estricto.
- Si negocio requiere totales globales auditables, se recomienda endpoint agregado server-side en una fase posterior.
- Si el orden de aplicacion debe ser juridico/contable, se requiere regla formal; por ahora el saldo acumulado es solo lectura operativa por orden visual.
- El reporte sigue siendo preliminar y no debe compartirse como informe formal exportable.

## Validacion ejecutada

Validacion tecnica:

- `npm run build`: exitoso; conserva warning no bloqueante de budget CSS del componente Donatarias.
- `npm test -- --watch=false`: exitoso, `6` archivos y `22` tests.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings, `0` errores.
- `git diff -- src/backend`: sin cambios.
- `git diff --check`: exitoso.

Validacion visual/operativa:

- Stack local aislado: base `FMCPA_DonatariasPhase2Validation`, API `5108`, frontend `4220`.
- Donacion parcial: `Asociacion Aliada Transparencia Fase 2 Parcial`, total `100000`, aplicado `55000`, saldo `45000`, dos aplicaciones, una con evidencia y una sin evidencia.
- Donacion aplicada al 100% no cerrada: `Fundacion Transparencia Aplicada Fase 2`, total `50000`, aplicado `50000`, saldo `0`, evidencia minima registrada.
- Donacion cerrada con saldo pendiente: `Asociacion Cierre Operativo con Saldo Fase 2`, total `80000`, aplicado `20000`, saldo `60000`, cierre operativo registrado.
- Playwright local valido tabs, seleccion por `donationId`/`applicationId`, KPIs de lista, saldo pendiente, orden por evidencia pendiente, distribucion, reporte preliminar, `Lista para presentar` y separacion entre aplicada financieramente y cerrada operativamente.
