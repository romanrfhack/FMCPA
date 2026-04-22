# Acceptance History

## Uso del documento
- Registrar solo entregables o etapas ya aprobadas.
- Indicar quien aprueba cuando esa informacion exista.
- Mantener referencia a la evidencia de aceptacion.

## Estado actual
- A la fecha no hay etapas aprobadas de forma definitiva registradas.
- La linea base documental, el roadmap, STAGE-01 Foundation, STAGE-02 Contacts and Shared Catalogs, STAGE-03 Markets, STAGE-04 Donations and Applications, STAGE-05 Financials and Credits, STAGE-06 Federation and Commissions y STAGE-07 Dashboard History and Closeout existen y estan entregados, pero siguen pendientes de aprobacion formal por etapa.
- El MVP queda documentalmente cerrado como `Cerrado con reservas`, sujeto a aprobacion formal.
- `Track 1: Hardening y consistencia operativa` ya tiene implementacion inicial, endurecimiento documental minimo, estabilizacion del entorno local, tooling operativo local minimo, smoke MVP automatizado, wiring local estable entre frontend y API, y ergonomia operativa local mejorada entregados, pero sigue pendiente de aprobacion formal.
- `Track 1: Hardening y consistencia operativa` tambien incorpora ya regularizacion retrospectiva controlada del historico legado y endurecimiento minimo de transiciones/cierre, pero sigue pendiente de aprobacion formal en conjunto.
- Ningun track tecnico posterior a `Track 1` esta aprobado aun.

## Historial

