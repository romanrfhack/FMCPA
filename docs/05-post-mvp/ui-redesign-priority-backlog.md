# Backlog priorizado de rediseño visual FMCPA

## Principios de la siguiente fase

- Donatarias, login, header y navegacion principal son la referencia visual vigente.
- Los patrones compartidos de pantallas redisenadas viven en utilidades/alias globales `fmcpa-*`; nuevas pantallas deben reutilizarlos antes de copiar CSS local.
- Priorizar layout, copy e interaccion local antes que cambios de datos.
- No introducir dependencias backend para resolver problemas visuales.
- Mover captura secundaria a modales/drawers cuando la pantalla principal deba ser de consulta.
- Conservar fondo crema, verde petroleo, teal, tarjetas limpias, badges pill, layout compacto y lenguaje no tecnico.

## Fases sugeridas

### Fase 1 - Correcciones visuales criticas y responsive

| Prioridad | Pantalla | Objetivo | Tipo de cambio | Riesgo | Requiere backend | Criterios de aceptacion |
| --- | --- | --- | --- | --- | --- | --- |
| P0 | `/documents` | Etapa A implementada: eliminar overflow movil y reducir apariencia de panel tecnico. | Layout/copy/responsive | Medio | No | Cumplido en Etapa A: 390/768/1366 sin overflow global; filtros compactos; codigos principales traducidos; detalle prioriza datos operativos y conserva metadata tecnica en seccion secundaria. |
| P0 | `/documents/work-queue` | Etapa A implementada: corregir overflow desktop y clarificar severidad. | Layout/copy/responsive | Bajo | No | Cumplido en Etapa A: 390/768/1366 sin overflow global; `Exportar CSV` queda en area secundaria; severidades se leen como Alta/Media/Baja y tipos de pendiente usan lenguaje operativo. |
| P0 | `/admin/users` | Etapa A implementada: eliminar overflow desktop y comprimir acciones por usuario. | Layout/interaccion local | Medio | No | Cumplido en Etapa A: 390/768/1366 sin overflow global; alta abre en dialog; acciones de rol, contraseña y bloqueo quedan en panel `Gestionar`; activacion queda visible por fila. |
| P0 | `/federation` | Etapa A/B implementadas: reducir scroll extremo y separar focos operativos. | Layout/interaccion local | Medio | No | Cumplido en Etapa B: tabs por dominio, resumen ejecutivo, listados/detalles enfocados y altas de gestion/participante/donacion/aplicacion/comision/evidencia en modales contextuales sin formularios inline permanentes. |
| P1 | `/financials` | Etapa A implementada: convertir captura financiera en flujo guiado, no formulario permanente. | Layout/interaccion local | Medio | No | Cumplido en Etapa A: ficha operativa destacada; tabs para contexto/oficios/creditos/comisiones/renovaciones; alta de oficio, credito, comision y renovacion en modales; 390/768/1366 sin overflow global. |
| P1 | `/markets` | Etapa A implementada: reducir sidebar largo y hacer el detalle mas operativo. | Layout/interaccion local | Medio | No | Cumplido en Etapa A: resumen ejecutivo, tabs locales, filtros/listado compactos, altas de mercado/locatario/incidencia en modales, documentos/cédulas separados y 390/768/1366 sin overflow global. |

### Fase 2 - Limpieza de lenguaje tecnico

| Prioridad | Pantalla | Objetivo | Tipo de cambio | Riesgo | Requiere backend | Criterios de aceptacion |
| --- | --- | --- | --- | --- | --- | --- |
| P1 | Todas excepto login/header/nav/Donatarias | Retirar `STAGE`, `TRACK`, `MVP`, "minima/minimo" y copy de implementacion. | Copy | Bajo | No | Ninguna pantalla visible al usuario muestra etiquetas de etapa o referencias MVP. |
| P1 | `/operations` | Traducir centro operativo a lenguaje institucional. | Copy/layout | Bajo | No | No se muestran `SUMMARY`, `HIGH`, `MEDIUM`, `LOW`, `SECURITY`, `DOCUMENTS` como labels principales; exportaciones son secundarias. |
| P1 | `/documents` | Traducir taxonomia documental. | Copy/layout | Bajo | No | `GUID`, `Entity ID`, `Content type`, `Baseline`, `Effective`, `Hold admin` dejan de ser labels de primer nivel. |
| P1 | `/admin/security` | Traducir eventos y bloqueo a lenguaje de administracion. | Copy | Bajo | No | Eventos de seguridad se leen como actividad administrativa; codigos quedan solo en detalle expandido si son necesarios. |
| P2 | `/history`, `/bitacora`, `/commissions` | Homologar lenguaje de cierre, historial y comisiones. | Copy/layout | Bajo | No | Encabezados y empty states hablan de operacion FMCPA, no de tracks ni cierre de MVP. |

### Fase 3 - Homologacion de catalogos y pantallas administrativas simples

