# Guion de demo - Donatarias Transparencia

Duración sugerida: 3 a 5 minutos.

Audiencia: asociación donante, dirección operativa y responsables internos de seguimiento.

Objetivo: mostrar cómo FMCPA Platform permite explicar el ciclo del recurso donado con datos operativos actuales: recibido, aplicado, saldo pendiente, evidencias y reporte de transparencia.

## 1. Apertura

"Gracias por el espacio. La intención de esta breve demostración es mostrar cómo FMCPA registra y da seguimiento operativo a una donación desde su recepción hasta su aplicación, con evidencia mínima registrada y una vista de transparencia preparada para revisión."

"Lo que vamos a ver es una herramienta operativa. No sustituye dictámenes, comprobaciones legales, fiscales o contables, pero sí ordena la información clave para que el donante pueda entender qué se recibió, cómo se aplicó y qué queda pendiente."

## 2. Problema que resuelve

"Cuando una donación se aplica en varios destinos, la conversación con el donante puede dispersarse: montos, beneficiarios, comprobantes, pendientes y cierre quedan en lugares distintos."

"Donatarias Transparencia concentra esa historia en una sola pantalla: total recibido, total aplicado, saldo pendiente, porcentaje aplicado, aplicaciones, evidencias, semáforos y reporte."

## 3. Recorrido por Donatarias

Entrar a `/donatarias` y mostrar la estructura por tabs:

- `Resumen`: KPIs de la donación seleccionada y semáforos financiero, documental y operativo.
- `Donaciones`: listado, filtros y captura de donaciones.
- `Aplicaciones / distribución`: detalle de cómo se distribuye el recurso.
- `Evidencias`: documentos cargados por aplicación.
- `Reporte de transparencia`: vista consolidada para consulta e impresión.

Frase sugerida:

"La pantalla está organizada para separar consulta, captura, evidencias y reporte. Así evitamos mezclar la operación diaria con la presentación de transparencia."

## 4. Cómo se registra la donación

Mostrar el botón `Registrar donación`.

Explicar que el modal permite capturar la información base: donante, fecha, tipo, monto base, referencia, estatus y observaciones.

Frase sugerida:

"La donación se registra desde un modal contextual. Esto deja la pantalla principal enfocada en revisión y seguimiento, sin perder el control de captura."

Para el escenario demo:

- Donante: `Asociación Donante Demo Final`.
- Total recibido: `100000`.
- Referencia demo: usar la referencia ya preparada en el ambiente de demo.

## 5. Cómo se distribuye el recurso

Ir al tab `Aplicaciones / distribución`.

Mostrar tres aplicaciones:

- `35000`.
- `30000`.
- `20000`.

Explicar:

- Total aplicado: `85000`.
- Saldo pendiente: `15000`.
- Porcentaje aplicado: `85%`.

Frase sugerida:

"Aquí se ve que la donación no se trata como una sola bolsa opaca. Cada aplicación tiene monto, destino, responsable, estado y peso relativo sobre el total recibido."

"El saldo pendiente sigue visible. Eso es importante porque la demo no presenta una donación artificialmente perfecta; presenta una donación trazable con un pendiente explícito."

## 6. Cómo se muestran evidencias

Ir al tab `Evidencias` o seleccionar una aplicación con evidencia.

Mostrar que la evidencia está agrupada por aplicación y que puede descargarse cuando existe.

Explicar el caso remediado:

"En este escenario una aplicación se dejó inicialmente sin evidencia para mostrar el proceso de corrección. Desde el faltante documental se selecciona la aplicación correspondiente, se abre el modal contextual y se carga un PDF válido de prueba."

Frase sugerida:

"El semáforo documental ayuda a detectar faltantes. La evidencia mínima registrada confirma que el sistema tiene un documento asociado; no convierte automáticamente el caso en cumplimiento legal, fiscal o contable."

## 7. Cómo se imprime el reporte

Ir al tab `Reporte de transparencia`.

Mostrar:

- Datos del donante.
- Referencia.
- Fecha de corte o generación.
- Total recibido.
- Total aplicado.
- Saldo pendiente.
- Estados financiero, documental y operativo.
- Aplicaciones.
- Evidencias.
- Faltantes, si existen.
- Nota de alcance legal, fiscal y contable.

Usar `Imprimir reporte`.

Frase sugerida:

"La vista imprimible limpia navegación, tabs, formularios y botones de captura. Conserva la información operativa necesaria para revisar el caso. No genera un PDF oficial, folio, firma ni snapshot persistido."

## 8. Cierre institucional

"Con Donatarias Transparencia, FMCPA puede sostener una conversación más clara con el donante: cuánto se recibió, cuánto se aplicó, dónde se aplicó, qué evidencia existe y qué pendiente sigue abierto."

"El caso queda en estado `PARTIAL` porque todavía existe un saldo pendiente de `15000`. Eso es correcto y deseable para esta demo: el sistema no fuerza una lectura de cumplimiento total cuando la información financiera todavía no está cerrada."

"Si más adelante se requiere un reporte formal, el siguiente paso sería definir el formato institucional, revisión legal/fiscal/contable, folio, firma, versionamiento y criterios de autorización."
