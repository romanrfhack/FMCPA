# MVP Scope

## Proposito
Delimitar el alcance minimo viable del sistema desde la perspectiva documental y de planeacion, dejando claro que el MVP se construira y aprobara por etapas.

## Que entra al MVP
- Base comun de proyecto, trazabilidad, bitacora minima y reglas transversales por modulo.
- Catalogo compartido de contactos internos y externos reutilizable entre proyectos.
- Alcance minimo del modulo de Mercados:
  - alcalde o alcaldia
  - secretario general
  - locatarios
  - cedulas digitalizadas
  - vigencias
  - alertas
- Alcance minimo del frente de Donatarias:
  - registro de donaciones
  - multiples aplicaciones por donacion
  - trazabilidad de relaciones
- Alcance minimo del modulo de Financieras:
  - oficios
  - vigencias
  - stands
  - creditos individuales
  - comisiones negociadas
- Alcance minimo del modulo de Federacion de Mercados:
  - gestiones con personas internas y externas
  - donaciones y aplicaciones en su contexto operativo
  - comision
  - evidencia
- Reglas de historico y cierre:
  - un proyecto cerrado permanece consultable
  - un proyecto cerrado no genera alertas activas
- Visibilidad minima de seguimiento para consulta operativa y de historico al cierre del roadmap.

## Que no entra al MVP
- Cualquier funcionalidad no descrita o no aprobada en la documentacion actual.
- Integraciones externas no documentadas.
- Automatizaciones avanzadas de alertas no definidas por modulo.
- Reporteria o analitica avanzada mas alla de la visibilidad minima del roadmap.
- Variantes adicionales de flujo por rol que no hayan sido aprobadas por etapa.
- Reglas nuevas de negocio no registradas en gobernanza y producto antes de su aprobacion.

## Supuestos operativos iniciales
- El proyecto se controlara por sesiones y por etapas aprobadas.
- Los estatus continuaran siendo por modulo.
- Los contactos internos y externos deben reutilizarse entre proyectos en lugar de redefinirse sin control.
- La trazabilidad debe permitir consultar proyectos anteriores y reutilizar relaciones relevantes.
- La bitacora debe registrar hechos clave para dar continuidad operativa y soporte a consulta historica.
- La regla de una donacion con multiples aplicaciones se mantendra como base del alcance de Donatarias y de su consumo posterior en Federacion.
- El cierre de un proyecto no elimina el historico; solo detiene alertas activas y movimiento operativo.

## Dependencias criticas
- Aprobacion del roadmap general y de los gates por etapa.
- Definicion comun de proyecto, trazabilidad y bitacora en STAGE-01.
- Definicion del catalogo compartido de contactos en STAGE-02.
- Delimitacion aprobada de la regla de donacion y aplicaciones en STAGE-04 antes de cerrar Federacion.
- Definicion aprobada de comisiones negociadas en Financieras antes de cerrar alineacion transversal en STAGE-06.
- Criterios claros de historico, cierre y alertas antes de aprobar STAGE-07.

## Regla de control
- Ningun elemento fuera de este MVP debe pasar a diseno o implementacion sin aprobacion documental explicita.
