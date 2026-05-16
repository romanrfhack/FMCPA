# Estructura propuesta de guía rápida de usuario FMCPA

## 1. Portada

- Nombre: **FMCPA Platform**
- Subtítulo: Guía rápida de operación
- Corte de contenido: basado en el repositorio al 2026-05-16
- Nota de alcance: guía operativa, no manual legal/compliance avanzado

## 2. Objetivo del sistema

- Explicar que FMCPA centraliza la operación de mercados, donatarias, financieras, federación, documentos, seguimiento, alertas e historial.
- Indicar que el sistema cuenta con una base operativa amplia y que algunas capacidades formales quedan para fase posterior.

## 3. Convención visual de estados

- `[Disponible]`
- `[Disponible con alcance acotado]`
- `[En validación]`
- `[Pendiente de fase posterior]`
- `[Solo ADMIN]`

Agregar una nota breve: la guía rápida no sustituye capacitación completa y algunas funciones dependen del rol asignado.

## 4. Acceso e inicio de sesión

- Login institucional con logo FMCPA.
- Header autenticado con logo FMCPA, nombre de plataforma y menú de usuario compacto.
- Cambio de contraseña desde el menú de usuario.
- Cierre de sesión desde el menú de usuario.
- Roles visibles: `ADMIN`, `OPERATOR`, `READONLY`.
- Explicar que las pantallas visibles dependen de permisos.

## 5. Navegación general

- Shell principal con header institucional.
- Navegación agrupada por secciones:
  - Inicio
  - Operación
  - Control
  - Administración
- Visibilidad por permisos y ocultamiento de grupos sin opciones.
- Sin referencias de implementación visibles en la portada o secciones para usuario final.

## 6. Centro operativo / Operations

- Ruta `/operations`
- KPIs y bandeja transversal
- Severidades `HIGH`, `MEDIUM`, `LOW`
- Ventanas temporales
- Exportación CSV ligera
- Quick action de desbloqueo solo ADMIN
- Límites: no es workflow ni BI

## 7. Dashboard

- Ruta `/dashboard`
- Resumen ejecutivo
- Alertas principales
- Histórico reciente

## 8. Mercados

- Ruta `/markets`
- Tabs locales y resumen operativo
- Alta de mercados
- Locatarios
- Cédulas digitalizadas
- Incidencias/mejoras
- Alertas de vigencia
- Capturas principales en modales contextuales
- Cierre formal solo ADMIN
- Documentos relacionados

## 9. Donatarias

- Ruta `/donatarias`
- Transparencia del recurso donado
- Tabs: Resumen, Donaciones, Aplicaciones / distribución, Evidencias, Reporte de transparencia
- KPIs de recibido, aplicado, saldo, porcentaje, aplicaciones y evidencias
- Distribución de aplicaciones
- Evidencia documental por aplicación
- Captura de donación, aplicación y evidencia en modales contextuales
- Semáforos financiero, documental y operativo
- Readiness `READY`, `PARTIAL`, `NOT_READY`
- Reporte de transparencia
- Vista imprimible operativa
- Cierre formal solo ADMIN
- Alcance y límites: evidencia mínima no sustituye revisión legal, fiscal o contable
- Pendientes: PDF oficial, folio/firma/versionamiento, checklist avanzado, validación legal/fiscal/contable y catálogo formal de donantes

## 10. Financieras

- Ruta `/financials`
- Oficios/autorizaciones
- Tabs y ficha operativa
- Vigencias
- Búsqueda de permiso vigente
- Cadena de renovación
- Créditos
- Comisiones por crédito
- Renovación y cadena
- Captura contextual
- Capturas principales en modales contextuales
- Límites: sin catálogo maestro ni versionado contractual completo

## 11. Federación

- Ruta `/federation`
- Gestiones
- Participantes
- Donaciones de Federación
- Aplicaciones
- Comisiones
- Evidencias
- Tabs internos y modales contextuales
- Cierres formales solo ADMIN

## 12. Contactos y catálogos

- Ruta `/contacts`
- Catálogos ADMIN:
  - `/catalogs/commission-types`
  - `/catalogs/evidence-types`
  - `/catalogs/module-statuses`

## 13. Documentos

- Ruta `/documents`
- Resumen ejecutivo documental
- Catálogo compacto, filtros, detalle, descarga
- Labels operativos
- Pendientes documentales
- Metadata, retención, hold y archivado ADMIN
- Ruta `/documents/work-queue` como work queue compacta
- Ruta `/documents/review` solo ADMIN
- Límites: sin OCR, legal hold formal, backup real, borrado físico o cumplimiento avanzado

## 14. Histórico, bitácora y comisiones

- `/history`
- `/bitacora`
- `/commissions`
- Explicar límites de auditoría, timestamps y consolidado operativo

## 15. Seguridad / usuarios

- `/admin/users` solo ADMIN
- Alta de usuario en diálogo
- Acciones agrupadas en `Gestionar`
- `/admin/security` solo ADMIN
- Roles y permisos
- Lockout, reset, activación/desactivación
- Límites: sin MFA, forgot password, IdP externo, SIEM o sesiones avanzadas

## 16. Alertas y pendientes

- Alertas por vigencias, donaciones parciales/no aplicadas, documentos incompletos/integridad/retención y usuarios bloqueados
- Distinguir alertas operativas de notificaciones externas

## 17. Funcionalidades en validación o de fase posterior

- Aprobación formal de líneas de trabajo documentadas
- Bitácora robusta y cierre/histórico consistente
- Política documental transversal, backup, retención formal y cumplimiento
- Analítica/BI/reportes avanzados
- Notificaciones externas
- Seguridad avanzada
- Catálogo maestro y normalización fuerte de Financieras
- Contacts UX compacta
- Catálogos UX compacta

## 18. Recomendaciones de uso

- Revisar centro operativo al inicio de la jornada
- Atender pendientes documentales desde contexto
- Usar cierres formales con ADMIN
- Evitar interpretar CSV como reporte oficial histórico
- Registrar notas y evidencias de forma consistente
