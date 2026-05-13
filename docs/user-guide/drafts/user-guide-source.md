# FMCPA Platform

## Guía rápida de operación

Contenido base preparado para revisión y conversión posterior a PDF/DOCX. Esta guía se basa en el estado real del repositorio al 2026-05-13.

## Convención de estados

- `[Disponible]`: la función existe en el sistema y cuenta con pantalla o flujo operativo.
- `[Disponible con alcance acotado]`: la función existe, pero su alcance está acotado o tiene reservas documentadas.
- `[En validación]`: la función está implementada o iniciada como track reciente, pero todavía requiere aprobación formal o consolidación.
- `[Pendiente de fase posterior]`: falta una decisión, control o capacidad importante para una operación más exigente.
- `[Solo ADMIN]`: la función requiere permisos administrativos.

## Objetivo del sistema

[Disponible] FMCPA Platform centraliza la operación de proyectos y frentes relacionados con Mercados, Donatarias, Financieras y Federación. También integra consulta documental, alertas, bitácora, histórico, comisiones, centro operativo y administración mínima de seguridad.

[Disponible con alcance acotado] El producto conserva el estado documental de MVP cerrado con reservas. En el repositorio ya existen mejoras post-MVP importantes, especialmente en seguridad, documentos, centro operativo y Financieras, pero varias secciones siguen marcadas como pendientes de aprobación formal.

## Acceso e inicio de sesión

[Disponible] El acceso se realiza desde `/login`. El sistema usa autenticación local con mecanismo de sesión interno y protege las rutas principales.

[Disponible] Al iniciar sesión, el sistema muestra una tarjeta de sesión con nombre visible, usuario y rol. Desde ahí se puede cerrar sesión y acceder al cambio de contraseña en `/account/password`.

[Disponible] Los roles visibles en el repositorio son:

- `READONLY`: consulta información, sin crear registros ni ejecutar cierres.
- `OPERATOR`: consulta y ejecuta escrituras funcionales normales.
- `ADMIN`: puede operar módulos, administrar catálogos, usuarios, seguridad, documentos administrativos y cierres formales.

[Disponible con alcance acotado] La seguridad actual es una base mínima operativa. No incluye MFA, recuperación avanzada de contraseña, proveedor externo de identidad, sesiones avanzadas o monitoreo avanzado de seguridad.

## Navegación general

[Disponible] La navegación principal se muestra como menú lateral dentro del shell autenticado. El menú se filtra por permisos, por lo que cada usuario ve solo las secciones permitidas.

Pantallas reales detectadas:

- Dashboard: `/dashboard`
- Operaciones: `/operations`
- Histórico: `/history`
- Comisiones: `/commissions`
- Bitácora: `/bitacora`
- Documentos: `/documents`
- Bandeja documental: `/documents/work-queue`
- Revisión documental: `/documents/review`
- Contactos: `/contacts`
- Usuarios: `/admin/users`
- Seguridad: `/admin/security`
- Tipos de comisión: `/catalogs/commission-types`
- Tipos de evidencia: `/catalogs/evidence-types`
- Estatus por módulo: `/catalogs/module-statuses`
- Mercados: `/markets`
- Donatarias: `/donatarias`
- Financieras: `/financials`
- Federación: `/federation`

## Centro operativo

[En validación] La pantalla `/operations` consolida señales de negocio, documentos y seguridad según los permisos del usuario. Incluye KPIs, enlaces a contexto, filtros por severidad, ventanas temporales y exportación CSV ligera.

El usuario puede:

- Revisar prioridades operativas agrupadas.
- Cambiar la ventana temporal: hoy, últimos 7 días, próximos 30 días o todo.
- Filtrar la bandeja por severidad `HIGH`, `MEDIUM` o `LOW`.
- Abrir el contexto de resolución desde cada elemento.
- Exportar resumen y bandeja como CSV ligero.

[Solo ADMIN] La única acción rápida detectada en este centro es el desbloqueo de usuarios bloqueados, cuando el backend lo permite.

[Disponible con alcance acotado] Esta pantalla no es un workflow, no asigna responsables, no maneja SLA y no reemplaza las pantallas fuente.

## Dashboard

[Disponible] La pantalla `/dashboard` muestra un resumen ejecutivo del MVP, tarjetas por módulo, alertas activas principales e histórico reciente.

El usuario puede:

- Consultar conteos y señales de Mercados, Donatarias, Financieras y Federación.
- Ir al módulo de origen desde las tarjetas.
- Revisar alertas activas principales.
- Abrir histórico reciente.

[Disponible con alcance acotado] El dashboard es ejecutivo mínimo; no es un tablero analítico avanzado ni BI.

## Mercados

