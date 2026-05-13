# Donatarias - Auditoria funcional para transparencia del recurso donado

Fecha de auditoria: 2026-05-12

## Resumen ejecutivo

El modulo de Donatarias ya permite registrar una donacion maestra, distribuirla en aplicaciones, calcular avance financiero basico, cargar evidencias por aplicacion, consultar documentos relacionados desde el catalogo documental transversal y cerrar formalmente la donacion con permiso administrativo.

La base funcional existe, pero la experiencia actual esta orientada a captura operativa, no a explicar la transparencia ante un donante. La informacion clave si existe en varios puntos: monto recibido, aplicado, remanente, porcentaje, aplicaciones, evidencias, alertas, completitud documental minima y bitacora. El problema principal es que esta dispersa entre sidebar, detalle, formularios, lista de aplicaciones, lista de evidencias y panel documental.

La brecha mas importante no es registrar la donacion, sino presentarla como historia verificable: cuanto entro, como se aplico, que sigue pendiente, que evidencia respalda cada aplicacion y si ya puede considerarse lista para compartir o cerrar. Hoy no existe reporte de transparencia para donante, exportacion especifica de Donatarias, vista imprimible, cierre con checklist documental ni semaforo documental agregado por donacion.

## Alcance revisado

- `src/frontend/src/app/features/donatarias/donatarias-page.component.ts`
- `src/frontend/src/app/core/services/donations.service.ts`
- `src/frontend/src/app/core/models/donations.models.ts`
- `src/frontend/src/app/features/documents/related-documents-panel.component.ts`
- `src/frontend/src/app/core/services/document-catalog.service.ts`
- `src/frontend/src/app/core/models/document-catalog.models.ts`
- `src/backend/src/FMCPA.Api/Endpoints/DonationsEndpoints.cs`
- `src/backend/src/FMCPA.Api/Contracts/Donations/DonationsContracts.cs`
- `src/backend/src/FMCPA.Domain/Entities/Donations/*`
- `src/backend/src/FMCPA.Infrastructure/Persistence/Configurations/Donations/*`
- `src/backend/src/FMCPA.Infrastructure/Persistence/Configurations/Shared/SharedCatalogSeedData.cs`
- `src/backend/src/FMCPA.Api/DocumentRules/DocumentRuleRegistry.cs`
- `src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/*`
- `docs/05-post-mvp/*`
- `docs/user-guide/drafts/feature-status-matrix.md`
- `docs/user-guide/drafts/user-guide-source.md`

## Inventario funcional actual

### Pantallas y componentes

| Area | Existe | Funcion actual |
| --- | --- | --- |
| Ruta `/donatarias` | Si | Ruta Angular protegida por `DONATIONS_READ`. |
| Navegacion principal | Si | Link visible para usuarios con `DONATIONS_READ`. |
| `DonatariasPageComponent` | Si | Pantalla unica con hero, filtros, alta, listado, alertas, detalle, aplicaciones y evidencias. |
| Filtro lateral | Si | Filtra por estatus de donacion y por `alertsOnly`. |
| Alta de donacion | Si | Registra donante, fecha, tipo, monto base, referencia, estatus inicial y notas. |
| Listado de donaciones | Si | Muestra donante, tipo, referencia, monto base, monto aplicado, porcentaje y alerta. |
| Alertas activas | Si | Lista donaciones no aplicadas o parcialmente aplicadas. |
| Detalle de donacion | Si | Muestra estatus, alerta, notas, monto base, aplicado, remanente, porcentaje, aplicaciones y evidencias. |
| Cierre formal | Si | Boton en detalle, condicionado a `FORMAL_CLOSE_ADMIN` y bloqueado si ya esta terminal. Usa `prompt`. |
| Alta de aplicacion | Si | Registra beneficiario, fecha, responsable, monto aplicado, estatus, comprobacion y datos de cierre. |
| Lista de aplicaciones | Si | Muestra beneficiario, fecha, responsable, estatus, monto, evidencias y detalle de comprobacion. |
| Alta de evidencia | Si | Carga archivo con tipo y descripcion, asociada a una aplicacion. |
| Lista de evidencias | Si | Muestra tipo, archivo, tamano, descripcion, fecha UTC y descarga. |
| Panel documental relacionado | Si | Reutiliza `RelatedDocumentsPanelComponent` para documentos de `DONATION_APPLICATION`. |
| Historia documental | Si | El panel documental muestra timeline por entidad cuando el catalogo lo devuelve. |

