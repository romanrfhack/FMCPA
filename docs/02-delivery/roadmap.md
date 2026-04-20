# Roadmap

## Proposito
Convertir el backlog inicial en una secuencia ejecutable por etapas aprobables, manteniendo control documental y evitando avanzar a implementacion sin cierre formal de cada etapa.

## Secuencia completa de etapas

| Orden | Etapa | Objetivo principal | Depende de | Resultado esperado |
| --- | --- | --- | --- | --- |
| 1 | [STAGE-01 Foundation](./stages/STAGE-01-foundation.md) | Fijar la base operativa comun del proyecto, la trazabilidad, la bitacora minima y las reglas base para trabajar por modulo. | Ninguna | Marco comun aprobado para el resto del roadmap |
| 2 | [STAGE-02 Contacts and Shared Catalogs](./stages/STAGE-02-contacts-and-shared-catalogs.md) | Definir el catalogo compartido de contactos y los catalogos comunes estrictamente necesarios para reutilizar relaciones entre proyectos. | STAGE-01 | Base comun reutilizable para los modulos |
| 3 | [STAGE-03 Markets](./stages/STAGE-03-markets.md) | Delimitar el modulo de Mercados con sus actores, documentos, vigencias y alertas. | STAGE-01, STAGE-02 | Alcance aprobado del modulo de Mercados |
| 4 | [STAGE-04 Donations and Applications](./stages/STAGE-04-donations-and-applications.md) | Delimitar el alcance documental del frente de Donatarias a partir del registro de donaciones y sus multiples aplicaciones. | STAGE-01, STAGE-02 | Modelo base aprobado para donaciones y aplicaciones |
| 5 | [STAGE-05 Financials and Credits](./stages/STAGE-05-financials-and-credits.md) | Delimitar Financieras con oficios, vigencias, stands, creditos y comisiones negociadas. | STAGE-01, STAGE-02 | Alcance aprobado del modulo de Financieras |
| 6 | [STAGE-06 Federation and Commissions](./stages/STAGE-06-federation-and-commissions.md) | Integrar Federacion de Mercados y Comisiones sobre la base de contactos, donaciones, aplicaciones y evidencia. | STAGE-02, STAGE-04, STAGE-05 | Reglas operativas aprobadas de Federacion y comisiones |
| 7 | [STAGE-07 Dashboard History and Closeout](./stages/STAGE-07-dashboard-history-and-closeout.md) | Consolidar consulta historica, bitacora, cierre operativo y visibilidad minima del MVP. | STAGE-01 a STAGE-06 | Cierre del MVP documental listo para decision de implementacion |

## Objetivo por etapa

### STAGE-01 Foundation
- Establecer el marco de proyecto, trazabilidad, bitacora y criterios comunes que deben consumir las etapas posteriores.

### STAGE-02 Contacts and Shared Catalogs
- Definir como se registran, reutilizan y consultan contactos internos y externos y los catalogos compartidos minimos.

### STAGE-03 Markets
- Bajar a detalle el alcance de Mercados sin mezclarlo con Financieras, Federacion ni Donatarias.

### STAGE-04 Donations and Applications
- Formalizar la regla de una donacion con multiples aplicaciones como base del frente de Donatarias.

### STAGE-05 Financials and Credits
- Delimitar el alcance operativo de Financieras y su relacion con vigencias y comisiones negociadas.

### STAGE-06 Federation and Commissions
- Precisar el funcionamiento de Federacion de Mercados y el uso de comisiones y evidencia en su contexto.

### STAGE-07 Dashboard History and Closeout
- Cerrar el MVP documental con reglas de consulta historica, cierre y visibilidad consolidada, sin convertirlo en una iniciativa de analitica avanzada.

## Dependencia entre etapas
- STAGE-01 es prerequisito estructural para todo el roadmap.
- STAGE-02 depende de STAGE-01 porque reutiliza la definicion comun de proyecto, trazabilidad y bitacora.
- STAGE-03 y STAGE-05 dependen de STAGE-02 para reutilizar contactos y relaciones.
- STAGE-04 depende de STAGE-02 para reutilizar contactos y de STAGE-01 para mantener trazabilidad y estatus por modulo.
- STAGE-06 depende de STAGE-04 para reutilizar la definicion base de donacion y aplicacion, y de STAGE-05 para alinear el manejo de comisiones.
- STAGE-07 depende del cierre aprobado de las etapas anteriores porque consolida historico, bitacora, cierre y tablero minimo sobre alcance ya definido.

## Orden recomendado de ejecucion
1. Aprobar `mvp-scope.md`, `roadmap.md` y `stage-gates.md`.
2. Ejecutar y aceptar STAGE-01.
3. Ejecutar y aceptar STAGE-02.
4. Ejecutar y aceptar STAGE-03.
5. Ejecutar y aceptar STAGE-04.
6. Ejecutar y aceptar STAGE-05.
7. Ejecutar y aceptar STAGE-06.
8. Ejecutar y aceptar STAGE-07.
9. Tomar la decision de pasar o no a una etapa posterior de diseno o implementacion.

## Notas de control
- No se recomienda ejecutar etapas en paralelo mientras existan dependencias abiertas.
- Cada etapa debe cerrarse contra [stage-gates.md](./stage-gates.md).
- El backlog operativo del roadmap se mantiene en [docs/00-governance/backlog.md](../00-governance/backlog.md).
