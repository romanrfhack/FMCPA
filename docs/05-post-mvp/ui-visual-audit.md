# Auditoria visual post-rediseño FMCPA

## Resumen ejecutivo

Se reviso el shell autenticado y las pantallas principales despues del rediseño de login, header, navegacion principal y Donatarias. Esas cuatro piezas ya funcionan como referencia visual: fondo crema, verde petroleo, teal, tarjetas limpias, badges tipo pill, layout mas compacto y lenguaje de usuario final.

El resto de la aplicacion todavia conserva patrones de prototipo tecnico: etiquetas `STAGE`/`TRACK`, referencias a MVP, codigos internos en ingles, formularios largos siempre visibles, acciones administrativas expuestas de forma permanente y tarjetas con demasiado radio/peso visual frente al nuevo estandar.

La prioridad visual mas alta esta en `/federation`, `/financials`, `/documents`, `/markets` y `/admin/users`. Estas pantallas concentran exceso de scroll, demasiadas acciones visibles, formularios inline extensos y riesgo responsive. Donatarias debe conservarse como referencia y no como pantalla por rehacer.

## Validacion visual realizada

- Se levanto el frontend local en `http://127.0.0.1:4200/`.
- Se recorrio la aplicacion con Chromium/Playwright headless en `390px`, `768px` y `1366px`.
- Se uso sesion autenticada mock y respuestas API sinteticas para poder renderizar las rutas sin tocar backend ni datos locales.
- No se generaron capturas inventadas ni se modifico codigo funcional.
- Hallazgos medidos destacados:
  - `/federation`: 8 formularios, 36 campos, 22 botones y scroll movil aproximado de `9565px`.
  - `/financials`: 5 formularios, 30 campos, 17 botones y scroll movil aproximado de `8313px`.
  - `/markets`: 4 formularios, 25 campos y scroll movil aproximado de `6952px`.
  - `/documents`: overflow horizontal en movil en la pasada completa (`scrollWidth 479px` sobre `390px`) y alto scroll en desktop.
  - `/documents/work-queue`: overflow horizontal desktop por acciones de filtro (`scrollWidth 1378px` sobre `1366px`).
  - `/admin/users`: overflow horizontal desktop por filas/acciones de usuario (`scrollWidth 1589px` sobre `1366px`).

## Tabla por pantalla

