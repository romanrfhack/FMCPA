# Current Phase

## Fase actual
**Track 1 post-MVP: Hardening y consistencia operativa**

## Estado actual
- Fecha de inicio documentada: 2026-04-20
- Estado de la fase: Implementacion inicial, hardening documental minimo, estabilizacion del entorno local, tooling operativo local minimo, smoke MVP automatizado, wiring local estable entre frontend y API, ergonomia operativa local mejorada, regularizacion retrospectiva controlada del historico legado y endurecimiento minimo de transiciones de estado y cierre completados, pendiente de aprobacion
- Estado del MVP: **Cerrado con reservas**
- Enfoque: sustituir parcialmente la trazabilidad derivada del MVP por una bitacora transversal minima real, incorporar cierre formal, mejorar la consulta historica, endurecer el storage documental existente y dejar un entorno local repetible con preflight y reset controlado sin abrir nuevos modulos
- Estado del siguiente track tecnico: **Aun no aprobado**

## Nota operativa
- El MVP permanece cerrado con reservas; este track no reabre el alcance funcional del MVP.
- `Track 1` se ejecuto por instruccion explicita para hardening post-MVP, pero su aceptacion formal sigue pendiente.
- La bitacora transversal nueva registra eventos reales nuevos hacia adelante; no retroconstruye exhaustivamente los eventos del MVP previo.
- El historico ahora prioriza `FORMAL_CLOSE_EVENT`, despues `LEGACY_CLOSE_NORMALIZED` y solo usa `LEGACY_TIMESTAMP_FALLBACK` cuando un registro heredado cerrado o archivado aun no fue regularizado.
- La regularizacion retrospectiva de cierres heredados no corre automaticamente: se ejecuta de forma explicita con `scripts/local/normalize-legacy-history.sh` y deja trazabilidad honesta de que el evento fue reconstruido desde metadata previa.
- Las operaciones de cierre formal ahora bloquean estados ya terminales y los intentos de mutacion obvia sobre padres cerrados ya no generan eventos falsos en bitacora.
- El storage documental local por modulo ahora convive con un registro transversal ligero `StoredDocument`, validacion basica de integridad y backfill minimo de metadatos existentes.
- La convencion local de desarrollo queda estandarizada con SQL Server en Docker sobre `127.0.0.1:14333`, backend en `127.0.0.1:5080`, frontend en `127.0.0.1:4200`, storage bajo `App_Data/` y scripts operativos en `scripts/local/`.
- El tooling local ahora incluye `doctor.sh` para preflight operativo y `reset-db.sh` para reset controlado de la base local configurada.
- El tooling local ahora incluye `smoke-mvp.sh` como verificacion funcional minima del MVP por API, complementaria al `smoke.sh` basico de plataforma.
- El frontend de desarrollo ahora resuelve la API por proxy local generado desde `run-frontend.sh`, evitando depender de editar `environment.ts` cuando `FMCPA_API_PORT` cambia.
- El tooling local ahora incluye un manifiesto local de `dotnet-ef`, salida mas clara de configuracion efectiva y scripts compuestos `dev-up.sh` / `dev-down.sh` para sesiones de trabajo mas cortas.
- Los tracks posteriores de seguridad, estrategia documental, analitica y evolucion funcional permanecen sin aprobacion.

## Objetivos de la fase
- Incorporar una entidad transversal minima de bitacora operativa real.
- Registrar cierres formales en entidades cerrables clave del sistema.
- Mejorar la consistencia del historico sin reestructurar profundamente los modulos existentes.
- Reducir el riesgo de metadatos documentales huerfanos en Mercados, Donatarias y Federacion.
- Exponer la nueva trazabilidad real en backend y frontend con cambios acotados.
- Documentar claramente la convivencia entre la bitacora nueva y la trazabilidad derivada previa del MVP.
- Reducir la friccion de arranque local para futuras sesiones con una convencion estable de base, backend, frontend y smoke checks.
- Dejar un preflight simple y un reset controlado para recuperar el entorno local sin improvisacion.
- Dejar un smoke funcional corto y repetible que detecte regresiones tempranas en dashboard, alertas, comisiones, bitacora, historico e integridad documental.
- Estabilizar el wiring local entre Angular y la API para que el frontend de desarrollo siga funcionando aun cuando el puerto del backend se mueva por override local.
- Reducir friccion adicional con tooling .NET alineado, salida de configuracion efectiva y flujos cortos de arranque y apagado local.
- Reducir la dependencia del historico respecto de `LEGACY_TIMESTAMP_FALLBACK` mediante una regularizacion retrospectiva controlada de cierres heredados.
- Endurecer transiciones de cierre y mutaciones obvias sobre registros terminales para mejorar consistencia entre alertas, historico y bitacora.

