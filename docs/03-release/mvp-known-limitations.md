# MVP Known Limitations

## Objetivo del documento
- Centralizar las reservas y limitaciones conocidas del MVP ya implementado.
- Evitar que estas limitaciones queden dispersas entre notas de etapa o backlog.

## Limitaciones confirmadas
- La bitacora MVP no cubre ediciones, cambios finos de estatus ni eventos formales de cierre.
- El historico usa el ultimo timestamp conocido del registro (`UpdatedUtc` o `CreatedUtc`) y no una marca formal de cierre.
- El storage documental sigue siendo local por modulo y no existe un sistema documental transversal unico.
- No hay autenticacion completa.
- No hay autorizacion completa ni un modelo de roles cerrado para operacion productiva.
- No hay notificaciones reales por correo, WhatsApp u otros canales.
- No hay analitica avanzada ni dashboard analitico.
- No existen exportaciones complejas ni reporteria pesada.
- La validacion local dependio de SQL Server temporal en `127.0.0.1:14333`.
- La vista transversal de comisiones es operativa, pero no equivale aun a una capa analitica o contable consolidada.

## Alcance real de la bitacora MVP
- Altas de registros principales
- Vinculos de participantes o responsables cuando aplica
- Aplicaciones de donacion
- Registro de comisiones
- Registro de evidencias

## Implicaciones operativas
- El MVP sirve para cierre funcional y visibilidad minima, pero no para auditoria avanzada.
- El almacenamiento documental requiere una estrategia de respaldo y retencion antes de un uso mas exigente.
- La seguridad transversal queda expresamente diferida a post-MVP.

## Referencias
- [MVP Release Note](./mvp-release-note.md)
- [Hardening Track](../05-post-mvp/hardening-track.md)
- [Document Management Track](../05-post-mvp/document-management-track.md)
- [Security Track](../05-post-mvp/security-track.md)
- [Analytics Track](../05-post-mvp/analytics-track.md)