### Endpoints actuales de Donatarias

| Metodo | Ruta | Permiso | Respuesta / accion | Observaciones |
| --- | --- | --- | --- | --- |
| `GET` | `/api/donations` | `DONATIONS_READ` | `DonationSummaryResponse[]` | Acepta `statusCode` y `alertsOnly`. Calcula aplicado, remanente, porcentaje, conteos y alerta. |
| `GET` | `/api/donations/alerts` | `DONATIONS_READ` | `DonationAlertResponse[]` | Devuelve solo no aplicadas o parcialmente aplicadas, si el estatus permite alertas. |
| `GET` | `/api/donations/{donationId}` | `DONATIONS_READ` | `DonationDetailResponse` | Incluye aplicaciones y evidencias por aplicacion. |
| `GET` | `/api/donations/{donationId}/progress` | `DONATIONS_READ` | `DonationProgressResponse` | Resume base, aplicado, remanente, porcentaje y numero de aplicaciones. |
| `GET` | `/api/donations/{donationId}/applications` | `DONATIONS_READ` | `DonationApplicationResponse[]` | Lista aplicaciones con evidencias. |
| `GET` | `/api/donations/applications/{applicationId}/evidences` | `DONATIONS_READ` | `DonationApplicationEvidenceResponse[]` | Lista evidencias de una aplicacion. |
| `GET` | `/api/donations/applications/evidences/{evidenceId}/download` | `DONATIONS_READ` | Archivo protegido | Valida integridad contra `StoredDocument`/storage antes de descargar. |
| `POST` | `/api/donations` | `DONATIONS_WRITE` | Crea donacion | Solo permite estatus inicial `NOT_APPLIED` o `CLOSED`. |
| `POST` | `/api/donations/{donationId}/applications` | `DONATIONS_WRITE` | Crea aplicacion | Bloquea donacion terminal y evita rebasar el monto base. Recalcula estatus de donacion. |
| `POST` | `/api/donations/applications/{applicationId}/evidences` | `DONATIONS_WRITE` | Crea evidencia | Bloquea donacion terminal, valida upload, crea `DonationApplicationEvidence` y `StoredDocument`. |
| `POST` | `/api/donations/{donationId}/close` | `FORMAL_CLOSE_ADMIN` | Cierre formal | Registra evento en bitacora y sincroniza estatus `CLOSED` si aplica. |

### Endpoints transversales usados por Donatarias

| Metodo | Ruta | Uso en Donatarias |
| --- | --- | --- |
| `GET` `/api/module-statuses?moduleCode=DONATARIAS&contextCode=DONATION` | Catalogo de estatus de donacion. |
| `GET` `/api/module-statuses?moduleCode=DONATARIAS&contextCode=DONATION_APPLICATION` | Catalogo de estatus de aplicacion. |
| `GET` `/api/evidence-types` | Tipos de evidencia para carga. |
| `GET` `/api/contacts` | Contactos reutilizables para responsable de aplicacion. |
| `GET` `/api/documents/by-entity` | Documentos relacionados de una aplicacion. |
| `GET` `/api/documents/requirements/by-entity` | Requisito documental minimo de una aplicacion. |
| `GET` `/api/documents/completeness/by-entity` | Estado de completitud puntual. |
| `GET` `/api/documents/timeline/by-entity` | Historia documental de la aplicacion. |
| `GET` `/api/documents/summary` | Resumen documental transversal, no especifico de Donatarias. |
| `GET` `/api/documents/export` | Exportacion CSV documental general, no reporte de transparencia de Donatarias. |

### Entidades principales

| Entidad | Campos principales actuales |
| --- | --- |
| `Donation` | `Id`, `DonorEntityName`, `DonationDate`, `DonationType`, `BaseAmount`, `Reference`, `Notes`, `StatusCatalogEntryId`, `CreatedUtc`, `UpdatedUtc`, `Applications`. |
| `DonationApplication` | `Id`, `DonationId`, `BeneficiaryName`, `ResponsibleContactId`, `ResponsibleName`, `ApplicationDate`, `AppliedAmount`, `StatusCatalogEntryId`, `VerificationDetails`, `ClosingDetails`, `CreatedUtc`, `Evidences`. |
| `DonationApplicationEvidence` | `Id`, `DonationApplicationId`, `EvidenceTypeId`, `Description`, `OriginalFileName`, `StoredRelativePath`, `ContentType`, `FileSizeBytes`, `UploadedUtc`. |
| `StoredDocument` | Metadata transversal de archivo, clase documental, proposito, retencion, integridad, estado, hold, reemplazo y descarga unificada. |
| `ModuleStatusCatalogEntry` | Estatus configurable por modulo/contexto con `IsClosed` y `AlertsEnabledByDefault`. |
| `EvidenceType` | Tipos activos para clasificar evidencia de carga. |
| `ContactParticipation` | Vinculo de contacto responsable cuando la aplicacion usa `ResponsibleContactId`. |
| `AuditEvent` | Bitacora de creacion, aplicacion, adjunto de evidencia y cierre formal. |

