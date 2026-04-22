# Post-MVP Backlog

## Objetivo
- Mantener una lista accionable, priorizada y trazable de trabajo post-MVP.
- Separar claramente backlog de endurecimiento transversal respecto del MVP ya cerrado.

## Prioridades
- `P1`: necesario para bajar reservas del MVP
- `P2`: recomendado para operacion mas segura y consistente
- `P3`: evolucion posterior, no bloqueante para cierre documental

## Backlog priorizado

| ID | Prioridad | Track | Tema | Item accionable | Estado |
| --- | --- | --- | --- | --- | --- |
| PMB-001 | P1 | Hardening | Bitacora robusta | Definir eventos operativos minimos obligatorios y ampliar cobertura de bitacora para cambios de estatus y eventos de cierre | Pendiente |
| PMB-002 | P1 | Hardening | Evento formal de cierre | Diseñar y acordar una marca formal de cierre para historico por modulo | Pendiente |
| PMB-003 | P1 | Hardening | Historico consistente | Revisar la consulta historica para dejar claro que registros pasan a historico y con que timestamp | Pendiente |
| PMB-004 | P1 | Hardening | Endurecimiento de uploads | Revisar validaciones de archivos, tamano, tipos permitidos y manejo de errores de uploads por modulo | Pendiente |
| PMB-005 | P1 | Security | Autenticacion | Definir el esquema inicial de autenticacion para la plataforma | Pendiente |
| PMB-006 | P1 | Security | Autorizacion futura | Definir un modelo inicial de autorizacion por roles o perfiles para evolucion posterior | Pendiente |
| PMB-007 | P1 | Document Management | Politica documental transversal | Diseñar una estrategia documental comun para Mercados, Donatarias y Federacion | Pendiente |
| PMB-008 | P1 | Document Management | Respaldo y retencion | Definir respaldo, retencion y limpieza segura de archivos y metadatos documentales | Pendiente |
| PMB-009 | P2 | Hardening | Comisiones consolidadas | Endurecer la consulta transversal de comisiones con referencias operativas mas consistentes y filtros mas fuertes | Pendiente |
| PMB-010 | P2 | Security | Seguridad de adjuntos | Alinear acceso a evidencias y archivos con futuras reglas de autenticacion y autorizacion | Pendiente |
| PMB-011 | P2 | Analytics | Dashboard analitico | Definir indicadores posteriores al MVP sin rebasar aun la trazabilidad real disponible | Pendiente |
| PMB-012 | P2 | Analytics | Exportaciones | Evaluar exportaciones simples y acotadas antes de abrir reporteria mas pesada | Pendiente |
| PMB-013 | P3 | Evolution | Evolucion funcional posterior | Identificar mejoras funcionales posteriores una vez cerrados los tracks transversales prioritarios | Pendiente |
| PMB-014 | P1 | Hardening | Entorno local estable | Estandarizar SQL Server local en Docker, puertos, storage, migraciones, arranque y smoke checks para sesiones repetibles de desarrollo y validacion | Entregado, pendiente de aprobacion |
| PMB-015 | P1 | Hardening | Tooling de doctor y reset | Incorporar preflight operativo para detectar prerequisitos y conflictos locales, y reset controlado de base local con confirmacion explicita o `--force` | Entregado, pendiente de aprobacion |
| PMB-016 | P1 | Hardening | Wiring local frontend/API | Resolver el frontend de desarrollo contra el backend configurado via proxy local y `apiBaseUrl` relativo en desarrollo, sin editar codigo fuente cuando cambie el puerto del API | Entregado, pendiente de aprobacion |
| PMB-017 | P1 | Hardening | Tooling y ergonomia local | Versionar `dotnet-ef`, reforzar la lectura de configuracion efectiva y agregar un flujo corto `dev-up.sh` / `dev-down.sh` para sesiones locales gestionadas | Entregado, pendiente de aprobacion |

## Corte sugerido para la primera aprobacion post-MVP
- PMB-001
- PMB-002
- PMB-004
- PMB-005
- PMB-007

## Referencias
- [Post-MVP Roadmap](./post-mvp-roadmap.md)
- [Hardening Track](./hardening-track.md)
- [Security Track](./security-track.md)
- [Document Management Track](./document-management-track.md)
- [Analytics Track](./analytics-track.md)