[Disponible] La pantalla `/markets` permite administrar la operación de mercados, locatarios, incidencias y cédulas digitalizadas.

El usuario puede:

- Filtrar mercados por estatus o por alertas activas.
- Registrar un mercado con nombre, alcaldía, estatus, contacto/secretario general y observaciones.
- Seleccionar un mercado para ver su detalle.
- Registrar locatarios con datos de contacto, giro, número de cédula, vigencia y archivo digitalizado.
- Descargar la cédula digitalizada cuando existe.
- Registrar incidencias o mejoras con tipo, fecha, descripción, avance, estatus, seguimiento y satisfacción final.
- Revisar alertas de cédulas por vencer o vencidas.
- Consultar documentos relacionados del locatario.

[Solo ADMIN] El cierre formal de un mercado requiere permisos administrativos de cierre.

[Disponible con alcance acotado] Las alertas de vigencia existen y son útiles, pero el repositorio mantiene riesgos abiertos sobre reglas de alertas más detalladas.

## Donatarias

[Disponible] La pantalla `/donatarias` administra la transparencia del recurso donado. La vista está organizada en tabs para separar consulta, captura, evidencias y reporte.

Tabs principales:

- `Resumen`: KPIs, semáforos, estado documental agregado y criterio operativo de presentación.
- `Donaciones`: listado, filtros y captura de donaciones.
- `Aplicaciones / distribución`: distribución financiera por beneficiario o destino.
- `Evidencias`: evidencias agrupadas por aplicación y documentos relacionados.
- `Reporte de transparencia`: vista consolidada para consulta e impresión.

KPIs visibles de la donación seleccionada:

- Total recibido.
- Total aplicado.
- Saldo pendiente.
- Porcentaje aplicado.
- Número de aplicaciones.
- Evidencias registradas.

El usuario puede:

- Filtrar donaciones por estatus o alertas activas.
- Registrar una donación con donante, fecha, tipo, monto base, referencia, estatus y observaciones.
- Consultar total recibido, total aplicado, saldo pendiente, porcentaje aplicado, aplicaciones y evidencias.
- Revisar la distribución por aplicación con beneficiario, fecha, responsable, monto, porcentaje del total recibido, saldo restante, estatus y detalle de comprobación.
- Registrar aplicaciones con beneficiario, fecha, responsable, monto aplicado, estatus, comprobación y datos de cierre.
- Cargar evidencia por aplicación.
- Descargar evidencia existente.
- Revisar alertas de donaciones no aplicadas o parcialmente aplicadas.
- Consultar documentos relacionados de la aplicación.
- Revisar semáforos financiero, documental y operativo.
- Consultar el reporte de transparencia calculado por donación.
- Usar la vista imprimible del reporte desde el tab `Reporte de transparencia`.

[Disponible] Los semáforos ayudan a separar tres lecturas:

- Financiero: muestra si el recurso está sin aplicar, parcialmente aplicado o aplicado.
- Documental: muestra si hay evidencia mínima pendiente o completa.
- Operativo: muestra si la donación está abierta o cerrada operativamente.

[Disponible] El reporte de transparencia muestra donante, referencia, fecha de donación, fecha de corte/generación, tipo de donación, totales financieros, estado financiero, estado documental, estado operativo, aplicaciones, evidencias, faltantes y notas de alcance.

[Disponible] El criterio de readiness del reporte puede mostrarse como:

- `READY`: sin saldo pendiente y con evidencia mínima registrada.
- `PARTIAL`: existe información útil para presentar, pero quedan pendientes financieros o documentales.
- `NOT_READY`: no hay aplicaciones o falta información mínima para una presentación operativa.

[Disponible] La vista imprimible usa los datos actuales del reporte de transparencia y oculta navegación, formularios y botones de captura para facilitar una revisión visual limpia. No genera PDF oficial desde backend.

[Solo ADMIN] El cierre formal de una donación requiere permiso administrativo.

[Disponible con alcance acotado] La evidencia mínima registrada no sustituye revisión legal, fiscal o contable. El reporte de transparencia es una vista operativa y no debe presentarse como documento oficial legal, fiscal o contable.

[Pendiente de fase posterior] Quedan fuera de esta versión: CSV específico de Donatarias, PDF oficial, folio, firma, versionamiento del reporte, validación legal/fiscal/contable, checklist documental avanzado y catálogo formal de donantes.

## Financieras

[Disponible] La pantalla `/financials` administra oficios o autorizaciones, vigencias, créditos y comisiones por crédito.

El usuario puede:

