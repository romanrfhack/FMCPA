# FMCPA Platform

## Guía rápida de operación

**Versión de guía:** Final 1.2
**Corte de contenido:** 16 de mayo de 2026
**Alcance del manual:** guía rápida para operación diaria del sistema, basada en las pantallas, rutas, permisos y documentación disponibles en el repositorio.

Esta guía está redactada para usuarios operativos y administrativos de FMCPA Platform. Su objetivo es ayudar a ubicar módulos, entender qué permite hacer cada pantalla y reconocer qué funciones están disponibles, acotadas, en validación o previstas para una fase posterior.

## Cómo usar esta guía

[Disponible] Esta es una guía rápida. Sirve como referencia inicial para navegar el sistema y operar los módulos principales, pero no sustituye una capacitación completa, procedimientos internos ni criterios administrativos definidos por la organización.

[Disponible] Algunas funciones dependen del rol asignado al usuario. Si una pantalla o acción no aparece en el menú, puede deberse a permisos del perfil.

[Solo ADMIN] Las funciones administrativas requieren un usuario con rol `ADMIN`. Esto incluye administración de usuarios, seguridad, catálogos administrativos, revisión documental administrativa y cierres formales.

## Convención de estados

- `[Disponible]`: la función existe en el sistema y cuenta con pantalla o flujo operativo.
- `[Disponible con alcance acotado]`: la función existe, pero su alcance tiene límites documentados.
- `[En validación]`: la función está implementada o iniciada, pero requiere aprobación formal o consolidación operativa.
- `[Pendiente de fase posterior]`: no está incluida en esta versión o requiere definición adicional antes de considerarse cerrada.
- `[Solo ADMIN]`: requiere permisos administrativos.

## Objetivo del sistema

[Disponible] FMCPA Platform centraliza la operación de Mercados, Donatarias, Financieras y Federación. También integra consulta documental, alertas, bitácora, histórico, comisiones, centro operativo y administración básica de usuarios y seguridad, con el rediseño visual global ya validado.

[Disponible con alcance acotado] El sistema cuenta con una base operativa amplia, pero algunas capacidades avanzadas, como reportes formales, notificaciones externas, seguridad avanzada, auditoría forense y cumplimiento documental especializado, no están incluidas en esta versión.

## Acceso e inicio de sesión

[Disponible] El acceso principal se realiza desde `/login`, mediante una pantalla institucional con logo FMCPA y lenguaje orientado a usuario final.

[Disponible] Una vez iniciada la sesión, el sistema muestra un header autenticado con logo FMCPA, nombre de plataforma y menú de usuario compacto. Desde ese menú se puede cerrar sesión y acceder al cambio de contraseña.

[Disponible] Los roles visibles en la documentación y en el sistema son:

| Rol | Alcance general |
| --- | --- |
| `READONLY` | Consulta información permitida. No crea registros ni ejecuta cierres. |
| `OPERATOR` | Consulta y registra información operativa en módulos de negocio. No administra usuarios, catálogos ni cierres formales. |
| `ADMIN` | Puede operar módulos, administrar usuarios, seguridad, catálogos, revisión documental y cierres formales. |

[Pendiente de fase posterior] No se incluyen en esta versión funciones avanzadas como autenticación multifactor, proveedor externo de identidad, recuperación avanzada de contraseña, administración avanzada de sesiones o monitoreo especializado de seguridad.

## Navegación general

[Disponible] La navegación principal se presenta dentro del entorno autenticado del sistema. El menú se adapta a los permisos efectivos del usuario y se agrupa en `Inicio`, `Operación`, `Control` y `Administración`.

Pantallas reales identificadas:

| Sección | Ruta |
| --- | --- |
| Dashboard | `/dashboard` |
| Centro operativo | `/operations` |
| Histórico | `/history` |
| Comisiones | `/commissions` |
| Bitácora | `/bitacora` |
| Documentos | `/documents` |
| Bandeja documental | `/documents/work-queue` |
| Revisión documental | `/documents/review` |
| Contactos | `/contacts` |
| Usuarios | `/admin/users` |
| Seguridad | `/admin/security` |
| Tipos de comisión | `/catalogs/commission-types` |
| Tipos de evidencia | `/catalogs/evidence-types` |
| Estatus por módulo | `/catalogs/module-statuses` |
| Mercados | `/markets` |
| Donatarias | `/donatarias` |
| Financieras | `/financials` |
| Federación | `/federation` |
| Cambio de contraseña | `/account/password` |

