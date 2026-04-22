# Post-MVP Roadmap

## Estado de partida
- El MVP queda documentado como **Cerrado con reservas**.
- El trabajo post-MVP entra en fase de definicion.
- Aun no existe aprobacion formal del siguiente track tecnico.

## Secuencia propuesta

| Orden | Track | Objetivo | Motivo del orden | Estado |
| --- | --- | --- | --- | --- |
| 1 | Hardening y consistencia operativa | Reducir ambiguedades del MVP, endurecer bitacora, historico, uploads y eventos de cierre | Es la base para que lo siguiente no se construya sobre comportamientos todavia blandos | Pendiente de aprobacion |
| 2 | Seguridad transversal | Incorporar autenticacion, autorizacion futura y controles de acceso minimos | Conviene partir de una base ya mas consistente antes de cerrar seguridad operativa | Pendiente de aprobacion |
| 3 | Estrategia documental transversal | Pasar del storage por modulo a una politica transversal de documentos, respaldo y retencion | Requiere primero criterios mas firmes de operacion y seguridad | Pendiente de aprobacion |
| 4 | Analitica y reporteo | Evolucionar dashboard y consulta transversal hacia analitica y exportaciones razonables | Debe apoyarse en datos mas consistentes, historico mas claro y seguridad minima | Pendiente de aprobacion |
| 5 | Evolucion funcional posterior | Abrir mejoras funcionales ya fuera del cierre del MVP | Debe iniciar solo despues de estabilizar la base transversal | Pendiente de definicion |

## Dependencias entre tracks
- Track 2 depende de acuerdos minimos del Track 1.
- Track 3 depende de decisiones del Track 1 y de criterios de seguridad del Track 2.
- Track 4 depende de datos y cierres mas consistentes, y se beneficia de controles de acceso ya definidos.
- Track 5 no debe abrirse mientras los tracks transversales sigan indefinidos.

## Criterio de avance recomendado
- No abrir el siguiente track tecnico hasta dejar aprobado el alcance, entregables y criterio de salida del track anterior.
- Mantener trazabilidad por track y evitar mezclar endurecimiento transversal con nuevas funcionalidades de negocio.

## Referencias
- [Post-MVP Backlog](./post-mvp-backlog.md)
- [Hardening Track](./hardening-track.md)
- [Security Track](./security-track.md)
- [Document Management Track](./document-management-track.md)
- [Analytics Track](./analytics-track.md)
- [MVP Release Note](../03-release/mvp-release-note.md)