### DTOs principales

| DTO | Uso |
| --- | --- |
| `DonationSummaryResponse` / `DonationSummary` | Listado y tarjetas de donacion. Incluye metricas calculadas. |
| `DonationDetailResponse` / `DonationDetail` | Detalle con aplicaciones y evidencias. |
| `CreateDonationRequest` | Alta de donacion. |
| `DonationProgressResponse` / `DonationProgress` | Progreso financiero puntual. |
| `CreateDonationApplicationRequest` | Alta de aplicacion. |
| `DonationApplicationResponse` / `DonationApplication` | Aplicacion con evidencias. |
| `CreateDonationApplicationEvidenceRequest` | Upload multipart de evidencia. |
| `DonationApplicationEvidenceResponse` / `DonationApplicationEvidence` | Metadata de evidencia. |
| `DonationAlertResponse` / `DonationAlert` | Alertas por no aplicada o parcial. |
| `DocumentRequirement`, `DocumentCompleteness`, `DocumentCatalogItem` | Estado documental transversal de la aplicacion. |

### Catalogos actuales relevantes

#### Estatus de donacion

| Codigo | Nombre | Terminal | Alertas por default | Uso actual |
| --- | --- | --- | --- | --- |
| `NOT_APPLIED` | No aplicada | No | Si | Alta inicial y alerta. |
| `PARTIALLY_APPLIED` | Aplicacion parcial | No | Si | Calculado al registrar aplicaciones con saldo remanente. |
| `APPLIED` | Aplicada | No | No | Calculado cuando aplicado >= monto base. |
| `CLOSED` | Cerrada | Si | No | Cierre formal o alta inicial cerrada. |

#### Estatus de aplicacion

| Codigo | Nombre | Terminal | Alertas por default |
| --- | --- | --- | --- |
| `PARTIALLY_APPLIED` | Aplicacion parcial | No | Si |
| `APPLIED` | Aplicada | No | No |
| `CLOSED` | Cerrada | Si | No |

#### Tipos de evidencia

| Codigo | Nombre |
| --- | --- |
| `PHOTO` | Fotografia |
| `VIDEO` | Video |
| `SIGNED_DOCUMENT` | Documento firmado |
| `SUPPORT_DOCUMENT` | Documento soporte |
| `OTHER` | Otro |

### Flujos disponibles

1. Listar donaciones y filtrar por estatus o alertas.
2. Registrar una donacion nueva con estatus inicial permitido.
3. Seleccionar una donacion y consultar su avance financiero.
4. Registrar una aplicacion contra una donacion no terminal.
5. Validar que la suma de aplicaciones no exceda `BaseAmount`.
6. Recalcular estatus financiero de la donacion despues de registrar aplicacion.
7. Seleccionar una aplicacion y cargar evidencia.
8. Descargar evidencia con validacion de integridad documental.
9. Consultar documentos relacionados, requisito minimo, completitud y timeline documental por aplicacion.
10. Remediar faltante documental desde el panel relacionado si el usuario puede escribir.
11. Consultar alertas de donaciones no aplicadas o parcialmente aplicadas.
12. Cerrar formalmente una donacion como administrador.
13. Registrar eventos de bitacora para creacion, aplicacion, evidencia y cierre.

### Campos actuales de donacion

- Donante: `donorEntityName`.
- Fecha: `donationDate`.
- Tipo de donacion: `donationType`.
- Monto o valor base: `baseAmount`.
- Referencia: `reference`.
- Observaciones: `notes`.
- Estatus: `statusCatalogEntryId`, `statusCode`, `statusName`, `statusIsClosed`, `statusAlertsEnabledByDefault`.
- Monto aplicado calculado: `appliedAmountTotal`.
- Remanente calculado: `remainingAmount`.
- Porcentaje aplicado calculado: `appliedPercentage`.
- Numero de aplicaciones: `applicationCount`.
- Numero de evidencias: `evidenceCount`.
- Estado de alerta calculado: `alertState`.
- Fechas tecnicas: `createdUtc`, `updatedUtc`.

