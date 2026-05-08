# Security Authorization Surface Inventory

## Alcance
Inventario minimo de superficies HTTP protegidas para `Track 2`. Sirve como referencia de regresion para detectar endpoints expuestos por error, policies mal asignadas y diferencias no esperadas entre `READONLY`, `OPERATOR` y `ADMIN`.

No define permisos nuevos; documenta la matriz vigente basada en permisos derivados por rol.

## Artefacto operativo del guardrail
La referencia ejecutable para las pruebas automaticas es `docs/05-post-mvp/security-authorization-surface-guardrails.json`.

Este Markdown es el inventario humano de lectura y revision. Cuando se agregue, retire o cambie un endpoint, se deben actualizar juntos:
- la fila humana de este inventario, si cambia una superficie o regla esperada;
- el manifiesto JSON, para que `AuthorizationSurfaceGuardrailTests` pueda validar la superficie real del backend.

La suite falla si un endpoint real queda publico fuera del allowlist, si un endpoint `/api` no tiene regla publica/protegida en el manifiesto o si la policy real no coincide con la esperada.

## Endpoints publicos permitidos
| Superficie | Endpoint | Proteccion esperada | Nota |
| --- | --- | --- | --- |
| Health | `* /health` | Publico | Diagnostico local y operativo minimo; el endpoint de health se expone sin metadata HTTP method en el host de pruebas. |
| Raiz API | `GET /` | Publico | Metadatos no sensibles del servicio. |
| Login | `POST /api/auth/login` | Publico con rate limit | Emite JWT si credenciales y estado de cuenta son validos. |
| OpenAPI local | `GET /openapi/v1.json` | Publico solo en `Development` | Habilitado por entorno, no por policy funcional. |

## Matriz por rol
| Superficie | Endpoints representativos | Policy / permiso esperado | READONLY | OPERATOR | ADMIN |
| --- | --- | --- | --- | --- | --- |
| Sesion actual | `GET /api/auth/session` | Usuario autenticado y token vivo | Si | Si | Si |
| Cambio self-service de password | `POST /api/auth/change-password` | Usuario autenticado y token vivo | Si | Si | Si |
| Dashboard | `GET /api/dashboard/*` | `DASHBOARD_READ` | Si | Si | Si |
| Centro operativo transversal | `GET /api/operations/summary`, `GET /api/operations/summary/export`, `GET /api/operations/work-queue`, `GET /api/operations/work-queue/export` | `DASHBOARD_READ` + filtrado interno por permisos de superficie; seguridad solo con `USERS_ADMIN`; exportaciones reutilizan filtros/ventana/permisos | Si, sin seguridad | Si, sin seguridad | Si |
| History / Bitacora / Comisiones | `GET /api/bitacora`, `GET /api/history/*`, `GET /api/commissions/*` | `HISTORY_READ` | Si | Si | Si |
| Catalogo documental transversal | `GET /api/documents`, `GET /api/documents/export`, `GET /api/documents/summary`, `GET /api/documents/by-entity`, `GET /api/documents/timeline/by-entity`, `GET /api/documents/rules`, `GET /api/documents/requirements/by-entity`, `GET /api/documents/completeness/by-entity`, `GET /api/documents/pending`, `GET /api/documents/work-queue`, `GET /api/documents/work-queue/export`, `GET /api/documents/{documentId}`, `GET /api/documents/{documentId}/download`, `GET /api/documents/{documentId}/timeline` | Usuario autenticado + filtro efectivo por permisos `*_READ` del modulo documental | Si | Si | Si |
| Metadata documental | `PATCH /api/documents/{documentId}/metadata` | `USERS_ADMIN` | No | No | Si |
| Override de retencion documental | `PATCH /api/documents/{documentId}/retention-override`, `DELETE /api/documents/{documentId}/retention-override` | `USERS_ADMIN` | No | No | Si |
| Hold administrativo documental | `POST /api/documents/{documentId}/hold`, `DELETE /api/documents/{documentId}/hold` | `USERS_ADMIN` | No | No | Si |
| Ciclo de vida documental | `POST /api/documents/{documentId}/archive`, `POST /api/documents/{documentId}/restore` | `USERS_ADMIN` | No | No | Si |
| Revision de retencion documental | `GET /api/documents/review-queue`, `GET /api/documents/review-queue/export`, `PATCH /api/documents/{documentId}/retention-review` | `USERS_ADMIN` | No | No | Si |
| Contactos lectura | `GET /api/contact-types`, `GET /api/contacts` | `CONTACTS_READ` | Si | Si | Si |
| Contactos escritura | `POST /api/contacts` | `CONTACTS_WRITE` | No | Si | Si |
| Mercados lectura y documentos | `GET /api/markets`, `GET /api/markets/*`, downloads de cedula | `MARKETS_READ` | Si | Si | Si |
| Mercados escritura | `POST /api/markets`, tenants, incidencias, uploads de cedula | `MARKETS_WRITE` | No | Si | Si |
| Mercados cierre formal | `POST /api/markets/{marketId}/close` | `MARKETS_WRITE` + `FORMAL_CLOSE_ADMIN` | No | No | Si |
| Donatarias lectura y documentos | `GET /api/donations`, `GET /api/donations/*`, downloads de evidencia | `DONATIONS_READ` | Si | Si | Si |
| Donatarias escritura | `POST /api/donations`, aplicaciones, uploads de evidencia | `DONATIONS_WRITE` | No | Si | Si |
| Donatarias cierre formal | `POST /api/donations/{donationId}/close` | `DONATIONS_WRITE` + `FORMAL_CLOSE_ADMIN` | No | No | Si |
| Financieras lectura | `GET /api/financials`, `GET /api/financials/*` | `FINANCIALS_READ` | Si | Si | Si |
| Financieras escritura | `POST /api/financials`, `POST /api/financials/{permitId}/renew`, creditos, comisiones | `FINANCIALS_WRITE` | No | Si | Si |
| Financieras cierre formal | `POST /api/financials/{permitId}/close` | `FINANCIALS_WRITE` + `FORMAL_CLOSE_ADMIN` | No | No | Si |
| Federacion lectura y documentos | `GET /api/federation/*`, downloads de evidencia | `FEDERATION_READ` | Si | Si | Si |
| Federacion escritura | `POST /api/federation/actions`, donaciones, aplicaciones, uploads, comisiones | `FEDERATION_WRITE` | No | Si | Si |
| Federacion cierre formal | `POST /api/federation/actions/{actionId}/close`, `POST /api/federation/donations/{donationId}/close` | `FEDERATION_WRITE` + `FORMAL_CLOSE_ADMIN` | No | No | Si |
| Catalogos compartidos lectura | `GET /api/commission-types`, `GET /api/evidence-types`, `GET /api/module-statuses` | `CATALOGS_READ` | Si | Si | Si |
| Catalogos compartidos admin | `POST /api/commission-types`, `POST /api/evidence-types`, `POST /api/module-statuses` | `CATALOGS_ADMIN` | No | No | Si |
| Administracion de usuarios | `/api/admin/users/*` | `USERS_ADMIN` | No | No | Si |
| Operacion de seguridad ADMIN | `/api/admin/security/*` | `USERS_ADMIN` | No | No | Si |
| Normalizacion local de historico | `POST /api/history/normalize-legacy-closures` | `FORMAL_CLOSE_ADMIN` y solo `Development` | No | No | Si |

