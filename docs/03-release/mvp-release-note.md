# MVP Release Note

## Estado del release
- Release objetivo: MVP FMCPA
- Estado documental: preparado
- Estado funcional documentado: **Cerrado con reservas**
- Fecha de cierre documental: 2026-04-20

## Alcance incluido en el MVP
- Shared catalogs y contactos reutilizables
- Mercados
- Donatarias
- Financieras
- Federacion
- Dashboard ejecutivo minimo
- Alertas activas visibles dentro de la aplicacion
- Historico consultable de cerrados
- Bitacora MVP visible
- Vista transversal operativa de comisiones de Financials y Federacion

## Cobertura funcional lograda
- Catalogos compartidos de contactos, tipos de comision, tipos de evidencia y estatus por modulo
- Mercados con locatarios, incidencias, cédulas digitalizadas y alertas de vigencia
- Donatarias con donacion maestra, multiples aplicaciones, evidencias y porcentaje aplicado
- Financieras con oficios o autorizaciones, creditos individuales y comisiones por credito
- Federacion con gestiones, participantes, donaciones, aplicaciones, comision por aplicacion y evidencias
- Dashboard de cierre con resumen operativo y alertas consolidadas
- Consulta historica de registros cerrados o archivados

## Exclusiones conocidas del MVP
- Analitica avanzada
- Reporteria o exportaciones complejas
- Notificaciones reales por correo, WhatsApp u otros canales
- Autenticacion y autorizacion completas
- Sistema documental transversal unico
- Auditoria o bitacora avanzada de cambios

## Reservas del release
- La bitacora visible cubre solo eventos MVP derivados de registros existentes.
- El historico usa el ultimo timestamp conocido del registro.
- El almacenamiento documental sigue siendo local por modulo.
- La validacion local se apoyo en SQL Server temporal en `127.0.0.1:14333`.
- El endurecimiento transversal de seguridad y uploads queda para post-MVP.

## Recomendacion de cierre
- El MVP puede considerarse utilizable para demostracion, validacion funcional y continuidad de planeacion.
- El siguiente paso recomendado no es abrir una nueva area de negocio, sino aprobar un track post-MVP enfocado en endurecimiento, seguridad y estrategia documental transversal.

## Referencias
- [Known Limitations](./mvp-known-limitations.md)
- [Validation Summary](./mvp-validation-summary.md)
- [Local Runbook](./mvp-local-runbook.md)
- [Current Phase](../00-governance/current-phase.md)
- [STAGE-07 Implementation Note](../02-delivery/stages/STAGE-07-dashboard-history-and-closeout-implementation-note.md)