## Inicio rápido

[Disponible] Para una revisión diaria inicial se recomienda:

1. Abrir `/operations` para revisar prioridades transversales.
2. Revisar `/dashboard` para consultar resumen ejecutivo y alertas principales.
3. Atender pendientes documentales desde `/documents/work-queue` o desde el módulo de origen.
4. Entrar al módulo correspondiente para registrar avances, evidencias o cierres según el rol.
5. Reservar las acciones administrativas para usuarios `ADMIN`.

## Centro operativo

[En validación] La pantalla `/operations` consolida señales de negocio, documentos y seguridad de acuerdo con los permisos del usuario. Incluye indicadores, bandeja transversal, severidades, ventanas temporales y exportación CSV ligera.

El usuario puede:

- Revisar prioridades operativas agrupadas.
- Cambiar la ventana temporal de consulta.
- Filtrar pendientes por severidad.
- Abrir el contexto de resolución desde cada elemento.
- Exportar información ligera para seguimiento operativo.

[Solo ADMIN] La acción rápida administrativa detectada es el desbloqueo de usuarios bloqueados, cuando el sistema lo permite.

[Disponible con alcance acotado] Esta pantalla no reemplaza los módulos fuente. No administra responsables, acuerdos de servicio ni flujos de aprobación completos.

## Dashboard

[Disponible] La pantalla `/dashboard` muestra un resumen ejecutivo de los módulos principales, alertas activas e histórico reciente.

El usuario puede:

- Consultar conteos y señales de Mercados, Donatarias, Financieras y Federación.
- Ir al módulo de origen desde tarjetas o enlaces.
- Revisar alertas activas principales.
- Consultar histórico reciente.

[Disponible con alcance acotado] El dashboard es una vista ejecutiva operativa. No equivale a analítica avanzada ni inteligencia de negocio.

## Mercados

[Disponible] La pantalla `/markets` permite administrar mercados, locatarios, cédulas digitalizadas, incidencias y mejoras con resumen operativo, tabs locales, listados compactos y modales contextuales.

El usuario puede:

- Filtrar mercados por estatus o alertas activas.
- Registrar mercados desde modal contextual con datos generales y observaciones.
- Seleccionar un mercado para consultar detalle.
- Registrar locatarios desde modal contextual con datos de contacto, giro, número de cédula, vigencia y archivo digitalizado.
- Descargar cédulas digitalizadas cuando existen.
- Registrar incidencias o mejoras desde modal contextual con seguimiento, avance y estatus.
- Revisar alertas de cédulas por vencer o vencidas.
- Consultar documentos relacionados.

[Solo ADMIN] El cierre formal de mercado requiere permisos administrativos.

[Disponible con alcance acotado] Las alertas de vigencia existen, pero reglas más detalladas de alertamiento quedan sujetas a definición adicional.

## Donatarias

[Disponible] La pantalla `/donatarias` administra la transparencia del recurso donado. El módulo está organizado en tabs para separar revisión ejecutiva, captura, distribución, evidencias y reporte, con modales contextuales para registrar donaciones, aplicaciones y evidencias.

Tabs disponibles:

- `Resumen`: indicadores principales, semáforos y estado documental agregado.
- `Donaciones`: listado, filtros y registro de donaciones.
- `Aplicaciones / distribución`: distribución financiera del recurso por aplicación.
- `Evidencias`: evidencia agrupada por aplicación y documentos relacionados.
- `Reporte de transparencia`: reporte operativo con vista imprimible.

KPIs principales:

- Total recibido.
- Total aplicado.
- Saldo pendiente.
- Porcentaje aplicado.
- Número de aplicaciones.
- Evidencias registradas.

El usuario puede:

- Filtrar donaciones por estatus o alertas.
- Registrar donaciones desde modal contextual con donante, fecha, tipo, monto, referencia y observaciones.
- Consultar total recibido, total aplicado, saldo pendiente y porcentaje aplicado.
- Revisar la distribución por aplicación, incluyendo beneficiario, fecha, responsable, monto aplicado, porcentaje del total recibido, saldo restante, estatus y detalle de comprobación.
- Registrar aplicaciones desde modal contextual con beneficiario, responsable, monto, estatus, comprobación y datos de cierre.
- Cargar evidencias desde modal contextual y descargar evidencias por aplicación.
- Revisar alertas de donaciones no aplicadas o parcialmente aplicadas.
- Consultar documentos relacionados.
- Revisar semáforos financiero, documental y operativo.
- Consultar el reporte de transparencia de una donación.
- Usar la vista imprimible del reporte cuando exista una donación seleccionada.