| Prioridad | Pantalla | Objetivo | Tipo de cambio | Riesgo | Requiere backend | Criterios de aceptacion |
| --- | --- | --- | --- | --- | --- | --- |
| P2 | `/contacts` | Convertir alta inline a accion secundaria. | Interaccion local | Bajo | No | Pantalla inicial muestra listado/filtro y boton `Nuevo contacto`; formulario abre en modal. |
| P2 | `/catalogs/commission-types` | Homologar catalogo simple. | Layout/copy/interaccion local | Bajo | No | Boton `Nuevo tipo`; formulario en modal; `Codigo` y `Orden` tratados como campos administrativos. |
| P2 | `/catalogs/evidence-types` | Homologar catalogo simple. | Layout/copy/interaccion local | Bajo | No | Misma estructura que tipos de comision; badges activos/inactivos tipo pill. |
| P2 | `/catalogs/module-statuses` | Reducir complejidad del catalogo de estatus. | Layout/interaccion local | Medio | No | Alta en modal con secciones; campos de codigo agrupados como datos administrativos; listado legible en movil. |
| P2 | `/account/password` | Limpiar copy tecnico. | Copy/layout | Bajo | No | Sin `TRACK`; no menciona token; conserva validaciones y flujo actual. |

### Fase 4 - Pulido de dashboards y reportes transversales

| Prioridad | Pantalla | Objetivo | Tipo de cambio | Riesgo | Requiere backend | Criterios de aceptacion |
| --- | --- | --- | --- | --- | --- | --- |
| P3 | `/dashboard` | Hacerlo ejecutivo y compacto. | Layout/copy | Bajo | No | KPIs compactos, alertas priorizadas, sin referencias MVP; primera vista desktop no se siente como hero de prototipo. |
| P3 | `/operations` | Consolidarlo como tablero de trabajo diario. | Layout/copy | Bajo | No | Acciones principales visibles; filtros de tiempo compactos; exportaciones bajo menu secundario. |
| P3 | `/commissions` | Alinear con reportes de Donatarias. | Layout/copy | Bajo | No | Filtros compactos, totales claros, exportacion secundaria, badges consistentes. |
| P3 | `/history` | Mejorar lectura de cerrados. | Layout/copy | Bajo | No | Filtros ligeros; tarjetas compactas; lenguaje de consulta historica. |
| P3 | `/bitacora` | Mejorar lectura de auditoria operativa. | Layout/copy | Bajo | No | Eventos agrupados por fecha/modulo; metadatos tecnicos escondidos en detalle. |

## Quick wins

- Reemplazar kickers `STAGE-*` y `TRACK-*` por nombres de area: `Operacion`, `Control documental`, `Administracion`, `Historico`.
- Cambiar "Alta minima" por "Nuevo registro" o "Agregar".
- Traducir badges `HIGH/MEDIUM/LOW` a `Alta`, `Media`, `Baja`.
- Mover exportaciones a boton secundario o menu compacto.
- Reducir radios grandes de tarjetas antiguas para igualar el shell.
- Usar headings mas cortos y operativos en dashboard, historico, bitacora y catalogos.
- Convertir copy de "MVP", "modulo real", "sembrado", "base minima" a lenguaje institucional.

## Validacion global 2026-05-16

- Estado: etapa visual global aceptada con pendientes menores.
- Evidencia: `docs/05-post-mvp/ui-global-redesign-validation-note.md`.
- Playwright real con usuario `ADMIN` valido `/login`, shell/nav y rutas principales en `390px`, `768px` y `1366px`.
- Bugs corregidos durante validacion: copy tecnico visible en `/dashboard`, `/operations`, `/documents/review`, `/admin/security`, `/contacts`, `/commissions`, `/history`, `/bitacora` y `/account/password`; overflow horizontal movil en `/bitacora`.
- Sigue pendiente convertir `/contacts` y catalogos simples a la misma densidad/interaccion de modales, mas pulido visual de dashboards/reportes transversales.

## Actualizacion de guia de usuario 2026-05-16

- Estado: guia rapida y HTML final actualizados para reflejar el rediseño visual global aceptado.
- Evidencia: `docs/05-post-mvp/ui-global-redesign-user-guide-update-note.md`.
- La guia ya describe login institucional, header con logo, menu de usuario compacto, cambio de contraseña, cierre de sesion y navegacion agrupada.
- La guia ya describe Donatarias, Markets, Financials, Federation, Documents y Admin Users con tabs, pantallas compactas y modales contextuales donde aplica.
- Se mantienen visibles como fase posterior: Contacts UX, Catalogos UX, PDF oficial/folio/firma, validacion legal/fiscal/contable y reportes formales.

## Cambios de riesgo medio

- Modales/drawers para formularios que hoy viven inline.
- Menus de acciones por fila en usuarios y documentos.
- Tabs internas para modulos densos pendientes, siguiendo los patrones ya aplicados en `/financials` y `/federation`.
- Filtros colapsables en `/documents`.
- Reordenar contenido maestro-detalle en `/markets`, `/financials` y `/federation`.

## Cambios que deberian evitarse por ahora

- Crear endpoints nuevos para resolver layout.
- Dividir rutas a nivel router si no es necesario para la mejora visual.
- Cambiar contratos API, permisos, guards o modelos backend.
- Reestructurar datos de documentos/retencion/seguridad.
- Cambiar CI/CD o scripts locales.
- Rediseñar Donatarias desde cero; debe usarse como referencia.

## Siguiente fase recomendada

Continuar con `/contacts` y catalogos simples como siguientes pantallas densas pendientes. `/documents`, `/documents/work-queue`, `/admin/users`, `/financials`, `/federation` y `/markets` ya recibieron mejoras de densidad; `/federation` ya incluye Etapa A y Etapa B. La deuda de CSS budget de Donatarias, Documents, Financials, Federation y Markets quedo limpiada mediante utilidades `fmcpa-*` globales. La validacion global del 2026-05-16 quedo aceptada sin overflow horizontal y sin warnings de CSS budget, por lo que la siguiente fase debe reutilizar esos patrones antes de agregar CSS local nuevo.
