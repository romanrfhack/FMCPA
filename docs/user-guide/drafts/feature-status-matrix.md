# Matriz de estado funcional para guía rápida FMCPA

## Convención de estado

- `[Disponible]`: existe pantalla/ruta y respaldo funcional en frontend/backend.
- `[Disponible con alcance acotado]`: existe una capacidad útil, pero con alcance explícitamente acotado o reservas documentadas.
- `[En validación]`: existe implementación reciente o track abierto, pendiente de aprobación formal o consolidación.
- `[Pendiente de fase posterior]`: falta una decisión, cobertura o control importante para una operación más exigente; en la guía final se presenta con lenguaje institucional.
- `[Solo ADMIN]`: visible o ejecutable únicamente con permisos administrativos.

Nota de trazabilidad: esta matriz conserva la evidencia técnica y documental que sustenta cada estado. Los elementos que en la versión inicial se marcaron como `Pendiente crítica` se presentan al usuario final como `Pendiente de fase posterior`, `Requiere definición adicional` o `No incluido en esta versión`, según el contexto.

## Matriz por módulo, pantalla y flujo

| Módulo | Pantalla / flujo real | Estado | Justificación breve |
| --- | --- | --- | --- |
| Acceso | `/login` | `[Disponible]` | Pantalla real de login con JWT local; ruta pública protegida por `guestOnlyGuard`. Evidencia: `app.routes.ts:6-12`, `login-page.component.ts`. |
| Cuenta | `/account/password` | `[Disponible]` | Cambio de contraseña autenticado existe en rutas y servicio de auth. Evidencia: `app.routes.ts:22-27`, `auth.service.ts`. |
| Navegación | Shell autenticado y menú por permisos | `[Disponible]` | El menú se filtra por permisos efectivos del usuario. Evidencia: `app.ts:22-55`. |
| Dashboard | `/dashboard` | `[Disponible]` | Requiere `DASHBOARD_READ`; muestra resumen ejecutivo, alertas e histórico reciente. Evidencia: `app.routes.ts:28-35`, `dashboard-page.component.ts`. |
| Centro operativo | `/operations` | `[En validación]` | Implementado con KPIs, bandeja transversal, ventanas temporales, exportación CSV y quick action de desbloqueo; Track 4 sigue pendiente de aprobación formal. Evidencia: `app.routes.ts:36-43`, `operations-page.component.ts`, `current-phase.md`. |
| Centro operativo | Quick action de desbloqueo | `[Solo ADMIN]` | La acción existe solo cuando backend la permite y reutiliza unlock de usuarios; seguridad no se expone a perfiles sin `USERS_ADMIN`. Evidencia: `operations-page.component.ts`, `analytics-track.md`. |
| Mercados | `/markets` listado y filtro | `[Disponible]` | Pantalla real con filtro por estatus y alertas activas. Evidencia: `markets-page.component.ts`. |
| Mercados | Alta de mercado | `[Disponible]` | Formulario real; requiere `MARKETS_WRITE` en UI/backend. Evidencia: `markets-page.component.ts`, `markets.service.ts`. |
| Mercados | Locatarios, cédula digitalizada, descarga | `[Disponible]` | Alta con archivo, consulta y descarga de cédula. Evidencia: `markets-page.component.ts`, `markets.service.ts`. |
| Mercados | Incidencias / mejoras | `[Disponible]` | Registro y listado por mercado con estatus reusable. Evidencia: `markets-page.component.ts`. |
| Mercados | Cierre formal de mercado | `[Solo ADMIN]` | Botón deshabilitado si no hay permiso de cierre formal. Evidencia: `markets-page.component.ts`, `PlatformPermissionCodes.cs:48-53`. |
| Mercados | Alertas de vigencia de cédula | `[Disponible con alcance acotado]` | Existen alertas por vencimiento/vigencia, pero las reglas generales de alertas siguen documentadas como parciales. Evidencia: `markets-page.component.ts`, `risks.md` R-003. |
| Donatarias | `/donatarias` listado y filtro | `[Disponible]` | Pantalla real con donaciones maestras, progreso y alertas. Evidencia: `donatarias-page.component.ts`. |
| Donatarias | Tabs de transparencia | `[Disponible]` | La pantalla está organizada en `Resumen`, `Donaciones`, `Aplicaciones / distribucion`, `Evidencias` y `Reporte de transparencia`. Evidencia: notas Fase 1 y componente Donatarias. |
| Donatarias | KPIs y distribución financiera | `[Disponible]` | Muestra total recibido, total aplicado, saldo pendiente, porcentaje aplicado, aplicaciones, evidencias y distribución por aplicación. Evidencia: notas Fase 2 y componente Donatarias. |
| Donatarias | Semáforo documental agregado | `[Disponible]` | Existe cálculo agregado por donación con `GET /api/donations/{donationId}/documentary-status`; separa presencia mínima de evidencia de suficiencia legal. Evidencia: nota Fase 3. |
| Donatarias | Reporte de transparencia | `[Disponible]` | Existe `GET /api/donations/{donationId}/transparency-report` y tab real con resumen financiero, estados, aplicaciones, evidencias, faltantes, readiness y notas. Evidencia: nota Fase 4. |
| Donatarias | Vista imprimible del reporte | `[Disponible]` | Botón `Imprimir reporte` y CSS print acotado para ocultar navegación, formularios y controles no relevantes. Evidencia: nota Fase 5A. |
| Donatarias | Alta de donación | `[Disponible]` | Formulario de donante, fecha, tipo, monto, referencia, estatus y notas. Evidencia: `donatarias-page.component.ts`. |
| Donatarias | Aplicaciones de donación | `[Disponible]` | Permite registrar beneficiario, responsable, monto aplicado, estatus y comprobación. Evidencia: `donatarias-page.component.ts`. |
| Donatarias | Evidencias por aplicación | `[Disponible]` | Permite cargar y descargar evidencias asociadas a una aplicación. Evidencia: `donatarias-page.component.ts`, `donations.service.ts`. |
| Donatarias | Cierre formal | `[Solo ADMIN]` | Cierre formal visible/deshabilitado según permisos. Evidencia: `donatarias-page.component.ts`. |
| Donatarias | Readiness de presentación | `[Disponible con alcance acotado]` | El reporte muestra `READY`, `PARTIAL` o `NOT_READY` como criterio operativo preliminar; no equivale a aprobación legal/fiscal/contable. Evidencia: nota Fase 4 y Fase 5A. |
| Donatarias | Validación legal/fiscal/contable | `[Pendiente de fase posterior]` | La evidencia mínima no sustituye revisión legal, fiscal o contable; no existe aprobación documental humana ni dictamen formal. Evidencia: notas Fase 3-5A. |
| Donatarias | Checklist documental avanzado | `[Pendiente de fase posterior]` | No existe checklist persistido por tipo de evidencia, revisión humana ni criterios avanzados de suficiencia. Evidencia: backlog Donatarias Transparencia. |
| Donatarias | Exportación CSV específica de Donatarias | `[Pendiente de fase posterior]` | CSV específico del reporte de Donatarias queda fuera de Fase 5A. Evidencia: backlog Donatarias Transparencia. |
| Donatarias | PDF oficial / folio / firma | `[Pendiente de fase posterior]` | No hay PDF backend, folio oficial, firma, snapshot ni versionamiento del reporte. Evidencia: nota Fase 5A. |
| Donatarias | Catálogo formal de donantes | `[Pendiente de fase posterior]` | El donante se captura como dato de la donación; no existe catálogo formal dedicado. Evidencia: backlog Donatarias Transparencia. |
| Financieras | `/financials` listado y filtro | `[Disponible]` | Pantalla real de oficios/autorizaciones, alertas, créditos y comisiones. Evidencia: `financials-page.component.ts`. |
| Financieras | Alta de oficio/autorización | `[Disponible]` | Formulario real con financiera, institución, lugar, horario, vigencia, estatus, términos y notas. Evidencia: `financials-page.component.ts`, `financials.service.ts`. |
| Financieras | Créditos por oficio | `[Disponible]` | Alta y listado de créditos individuales, con contacto de promotor/beneficiario. Evidencia: `financials-page.component.ts`. |
| Financieras | Comisiones por crédito | `[Disponible]` | Registro por tipo de comisión, destinatario, base y monto. Evidencia: `financials-page.component.ts`. |
| Financieras | Renovación de oficio y cadena | `[En validación]` | Implementado técnicamente, pero Track 5 está abierto y pendiente de aprobación formal; no es versionado contractual completo. Evidencia: `current-phase.md`, `post-mvp-backlog.md:45-53`. |
| Financieras | Captura contextual de crédito | `[En validación]` | Implementada con resolución de permiso vigente y reutilización del alta existente; pendiente de aprobación formal. Evidencia: `financials-page.component.ts`, `current-phase.md`. |
| Financieras | Unicidad operativa de permiso vigente | `[Disponible con alcance acotado]` | Existe validación de aplicación, pero no constraint SQL persistido y hay riesgo de carrera concurrente. Evidencia: `post-mvp-backlog.md:48`, `risks.md` R-093. |
| Financieras | Catálogo maestro de financieras/dependencias | `[Pendiente de fase posterior]` | No existe catálogo maestro; la normalización es básica y puede no reconocer alias o abreviaturas. Evidencia: `risks.md` R-094/R-095. |
| Federación | `/federation` gestiones | `[Disponible]` | Permite registrar gestiones, participantes, filtrar por estatus y consultar alertas. Evidencia: `federation-page.component.ts`. |
| Federación | Donaciones de federación | `[Disponible]` | Permite donaciones maestras, aplicaciones, comisiones y evidencias. Evidencia: `federation-page.component.ts`. |
| Federación | Cierres formales | `[Solo ADMIN]` | Cierre de gestión y donación condicionado a permiso formal. Evidencia: `federation-page.component.ts`. |
| Federación | Reglas operativas finas de evidencias/comisiones | `[Disponible con alcance acotado]` | El repositorio documenta riesgo abierto de precisión operativa en Federación. Evidencia: `risks.md` R-004/R-017. |
| Contactos | `/contacts` | `[Disponible]` | Catálogo compartido de contactos con alta mínima y listado. Evidencia: `contacts-page.component.ts`. |
| Contactos | Escritura de contactos | `[Disponible]` | El formulario se deshabilita sin `CONTACTS_WRITE`; `READONLY` solo consulta. Evidencia: `contacts-page.component.ts`, `auth.service.ts`. |
| Catálogos | `/catalogs/commission-types` | `[Solo ADMIN]` | Requiere `CATALOGS_ADMIN`; permite alta mínima y listado. Evidencia: `app.routes.ts:132-139`, `commission-types-page.component.ts`. |
| Catálogos | `/catalogs/evidence-types` | `[Solo ADMIN]` | Requiere `CATALOGS_ADMIN`; permite alta mínima y listado. Evidencia: `app.routes.ts:140-147`. |
| Catálogos | `/catalogs/module-statuses` | `[Solo ADMIN]` | Requiere `CATALOGS_ADMIN`; permite estatus por módulo con flags de cierre/alerta. Evidencia: `app.routes.ts:148-155`. |
| Documentos | `/documents` catálogo documental | `[Disponible]` | Listado, filtros, detalle, descarga, resumen ejecutivo y exportación CSV. Evidencia: `documents-page.component.ts`, `document-catalog.service.ts`. |
| Documentos | Pendientes documentales | `[Disponible]` | Lista entidades incompletas y navega al origen. Evidencia: `documents-page.component.ts`, `document-management-track.md`. |
| Documentos | Bandeja documental `/documents/work-queue` | `[Disponible]` | Filtra por módulo, tipo y severidad; exporta CSV. Evidencia: `documents-work-queue-page.component.ts`. |
| Documentos | Revisión de retención `/documents/review` | `[Solo ADMIN]` | Requiere `USERS_ADMIN`; permite marcar revisado, diferir y exportar. Evidencia: `app.routes.ts:92-99`, `documents-review-page.component.ts`. |
| Documentos | Metadata, override, hold, archivo/restauración | `[Solo ADMIN]` | Acciones administrativas visibles solo con `USERS_ADMIN`. Evidencia: `documents-page.component.ts:609-706`, `documents-page.component.ts:1130`. |
| Documentos | Compliance, legal hold, OCR, backup, borrado automático | `[Pendiente de fase posterior]` | Están fuera de alcance explícito; la retención actual es señal operativa, no cumplimiento formal. Evidencia: `document-management-track.md`, `risks.md` R-068/R-070/R-082. |
| Histórico | `/history` | `[Disponible con alcance acotado]` | Existe consulta de cerrados, pero hay reservas sobre timestamp formal y consistencia histórica. Evidencia: `history-page.component.ts`, `post-mvp-backlog.md:17-18`, `risks.md` R-021. |
| Bitácora | `/bitacora` | `[Disponible con alcance acotado]` | Existe bitácora transversal, pero no cubre todo evento histórico ni trazabilidad forense completa. Evidencia: `bitacora-page.component.ts`, `post-mvp-backlog.md:16`, `risks.md` R-020/R-023. |
| Comisiones | `/commissions` | `[Disponible con alcance acotado]` | Vista operativa consolidada real para Financieras y Federación; no equivale a capa contable/BI definitiva. Evidencia: `commissions-page.component.ts`, `mvp-known-limitations.md`. |
| Seguridad | `/admin/users` | `[Solo ADMIN]` | Administración mínima de usuarios internos con alta, rol, activación, reset y lockout. Evidencia: `users-admin-page.component.ts`, `app.routes.ts:116-123`. |
| Seguridad | `/admin/security` | `[Solo ADMIN]` | Observabilidad mínima de eventos SECURITY y usuarios bloqueados. Evidencia: `security-admin-page.component.ts`, `app.routes.ts:124-131`. |
| Seguridad | Roles `ADMIN`, `OPERATOR`, `READONLY` | `[Disponible]` | Matriz de permisos derivada por rol existe en backend y frontend. Evidencia: `PlatformPermissionCodes.cs:26-53`, `auth.service.ts`. |
| Seguridad | MFA, IdP externo, forgot password, sesiones avanzadas, SIEM | `[Pendiente de fase posterior]` | Están explícitamente fuera del alcance actual de seguridad avanzada. Evidencia: `security-track.md`, `risks.md` R-037/R-042/R-054/R-059. |
| Reportes | CSV ligero de documentos y operaciones | `[Disponible]` | Implementado para catálogos/bandejas documentales y operations, con límite paginado. Evidencia: `document-catalog.service.ts`, `operations.service.ts`, `analytics-track.md`. |
| Reportes | BI, analítica avanzada, exportación masiva, XLSX formal | `[Pendiente de fase posterior]` | No existe; el track de analytics mantiene BI pesada fuera de alcance. Evidencia: `analytics-track.md`, `post-mvp-backlog.md:26-27`. |
| Notificaciones | Correo, WhatsApp u otros canales | `[Pendiente de fase posterior]` | No existen notificaciones reales en el alcance actual. Evidencia: `mvp-release-note.md`, `mvp-known-limitations.md`. |
| Aprobación formal | MVP y tracks post-MVP | `[Pendiente de fase posterior]` | El MVP está cerrado con reservas; múltiples tracks están entregados o iniciados, pero pendientes de aprobación formal. Evidencia: `current-phase.md`, `backlog.md`, `post-mvp-backlog.md`. |

## Lectura rápida por rol

| Rol | Alcance visible/operativo |
| --- | --- |
| `READONLY` | Consulta dashboard, centro operativo, histórico, bitácora, comisiones, documentos permitidos, contactos y módulos de negocio. No crea ni cierra. |
| `OPERATOR` | Incluye lectura y escrituras funcionales normales en contactos, mercados, donatarias, financieras y federación. No administra usuarios, catálogos ni cierres formales. |
| `ADMIN` | Incluye escrituras funcionales, administración de catálogos, usuarios, seguridad, revisión documental y cierres formales. |