[Disponible] El reporte de transparencia muestra donante, referencia, fecha de donación, fecha de corte o generación, tipo de donación, totales financieros, estado financiero, estado documental, estado operativo, aplicaciones, evidencias, faltantes y notas de alcance.

[Disponible] El readiness del reporte se presenta como:

- `READY`: sin saldo pendiente y con evidencia mínima registrada.
- `PARTIAL`: útil para revisión operativa, pero con pendientes financieros o documentales.
- `NOT_READY`: falta información mínima, por ejemplo aplicaciones o evidencia suficiente para una presentación operativa.

[Disponible] La vista imprimible oculta navegación, formularios y botones de captura. Conserva encabezado, KPIs, estados, aplicaciones, evidencias, faltantes y notas de alcance. Es una vista operativa y no genera PDF oficial.

[Solo ADMIN] El cierre formal de una donación requiere permisos administrativos.

[Disponible con alcance acotado] La evidencia mínima registrada no sustituye revisión legal, fiscal o contable. El reporte es una vista operativa de transparencia, no un dictamen ni documento oficial.

[Pendiente de fase posterior] Siguen fuera de esta versión: PDF oficial, folio, firma, versionamiento del reporte, validación legal/fiscal/contable, checklist documental avanzado y catálogo formal de donantes. Cualquier CSV o exportación ligera debe tratarse como apoyo operativo, no como reporte oficial.

## Financieras

[Disponible] La pantalla `/financials` administra oficios o autorizaciones, vigencias, créditos y comisiones por crédito con tabs, ficha operativa, búsqueda de vigente, cadena y modales contextuales.

El usuario puede:

- Filtrar oficios por estatus o alertas.
- Registrar oficios/autorizaciones desde modal contextual con financiera, institución o dependencia, lugar o stand, horario, vigencia, estatus, términos y observaciones.
- Buscar el permiso vigente por financiera, institución o dependencia y lugar o stand.
- Registrar créditos individuales desde modal contextual con promotor, beneficiario, contacto, fecha, monto y notas.
- Registrar comisiones por crédito desde modal contextual con tipo, destinatario, base y monto.
- Consultar alertas de oficios vencidos, por vencer o en renovación.

[En validación] La renovación de oficios está implementada como renovación mínima. Conserva el oficio anterior como histórico, crea uno nuevo vigente y muestra una cadena de renovación.

[En validación] La captura contextual de crédito permite capturar desde un contexto operativo y resolver el oficio vigente antes de abrir el formulario de crédito.

[Disponible con alcance acotado] La unicidad operativa de permisos vigentes se valida desde la aplicación. Persiste riesgo residual ante solicitudes concurrentes muy cercanas.

[Pendiente de fase posterior] No existe catálogo maestro de financieras, instituciones, dependencias o stands. La normalización actual es básica y puede no reconocer alias, abreviaturas o sinónimos.

[Solo ADMIN] El cierre formal de un oficio requiere permisos administrativos.

## Federación

[Disponible] La pantalla `/federation` concentra gestiones de Federación y donaciones de Federación en tabs internos, con resumen, listados compactos y modales contextuales.

En gestiones, el usuario puede:

- Filtrar por estatus o alertas.
- Registrar gestiones desde modal contextual con tipo, fecha, contraparte, estatus, objetivo y observaciones.
- Agregar participantes internos o externos desde contactos compartidos mediante modal contextual.
- Consultar participantes y alertas de seguimiento.

En donaciones de Federación, el usuario puede:

- Registrar donaciones desde modal contextual con donante, fecha, tipo, monto, referencia, estatus y notas.
- Registrar aplicaciones desde modal contextual con beneficiario o destino, fecha, monto, estatus, comprobación y datos de cierre.
- Registrar comisiones por aplicación desde modal contextual.
- Cargar evidencias desde modal contextual y descargar evidencias existentes.
- Consultar documentos relacionados.

[Solo ADMIN] Los cierres formales de gestiones y donaciones requieren permisos administrativos.

[Disponible con alcance acotado] Permanecen reservas documentadas sobre reglas finas de evidencias, comisiones y aplicaciones.

## Contactos y catálogos

[Disponible] La pantalla `/contacts` administra contactos compartidos reutilizables entre módulos.

El usuario puede:

- Registrar contactos con nombre, tipo, organización o dependencia, cargo, celular, WhatsApp, correo y notas.
- Consultar contactos existentes.