- Filtrar oficios por estatus o alertas activas.
- Registrar un oficio/autorización con financiera, institución/dependencia, lugar o stand, horario, vigencia, estatus, términos y observaciones.
- Buscar el permiso vigente por financiera, institución/dependencia y lugar/stand.
- Registrar créditos individuales con promotor, beneficiario, datos de contacto, fecha de autorización, monto y notas.
- Registrar comisiones por crédito con tipo, categoría de destinatario, contacto, destinatario, base y monto.
- Consultar alertas de oficios por vencer, vencidos o en renovación.

[En validación] La renovación de oficios está implementada como renovación mínima. El sistema conserva el oficio anterior como histórico, crea uno nuevo vigente y muestra historial/cadena.

[En validación] La captura contextual de crédito permite capturar desde un contexto operativo y resolver el oficio vigente antes de abrir el formulario de crédito.

[Disponible con alcance acotado] La unicidad de permiso vigente se valida en la aplicación, no como restricción SQL persistida. Existe riesgo residual de concurrencia si dos solicitudes muy cercanas intentan crear permisos equivalentes.

[Pendiente de fase posterior] No existe catálogo maestro de financieras, instituciones o stands. La normalización es básica y no resuelve alias, abreviaturas o sinónimos.

[Solo ADMIN] El cierre formal de un oficio requiere permiso administrativo.

## Federación

[Disponible] La pantalla `/federation` contiene dos frentes: gestiones de Federación y donaciones de Federación.

En gestiones, el usuario puede:

- Filtrar por estatus o alertas activas.
- Registrar gestiones con tipo, fecha, contraparte/institución, estatus, objetivo y observaciones.
- Agregar participantes internos o externos desde contactos compartidos.
- Consultar participantes y alertas de seguimiento.

En donaciones de Federación, el usuario puede:

- Registrar donaciones con donante, fecha, tipo, monto base, referencia, estatus y notas.
- Registrar aplicaciones con beneficiario/destino, fecha, monto, estatus, comprobación y datos de cierre.
- Registrar comisiones por aplicación.
- Cargar y descargar evidencias.
- Consultar documentos relacionados de la aplicación.

[Solo ADMIN] Los cierres formales de gestiones y donaciones requieren permisos administrativos.

[Disponible con alcance acotado] El repositorio mantiene riesgos abiertos sobre precisión operativa de evidencias, comisiones y aplicaciones en Federación.

## Contactos

[Disponible] La pantalla `/contacts` administra contactos compartidos reutilizables entre módulos.

El usuario puede:

- Registrar contactos con nombre, tipo, organización/dependencia, cargo, celular, WhatsApp, correo y notas.
- Consultar contactos registrados.

[Disponible con alcance acotado] El catálogo de contactos es mínimo; no reemplaza una gestión avanzada de identidad, duplicados o reglas formales de relación.

## Catálogos

[Solo ADMIN] Las pantallas de catálogos administrativos son:

- `/catalogs/commission-types`: tipos de comisión.
- `/catalogs/evidence-types`: tipos de evidencia.
- `/catalogs/module-statuses`: estatus por módulo, incluyendo banderas de cierre y alertas.

El usuario ADMIN puede dar de alta registros mínimos y actualizar la vista. No se detectó edición avanzada ni eliminación desde estas pantallas.

## Documentos

[Disponible] La pantalla `/documents` funciona como catálogo documental transversal sobre `StoredDocument`.

El usuario puede:

- Consultar resumen ejecutivo documental.
- Filtrar por módulo, área documental, integridad, estado operativo, clase, retención, estado y entidad.
- Buscar documentos y ver detalle.
- Descargar documentos autorizados.
- Exportar CSV ligero del catálogo.
- Consultar pendientes documentales.
- Abrir el origen o resolver en origen cuando tiene permiso de escritura del módulo.
- Ver historia documental del documento seleccionado.

[Disponible] La pantalla `/documents/work-queue` muestra una bandeja documental con pendientes de completitud, integridad y revisión de retención. Permite filtrar por módulo, tipo, severidad y exportar CSV.

[Solo ADMIN] La pantalla `/documents/review` permite revisar documentos por retención: marcar revisado, diferir revisión, descargar y exportar CSV.

[Solo ADMIN] En `/documents`, ADMIN puede editar metadata documental, aplicar o limpiar override de retención, aplicar o limpiar hold administrativo, archivar y restaurar documentos.

[Disponible con alcance acotado] La retención documental actual es operativa. `EXPIRED_RETENTION` significa que requiere revisión; no autoriza borrado automático ni equivale a cumplimiento legal.

[Pendiente de fase posterior] No hay backup real, storage externo, OCR, clasificación automática avanzada, legal hold formal, borrado físico seguro ni cumplimiento documental/normativo avanzado.

## Histórico

[Disponible con alcance acotado] La pantalla `/history` permite consultar registros cerrados o archivados, filtrar por módulo y buscar por título, subtítulo o referencia.

