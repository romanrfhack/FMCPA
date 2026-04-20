# Stage Gates

## Proposito
Definir las reglas minimas para considerar aceptada una etapa y controlar si el roadmap puede avanzar a la siguiente.

## Reglas para considerar una etapa aceptada
1. El objetivo de la etapa esta cumplido y documentado sin contradicciones con `vision.md`, `business-rules.md` y `roadmap.md`.
2. El alcance y el fuera de alcance quedaron definidos de forma verificable.
3. Los entregables comprometidos por la etapa quedaron completos y trazables.
4. Las dependencias quedaron resueltas, aprobadas o formalmente diferidas.
5. Los riesgos de la etapa quedaron registrados y evaluados.
6. Los criterios de aceptacion fueron revisados contra evidencia documental.
7. La validacion minima de la etapa fue ejecutada y registrada.
8. El backlog, la fase actual y el registro de sesion fueron actualizados.
9. Existe una decision explicita de cierre de etapa.
10. Existe aprobacion explicita para continuar a la siguiente etapa.

## Condiciones para no avanzar a la siguiente etapa
- El alcance de la etapa sigue ambiguo o contradice documentos base.
- Existen dependencias criticas abiertas que impactan la etapa siguiente.
- Los riesgos abiertos impiden tomar una decision informada.
- Los criterios de aceptacion no se cumplieron por completo.
- La validacion minima no se realizo o no fue concluyente.
- Se introdujo alcance nuevo sin actualizacion y aprobacion documental.
- No existe aprobacion explicita de cierre.

## Checklist minimo de revision por etapa
- El objetivo de la etapa es claro y entendible por si mismo.
- El alcance esta delimitado y el fuera de alcance evita interpretaciones expansivas.
- Los entregables tienen forma de documento, matriz, regla o criterio verificable.
- Las dependencias con etapas anteriores estan referenciadas.
- Los riesgos principales estan visibles.
- Los criterios de aceptacion son observables.
- La validacion minima indica como revisar la etapa sin implementar codigo.
- La decision esperada al cierre esta formulada.
- `current-phase.md` refleja el punto real del roadmap.
- `session-log.md`, `backlog.md` y `prompts-log.md` quedaron actualizados.

## Regla de gobierno
- El paso de una etapa a otra es una decision de control del proyecto, no una consecuencia automatica del avance documental.
