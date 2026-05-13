# Donatarias Transparencia - Nota de implementacion Fase 1

Fecha: 2026-05-12

## Alcance implementado

La Fase 1 reorganizo la pantalla `/donatarias` para presentar primero la transparencia de la donacion seleccionada y mantener la captura como accion contextual. El cambio se hizo solo en frontend, sin modificar backend, contratos API, schema, permisos ni migraciones.

Se implemento:

- Navegacion por tabs: Resumen, Donaciones, Aplicaciones / distribucion, Evidencias y Reporte de transparencia.
- Encabezado de transparencia para la donacion seleccionada con donante, referencia, fecha, tipo, total recibido, total aplicado, saldo pendiente, porcentaje aplicado y tres estatus separados.
- KPIs principales en Resumen: total recibido, total aplicado, saldo pendiente, porcentaje aplicado, numero de aplicaciones, evidencias totales, aplicaciones sin evidencia y estado operativo.
- Semaforos separados:
  - Financiero: Sin aplicar, Parcialmente aplicada, Aplicada.
  - Documental: Sin aplicaciones que comprobar, Evidencia pendiente, Comprobacion minima completa.
  - Operativo: Abierta, Cerrada.
- Tab Donaciones con listado, filtros, alertas, seleccion y registro de donacion conservados.
- Tab Aplicaciones / distribucion con aplicaciones de la donacion seleccionada, porcentaje del total recibido, evidencia asociada y acciones actuales.
- Tab Evidencias con evidencia agrupada por aplicacion cuando la informacion esta disponible, carga y descarga conservadas, y nota de alcance documental.
- Vista preliminar de Reporte de transparencia con datos actuales, corte, aplicaciones, evidencias y faltantes basicos.
- Seleccion inicial por query params `donationId` y `applicationId`.
- Panel de cierre formal con motivo y advertencias sobre saldo pendiente, evidencia pendiente y alcance operativo del cierre.

## Datos actuales reutilizados

La implementacion reutiliza los DTOs, modelos y servicios existentes:

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

Para "aplicaciones sin evidencia" se usa la validacion minima `evidenceCount === 0` por aplicacion. Esta regla solo indica presencia documental minima en el sistema; no representa revision legal, fiscal o contable.

## Placeholder o vista preliminar

El tab Reporte de transparencia queda como vista preliminar. Usa datos actuales disponibles en frontend y muestra explicitamente que la exportacion formal se implementara en una fase posterior.

No se implemento:

- Exportacion PDF.
- Exportacion CSV.
- Endpoint nuevo de reporte.
- Snapshot formal de reporte.
- Versionamiento, folio o firma de reporte.
- Validacion legal o contable de suficiencia documental.

## Fuera de alcance

Quedo fuera de esta fase:

- Cambios de backend.
- Nuevos contratos API.
- Migraciones.
- Cambios de permisos.
- Cambios al significado de estatus existentes.
- Reporte formal para donante.
- Vista imprimible.
- Checklist documental avanzado.
- Nuevos modelos para revision, aprobacion o cierre documental.

## Como validar localmente

Validacion automatizada:

```bash
cd src/frontend
npm run build
npm test -- --watch=false
```

Validacion manual sugerida:

1. Abrir `/donatarias`.
2. Cambiar entre tabs: Resumen, Donaciones, Aplicaciones / distribucion, Evidencias y Reporte de transparencia.
3. Seleccionar una donacion y confirmar que se muestran total recibido, total aplicado, saldo pendiente y porcentaje aplicado sin desplazamiento largo.
4. Confirmar que los semaforos financiero, documental y operativo aparecen separados.
5. Confirmar que el tab Aplicaciones muestra porcentaje del total recibido por aplicacion.
6. Confirmar que las aplicaciones sin evidencia quedan visibles.
7. Confirmar que carga y descarga de evidencia siguen disponibles.
8. Probar `/donatarias?donationId=<id>&applicationId=<id>` con ids existentes y confirmar que se selecciona el contexto inicial.
9. Confirmar que el Reporte de transparencia aparece como vista preliminar y no ofrece exportacion formal.
10. Confirmar que el panel de cierre formal advierte sobre saldo pendiente, evidencia pendiente y alcance operativo.

## Validacion visual/operativa cerrada

Fecha de cierre de validacion: 2026-05-12

Datos de prueba usados:

- Stack local aislado: base `FMCPA_DonatariasValidation`, API `5098`, frontend `4218`.
- Donante: `Asociacion Aliada Transparencia Fase 1`.
- Referencia: `DON-TR-F1-20260513003732`.
- Fecha de donacion: `2026-05-12`.
- Tipo: `Transferencia bancaria`.
- Total recibido: `100000`.
- Total aplicado: `55000`.
- Saldo pendiente: `45000`.
- Porcentaje aplicado: `55`.
- Estatus financiero backend: `PARTIALLY_APPLIED` / `Aplicacion parcial`.
- Aplicaciones:
  - `Programa Becas Salud Comunitaria`, monto `30000`, con evidencia PDF.
  - `Entrega Insumos Clinica Movil`, monto `25000`, sin evidencia.
- Evidencia descargada correctamente como `application/pdf`.

Validacion ejecutada:

- `npm run build`: exitoso; conserva warning no bloqueante de budget CSS del componente Donatarias.
- `npm test -- --watch=false`: exitoso, `6` archivos y `22` tests.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0` warnings, `0` errores.
- Playwright local temporal: exitoso, valida login por API/proxy, `/donatarias?donationId=...&applicationId=...`, tabs, KPIs, semaforos, aplicaciones, evidencias, descarga, reporte preliminar y panel de cierre formal.
- `git diff --check`: exitoso.

Bugs minimos corregidos durante la validacion:

- KPI `Aplicaciones` se renombro a `Numero de aplicaciones`.
- Columnas del tab Aplicaciones se alinearon al alcance: `% del total recibido`, `Estatus de aplicacion`, `Estado documental simple` y `Detalle de comprobacion`.
- El reporte preliminar agrego seccion explicita `Faltantes basicos`.
- El panel de cierre formal aclaro que el cierre operativo no equivale a comprobacion legal o contable completa.

## Riesgos y pendientes

- El semaforo documental es deliberadamente minimo porque se basa en presencia de evidencia registrada, no en suficiencia legal o contable.
- La vista preliminar de reporte depende de la informacion cargada por los DTOs actuales; un reporte formal deberia consolidarse con contrato o endpoint dedicado en una fase posterior.
- La reorganizacion aumento el tamano de estilos del componente; conviene extraer o compactar estilos si el presupuesto de CSS se vuelve bloqueante.
- Para Fase 2 y Fase 3 se recomienda validar con negocio si los textos de semaforos y faltantes documentales son suficientes para presentacion a donantes.
