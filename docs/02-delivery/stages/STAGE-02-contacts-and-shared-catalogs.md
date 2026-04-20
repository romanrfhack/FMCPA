# STAGE-02 Contacts and Shared Catalogs

## Objetivo
Definir el catalogo compartido de contactos internos y externos y los catalogos comunes minimos necesarios para reutilizar relaciones entre proyectos y modulos.

## Alcance
- Delimitar el catalogo compartido de contactos internos y externos.
- Definir reglas de reutilizacion de contactos entre proyectos.
- Definir criterios minimos de relacion entre contacto, proyecto y modulo.
- Identificar catalogos compartidos estrictamente necesarios para soportar relaciones reutilizables.
- Alinear como se consultan contactos vigentes e historicos dentro del proyecto.

## Fuera de alcance
- Detalle operativo completo de cada modulo de negocio.
- Catalogos especializados que solo apliquen a un modulo y no al marco compartido.
- Reglas de permisos o administracion avanzada no definidas.

## Entregables
- Definicion del catalogo compartido de contactos.
- Reglas de reutilizacion y vinculacion entre proyectos.
- Inventario de catalogos compartidos minimos.
- Criterios para relacionar contactos con modulos sin duplicidad conceptual.

## Dependencias
- STAGE-01 aprobado.

## Riesgos
- Duplicar conceptos entre contacto reutilizable y actor especifico de un modulo.
- Definir demasiados catalogos compartidos sin necesidad real.
- No dejar suficientemente clara la consulta de relaciones historicas.

## Criterios de aceptacion
- El catalogo de contactos queda definido como reutilizable entre proyectos.
- Se aclara como se relacionan contactos con modulos sin perder trazabilidad.
- Los catalogos compartidos minimos quedan acotados y justificados.
- Mercados, Donatarias, Financieras y Federacion pueden consumir esta base sin redefinir contactos.

## Validacion minima
- Recorrer un escenario con un mismo contacto reutilizado en mas de un proyecto.
- Recorrer un escenario con un mismo contacto vinculado a modulos diferentes.
- Confirmar que el historico preserve la relacion sin duplicar el catalogo.

## Decision esperada al cierre
- Aprobar STAGE-02 como base obligatoria de reutilizacion de contactos y relaciones.
