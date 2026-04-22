# Hardening Track

## Objetivo
- Reducir las reservas tecnicas y operativas del MVP sin abrir nuevas areas de negocio.

## Alcance propuesto
- Bitacora mas robusta
- Evento formal de cierre
- Historico mas consistente
- Endurecimiento de uploads y validaciones operativas
- Registro transversal ligero de metadatos documentales
- Reglas mas claras de consulta transversal operativa
- Estabilizacion del entorno local de desarrollo y validacion
- Tooling operativo local minimo de preflight y reset controlado
- Smoke funcional minimo, repetible y automatizado del MVP
- Wiring local estable entre Angular y la API con soporte a puertos override
- Tooling .NET local versionado y ergonomia operativa mejorada
- Regularizacion retrospectiva controlada del historico legado cerrado o archivado
- Endurecimiento minimo de transiciones de cierre y mutaciones sobre padres terminales

## Fuera de alcance
- Analitica avanzada
- Integraciones externas
- Nuevos modulos de negocio

## Problemas que atiende
- Bitacora MVP con cobertura limitada
- Historico basado en ultimo timestamp conocido
- Uploads funcionales pero aun no endurecidos transversalmente
- Consolidacion operativa de comisiones aun minima

## Entregables esperados
- Propuesta aprobable de eventos minimos de bitacora
- Criterio formal de cierre por modulo implementable despues
- Definicion operativa de timestamps de historico
- Lineamientos tecnicos minimos para endurecimiento de uploads
- Integridad documental operativa minima y deteccion de inconsistencias
- Convencion local repetible para SQL Server Docker, backend, frontend, migraciones y smoke checks
- Preflight local con salida accionable y reset controlado de base local
- Smoke MVP automatizado con datos tecnicos minimos, cierres formales reales y salida util para automatizacion local
- Proxy local de Angular generado por scripting para que `FMCPA_API_PORT` pueda cambiar sin edicion manual de codigo
- Manifiesto local de `dotnet-ef`, salida consistente de configuracion efectiva y flujo corto `dev-up.sh` / `dev-down.sh`
- Jerarquia de historico `FORMAL_CLOSE_EVENT` -> `LEGACY_CLOSE_NORMALIZED` -> `LEGACY_TIMESTAMP_FALLBACK` y mecanismo explicito para normalizar cierres heredados sin falsearlos como cierres formales
- Reglas minimas de cierre y transicion para evitar cierres repetidos, cierres sobre terminales y mutaciones nuevas sobre registros ya cerrados o archivados
- Implementacion inicial documentada en `hardening-track-implementation-note.md`

## Items candidatos
- PMB-001
- PMB-002
- PMB-003
- PMB-004
- PMB-009
- PMB-014
- PMB-015

## Criterios de salida sugeridos
- Las reservas centrales del MVP quedan reducidas o mejor delimitadas.
- Existe una definicion clara de que significa cerrar formalmente un registro.
- La bitacora deja de depender solo de derivados minimos donde sea critico mejorar.

## Dependencias
- Ninguna tecnica nueva; parte del estado actual del MVP.

## Estado
- Implementacion inicial, endurecimiento documental minimo, estabilizacion del entorno local, tooling operativo local minimo, smoke MVP automatizado, wiring local estable entre frontend y API, ergonomia operativa local mejorada, regularizacion retrospectiva controlada del historico legado y endurecimiento minimo de transiciones/cierre completados, pendientes de aprobacion

## Referencia de implementacion
- [Hardening Track Implementation Note](./hardening-track-implementation-note.md)
- [Hardening Track Document Storage Implementation Note](./hardening-track-document-storage-implementation-note.md)
- [Hardening Track Local Environment Implementation Note](./hardening-track-local-environment-implementation-note.md)
- [Hardening Track Doctor and Reset Implementation Note](./hardening-track-doctor-and-reset-implementation-note.md)
- [Hardening Track Smoke MVP Implementation Note](./hardening-track-smoke-mvp-implementation-note.md)
- [Hardening Track Frontend Local Wiring Implementation Note](./hardening-track-frontend-local-wiring-implementation-note.md)
- [Hardening Track Tooling and Ergonomics Implementation Note](./hardening-track-tooling-and-ergonomics-implementation-note.md)
- [Hardening Track Legacy History Normalization Implementation Note](./hardening-track-legacy-history-normalization-implementation-note.md)
- [Hardening Track State Transitions Implementation Note](./hardening-track-state-transitions-implementation-note.md)
