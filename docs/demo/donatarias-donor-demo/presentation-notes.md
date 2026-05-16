# Notas para el presentador - Donatarias Transparencia

## Enfoque de la demo

La demo debe presentar Donatarias como una herramienta de transparencia operativa. El mensaje principal es que FMCPA puede explicar el recurso donado con claridad: cuánto recibió, cuánto aplicó, dónde lo aplicó, qué evidencia registró y qué pendiente sigue abierto.

Evitar un tono técnico. La conversación debe centrarse en trazabilidad, claridad institucional y seguridad operativa para el donante.

## Frases recomendadas

- "Esta vista concentra la historia operativa de la donación."
- "El sistema permite separar lo financiero, lo documental y lo operativo."
- "El saldo pendiente permanece visible; no se oculta ni se interpreta como cierre."
- "La evidencia mínima registrada ayuda a documentar la aplicación, pero no sustituye una revisión legal, fiscal o contable."
- "La vista imprimible es un apoyo operativo para revisión; no es un PDF oficial."
- "El readiness `PARTIAL` es correcto en este caso porque todavía hay saldo pendiente."
- "Para un reporte formal habría que definir formato, responsables, folio, firma, versión y criterios de autorización."

## Cosas que no debe prometer

- No prometer cumplimiento legal.
- No prometer cumplimiento fiscal.
- No prometer cumplimiento contable.
- No decir que la evidencia cargada es suficiente para auditoría formal.
- No prometer PDF oficial.
- No prometer folio, firma o sello.
- No prometer snapshot persistido o versionamiento del reporte.
- No prometer que el cierre formal puede hacerlo cualquier usuario.
- No presentar la vista imprimible como documento oficial.

## Cómo explicar READY / PARTIAL / NOT_READY

`READY`: la donación no tiene saldo pendiente y cuenta con evidencia mínima registrada. Debe explicarse como lista para presentación operativa, no como aprobación legal, fiscal o contable.

`PARTIAL`: existe información útil para presentar, pero quedan pendientes financieros o documentales. En el caso demo final, el estado es `PARTIAL` por saldo pendiente de `15000`.

`NOT_READY`: no hay aplicaciones o falta información mínima para una presentación operativa. Debe explicarse como un caso que requiere completar datos antes de presentarlo.

## Cómo explicar evidencia mínima

La evidencia mínima significa que el sistema tiene un documento asociado a una aplicación. Sirve para ordenar la comprobación operativa y facilitar revisión.

No significa que el documento sea suficiente para efectos legales, fiscales, contables o de auditoría. Si el donante pregunta por suficiencia formal, responder que debe pasar por los criterios institucionales y la revisión especializada correspondiente.

## Cómo explicar la vista imprimible

La vista imprimible usa la información actual del reporte de transparencia. Al imprimir, oculta navegación, tabs, formularios y botones para dejar una revisión limpia con KPIs, aplicaciones, evidencias, faltantes y notas.

Debe llamarse "vista imprimible operativa" o "apoyo visual de revisión". No llamarla PDF oficial, reporte oficial, dictamen, constancia ni comprobante fiscal.

## Cómo manejar objeciones

Si preguntan "¿esto ya es suficiente para comprobar la donación?", responder:

"Es suficiente para una revisión operativa de transparencia dentro del sistema. Para comprobar formalmente la donación habría que aplicar los criterios legales, fiscales, contables y documentales que defina FMCPA."

Si preguntan "¿por qué sigue en PARTIAL?", responder:

"Porque quedan `15000` sin aplicar. El sistema mantiene visible ese pendiente para que la conversación con el donante sea clara."

Si preguntan "¿nos pueden mandar el PDF?", responder:

"Actualmente existe una vista imprimible operativa. Para un PDF oficial se tendría que definir un proceso formal con folio, firma, versión y revisión institucional."

Si preguntan "¿qué pasa si falta evidencia?", responder:

"El sistema lo señala como faltante documental y permite cargar evidencia desde el contexto de la aplicación correspondiente."

## Cómo explicar saldo pendiente

El saldo pendiente debe tratarse como una señal de transparencia, no como una falla. En el escenario demo, el saldo pendiente de `15000` permite mostrar que FMCPA puede presentar avances sin declarar cerrado lo que todavía no está aplicado.

Frase sugerida:

"Este saldo pendiente es parte de la claridad del reporte. El donante puede ver qué ya fue aplicado y qué sigue pendiente de aplicación."

## Recomendación de cierre

Cerrar la demo con un paso concreto:

"Si este nivel de trazabilidad les resulta útil, el siguiente paso sería acordar qué información debe integrar un reporte formal para donantes y quién debe revisarlo antes de emitirse."