[Disponible con alcance acotado] El catálogo de contactos es operativo y básico. No sustituye una gestión avanzada de identidad, deduplicación o relaciones formales.

[Solo ADMIN] Las pantallas de catálogos administrativos son:

- `/catalogs/commission-types`: tipos de comisión.
- `/catalogs/evidence-types`: tipos de evidencia.
- `/catalogs/module-statuses`: estatus por módulo.

## Documentos

[Disponible] La pantalla `/documents` funciona como catálogo documental transversal compacto, con labels operativos, filtros, detalle y acciones secundarias.

El usuario puede:

- Consultar resumen ejecutivo documental.
- Filtrar por módulo, área documental, integridad, estado operativo, clase, retención, estado y entidad.
- Buscar documentos y ver detalle.
- Descargar documentos autorizados.
- Exportar CSV ligero del catálogo.
- Consultar pendientes documentales.
- Abrir o resolver en origen cuando tiene permisos del módulo.
- Ver historia documental del documento seleccionado.

[Disponible] La pantalla `/documents/work-queue` muestra una work queue documental compacta con pendientes de completitud, integridad y revisión de retención. Permite filtrar por módulo, tipo, prioridad y exportar CSV ligero.

[Solo ADMIN] La pantalla `/documents/review` permite revisar documentos por retención: marcar revisado, diferir revisión, descargar y exportar CSV.

[Solo ADMIN] En `/documents`, el usuario `ADMIN` puede editar metadata documental, aplicar o limpiar override de retención, aplicar o limpiar hold administrativo, archivar y restaurar documentos.

[Disponible con alcance acotado] La retención documental actual es operativa. Un documento con retención vencida requiere revisión; no implica autorización automática de borrado ni cumplimiento legal completo.

[Pendiente de fase posterior] No se incluye en esta versión respaldo externo formal, OCR, clasificación automática avanzada, legal hold formal, borrado físico seguro ni cumplimiento documental/normativo avanzado.

## Histórico

[Disponible con alcance acotado] La pantalla `/history` permite consultar registros cerrados o archivados, filtrar por módulo y buscar por título, subtítulo o referencia.

[Pendiente de fase posterior] La lectura histórica mantiene reservas en la marca formal de cierre y consistencia de algunos eventos.

## Bitácora

[Disponible con alcance acotado] La pantalla `/bitacora` permite consultar eventos transversales con filtros por módulo, entidad, texto y fechas. También muestra señales de integridad documental.

[Pendiente de fase posterior] La bitácora no equivale a auditoría forense completa. No reconstruye todo evento histórico previo ni cubre todos los cambios finos de operación.

## Comisiones

[Disponible con alcance acotado] La pantalla `/commissions` consolida comisiones operativas de Financieras y Federación. Permite filtrar por módulo origen, tipo de comisión, categoría de destinatario, fechas y texto.

[Pendiente de fase posterior] Esta vista no debe interpretarse como contabilidad definitiva, inteligencia de negocio ni reporte financiero formal.

## Seguridad y usuarios

[Solo ADMIN] La pantalla `/admin/users` permite administración compacta de usuarios internos, con alta en diálogo y acciones agrupadas en `Gestionar`:

- Crear usuarios.
- Asignar rol base.
- Activar o desactivar usuarios.
- Cambiar rol.
- Restablecer contraseña.
- Limpiar bloqueo de usuario.

[Solo ADMIN] La pantalla `/admin/security` muestra operación básica de seguridad:

- Resumen de eventos recientes.
- Inicios de sesión fallidos.
- Usuarios bloqueados.
- Usuarios con fallos acumulados.
- Eventos de seguridad filtrables.
- Acción de limpiar bloqueo.

[Pendiente de fase posterior] Las capacidades avanzadas de seguridad, monitoreo, identidad externa y sesiones quedan fuera de esta versión.

## Alertas y pendientes

[Disponible] El sistema muestra alertas y pendientes en varias superficies:

- Dashboard: alertas principales y navegación a módulos.
- Mercados: cédulas por vencer o vencidas.
- Donatarias: donaciones no aplicadas o parciales.
- Financieras: oficios vencidos, por vencer o en renovación.
- Federación: gestiones con seguimiento y donaciones parciales/no aplicadas.
- Documentos: completitud, integridad y retención.
- Seguridad: usuarios bloqueados y eventos de seguridad para ADMIN.
- Centro operativo: consolidación transversal por severidad.

