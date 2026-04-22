# Backlog

## Uso del documento
- Mantener separado el trabajo del MVP ya ejecutado respecto del trabajo post-MVP.
- Usar este documento como vista de gobierno.
- Mantener el detalle operativo post-MVP en `docs/05-post-mvp/post-mvp-backlog.md`.

## Nota operativa
- La apertura de `RB-003` a `RB-008` fue solicitada de forma explicita para continuar la ejecucion tecnica del MVP.
- El MVP queda documentalmente cerrado como `Cerrado con reservas`, pero su aprobacion formal sigue pendiente.
- `Track 1` ya tiene implementacion inicial, hardening documental minimo, estabilizacion del entorno local, tooling operativo local minimo, smoke MVP automatizado, wiring local estable entre frontend y API, ergonomia operativa local mejorada, regularizacion retrospectiva controlada del historico legado y endurecimiento minimo de transiciones/cierre entregados y validados localmente, pero sigue pendiente de aprobacion formal.
- Ningun track tecnico posterior a `Track 1` esta aprobado.

## Backlog del MVP ejecutado

| ID | Tipo | Tema | Descripcion | Depende de | Prioridad | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| RB-001 | Control | Aprobacion del roadmap | Revisar y aprobar `roadmap.md`, `mvp-scope.md` y `stage-gates.md` como base de ejecucion. | Ninguna | Alta | Completado |
| RB-002 | Etapa | STAGE-01 Foundation | Implementar y validar base tecnica minima con backend .NET 10, frontend Angular 21, health endpoint y persistencia neutral. | RB-001 | Alta | Entregado, pendiente de aprobacion |
| RB-003 | Etapa | STAGE-02 Contacts and Shared Catalogs | Implementar contactos reutilizables, catalogos compartidos minimos y su wiring tecnico para consumo posterior. | RB-002 | Alta | Entregado, pendiente de aprobacion |
| RB-004 | Etapa | STAGE-03 Markets | Implementar el primer modulo de negocio real con mercados, locatarios, incidencias, cédulas digitalizadas y alertas minimas de vigencia. | RB-002, RB-003 | Alta | Entregado, pendiente de aprobacion |
| RB-005 | Etapa | STAGE-04 Donations and Applications | Implementar Donatarias con donacion maestra, multiples aplicaciones, porcentaje aplicado, evidencias por aplicacion y alertas basicas. | RB-002, RB-003 | Alta | Entregado, pendiente de aprobacion |
| RB-006 | Etapa | STAGE-05 Financials and Credits | Implementar Financieras con oficios o autorizaciones, vigencias, stands, creditos individuales y comisiones por credito. | RB-002, RB-003 | Alta | Entregado, pendiente de aprobacion |
| RB-007 | Etapa | STAGE-06 Federation and Commissions | Implementar Federacion con gestiones, participantes, donaciones, aplicaciones, comision por aplicacion y evidencia acotada al modulo. | RB-003, RB-005, RB-006 | Alta | Entregado, pendiente de aprobacion |
| RB-008 | Etapa | STAGE-07 Dashboard History and Closeout | Consolidar historico, bitacora, cierre, visibilidad minima del MVP y alcance final del consolidado transversal necesario. | RB-002 a RB-007 | Alta | Entregado, pendiente de aprobacion |
| RB-009 | Control | Cierre documental del MVP | Publicar release note, limitaciones, resumen de validacion, runbook local y orden de trabajo post-MVP. | RB-008 | Alta | Completado |

## Backlog de definicion post-MVP