[Pendiente de fase posterior] El histórico mantiene reservas: algunas lecturas dependen del último timestamp conocido y no siempre de una marca formal de cierre.

## Bitácora

[Disponible con alcance acotado] La pantalla `/bitacora` permite consultar eventos transversales con filtros por módulo, entidad, texto y fechas. También muestra integridad documental mínima.

[Pendiente de fase posterior] La bitácora no equivale a auditoría forense completa. No reconstruye todo evento histórico previo ni cubre de forma total cambios finos, ediciones y eventos de cierre de etapas anteriores.

## Comisiones

[Disponible con alcance acotado] La pantalla `/commissions` consolida comisiones operativas de Financieras y Federación. Permite filtrar por módulo origen, tipo de comisión, categoría de destinatario, fechas y búsqueda.

[Pendiente de fase posterior] Esta vista no debe interpretarse como contabilidad definitiva, BI ni reporte financiero formal.

## Seguridad y usuarios

[Solo ADMIN] La pantalla `/admin/users` permite administración mínima de usuarios internos:

- Crear usuarios.
- Asignar rol base.
- Activar o desactivar.
- Cambiar rol.
- Resetear contraseña.
- Limpiar lockout.

[Solo ADMIN] La pantalla `/admin/security` muestra operación mínima de seguridad:

- Resumen de eventos recientes.
- Logins fallidos.
- Usuarios bloqueados.
- Usuarios con fallos acumulados.
- Eventos SECURITY filtrables.
- Acción de limpiar lockout.

[Disponible] La matriz de permisos vigente es:

| Rol | Puede hacer |
| --- | --- |
| `READONLY` | Leer dashboard, histórico, contactos, mercados, donatarias, financieras, federación y catálogos de lectura. |
| `OPERATOR` | Todo lo anterior y escrituras funcionales normales en contactos y módulos de negocio. |
| `ADMIN` | Todo lo anterior, más catálogos administrativos, usuarios, seguridad, revisión documental y cierres formales. |

[Pendiente de fase posterior] No hay MFA, recuperación avanzada de contraseña, proveedor externo de identidad, sesiones avanzadas, revocación multi-dispositivo avanzada ni monitoreo avanzado de seguridad.

## Alertas y pendientes

[Disponible] El sistema muestra alertas y pendientes en varias superficies:

- Dashboard: alertas principales y navegación a módulos.
- Mercados: cédulas por vencer o vencidas.
- Donatarias: donaciones no aplicadas o parciales.
- Financieras: oficios vencidos, por vencer o en renovación.
- Federación: gestiones con seguimiento y donaciones parciales/no aplicadas.
- Documentos: completitud, integridad y retención.
- Seguridad: usuarios bloqueados y eventos SECURITY para ADMIN.
- Centro operativo: consolidación transversal por severidad.

[Pendiente de fase posterior] No existen notificaciones reales por correo, WhatsApp u otros canales. Las alertas son visibles dentro de la aplicación.

## Funcionalidades en validación o de fase posterior

[Pendiente de fase posterior] Aprobación formal: el MVP y varios tracks post-MVP aparecen como entregados, iniciados o validados localmente, pero pendientes de aprobación formal.

[Pendiente de fase posterior] Bitácora e histórico: falta robustecer eventos mínimos obligatorios, cambios finos de estatus y marca formal de cierre consistente.

[Pendiente de fase posterior] Política documental: falta formalizar respaldo, retención, limpieza segura, cumplimiento avanzado, legal hold formal, OCR y storage externo.

[Pendiente de fase posterior] Analítica y reportes: no hay BI, analítica predictiva, reportes históricos formales ni exportaciones masivas.

[Pendiente de fase posterior] Seguridad avanzada: no hay MFA, proveedor externo de identidad, recuperación avanzada de contraseña, monitoreo avanzado de seguridad ni sesiones avanzadas.

[Pendiente de fase posterior] Financieras: no existe catálogo maestro ni normalización fuerte de financieras, instituciones, dependencias y stands.

[Pendiente de fase posterior] Notificaciones: no hay envíos externos de alertas.

## Recomendaciones de uso

- Iniciar la jornada revisando `/operations` y `/dashboard`.
- Atender pendientes documentales desde `/documents/work-queue` o desde el módulo origen.
- Usar cierres formales únicamente con criterio administrativo y usuario `ADMIN`.
- No usar exportaciones CSV ligeras como reporte histórico oficial o respaldo documental.
- Registrar evidencias desde el contexto correcto: locatario, aplicación de donación o aplicación de Federación.
- En Financieras, confirmar el contexto operativo antes de capturar crédito o crear/renovar oficio.
- Para asuntos legales, cumplimiento documental, auditoría forense o reportes formales, tratar las vistas actuales como base operativa y no como cierre definitivo.