[Pendiente de fase posterior] No existen notificaciones externas por correo, WhatsApp u otros canales. Las alertas disponibles son visibles dentro de la aplicación.

## Módulos principales

| Módulo | Ruta principal | Estado de uso |
| --- | --- | --- |
| Dashboard | `/dashboard` | `[Disponible]` |
| Centro operativo | `/operations` | `[En validación]` |
| Mercados | `/markets` | `[Disponible]` |
| Donatarias | `/donatarias` | `[Disponible]` con tabs, KPIs, modales, reporte y vista imprimible |
| Financieras | `/financials` | `[Disponible]` con tabs, ficha operativa, búsqueda de vigente, cadena y modales |
| Federación | `/federation` | `[Disponible]` con tabs y modales contextuales |
| Contactos | `/contacts` | `[Disponible]` con alcance acotado |
| Documentos | `/documents` | `[Disponible]` catálogo compacto |
| Bandeja documental | `/documents/work-queue` | `[Disponible]` |
| Revisión documental | `/documents/review` | `[Solo ADMIN]` |
| Histórico | `/history` | `[Disponible con alcance acotado]` |
| Bitácora | `/bitacora` | `[Disponible con alcance acotado]` |
| Comisiones | `/commissions` | `[Disponible con alcance acotado]` |
| Usuarios | `/admin/users` | `[Solo ADMIN]` alta en diálogo y acciones `Gestionar` |
| Seguridad | `/admin/security` | `[Solo ADMIN]` |
| Catálogos administrativos | `/catalogs/...` | `[Solo ADMIN]` |

## Funciones de fase posterior

[Pendiente de fase posterior] Siguen visibles como no incluidas o sujetas a definición adicional:

- Aprobación formal de líneas de trabajo documentadas.
- Robustecimiento de bitácora, histórico y marca formal de cierre.
- Política documental avanzada: respaldo formal, retención formal, limpieza segura, legal hold formal, OCR, almacenamiento externo y cumplimiento especializado.
- Reportes avanzados, inteligencia de negocio, exportaciones masivas y reportes históricos formales.
- Donatarias: PDF oficial, folio, firma, versionamiento del reporte, checklist documental avanzado y validación legal/fiscal/contable.
- Seguridad avanzada: autenticación multifactor, proveedor externo de identidad, recuperación avanzada de contraseña, monitoreo especializado y sesiones avanzadas.
- Catálogo maestro y normalización fuerte para financieras, instituciones, dependencias y stands.
- Contacts UX compacta.
- Catálogos UX compacta.
- Notificaciones externas por correo, WhatsApp u otros canales.

## Recomendaciones diarias

- Iniciar la jornada revisando `/operations` para identificar prioridades transversales.
- Consultar `/dashboard` para validar alertas principales y comportamiento general de módulos.
- Resolver pendientes desde el módulo de origen cuando el sistema lo permita.
- Registrar avances, observaciones y evidencias en el contexto correcto.
- Usar exportaciones ligeras solo como apoyo operativo.

## Recomendaciones documentales

- Cargar evidencias desde el registro correcto: locatario, aplicación de donación o aplicación de Federación.
- Revisar `/documents/work-queue` para atender pendientes de completitud, integridad o retención.
- No asumir que la presencia de un archivo equivale a suficiencia legal o documental.
- En Donatarias, revisar el semáforo documental y el readiness antes de compartir visualmente el reporte.
- Tratar la retención vencida como señal de revisión, no como autorización automática de borrado.
- Reservar acciones de metadata, hold, archivado y restauración a usuarios `ADMIN`.

## Recomendaciones de seguridad

- Usar cuentas individuales y no compartir credenciales.
- Solicitar rol `ADMIN` solo para usuarios que realmente administren seguridad, usuarios, catálogos, revisión documental o cierres.
- Atender usuarios bloqueados desde `/admin/security` o `/operations` únicamente con criterio administrativo.
- Cambiar contraseñas cuando lo indique la política interna.
- Considerar las capacidades avanzadas de seguridad como fase posterior.

## Recomendaciones para Financieras

- Confirmar financiera, institución o dependencia y lugar o stand antes de registrar un oficio o crédito.
- Revisar si existe un permiso vigente antes de crear uno nuevo.
- Usar la renovación de oficio con cuidado operativo mientras permanece en validación.
- Registrar comisiones desde el crédito correspondiente para conservar trazabilidad.
- Considerar que no existe catálogo maestro de financieras o dependencias en esta versión.