| ID | Tipo | Tema | Descripcion | Depende de | Prioridad | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| RB-010 | Control | Decision de cierre formal del MVP | Confirmar si el MVP queda formalmente aprobado como `Cerrado con reservas`. | RB-009 | Alta | Pendiente |
| RB-011 | Track | Track 1: Hardening y consistencia operativa | Implementar bitacora transversal minima real, cierre formal, mejora de historico y endurecimiento documental minimo sin rehacer el MVP. | RB-010 | Alta | Entregado, pendiente de aprobacion |
| RB-012 | Track | Track 2: Seguridad transversal | Evaluar y aprobar el track de autenticacion, autorizacion futura y acceso seguro a archivos. | RB-011 | Alta | Bloqueado hasta aprobar RB-011 |
| RB-013 | Track | Track 3: Estrategia documental transversal | Evaluar y aprobar el track de politica documental, respaldo y retencion. | RB-011, RB-012 | Media | Bloqueado hasta aprobar RB-011 y RB-012 |
| RB-014 | Track | Track 4: Analitica y reporteo | Evaluar y aprobar el track de dashboard analitico, exportaciones y consolidacion operativa mas fuerte. | RB-011 a RB-013 | Media | Bloqueado hasta aprobar tracks previos |
| RB-015 | Track | Track 5: Evolucion funcional posterior | Mantener en espera la evolucion funcional posterior hasta cerrar los tracks transversales prioritarios. | RB-011 a RB-014 | Media | En espera |
| RB-016 | Hardening | Storage documental transversal minimo | Registrar metadatos homogeneos en `StoredDocument`, verificar integridad minima, endurecer uploads/descargas existentes y documentar convivencia con storage local por modulo. | RB-011 | Alta | Entregado, pendiente de aprobacion |
| RB-017 | Hardening | Entorno local estandar | Estandarizar SQL Server local en Docker, convencion de puertos y storage, scripts operativos minimos, migraciones y smoke checks repetibles para futuras sesiones. | RB-011 | Alta | Entregado, pendiente de aprobacion |
| RB-018 | Hardening | Doctor y reset controlado | Incorporar preflight operativo `doctor.sh` y reset controlado `reset-db.sh` integrados con la convencion local ya existente. | RB-017 | Alta | Entregado, pendiente de aprobacion |
| RB-019 | Hardening | Smoke MVP automatizado | Incorporar `smoke-mvp.sh` como validacion funcional minima y repetible del MVP por API, reutilizando la convencion local y los scripts operativos ya existentes. | RB-017, RB-018 | Alta | Entregado, pendiente de aprobacion |
| RB-020 | Hardening | Wiring local frontend/API | Estabilizar el wiring local entre Angular y la API para que el frontend de desarrollo siga al backend configurado via proxy local sin editar codigo manualmente al cambiar `FMCPA_API_PORT`. | RB-017 | Alta | Entregado, pendiente de aprobacion |
| RB-021 | Hardening | Tooling y ergonomia operativa local | Versionar `dotnet-ef`, clarificar configuracion efectiva en scripts y agregar `dev-up.sh` / `dev-down.sh` como flujo corto para sesiones locales gestionadas. | RB-017, RB-018, RB-020 | Alta | Entregado, pendiente de aprobacion |
| RB-022 | Hardening | Regularizacion retrospectiva del historico legado | Reducir la dependencia de `LEGACY_TIMESTAMP_FALLBACK` generando eventos `LEGACY_CLOSE_NORMALIZED` de forma controlada, idempotente y trazable para registros cerrados o archivados previos al hardening. | RB-011 | Alta | Entregado, pendiente de aprobacion |
| RB-023 | Hardening | Endurecimiento minimo de transiciones y cierre | Bloquear cierres formales invalidos y mutaciones nuevas sobre padres terminales en Mercados, Donatarias, Financieras y Federacion, con errores claros y sin generar eventos falsos en bitacora. | RB-011, RB-022 | Alta | Entregado, pendiente de aprobacion |

## Referencias
- [Current Phase](./current-phase.md)
- [Acceptance History](./acceptance-history.md)
- [Post-MVP Roadmap](../05-post-mvp/post-mvp-roadmap.md)
- [Post-MVP Backlog](../05-post-mvp/post-mvp-backlog.md)