| Pantalla | Problemas detectados | Severidad visual | Riesgo de cambio | Recomendacion |
| --- | --- | --- | --- | --- |
| `/dashboard` | Copy visible de `STAGE-07`, `MVP` y "minima"; hero y tarjetas grandes para informacion operativa; scroll movil alto. | Media | Bajo | Convertir a portada operativa compacta, retirar lenguaje de etapa/MVP y usar KPIs mas densos alineados con Donatarias. |
| `/operations` | Buen intento de centro transversal, pero conserva `TRACK 4`, `HIGH/MEDIUM/LOW`, `DOCUMENTS`, `SECURITY`, `Exportar summary` y `Hold admin`; demasiados botones en header. | Media | Bajo | Cambiar labels a severidades y areas en espanol; agrupar exportaciones en menu secundario; compactar acciones superiores. |
| `/markets` | 4 formularios inline, 25 campos, sidebar largo, alta de mercado siempre visible, alta de locatario/incidencia dentro del detalle y texto de etapa. | Alta | Medio | Mover altas a modales/drawers; dejar filtros, listado y detalle como flujo principal; retirar `STAGE-03` y copy de "modulo real". |
| `/donatarias` | Ya refleja el nuevo estandar: tabs, KPIs, reportes, modales y vista imprimible. Quedan restos menores de "minima" en textos. | Baja | Bajo | Mantener como referencia visual; solo limpiar copy residual cuando se haga homologacion global. |
| `/financials` | 5 formularios inline, 30 campos, contexto financiero, alta de oficio, creditos, comisiones y renovacion compiten en una sola pantalla; scroll movil/desktop muy alto. | Alta | Medio | Rediseñar como vista maestro-detalle con acciones primarias arriba y captura en modales; mantener ficha contextual como panel compacto. |
| `/federation` | Pantalla mas pesada: gestiones, donaciones, aplicaciones, comisiones y evidencias conviven con 8 formularios y 36 campos; scroll extremo. | Critica | Medio | Separar en tabs o subflujos visuales; mover todas las altas a modales/drawers; mantener un unico foco operativo por seccion. |
| `/documents` | Lenguaje tecnico abundante (`Entity ID`, `GUID`, codigos de integridad/retencion, `Hold admin`); filtros largos; detalle con demasiados campos; overflow movil detectado. | Alta | Medio | Crear vista documental ejecutiva con filtros colapsables; traducir codigos a labels; mover acciones de metadata, hold, retencion y archivado a modales. |
| `/documents/work-queue` | Filtros compactos pero con codigos `HIGH/MEDIUM/LOW`; accion `Exportar CSV` provoca overflow desktop; filas tienen terminos tecnicos. | Alta | Bajo | Ajustar responsive del bloque de acciones; traducir severidad y tipo; convertir exportacion en accion secundaria. |
| `/documents/review` | Pantalla mas acotada, pero el lenguaje de retencion sigue tecnico y la edicion queda inline. | Media | Medio | Usar copy de revision documental; mover actualizacion de revision/retencion a modal; conservar lista simple. |
| `/contacts` | Formulario de alta siempre visible, copy de `STAGE-02` y "catalogo reutilizable"; campos bien entendibles pero la pantalla se siente de administracion tecnica. | Media | Bajo | Mover alta a modal; dejar listado como primer plano; limpiar copy institucional. |
| `/commissions` | Densidad razonable, pero mantiene `STAGE-07`, `MVP/minima` y exportacion visible; puede integrarse mejor al lenguaje operativo. | Media | Bajo | Ajustar copy, badges y filtros; mantener estructura actual. |
| `/history` | Layout aceptable, pero contiene `TRACK 1 POST-MVP`, `MVP` y explicaciones de cierre interno; lenguaje no esta al nivel institucional. | Media | Bajo | Renombrar a "Historico operativo"; retirar referencias de etapa; compactar filtros. |
| `/bitacora` | Similar a historico: utilidad clara, pero `TRACK 1 POST-MVP`, "transversal real" y metadatos pueden sonar tecnicos. | Media | Bajo | Reescribir copy a auditoria operativa; ocultar datos tecnicos salvo detalle expandido. |
| `/admin/users` | Overflow horizontal desktop; grilla de acciones por fila demasiado ancha; `UserName`, `Password`, `READONLY/OPERATOR/ADMIN`, UTC y lockout expuestos; alta siempre visible. | Alta | Medio | Mover alta y reset de password a modales; comprimir acciones por usuario en menu; traducir roles/lockout a lenguaje administrativo. |
| `/admin/security` | Menos pesada, pero conserva `TRACK 2`, eventos `SECURITY`, login failed, lockout y labels de auditoria tecnica. | Media | Bajo | Mantener dashboard, traducir eventos a lenguaje de seguridad operativo y colapsar filtros avanzados. |
| `/catalogs/commission-types` | Formulario inline, `STAGE-02`, `Alta minima`, `Codigo`, placeholders en mayusculas y copy de catalogo sembrado. | Media | Bajo | Mover alta a modal; usar "Tipos de comision" con labels funcionales; retirar referencias de etapa. |
| `/catalogs/evidence-types` | Mismo patron que tipos de comision; es simple pero con estilo de catalogo tecnico. | Media | Bajo | Homologar con modal de alta y listado compacto; traducir codigo/orden como campos avanzados. |
| `/catalogs/module-statuses` | Formulario de 8 campos, varios codigos de modulo/contexto/estatus y copy tecnico; mayor riesgo de confusion que otros catalogos. | Alta | Medio | Separar alta en modal con secciones; ocultar codigos en modo avanzado; priorizar nombre visible y descripcion. |
| `/account/password` | Flujo claro y acotado, pero conserva `TRACK 2 SEGURIDAD` y menciona token; visualmente compacto. | Baja | Bajo | Limpiar kicker/copy; mantener formulario actual. |

