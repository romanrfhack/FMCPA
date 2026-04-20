# Entities Overview

## Criterio
Este documento resume las entidades de negocio visibles en el contexto actual, sin fijar aun un modelo tecnico definitivo.

## Entidades principales

### Proyecto
- Unidad central de seguimiento.
- Debe permitir consulta historica y reutilizacion de relaciones.

### Mercado
- Agrupa informacion operativa de mercados dentro del proyecto.
- Puede involucrar alcalde o alcaldia, secretario general, locatarios, cedulas, vigencias y alertas.

### Donataria
- Representa el frente de trabajo relacionado con donatarias dentro del proyecto.

### Financiera
- Representa el frente de trabajo relacionado con financieras.
- Puede asociar oficios, vigencias, stands, creditos individuales y comisiones negociadas.

### Gestion de Federacion
- Representa gestiones realizadas en Federacion de Mercados con personas internas y externas.

### Donacion
- Registro principal de una donacion.
- Una donacion puede tener multiples aplicaciones.

### Aplicacion de donacion
- Uso o destino registrado para una donacion.
- Puede incluir comision y evidencia.

### Comision
- Registro asociado a negociaciones o aplicaciones segun el modulo.

### Contacto
- Persona interna o externa reutilizable entre proyectos.

### Vigencia
- Fecha o condicion con seguimiento temporal dentro de un modulo.

### Alerta
- Aviso asociado a vigencias o seguimiento operativo.
- No debe generarse para proyectos cerrados.

### Cedula digitalizada
- Documento digital asociado al contexto de Mercados.

### Oficio
- Documento asociado al contexto de Financieras.

### Evidencia
- Soporte documental asociado a gestiones o aplicaciones, especialmente en Federacion.

### Bitacora
- Registro cronologico de hechos relevantes del proyecto.

## Relaciones explicitamente conocidas
- Un proyecto puede conservar historico de sus relaciones.
- Un contacto puede reutilizarse entre multiples proyectos.
- Una donacion puede relacionarse con multiples aplicaciones.
- Los estatus se controlan por modulo.
