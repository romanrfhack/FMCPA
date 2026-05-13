# Donatarias Transparencia Fase 5A - Vista imprimible no oficial

Fecha: 2026-05-13

## Objetivo

Agregar una vista imprimible del reporte de transparencia para donante dentro de `/donatarias`, usando los datos actuales del endpoint existente `GET /api/donations/{donationId}/transparency-report`, sin abrir generacion PDF server-side, CSV, folio oficial, firma, snapshot/versionamiento ni aprobacion documental humana.

## Implementado

- Boton `Imprimir reporte` dentro del tab `Reporte de transparencia`.
- Ejecucion simple con `window.print()`.
- Estado vacio con mensaje claro y boton de impresion deshabilitado cuando no hay donacion seleccionada.
- Encabezado del reporte con titulo `Reporte de transparencia de donación`, donante y fecha de corte/generacion.
- Nota de alcance visible en pantalla e impresion:
  - "Este reporte es una vista operativa de transparencia basada en la información registrada en el sistema. La evidencia mínima registrada no sustituye revisión legal, fiscal o contable."
- Readiness visible con codigo `READY`, `PARTIAL` o `NOT_READY` y texto operativo claro.
- CSS print acotado por `body.donatarias-print-active` para ocultar shell/controles solo cuando la impresion se dispara desde Donatarias.
- Reglas de impresion para que tablas, tarjetas y evidencias usen texto legible, contraste sobrio y `break-inside: avoid` donde aplica.

## Que se imprime

- Titulo del reporte.
- Donante, referencia, fecha de donacion, tipo de donacion y fecha de corte/generacion.
- Total recibido, total aplicado, saldo pendiente y porcentaje aplicado.
- Estado financiero, documental y operativo.
- Readiness `READY/PARTIAL/NOT_READY` con razones.
- Resumen documental agregado.
- Tabla de aplicaciones.
- Evidencias por aplicacion con tipo, archivo, descripcion y fecha de carga.
- Faltantes documentales basicos.
- Notas de alcance del reporte.

## Que se oculta en impresion

- Header y navegacion principal del shell.
- Tabs de `/donatarias`.
- Formularios de captura.
- Panel de cierre formal.
- Filtros, listas operativas y KPIs de lista visible que no pertenecen al reporte.
- Botones de captura, descarga y controles tecnicos.
- Mensajes de alerta transitorios de pantalla.

## Limites del reporte imprimible

- Es una vista operativa, no un reporte legal, fiscal, contable ni documental oficial.
- No genera PDF desde backend.
- No genera CSV.
- No crea folio oficial.
- No agrega firma.
- No persiste snapshot ni versionamiento del reporte.
- No abre aprobacion documental humana.
- No cambia permisos: sigue dependiendo de `DONATIONS_READ` por la ruta y el endpoint existente.
- No cambia schema ni migraciones.
- No cambia el significado de estados financieros, documentales u operativos.

## Query params

La seleccion inicial existente se conserva:

- `/donatarias?donationId=...`
- `/donatarias?donationId=...&applicationId=...`

Con esos parametros, la pantalla carga la donacion, el reporte de transparencia y la aplicacion seleccionada cuando aplica. Desde el tab `Reporte de transparencia`, el boton `Imprimir reporte` usa el reporte ya cargado.

## Validacion local

Validacion ejecutada en la sesion:

- `npm run build`: exitoso. Conserva advertencia existente de presupuesto CSS del componente Donatarias.
- `npm test -- --watch=false`: `22/22`.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso, `0 Warning(s)`, `0 Error(s)`.
- Playwright con API mockeada sobre Angular local `http://127.0.0.1:4223/`:
  - donacion sin aplicaciones: `NOT_READY`, sin aplicaciones para reportar.
  - donacion parcial con evidencia pendiente: `PARTIAL`, saldo pendiente, faltante documental e impresion disparada.
  - donacion completa con evidencia: `READY`, saldo cero, evidencia visible y sin faltantes basicos.
  - modo print: shell/nav ocultos, boton de impresion oculto, formularios ausentes y tabla de aplicaciones sin desbordar el viewport.
- `git diff --check`: sin observaciones.
- Migraciones: sin archivos nuevos en `src/backend/src/FMCPA.Infrastructure/Persistence/Migrations`.

Comandos minimos:

```bash
cd src/frontend
npm run build
npm test -- --watch=false
cd ../..
dotnet build src/backend/FMCPA.Backend.sln --no-restore
git diff --check
```

## Fuera de alcance

- PDF server-side u oficial.
- CSV.
- Folio oficial.
- Firma.
- Snapshot/versionamiento de reporte.
- Aprobacion documental humana.
- Checklist legal/documental avanzado.
- Validacion legal, fiscal o contable.
- Cambios de permisos, schema o migraciones.
- Cambios de produccion o CI/CD.