## Cobertura automatizada actual
La suite `FMCPA.Api.AuthorizationRegressionTests` valida una seleccion representativa por superficie:
- Publicos permitidos: `/`, `/health`, `/api/auth/login`.
- Rechazo anonimo: `401` para sesion, cambio self-service, dashboard, history, modulo read/write/close, catalogos y usuarios.
- Matriz por rol: lecturas para los tres roles, escrituras para `OPERATOR`/`ADMIN`, denegacion `403` para `READONLY`, cierres y admin-only denegados para `OPERATOR`/`READONLY`.
- Catalogo documental: listado/detalle/descarga transversal sobre `StoredDocument`, consulta por entidad, contexto origen acotado, bandeja unificada `work-queue`, exportacion CSV ligera de catalogo/work queue/review queue, filtro por permisos de modulo, filtro de estado `ACTIVE`/`ARCHIVED`, filtro de clase documental, integridad `VALID` para descarga y no exposicion de rutas internas.
- Centro operativo transversal: summary y work queue de negocio/documentos/seguridad, con seguridad omitida para no administradores y modulos no mapeados excluidos.
- Metadata documental: edicion minima ADMIN-only de clase, proposito, notas e indicador principal; no permite modificar storage, hash, content-type, entidad, integridad ni estado.
- Ciclo de vida documental: archivado/restauracion logica ADMIN-only, auditoria minima y denegacion `403` para `OPERATOR`/`READONLY`.
- Revision de retencion documental: bandeja ADMIN-only, marcado revisado, diferimiento con fecha futura, auditoria minima y denegacion `403` para `OPERATOR`/`READONLY`.
- Auth self-service: password actual incorrecto, password nueva invalida, cambio exitoso, `401` del token previo y login con nueva password.
- Guardrails de superficie: introspeccion de `EndpointDataSource`, allowlist publico explicito, cobertura obligatoria de todo `/api`, reglas protegidas con endpoint real y coincidencia de policy esperada.

## Endpoints publicos permitidos por guardrail
El allowlist operativo actual en `security-authorization-surface-guardrails.json` permite solo:
- `GET /`
- `* /health`
- `GET /openapi/{documentName}.json` en `Development`
- `POST /api/auth/login`

## Limites
- La suite no prueba cada endpoint individual ni cada combinacion de payload; cubre policies por superficie.
- No sustituye smoke funcional de negocio ni validaciones runtime con SQL Server real.
- No agrega RBAC fino, permisos manuales por usuario ni capacidades nuevas de seguridad.
