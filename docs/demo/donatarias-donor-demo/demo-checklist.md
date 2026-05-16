# Checklist previo a la demo - Donatarias Transparencia

Usar este checklist antes de presentar el módulo ante una asociación donante.

## Acceso y datos

- [ ] Validar que el login funciona con el usuario autorizado para la demo.
- [ ] Validar que el usuario tiene permisos suficientes para consultar Donatarias.
- [ ] Validar que los datos demo están cargados y son consistentes.
- [ ] Confirmar que el donante demo visible es `Asociación Donante Demo Final`.
- [ ] Confirmar total recibido de `100000`.
- [ ] Confirmar aplicaciones por `35000`, `30000` y `20000`.
- [ ] Confirmar total aplicado de `85000`.
- [ ] Confirmar saldo pendiente de `15000`.
- [ ] Confirmar readiness final `PARTIAL` por saldo pendiente.

## Navegación

- [ ] Validar que la ruta `/donatarias` carga correctamente.
- [ ] Validar que la donación demo puede seleccionarse desde el listado.
- [ ] Validar que `/donatarias?donationId=...` abre el contexto de la donación.
- [ ] Validar que `/donatarias?donationId=...&applicationId=...` abre el contexto de la aplicación cuando aplique.

## Tabs

- [ ] Validar tab `Resumen`.
- [ ] Validar tab `Donaciones`.
- [ ] Validar tab `Aplicaciones / distribución`.
- [ ] Validar tab `Evidencias`.
- [ ] Validar tab `Reporte de transparencia`.

## Indicadores y reporte

- [ ] Validar KPIs de recibido, aplicado, saldo y porcentaje.
- [ ] Validar distribución financiera por aplicación.
- [ ] Validar semáforo financiero.
- [ ] Validar semáforo documental.
- [ ] Validar semáforo operativo.
- [ ] Validar semáforo documental agregado por donación.
- [ ] Validar que el reporte muestra donante, referencia, corte, totales, estados, aplicaciones, evidencias, faltantes y notas.
- [ ] Validar que la nota legal/fiscal/contable es visible.
- [ ] Validar que el saldo pendiente se explica como pendiente operativo, no como error.

## Evidencias

- [ ] Validar que existe evidencia PDF válida de prueba.
- [ ] Validar que la evidencia registrada se puede descargar.
- [ ] Validar que una aplicación inicialmente sin evidencia puede explicarse como faltante remediado.
- [ ] Validar que, después de la remediación, el reporte ya no muestra faltante documental básico para esa aplicación.
- [ ] Validar que no se describa la evidencia como suficiente legal, fiscal o contablemente.

## Modales

- [ ] Validar que `Registrar donación` abre modal contextual.
- [ ] Validar que `Registrar aplicación` abre modal contextual solo con donación seleccionada y abierta.
- [ ] Validar que `Cargar evidencia` abre modal contextual con aplicación seleccionada.
- [ ] Validar que los modales pueden cancelarse/cerrarse sin afectar la demo.
- [ ] Validar que no aparecen formularios inline fuera de los modales principales.

## Impresión y límites

- [ ] Validar que `Imprimir reporte` está disponible desde `Reporte de transparencia`.
- [ ] Validar que la vista imprimible oculta navegación, tabs, formularios y botones.
- [ ] Validar que la vista imprimible conserva encabezado, KPIs, aplicaciones, evidencias, faltantes y notas.
- [ ] Validar que no se prometa PDF oficial.
- [ ] Validar que no se prometa folio, firma, snapshot persistido o versionamiento del reporte.
- [ ] Validar que el presentador diga explícitamente que el reporte es operativo, no legal, fiscal ni contable oficial.

## Cierre de demo

- [ ] Validar que no se confirme cierre formal durante la demo si no es necesario.
- [ ] Validar que se explique que el cierre formal requiere permiso administrativo.
- [ ] Validar que las advertencias de cierre se muestran cuando existe saldo pendiente.
- [ ] Validar que la siguiente conversación con el donante queda orientada a reporte formal, criterios documentales y proceso institucional.