## Entregables esperados de esta fase
- Entidad transversal `AuditEvent` y migracion de hardening inicial
- Endpoints minimos de consulta de bitacora y cierre formal
- Ajuste del historico para preferir fecha de cierre formal
- Entidad transversal ligera `StoredDocument`, migracion de backfill minimo y consulta operativa de integridad documental
- Pantallas de Bitacora e Historico actualizadas y acciones minimas de cierre formal en modulos clave
- `docker-compose.local.yml`, `.env.local.example` y scripts `scripts/local/*` para SQL Server local, migraciones, arranque y smoke
- `scripts/local/doctor.sh` y `scripts/local/reset-db.sh` integrados con la convencion local existente
- `scripts/local/smoke-mvp.sh` para validar el MVP por API con datos tecnicos minimos y cierres formales reales
- Proxy local de Angular generado desde `scripts/local/common.sh` y `scripts/local/run-frontend.sh` para resolver `/api` y `/health` hacia `FMCPA_API_PORT`
- Manifiesto local `.config/dotnet-tools.json` para `dotnet-ef` 10.0.6 y scripts `dev-up.sh` / `dev-down.sh` para sesion local compuesta
- Script `scripts/local/normalize-legacy-history.sh` y ajuste de historico para distinguir cierre formal, cierre retrospectivo normalizado y fallback legado
- Capa minima de reglas de transicion y cierre para `Market`, `Donation`, `FinancialPermit`, `FederationAction` y `FederationDonation`, con bloqueo de mutaciones sobre padres terminales
- Nota de implementacion del track bajo `docs/05-post-mvp`

## Criterios de salida de la fase
- Backend y frontend compilan sin romper el MVP.
- Existen eventos reales nuevos consultables en bitacora.
- Existen cierres formales al menos para `Market`, `Donation`, `FinancialPermit`, `FederationAction` y `FederationDonation`.
- El historico muestra `FORMAL_CLOSE_EVENT` cuando existe evento formal, `LEGACY_CLOSE_NORMALIZED` cuando hubo regularizacion retrospectiva valida y `LEGACY_TIMESTAMP_FALLBACK` solo como ultimo recurso.
- Los uploads nuevos de Mercados, Donatarias y Federacion registran metadatos homogeneos en `StoredDocument`.
- Existe una consulta operativa de integridad documental capaz de detectar faltantes fisicos e inconsistencias basicas.
- Existe una forma repetible y documentada de levantar SQL Server local, aplicar migraciones, correr backend, correr frontend y ejecutar smoke checks.
- Existe un preflight operativo con salida accionable y un reset controlado que no corre sin confirmacion o `--force`.
- Existe un smoke MVP automatizado con salida `OK/FAIL`, codigo de salida util y cobertura funcional minima de Markets, Donations, Financials y Federation.
- El frontend de desarrollo puede correr contra backend local en puerto override sin editar codigo fuente manualmente.
- El warning local de `dotnet-ef` queda resuelto o controlado mediante tooling versionado en el repositorio.
- Existe un flujo corto y claro para subir y bajar el stack local gestionado por el proyecto.
- Existe un mecanismo explicito, idempotente y trazable para regularizar cierres heredados sin falsearlos como cierres formales reales.
- Las operaciones invalidas de cierre o mutacion sobre registros terminales devuelven errores claros y no generan eventos falsos en bitacora.
- La convivencia con la trazabilidad derivada previa queda documentada.

## Siguiente decision esperada
- Revisar y aceptar o rechazar la implementacion inicial de `Track 1: Hardening y consistencia operativa`.
- Definir si el siguiente paso post-MVP sera endurecer seguridad transversal o profundizar el mismo track con backlog adicional ya documentado.

## Referencias
- [Hardening Track](../05-post-mvp/hardening-track.md)
- [Hardening Track Implementation Note](../05-post-mvp/hardening-track-implementation-note.md)
- [Hardening Track Document Storage Implementation Note](../05-post-mvp/hardening-track-document-storage-implementation-note.md)
- [Hardening Track Local Environment Implementation Note](../05-post-mvp/hardening-track-local-environment-implementation-note.md)
- [Hardening Track Doctor and Reset Implementation Note](../05-post-mvp/hardening-track-doctor-and-reset-implementation-note.md)
- [Hardening Track Smoke MVP Implementation Note](../05-post-mvp/hardening-track-smoke-mvp-implementation-note.md)
- [Hardening Track Frontend Local Wiring Implementation Note](../05-post-mvp/hardening-track-frontend-local-wiring-implementation-note.md)
- [Hardening Track Tooling and Ergonomics Implementation Note](../05-post-mvp/hardening-track-tooling-and-ergonomics-implementation-note.md)
- [Hardening Track Legacy History Normalization Implementation Note](../05-post-mvp/hardening-track-legacy-history-normalization-implementation-note.md)
- [Hardening Track State Transitions Implementation Note](../05-post-mvp/hardening-track-state-transitions-implementation-note.md)
- [Post-MVP Roadmap](../05-post-mvp/post-mvp-roadmap.md)
- [Post-MVP Backlog](../05-post-mvp/post-mvp-backlog.md)
- [MVP Known Limitations](../03-release/mvp-known-limitations.md)
- [MVP Local Runbook](../03-release/mvp-local-runbook.md)
- [Backlog](./backlog.md)
- [Historial de aceptacion](./acceptance-history.md)
