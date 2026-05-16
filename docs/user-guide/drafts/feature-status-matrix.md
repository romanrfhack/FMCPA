# Matriz de estado funcional para guía rápida FMCPA

## Convención de estado

- `[Disponible]`: existe pantalla/ruta y respaldo funcional en frontend/backend.
- `[Disponible con alcance acotado]`: existe una capacidad útil, pero con alcance explícitamente acotado o reservas documentadas.
- `[En validación]`: existe implementación reciente o línea de trabajo abierta, pendiente de aprobación formal o consolidación.
- `[Pendiente de fase posterior]`: falta una decisión, cobertura o control importante para una operación más exigente; en la guía final se presenta con lenguaje institucional.
- `[Solo ADMIN]`: visible o ejecutable únicamente con permisos administrativos.

Nota de trazabilidad: esta matriz conserva la evidencia técnica y documental que sustenta cada estado. Los elementos que en la versión inicial se marcaron como `Pendiente crítica` se presentan al usuario final como `Pendiente de fase posterior`, `Requiere definición adicional` o `No incluido en esta versión`, según el contexto.

## Matriz por módulo, pantalla y flujo

| Módulo | Pantalla / flujo real | Estado | Justificación breve |
| --- | --- | --- | --- |
| Acceso | Login institucional | `[Disponible]` | Pantalla de acceso con logo FMCPA, lenguaje institucional y sin copy técnico visible. Evidencia: nota de rediseño de login y validación global. |
| Cuenta | Menú de usuario compacto | `[Disponible]` | El header autenticado muestra logo FMCPA, nombre de plataforma y menú compacto con cambio de contraseña y cierre de sesión. Evidencia: notas de header/shell y validación global. |
| Cuenta | `/account/password` | `[Disponible]` | Cambio de contraseña disponible desde el menú de usuario. Evidencia: `app.routes.ts`, `auth.service.ts`. |
| Navegación | Navegación agrupada por permisos | `[Disponible]` | La navegación se organiza en `Inicio`, `Operación`, `Control` y `Administración`; el menú se filtra por permisos efectivos y oculta grupos vacíos. Evidencia: nota de rediseño de navegación del shell. |
| UX global | Rediseño visual global | `[Disponible]` | Login, header, navegación, Donatarias, Documents, Admin Users, Financials, Federation y Markets fueron validados en `390px`, `768px` y `1366px`. Evidencia: `ui-global-redesign-validation-note.md`. |
| UX global | Limpieza CSS/budget | `[Disponible]` | Utilidades `fmcpa-*` consolidadas y `npm run build` sin warnings de CSS budget. Evidencia: `ui-css-budget-cleanup-implementation-note.md`. |
| Dashboard | `/dashboard` | `[Disponible]` | Requiere `DASHBOARD_READ`; muestra resumen ejecutivo, alertas e histórico reciente. Evidencia: `app.routes.ts:28-35`, `dashboard-page.component.ts`. |
| Centro operativo | `/operations` | `[En validación]` | Implementado con KPIs, bandeja transversal, ventanas temporales, exportación CSV y acción rápida de desbloqueo; la línea de operaciones sigue pendiente de aprobación formal. Evidencia: `app.routes.ts`, `operations-page.component.ts`, `current-phase.md`. |
| Centro operativo | Quick action de desbloqueo | `[Solo ADMIN]` | La acción existe solo cuando backend la permite y reutiliza desbloqueo de usuarios; seguridad no se expone a perfiles sin `USERS_ADMIN`. Evidencia: `operations-page.component.ts` y notas de operaciones. |
| Mercados | `/markets` listado y filtro | `[Disponible]` | Pantalla real con filtro por estatus y alertas activas. Evidencia: `markets-page.component.ts`. |
| Mercados | Alta de mercado | `[Disponible]` | Formulario real; requiere `MARKETS_WRITE` en UI/backend. Evidencia: `markets-page.component.ts`, `markets.service.ts`. |
| Mercados | Locatarios, cédula digitalizada, descarga | `[Disponible]` | Alta con archivo, consulta y descarga de cédula. Evidencia: `markets-page.component.ts`, `markets.service.ts`. |
| Mercados | Incidencias / mejoras | `[Disponible]` | Registro y listado por mercado con estatus reusable. Evidencia: `markets-page.component.ts`. |
| Mercados | Cierre formal de mercado | `[Solo ADMIN]` | Botón deshabilitado si no hay permiso de cierre formal. Evidencia: `markets-page.component.ts`, `PlatformPermissionCodes.cs:48-53`. |
| Mercados | Alertas de vigencia de cédula | `[Disponible con alcance acotado]` | Existen alertas por vencimiento/vigencia, pero las reglas generales de alertas siguen documentadas como parciales. Evidencia: `markets-page.component.ts`, `risks.md` R-003. |
| Donatarias | Donatarias Transparencia | `[Disponible]` | Pantalla real con tabs, KPIs, distribución, evidencias, reporte de transparencia, vista imprimible y modales contextuales de captura. Evidencia: notas Fases 1-5A, notas de modales y validación global. |
| Donatarias | Tabs de transparencia | `[Disponible]` | La pantalla está organizada en `Resumen`, `Donaciones`, `Aplicaciones / distribucion`, `Evidencias` y `Reporte de transparencia`. Evidencia: notas Fase 1 y componente Donatarias. |
| Donatarias | KPIs y distribución financiera | `[Disponible]` | Muestra total recibido, total aplicado, saldo pendiente, porcentaje aplicado, aplicaciones, evidencias y distribución por aplicación. Evidencia: notas Fase 2 y componente Donatarias. |
| Donatarias | Semáforo documental agregado | `[Disponible]` | Existe cálculo agregado por donación con `GET /api/donations/{donationId}/documentary-status`; separa presencia mínima de evidencia de suficiencia legal. Evidencia: nota Fase 3. |
| Donatarias | Reporte de transparencia | `[Disponible]` | Existe `GET /api/donations/{donationId}/transparency-report` y tab real con resumen financiero, estados, aplicaciones, evidencias, faltantes, readiness y notas. Evidencia: nota Fase 4. |
| Donatarias | Vista imprimible del reporte | `[Disponible]` | Botón `Imprimir reporte` y CSS print acotado para ocultar navegación, formularios y controles no relevantes. Evidencia: nota Fase 5A. |
| Donatarias | Alta de donación | `[Disponible]` | Alta en modal contextual con donante, fecha, tipo, monto, referencia, estatus y notas. Evidencia: nota de modal de donación. |
| Donatarias | Aplicaciones de donación | `[Disponible]` | Registro en modal contextual de beneficiario, responsable, monto aplicado, estatus y comprobación. Evidencia: nota de modal de aplicación. |
| Donatarias | Evidencias por aplicación | `[Disponible]` | Carga en modal contextual y descarga de evidencias asociadas a una aplicación. Evidencia: nota de modal de evidencia. |
| Donatarias | Cierre formal | `[Solo ADMIN]` | Cierre formal visible/deshabilitado según permisos. Evidencia: `donatarias-page.component.ts`. |
| Donatarias | Readiness de presentación | `[Disponible con alcance acotado]` | El reporte muestra `READY`, `PARTIAL` o `NOT_READY` como criterio operativo preliminar; no equivale a aprobación legal/fiscal/contable. Evidencia: nota Fase 4 y Fase 5A. |
| Donatarias | Validación legal/fiscal/contable | `[Pendiente de fase posterior]` | La evidencia mínima no sustituye revisión legal, fiscal o contable; no existe aprobación documental humana ni dictamen formal. Evidencia: notas Fase 3-5A. |
| Donatarias | Checklist documental avanzado | `[Pendiente de fase posterior]` | No existe checklist persistido por tipo de evidencia, revisión humana ni criterios avanzados de suficiencia. Evidencia: backlog Donatarias Transparencia. |
| Donatarias | Exportación CSV específica de Donatarias | `[Pendiente de fase posterior]` | CSV específico del reporte de Donatarias queda fuera de Fase 5A. Evidencia: backlog Donatarias Transparencia. |
| Donatarias | PDF oficial / folio / firma | `[Pendiente de fase posterior]` | No hay PDF backend, folio oficial, firma, snapshot ni versionamiento del reporte. Evidencia: nota Fase 5A. |
| Donatarias | Catálogo formal de donantes | `[Pendiente de fase posterior]` | El donante se captura como dato de la donación; no existe catálogo formal dedicado. Evidencia: backlog Donatarias Transparencia. |
| Financieras | `/financials` tabs y ficha operativa | `[Disponible]` | Pantalla reorganizada con tabs, ficha operativa, listado compacto, búsqueda de vigente, cadena, créditos, comisiones y modales. Evidencia: `ui-financials-density-implementation-note.md`. |
| Financieras | Alta de oficio/autorización | `[Disponible]` | Alta en modal contextual con financiera, institución, lugar, horario, vigencia, estatus, términos y notas. Evidencia: `ui-financials-density-implementation-note.md`. |
| Financieras | Créditos por oficio | `[Disponible]` | Alta y listado de créditos individuales, con contacto de promotor/beneficiario. Evidencia: `financials-page.component.ts`. |
| Financieras | Comisiones por crédito | `[Disponible]` | Registro por tipo de comisión, destinatario, base y monto. Evidencia: `financials-page.component.ts`. |
| Financieras | Renovación de oficio y cadena | `[En validación]` | Implementado técnicamente, pero la línea funcional sigue abierta y pendiente de aprobación formal; no es versionado contractual completo. Evidencia: `current-phase.md` y backlog post-entrega. |
| Financieras | Captura contextual de crédito | `[En validación]` | Implementada con resolución de permiso vigente y reutilización del alta existente; pendiente de aprobación formal. Evidencia: `financials-page.component.ts`, `current-phase.md`. |
| Financieras | Unicidad operativa de permiso vigente | `[Disponible con alcance acotado]` | Existe validación de aplicación, pero no constraint SQL persistido y hay riesgo de carrera concurrente. Evidencia: backlog post-entrega y `risks.md` R-093. |
| Financieras | Catálogo maestro de financieras/dependencias | `[Pendiente de fase posterior]` | No existe catálogo maestro; la normalización es básica y puede no reconocer alias o abreviaturas. Evidencia: `risks.md` R-094/R-095. |
| Federación | `/federation` tabs y modales | `[Disponible]` | Pantalla reorganizada con tabs, resumen, gestiones, donaciones, aplicaciones/comisiones, evidencias y modales contextuales. Evidencia: notas `ui-federation-density-phase-1` y `phase-2-modals`. |
| Federación | Donaciones de federación | `[Disponible]` | Permite donaciones maestras, aplicaciones, comisiones y evidencias desde flujos contextuales. Evidencia: `federation-page.component.ts` y notas UX. |
| Federación | Cierres formales | `[Solo ADMIN]` | Cierre de gestión y donación condicionado a permiso formal. Evidencia: `federation-page.component.ts`. |
| Federación | Reglas operativas finas de evidencias/comisiones | `[Disponible con alcance acotado]` | El repositorio documenta riesgo abierto de precisión operativa en Federación. Evidencia: `risks.md` R-004/R-017. |
| Contactos | `/contacts` | `[Disponible]` | Catálogo compartido de contactos con alta mínima y listado. Evidencia: `contacts-page.component.ts`. |
| Contactos | Escritura de contactos | `[Disponible]` | El formulario se deshabilita sin `CONTACTS_WRITE`; `READONLY` solo consulta. Evidencia: `contacts-page.component.ts`, `auth.service.ts`. |
| Catálogos | `/catalogs/commission-types` | `[Solo ADMIN]` | Requiere `CATALOGS_ADMIN`; permite alta mínima y listado. Evidencia: `app.routes.ts:132-139`, `commission-types-page.component.ts`. |
| Catálogos | `/catalogs/evidence-types` | `[Solo ADMIN]` | Requiere `CATALOGS_ADMIN`; permite alta mínima y listado. Evidencia: `app.routes.ts:140-147`. |
| Catálogos | `/catalogs/module-statuses` | `[Solo ADMIN]` | Requiere `CATALOGS_ADMIN`; permite estatus por módulo con flags de cierre/alerta. Evidencia: `app.routes.ts:148-155`. |
| Documentos | `/documents` catálogo compacto | `[Disponible]` | Catálogo documental compacto con labels operativos, filtros, detalle, descarga, resumen ejecutivo y CSV ligero. Evidencia: `ui-documents-density-implementation-note.md`. |
| Documentos | Pendientes documentales | `[Disponible]` | Lista entidades incompletas, usa lenguaje operativo y navega al origen. Evidencia: `documents-page.component.ts` y notas documentales. |
| Documentos | Bandeja documental `/documents/work-queue` | `[Disponible]` | Work queue compacta; filtra por módulo, tipo y prioridad; exporta CSV ligero. Evidencia: `documents-work-queue-page.component.ts`. |
| Documentos | Revisión de retención `/documents/review` | `[Solo ADMIN]` | Requiere `USERS_ADMIN`; permite marcar revisado, diferir y exportar. Evidencia: `app.routes.ts:92-99`, `documents-review-page.component.ts`. |
| Documentos | Metadata, override, hold, archivo/restauración | `[Solo ADMIN]` | Acciones administrativas visibles solo con `USERS_ADMIN`. Evidencia: `documents-page.component.ts:609-706`, `documents-page.component.ts:1130`. |
| Documentos | Compliance, legal hold, OCR, backup, borrado automático | `[Pendiente de fase posterior]` | Están fuera de alcance explícito; la retención actual es señal operativa, no cumplimiento formal. Evidencia: notas documentales y `risks.md` R-068/R-070/R-082. |
| Histórico | `/history` | `[Disponible con alcance acotado]` | Existe consulta de cerrados, pero hay reservas sobre timestamp formal y consistencia histórica. Evidencia: `history-page.component.ts`, backlog post-entrega y `risks.md` R-021. |
| Bitácora | `/bitacora` | `[Disponible con alcance acotado]` | Existe bitácora transversal, pero no cubre todo evento histórico ni trazabilidad forense completa. Evidencia: `bitacora-page.component.ts`, backlog post-entrega y `risks.md` R-020/R-023. |
| Comisiones | `/commissions` | `[Disponible con alcance acotado]` | Vista operativa consolidada real para Financieras y Federación; no equivale a capa contable/BI definitiva. Evidencia: `commissions-page.component.ts` y limitaciones conocidas. |
| Seguridad | `/admin/users` | `[Solo ADMIN]` | Administración compacta de usuarios internos; alta en diálogo y acciones agrupadas en `Gestionar`. Evidencia: `ui-admin-users-density-implementation-note.md`. |
| Seguridad | `/admin/security` | `[Solo ADMIN]` | Observabilidad mínima de eventos SECURITY y usuarios bloqueados. Evidencia: `security-admin-page.component.ts`, `app.routes.ts:124-131`. |
| Seguridad | Roles `ADMIN`, `OPERATOR`, `READONLY` | `[Disponible]` | Matriz de permisos derivada por rol existe en backend y frontend. Evidencia: `PlatformPermissionCodes.cs:26-53`, `auth.service.ts`. |
| Seguridad | MFA, IdP externo, forgot password, sesiones avanzadas, SIEM | `[Pendiente de fase posterior]` | Están explícitamente fuera del alcance actual de seguridad avanzada. Evidencia: notas de seguridad y `risks.md` R-037/R-042/R-054/R-059. |
| Reportes | CSV ligero de documentos y operaciones | `[Disponible]` | Implementado para catálogos/bandejas documentales y operations, con límite paginado. Evidencia: `document-catalog.service.ts`, `operations.service.ts` y notas de operaciones. |
| Reportes | BI, analítica avanzada, exportación masiva, XLSX formal | `[Pendiente de fase posterior]` | No existe; la analítica avanzada se mantiene fuera de alcance. Evidencia: notas de operaciones y backlog post-entrega. |
| Notificaciones | Correo, WhatsApp u otros canales | `[Pendiente de fase posterior]` | No existen notificaciones reales en el alcance actual. Evidencia: notas de release y limitaciones conocidas. |
| Contactos | Rediseño UX compacto | `[Pendiente de fase posterior]` | Contactos sigue con alta inline y queda pendiente convertirla a interacción compacta/modal. Evidencia: `ui-redesign-priority-backlog.md`. |
| Catálogos | Rediseño UX compacto | `[Pendiente de fase posterior]` | Catálogos administrativos simples quedan pendientes de homologación visual completa. Evidencia: `ui-redesign-priority-backlog.md`. |
| Aprobación formal | Aceptación formal de líneas de trabajo | `[Pendiente de fase posterior]` | Varias líneas documentadas están entregadas o validadas localmente, pero pendientes de aprobación formal. Evidencia: `current-phase.md` y backlog post-entrega. |

## Lectura rápida por rol

| Rol | Alcance visible/operativo |
| --- | --- |
| `READONLY` | Consulta dashboard, centro operativo, histórico, bitácora, comisiones, documentos permitidos, contactos y módulos de negocio. No crea ni cierra. |
| `OPERATOR` | Incluye lectura y escrituras funcionales normales en contactos, mercados, donatarias, financieras y federación. No administra usuarios, catálogos ni cierres formales. |
| `ADMIN` | Incluye escrituras funcionales, administración de catálogos, usuarios, seguridad, revisión documental y cierres formales. |
