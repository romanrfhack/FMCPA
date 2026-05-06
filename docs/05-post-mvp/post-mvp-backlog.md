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
| PMB-004 | P1 | Hardening | Endurecimiento de uploads | Revisar validaciones de archivos, tamano, tipos permitidos y manejo de errores de uploads por modulo | Entregado, pendiente de aprobacion |
| PMB-005 | P1 | Security | Autenticacion | Definir el esquema inicial de autenticacion para la plataforma | Pendiente |
| PMB-006 | P1 | Security | Autorizacion futura | Definir un modelo inicial de autorizacion por roles o perfiles para evolucion posterior | Pendiente |
| PMB-007 | P1 | Document Management | Politica documental transversal | Diseñar una estrategia documental comun para Mercados, Donatarias y Federacion | Iniciado, pendiente de aprobacion |
| PMB-008 | P1 | Document Management | Respaldo y retencion | Definir respaldo, retencion, revision operativa y limpieza segura de archivos y metadatos documentales | Iniciado, pendiente de aprobacion |
| PMB-009 | P2 | Hardening | Comisiones consolidadas | Endurecer la consulta transversal de comisiones con referencias operativas mas consistentes y filtros mas fuertes | Pendiente |
| PMB-010 | P2 | Security | Seguridad de adjuntos | Alinear acceso a evidencias y archivos con futuras reglas de autenticacion y autorizacion | Entregado, pendiente de aprobacion |
| PMB-011 | P2 | Analytics | Dashboard analitico | Definir indicadores posteriores al MVP sin rebasar aun la trazabilidad real disponible | Pendiente |
| PMB-012 | P2 | Analytics | Exportaciones | Evaluar exportaciones simples y acotadas antes de abrir reporteria mas pesada | Pendiente |
| PMB-013 | P3 | Evolution | Evolucion funcional posterior | Identificar mejoras funcionales posteriores una vez cerrados los tracks transversales prioritarios | Pendiente |
| PMB-014 | P1 | Hardening | Entorno local estable | Estandarizar SQL Server local en Docker, puertos, storage, migraciones, arranque y smoke checks para sesiones repetibles de desarrollo y validacion | Entregado, pendiente de aprobacion |
| PMB-015 | P1 | Hardening | Tooling de doctor y reset | Incorporar preflight operativo para detectar prerequisitos y conflictos locales, y reset controlado de base local con confirmacion explicita o `--force` | Entregado, pendiente de aprobacion |
| PMB-016 | P1 | Hardening | Wiring local frontend/API | Resolver el frontend de desarrollo contra el backend configurado via proxy local y `apiBaseUrl` relativo en desarrollo, sin editar codigo fuente cuando cambie el puerto del API | Entregado, pendiente de aprobacion |
| PMB-017 | P1 | Hardening | Tooling y ergonomia local | Versionar `dotnet-ef`, reforzar la lectura de configuracion efectiva y agregar un flujo corto `dev-up.sh` / `dev-down.sh` para sesiones locales gestionadas | Entregado, pendiente de aprobacion |
| PMB-018 | P1 | Security | Borde HTTP y auth | Endurecer login y borde HTTP con rate limiting minimo, headers de seguridad, CORS controlado y fail-fast JWT/CORS fuera de Development | Entregado, pendiente de aprobacion |
| PMB-019 | P1 | Security | Credenciales y auditoria auth | Endurecer password policy, intentos fallidos, lockout temporal por usuario y eventos auditables de autenticacion sin abrir MFA/CAPTCHA/IdP externo | Entregado, pendiente de aprobacion |
| PMB-020 | P1 | Security | Cambio self-service de password | Permitir cambio de password por usuario autenticado con validacion de password actual, politica minima, rotacion de sesion y cierre local sin abrir forgot password/MFA/IdP externo | Entregado, pendiente de aprobacion |
| PMB-021 | P1 | Security | Regresion de autorizacion | Agregar suite de regresion e inventario explicito para superficies protegidas, roles y endpoints publicos permitidos sin abrir nuevas features | Entregado, pendiente de aprobacion |
| PMB-022 | P1 | Document Management | Revision de retencion documental | Agregar bandeja ADMIN-only para operar documentos con retencion proxima o vencida, marcando revisado o diferido sin borrar, mover ni legal hold complejo | Entregado, pendiente de aprobacion |
| PMB-023 | P1 | Document Management | Navegacion contextual documental | Conectar catalogo documental con entidades origen y mostrar documentos relacionados en Mercados, Donatarias y Federacion sin abrir busqueda full-text ni plataforma documental completa | Entregado, pendiente de aprobacion |
| PMB-024 | P1 | Document Management | Completitud documental minima | Detectar locatarios sin certificado activo y aplicaciones sin evidencia documental activa, exponiendo pendientes por modulo sin abrir compliance documental avanzado | Entregado, pendiente de aprobacion |
| PMB-025 | P1 | Document Management | Registry de reglas documentales | Centralizar reglas minimas de completitud en un registry reusable y exponerlas en solo lectura sin abrir edicion dinamica ni compliance avanzado | Entregado, pendiente de aprobacion |
| PMB-026 | P1 | Document Management | Requisitos documentales en contexto | Mostrar requisitos, estado de cumplimiento, conteo actual/minimo y remediacion minima desde la entidad de negocio sin abrir workflow documental complejo | Entregado, pendiente de aprobacion |
| PMB-027 | P1 | Document Management | Reemplazo documental trazable | Registrar relacion minima entre documento reemplazado y vigente, especialmente cédulas de `MarketTenant`, sin abrir versionado completo ni plataforma documental avanzada | Entregado, pendiente de aprobacion |
| PMB-028 | P1 | Document Management | Timeline documental minimo | Mostrar historia documental por documento y entidad usando `StoredDocument`, `AuditEvent` y replacement traceability sin abrir versionado completo ni plataforma documental avanzada | Entregado, pendiente de aprobacion |
| PMB-029 | P1 | Document Management | Bandeja documental unificada | Consolidar pendientes de completitud, integridad documental y revision de retencion en una superficie operativa filtrable sin abrir workflow ni asignaciones | Entregado, pendiente de aprobacion |

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
