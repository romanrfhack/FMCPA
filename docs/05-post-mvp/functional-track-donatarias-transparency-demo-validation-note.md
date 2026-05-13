# Donatarias Transparencia - Validacion demo integral

Fecha: 2026-05-13

## Objetivo

Validar el flujo completo de Donatarias Transparencia con datos realistas de prueba, incluyendo reporte imprimible, sin abrir funcionalidades nuevas, sin modificar backend, sin crear migraciones y sin cambiar permisos.

## Entorno usado

- Base local: `FMCPA_DonatariasDemoValidation_20260513`.
- SQL local: contenedor `fmcpa-sql-donatarias-demo` en `127.0.0.1:14361`.
- API local: `http://127.0.0.1:5115`.
- Frontend local: `http://127.0.0.1:4224`.
- Usuario UI/API: `ADMIN` local para validar lectura, descarga y panel de cierre formal.

## Datos usados

- Donante: `Asociación Donante Demo`.
- Referencia: `DON-DEMO-TRANSP-2026-001`.
- Fecha de donacion: `2026-05-13`.
- Tipo: `Donación en efectivo`.
- Total recibido: `100000`.
- Total aplicado: `75000`.
- Saldo pendiente: `25000`.
- Porcentaje aplicado: `75%`.
- Notas: donacion destinada a programas comunitarios de alimentacion, salud preventiva y educacion basica.

Aplicaciones:

| Aplicacion | Monto | Evidencia |
| --- | ---: | --- |
| Comedor Comunitario San Miguel | 35000 | `comprobante-comedor-san-miguel.pdf` |
| Brigada Médica Colonia Esperanza | 25000 | `comprobante-brigada-medica.pdf` |
| Programa de Apoyo Escolar La Paz | 15000 | Sin evidencia para validar faltante |

Resultado calculado:

- Estado documental: `EVIDENCE_PENDING`.
- Aplicaciones con evidencia pendiente: `1`.
- Readiness del reporte: `PARTIAL`.

## Resultado por punto validado

| Punto | Resultado |
| --- | --- |
| `/donatarias` carga correctamente | Validado en UI real. |
| Tabs visibles y entendibles | Validado: `Resumen`, `Donaciones`, `Aplicaciones / distribucion`, `Evidencias`, `Reporte de transparencia`. |
| Resumen muestra recibido, aplicado, saldo, porcentaje y semaforos | Validado con 100000, 75000, 25000, 75%, financiero parcial, documental pendiente y operativo abierto. |
| Aplicaciones muestran distribucion del recurso | Validado con tres aplicaciones, montos, porcentajes, saldo restante y detalle de comprobacion. |
| Evidencias agrupadas y descargables | Validado en UI y por descarga protegida HTTP `200` de evidencia PDF. |
| Semaforo documental agregado pendiente | Validado con una aplicacion sin evidencia minima activa. |
| Reporte de transparencia completo | Validado con donante, referencia, tipo, totales, aplicaciones, evidencias, faltantes, readiness y notas de alcance. |
| Vista imprimible oculta controles | Validado con `window.print()` interceptado, `@media print`, ocultamiento de shell, tabs, formularios y botones. |
| Query params siguen funcionando | Validado con `donationId` y `donationId + applicationId`; el segundo abre el contexto de evidencias. |
| Cierre formal advierte pendientes | Validado: muestra saldo pendiente 25000, una evidencia minima pendiente y advertencia legal/contable. |

## Validacion tecnica

- `npm run build`: correcto. Mantiene warning conocido de presupuesto CSS del componente Donatarias.
- `npm test -- --watch=false`: correcto, `22/22`.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: correcto, `0` warnings, `0` errors.
- Playwright/script UI real: correcto, `1/1`.
- `git diff --check`: correcto.
- Migraciones: no hubo cambios en backend ni en carpeta de migraciones.

Playwright genero un PDF de navegador en `/tmp/fmcpa-playwright-demo/donatarias-transparencia-demo.pdf` como artefacto local de validacion. No se genero PDF server-side ni se agrego descarga/exportacion al producto.

## Bugs detectados o corregidos

- No se detectaron bugs funcionales del producto durante esta validacion.
- No se modifico backend.
- No se modifico schema.
- No se crearon migraciones.
- No se cambiaron permisos.
- Solo se ajusto el selector del script temporal de Playwright para distinguir el tab `Evidencias` del boton `Ver evidencias`; no es cambio de producto.

## Riesgos de presentacion

- La donacion demo queda intencionalmente en readiness `PARTIAL`; debe explicarse como caso util para demostrar advertencias de saldo y evidencia pendiente.
- El reporte imprimible es una vista operativa no oficial. No debe presentarse como reporte legal, fiscal o contable.
- La evidencia minima registrada acredita presencia documental operativa, no revision legal/contable.
- La impresion depende del motor de impresion del navegador; no hay plantilla PDF oficial ni folio emitido.
- El cierre formal advierte pendientes pero no fue confirmado durante la validacion para no cerrar la donacion demo.

## Estado final

Lista para demo controlada ante una asociacion donante, con la advertencia explicita de que el caso muestra un reporte parcial por saldo y evidencia pendiente.

Queda fuera:

- PDF server-side.
- CSV.
- Folio oficial.
- Firma.
- Snapshot/versionamiento de reporte.
- Aprobacion documental humana.
- Cambios de backend, permisos, schema o migraciones.