### Campos actuales de aplicacion

- Beneficiario: `beneficiaryName`.
- Responsable vinculado: `responsibleContactId`.
- Responsable textual: `responsibleName`.
- Fecha de aplicacion: `applicationDate`.
- Monto aplicado: `appliedAmount`.
- Estatus: `statusCatalogEntryId`, `statusCode`, `statusName`, `statusIsClosed`.
- Detalle de comprobacion: `verificationDetails`.
- Datos de cierre: `closingDetails`.
- Numero de evidencias: `evidenceCount`.
- Evidencias embebidas: `evidences`.
- Fecha tecnica: `createdUtc`.

### Campos y evidencias disponibles

- Tipo de evidencia: `evidenceTypeId`, `evidenceTypeCode`, `evidenceTypeName`.
- Descripcion: `description`.
- Archivo original: `originalFileName`.
- Content-type: `contentType`.
- Tamano: `fileSizeBytes`.
- Fecha de carga: `uploadedUtc`.
- Ruta fisica: persistida internamente, no expuesta.
- Hash, integridad, clase documental, proposito, retencion, hold, archivado, reemplazo y timeline: disponibles via `StoredDocument` y catalogo documental transversal.
- Regla minima para `DonationApplication`: al menos una evidencia documental activa asociada a la aplicacion.

## Diagnostico de claridad operativa

### Informacion visible pero dispersa

- El monto recibido, aplicado, remanente y porcentaje aparecen en tarjetas del detalle, pero el usuario primero ve filtros, alta, listado y alertas antes de llegar al resumen.
- Las aplicaciones y evidencias estan en bloques separados; para entender una aplicacion hay que alternar entre lista de aplicaciones, formulario de evidencia, lista de evidencias y panel documental.
- La evidencia existe dos veces visualmente: lista propia de evidencias y panel documental transversal. Ambas son utiles, pero sin una jerarquia clara pueden parecer fuentes distintas.
- El estatus financiero (`NOT_APPLIED`, `PARTIALLY_APPLIED`, `APPLIED`) y el cierre operativo (`CLOSED`) conviven en un mismo badge de donacion.
- La completitud documental aparece dentro del panel relacionado de la aplicacion seleccionada, no como estado agregado de la donacion.
- Las alertas activas muestran no aplicada/parcial, pero no muestran evidencia pendiente, comprobacion incompleta ni donaciones listas para presentar.

### Informacion faltante para transparencia ante donante

- Resumen ejecutivo de una donacion listo para explicar al donante.
- Vista agregada por donacion de aplicaciones con y sin evidencia completa.
- Estado documental agregado por donacion.
- Semaforo separado de dinero, evidencia y cierre.
- Relacion explicita entre "recurso recibido" y "recurso aplicado por beneficiario/proposito".
- Reporte imprimible o exportable para compartir.
- Fecha de corte del reporte.
- Narrativa de uso del recurso: objetivo, destino, resultado y evidencia.
- Checklist de cierre: 100% aplicado, evidencia completa, comprobacion revisada, cierre formal.

### Flujos que pueden simplificarse

- Separar consulta de captura: la pantalla mezcla alta de donacion, filtros, listas, alertas, resumen, alta de aplicacion y alta de evidencia en una sola lectura vertical.
- Mover altas a acciones contextuales o modales: `Registrar donacion`, `Registrar aplicacion`, `Cargar evidencia`.
- Convertir el detalle en tabs: resumen primero, despues donaciones/aplicaciones/evidencias/reporte.
- Mostrar una tabla de aplicaciones con columnas de monto, porcentaje del total, evidencia y comprobacion.
- Agrupar evidencia por aplicacion para evitar que el usuario busque manualmente que archivo respalda que monto.
- Reemplazar el `prompt` de cierre formal por modal con motivo y resumen de impacto.

### Puntos que pueden confundir