## Problemas transversales

### Densidad visual

- Las pantallas operativas antiguas usan `hero-card` grande seguido de sidebars y formularios permanentes.
- `/federation`, `/financials` y `/markets` acumulan demasiados flujos de captura en una misma vista.
- Los catalogos y contactos muestran altas inline aunque son acciones secundarias.
- Muchas pantallas usan tarjetas grandes con radio mayor al nuevo estandar de shell/navegacion.

### Claridad del lenguaje

- Persisten etiquetas de implementacion: `STAGE`, `TRACK`, `POST-MVP`, `MVP`, "minima/minimo".
- Hay codigos visibles que deberian traducirse: `HIGH`, `MEDIUM`, `LOW`, `ACTIVE_OK`, `REVIEW_DUE`, `RETENTION_EXPIRED`, `READONLY`, `OPERATOR`, `ADMIN`.
- Hay labels tecnicos en formularios: `UserName`, `Password`, `Entity ID`, `GUID`, `Content type`, `Baseline retencion`, `Effective retencion`, `Hold admin`.

### Consistencia visual

- Donatarias y shell usan un lenguaje mas institucional; el resto alterna entre tarjetas antiguas, paneles tecnicos y badges con codigos.
- Botones secundarios y exportaciones aparecen con el mismo peso que acciones primarias.
- Las tablas fueron sustituidas por listas, pero varias listas siguen pareciendo dumps de propiedades.

### Responsive

- Riesgo confirmado en `/documents`, `/documents/work-queue` y `/admin/users`.
- Riesgo por longitud de scroll en `/federation`, `/financials` y `/markets`, aunque no siempre haya overflow horizontal.
- Los formularios inline hacen que movil priorice captura antes que consulta.

### Acciones principales

- Las acciones primarias no siempre estan arriba: en modulos densos quedan enterradas en sidebars o detalle.
- Demasiadas acciones permanentes deberian moverse a modales, drawers o menus de fila.
- Las exportaciones deberian ser acciones secundarias, no competir con lectura diaria.

### Riesgo funcional de rediseño

- Bajo: limpieza de copy, badges, colores, radios, espaciados, agrupacion visual, filtros colapsables.
- Medio: mover formularios a modales/drawers y compactar acciones por fila; requiere cuidar estado local, validaciones y foco.
- Alto: dividir rutas, cambiar contratos de datos, crear nuevas pantallas dependientes de backend o reestructurar entidades. Debe evitarse por ahora.

## Pantallas que deberian usar modales o drawers

- `/markets`: alta de mercado, alta de locatario, registro de incidencia y cierre formal.
- `/financials`: alta de oficio, captura de credito, registro de comision y preparacion de renovacion.
- `/federation`: alta de gestion, participante, donacion, aplicacion, comision y evidencia.
- `/documents`: metadata, retencion, hold administrativo, archivado/restauracion y filtros avanzados.
- `/documents/review`: actualizacion de revision documental.
- `/contacts`: alta de contacto.
- `/admin/users`: alta de usuario, cambio de rol, reset de password y desbloqueo.
- Catalogos: alta de item y, si se agrega despues, edicion de item.

## Conclusiones

La mejora global debe enfocarse primero en reducir densidad y lenguaje tecnico, no en crear mas funcionalidades. La aplicacion ya tiene una referencia clara en login/header/navegacion/Donatarias; el siguiente trabajo debe llevar el resto de pantallas a ese mismo patron con cambios de layout, copy e interaccion local, sin backend.