| Fecha | Etapa o entregable | Resultado | Aprobado por | Evidencia |
| --- | --- | --- | --- | --- |
| 2026-04-20 | Linea base documental inicial | Pendiente de aprobacion | Por definir | Creacion de documentos base bajo `/docs` |
| 2026-04-20 | Roadmap de entrega por etapas y delimitacion de MVP | Pendiente de aprobacion | Por definir | Creacion de `docs/02-delivery/*` y reorganizacion de gobernanza |
| 2026-04-20 | STAGE-01 Foundation implementado | Pendiente de aprobacion | Por definir | Base tecnica en `src/backend` y `src/frontend`, health endpoint validado y nota `STAGE-01-foundation-implementation-note.md` |
| 2026-04-20 | STAGE-02 Contacts and Shared Catalogs implementado | Pendiente de aprobacion | Por definir | Entidades y endpoints compartidos en `src/backend`, pantallas minimas en `src/frontend` y nota `STAGE-02-contacts-and-shared-catalogs-implementation-note.md` |
| 2026-04-20 | STAGE-03 Markets implementado | Pendiente de aprobacion | Por definir | Entidades y endpoints del modulo Mercados en `src/backend`, pantalla funcional en `src/frontend` y nota `STAGE-03-markets-implementation-note.md` |
| 2026-04-20 | STAGE-04 Donations and Applications implementado | Pendiente de aprobacion | Por definir | Entidades y endpoints del modulo Donatarias en `src/backend`, pantalla funcional en `src/frontend` y nota `STAGE-04-donations-and-applications-implementation-note.md` |
| 2026-04-20 | STAGE-05 Financials and Credits implementado | Pendiente de aprobacion | Por definir | Entidades y endpoints del modulo Financieras en `src/backend`, pantalla funcional en `src/frontend` y nota `STAGE-05-financials-and-credits-implementation-note.md` |
| 2026-04-20 | STAGE-06 Federation and Commissions implementado | Pendiente de aprobacion | Por definir | Entidades y endpoints del modulo Federacion en `src/backend`, pantalla funcional en `src/frontend` y nota `STAGE-06-federation-and-commissions-implementation-note.md` |
| 2026-04-20 | STAGE-07 Dashboard History and Closeout implementado | Pendiente de aprobacion | Por definir | Endpoints y pantallas de cierre en `src/backend` y `src/frontend`, mas nota `STAGE-07-dashboard-history-and-closeout-implementation-note.md` |
| 2026-04-20 | Estado final del MVP documentado | Cerrado con reservas, pendiente de aprobacion | Por definir | `docs/03-release/*`, `docs/05-post-mvp/*`, dashboard operativo, historico visible, bitacora MVP y consulta transversal de comisiones implementados |
| 2026-04-20 | Apertura de definicion post-MVP | Documentado, pendiente de aprobacion | Por definir | `docs/05-post-mvp/*`, reorganizacion de `current-phase.md` y `backlog.md` |
| 2026-04-20 | Track 1 post-MVP implementado parcialmente | Pendiente de aprobacion | Por definir | `AuditEvent`, migracion `Track1HardeningAuditAndFormalClose`, nuevos endpoints de cierre formal y bitacora real, ajuste de historico y nota `docs/05-post-mvp/hardening-track-implementation-note.md` |
| 2026-04-20 | Track 1 post-MVP ampliado con hardening documental minimo | Pendiente de aprobacion | Por definir | `StoredDocument`, migracion `Track1DocumentStorageHardening`, consulta `/api/documents/integrity`, endurecimiento de uploads/descargas y nota `docs/05-post-mvp/hardening-track-document-storage-implementation-note.md` |
| 2026-04-21 | Track 1 post-MVP ampliado con estabilizacion del entorno local | Pendiente de aprobacion | Por definir | `docker-compose.local.yml`, `.env.local.example`, `scripts/local/*`, runbook local reforzado y nota `docs/05-post-mvp/hardening-track-local-environment-implementation-note.md` |
| 2026-04-21 | Track 1 post-MVP ampliado con doctor y reset controlado | Pendiente de aprobacion | Por definir | `scripts/local/doctor.sh`, `scripts/local/reset-db.sh`, runbook local reforzado y nota `docs/05-post-mvp/hardening-track-doctor-and-reset-implementation-note.md` |
| 2026-04-21 | Track 1 post-MVP ampliado con smoke MVP automatizado | Pendiente de aprobacion | Por definir | `scripts/local/smoke-mvp.sh`, runbook local reforzado, validacion funcional minima del MVP por API y nota `docs/05-post-mvp/hardening-track-smoke-mvp-implementation-note.md` |
| 2026-04-21 | Track 1 post-MVP ampliado con wiring local estable entre frontend y API | Pendiente de aprobacion | Por definir | `environment.development.ts`, `scripts/local/common.sh`, `scripts/local/run-frontend.sh`, runbook local reforzado y nota `docs/05-post-mvp/hardening-track-frontend-local-wiring-implementation-note.md` |
| 2026-04-21 | Track 1 post-MVP ampliado con tooling y ergonomia operativa local | Pendiente de aprobacion | Por definir | `.config/dotnet-tools.json`, `scripts/local/dev-up.sh`, `scripts/local/dev-down.sh`, salida de configuracion efectiva reforzada y nota `docs/05-post-mvp/hardening-track-tooling-and-ergonomics-implementation-note.md` |
| 2026-04-21 | Track 1 post-MVP ampliado con regularizacion retrospectiva del historico legado | Pendiente de aprobacion | Por definir | Distincion `FORMAL_CLOSE_EVENT` / `LEGACY_CLOSE_NORMALIZED` / `LEGACY_TIMESTAMP_FALLBACK`, script `scripts/local/normalize-legacy-history.sh`, ajuste de Bitacora e Historico y nota `docs/05-post-mvp/hardening-track-legacy-history-normalization-implementation-note.md` |
| 2026-04-21 | Track 1 post-MVP ampliado con endurecimiento minimo de transiciones y cierre | Pendiente de aprobacion | Por definir | `StateTransitionSupport`, bloqueo de cierres invalidos y mutaciones sobre padres terminales, mensajes claros en API/frontend y nota `docs/05-post-mvp/hardening-track-state-transitions-implementation-note.md` |