- El texto actual del hero habla de `STAGE-04` y "donaciones maestras"; para cliente/donante conviene lenguaje de transparencia.
- `Monto base` puede entenderse como presupuesto estimado; para el caso de donante conviene "Total recibido / valor de la donacion".
- `Comprobacion / detalle` y `Datos de cierre` no tienen definicion operativa visible.
- Una aplicacion puede tener estatus `APPLIED` aunque el estado documental no este completo; hoy esos estados no estan claramente separados.
- `evidenceCount` cuenta evidencias, pero no necesariamente comunica si la evidencia cumple, esta activa o es suficiente legalmente.
- El cierre formal puede ocurrir aunque la donacion no este totalmente aplicada, por decision operativa; esto es valido en el sistema, pero debe explicarse en UI para no confundirlo con "comprobacion completa".
- El route hint documental sugiere `/donatarias?donationId=...&applicationId=...`, pero la pantalla actual no lee esos query params para seleccionar automaticamente la aplicacion.

## Brechas funcionales

| Tema | Estado actual | Brecha |
| --- | --- | --- |
| Control del recurso recibido | Existe `BaseAmount`, fecha, tipo, referencia y donante. | No hay moneda, origen bancario, comprobante de recepcion o conciliacion financiera explicita. |
| Distribucion del recurso | Existen aplicaciones con beneficiario, fecha, responsable y monto. | No hay categoria/proposito estructurado, partida, programa, ubicacion, meta o resultado de la aplicacion. |
| Saldo pendiente | Existe `remainingAmount` calculado. | No hay vista de saldo pendiente con acciones recomendadas ni aging de saldo. |
| Porcentaje aplicado | Existe `appliedPercentage`. | No hay visualizacion prioritaria ni contexto por aplicacion como porcentaje del total. |
| Evidencia por aplicacion | Existe carga y descarga por aplicacion. | No hay tablero agregado de aplicaciones sin evidencia completa por donacion. |
| Comprobacion documental | Existe regla minima: al menos una evidencia activa por aplicacion. | No valida suficiencia legal, calidad, correspondencia monto-evidencia, contenido, aprobacion o revision humana. |
| Reporte para donante | No existe especifico. | Falta vista que consolide recibido, aplicado, pendiente, evidencias y estado documental. |
| Exportacion/presentacion | Existe CSV documental transversal, no especifico de Donatarias. | Falta exportacion o vista imprimible de transparencia de una donacion. |
| Cierre de donacion | Existe cierre formal admin y bloqueo de mutaciones posteriores. | No hay checklist previo de cierre ni distincion visual entre cierre operativo y comprobacion completa. |
| Trazabilidad | Existen `AuditEvent`, timeline documental e integridad de descarga. | No hay timeline funcional unificada de donacion que combine alta, aplicaciones, evidencias, revisiones y cierre en una sola vista. |
| Edicion/correccion | No hay endpoints de edicion o baja funcional. | Si un dato se captura mal, no existe flujo de correccion controlada dentro de Donatarias. |
| Seleccion contextual | Hay route hints documentales. | La pantalla no consume query params para abrir directamente una donacion/aplicacion. |
| Donante como entidad | `donorEntityName` es texto libre. | No hay catalogo de donantes, contacto donante, RFC, convenio o representante. |

## Pruebas existentes relacionadas

- `AuthorizationRegressionTests` cubre permisos base para `GET /api/donations`, `POST /api/donations` y `POST /api/donations/{id}/close`.
- `DocumentCatalogTests` cubre catalogo documental, contexto origen, documentos por entidad, completitud, work queue, resumen, exportaciones, integridad, metadata, retencion, hold, archivado y permisos.
- No se encontro una prueba funcional dedicada que cubra el flujo completo de Donatarias: crear donacion, aplicar monto, recalcular estatus, cargar evidencia y cerrar.
- No se encontro prueba frontend especifica para `DonatariasPageComponent`.

## Conclusiones

1. El repositorio ya tiene la materia prima para transparencia basica: monto recibido, aplicado, saldo, porcentaje, aplicaciones, evidencia, descarga, requisito minimo documental y bitacora.
2. La propuesta debe iniciar con reorganizacion visual y semantica, sin cambiar schema.
3. Las brechas criticas para presentar a un donante son reporte, exportacion, semaforo documental agregado y narrativa clara de aplicacion del recurso.
4. La separacion de estatus financiero, documental y operativo es el cambio conceptual mas importante para evitar malinterpretaciones.
5. La fase inicial puede ser frontend-only si se limita a usar `DonationSummary`, `DonationDetail`, `DocumentRequirement` y `DocumentCatalogItem`.
