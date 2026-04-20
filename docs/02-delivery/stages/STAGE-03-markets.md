# STAGE-03 Markets

## Objetivo
Delimitar el alcance del modulo de Mercados con sus actores, documentos, vigencias y alertas, usando la base comun ya aprobada.

## Alcance
- Alcalde o alcaldia.
- Secretario general.
- Locatarios.
- Cedulas digitalizadas.
- Vigencias.
- Alertas del modulo de Mercados.
- Relacion de Mercados con contactos reutilizables y con trazabilidad historica.
- Criterios minimos de estatus del modulo de Mercados.

## Fuera de alcance
- Reglas de Financieras, Donatarias, Federacion o Comisiones fuera del contexto necesario de referencia.
- Implementacion de alertas o documentacion tecnica.
- Automatizacion avanzada de documentos.

## Entregables
- Delimitacion funcional aprobable del modulo de Mercados.
- Lista controlada de elementos del modulo y sus relaciones.
- Criterios de vigencias y alertas aplicables a Mercados.
- Criterios minimos de estatus de Mercados y su relacion con historico.

## Dependencias
- STAGE-01 aprobado.
- STAGE-02 aprobado.

## Riesgos
- Confundir actores especificos del mercado con contactos compartidos.
- No precisar suficientemente cuando una vigencia debe generar alerta.
- Dejar ambiguo el comportamiento del modulo cuando el proyecto se cierra.

## Criterios de aceptacion
- El modulo de Mercados queda documentado con alcance claro y sin invadir otros modulos.
- Se distingue entre actor del negocio y contacto reutilizable.
- Las vigencias y alertas del modulo quedan delimitadas a nivel operativo.
- Queda claro que los proyectos cerrados no generan alertas activas en este modulo.

## Validacion minima
- Recorrer un caso de Mercado con actores, cedulas, vigencias y alertas.
- Validar el comportamiento documental del modulo en un proyecto activo y en historico.
- Confirmar consistencia con `business-rules.md`.

## Decision esperada al cierre
- Aprobar STAGE-03 como definicion base del modulo de Mercados.
