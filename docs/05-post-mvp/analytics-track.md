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
- La analitica avanzada sigue fuera de alcance.

## Entregables esperados
- Centro operativo transversal de lectura/priorizacion
- Definicion de indicadores posteriores al MVP
- Priorizacion de exportaciones acotadas
- Criterios de consolidacion operativa mas fuerte de comisiones

## Items candidatos
- PMB-009
- PMB-011
- PMB-012

## Criterios de salida sugeridos
- Existe una vista `/operations` que consolida negocio, documentos y seguridad segun permisos.
- Existe una convencion simple de severidad `HIGH`/`MEDIUM`/`LOW`.
- Existe una ruta clara para analitica sin mezclarla con endurecimiento base.
- Los indicadores propuestos se apoyan en datos ya mas consistentes y seguros.

## Dependencias
- Requiere avances previos en Hardening, y preferentemente en Security y Document Management.

## Estado
- Iniciado con centro operativo ejecutivo transversal minimo, pendiente de aprobacion formal.

## Implementaciones
- [Operations Center Implementation Note](./analytics-track-operations-center-implementation-note.md)
