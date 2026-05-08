# Analytics Track

## Objetivo
- Evolucionar la capa de visibilidad del MVP hacia analitica y reporteo razonables una vez endurecida la base operativa.

## Alcance propuesto
- Centro operativo ejecutivo transversal minimo
- Dashboard analitico posterior al MVP
- Indicadores operativos mas ricos
- Exportaciones simples y acotadas
- Consolidacion de comisiones con mayor fuerza operativa

## Fuera de alcance
- BI compleja
- Analitica predictiva
- Exportaciones pesadas o masivas sin definicion previa

## Punto de partida real
- El MVP ya tiene dashboard ejecutivo minimo.
- Ya existe vista transversal operativa de comisiones.
- Track 4 abre con `/api/operations/summary`, `/api/operations/work-queue` y vista `/operations`, componiendo senales existentes sin persistir analitica.
- `/operations` ya expone items accionables con deep links canonicos, `actionKind`, `actionLabel`, contexto minimo y quick action acotada de desbloqueo cuando el backend la permite.
- `/operations` ya soporta ventanas temporales operativas para summary y work queue mediante `timeWindowCode` (`TODAY`, `LAST_7_DAYS`, `NEXT_30_DAYS`, `ALL`) o rango explicito `fromUtc`/`toUtc`.
- Las ventanas temporales solo aplican a senales con fecha operativa clara: alertas de negocio por `RelevantDate`, documentos por fecha de retencion/revision y seguridad por timestamp de evento o lockout.
- `/operations` ya permite exportar CSV ligero de summary y work queue, reutilizando filtros, ventanas temporales y permisos efectivos.
- La analitica avanzada sigue fuera de alcance.

## Entregables esperados
- Centro operativo transversal de lectura/priorizacion
- Accionabilidad minima con navegacion a contexto/remediacion y quick actions seguras existentes
- Ventanas temporales operativas para priorizacion por horizonte
- Exportacion CSV ligera del centro operativo
- Definicion de indicadores posteriores al MVP
- Priorizacion de exportaciones acotadas
- Criterios de consolidacion operativa mas fuerte de comisiones

## Items candidatos
- PMB-009
- PMB-011
- PMB-012

## Criterios de salida sugeridos
- Existe una vista `/operations` que consolida negocio, documentos y seguridad segun permisos.
- Cada item relevante de la bandeja transversal tiene una ruta util de contexto/remediacion y una accion declarada.
- Las quick actions se limitan a codigos backend explicitos y no abren workflow ni acciones masivas.
- Summary y work queue pueden consultarse con la misma ventana temporal y exponen metadata `timeWindow`.
- Summary y work queue pueden exportarse como CSV ligero, respetando filtros/ventanas/permisos y sin abrir reportes complejos.
- Existe una convencion simple de severidad `HIGH`/`MEDIUM`/`LOW`.
- Existe una ruta clara para analitica sin mezclarla con endurecimiento base.
- Los indicadores propuestos se apoyan en datos ya mas consistentes y seguros.

## Dependencias
- Requiere avances previos en Hardening, y preferentemente en Security y Document Management.

## Estado
- Iniciado con centro operativo ejecutivo transversal minimo, accionabilidad acotada, ventanas temporales operativas y exportacion CSV ligera, pendiente de aprobacion formal.

## Implementaciones
- [Operations Center Implementation Note](./analytics-track-operations-center-implementation-note.md)
- [Operations Actionability Implementation Note](./analytics-track-operations-actionability-implementation-note.md)
- [Operations Time Window Implementation Note](./analytics-track-operations-time-window-implementation-note.md)
- [Operations Light Export Implementation Note](./analytics-track-operations-light-export-implementation-note.md)
