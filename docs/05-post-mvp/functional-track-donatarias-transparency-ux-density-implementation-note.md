# Donatarias Transparencia - Compactacion visual de layout

Fecha: 2026-05-13

## Objetivo

Reducir desperdicio visual en `/donatarias` mediante ajustes frontend de layout, spacing, grillas y wrapping, sin cambiar backend, contratos API, permisos, schema, migraciones ni mover `Registrar donacion` a modal.

## Implementado

- Tarjetas de resumen y KPI mas compactas: menor padding, radio y separacion vertical.
- Grillas de metricas con `repeat(auto-fit, minmax(...))` para aprovechar mejor el ancho antes de saltar de renglon.
- Compactacion de `Donacion seleccionada`, `Lista cargada / filtrada` y resumen financiero visible.
- Filtro separado del layout generico de formularios:
  - desktop: controles en una fila compacta;
  - tablet: dos filas compactas;
  - movil: apilado usable.
- Wrapping defensivo para textos largos, badges y valores dentro de tarjetas.
- Eliminacion del texto descriptivo redundante del card `Filtro` para reducir altura.

## Validacion local

- `npm run build`: exitoso. Mantiene advertencia blanda de presupuesto CSS del componente Donatarias.
- `npm test -- --watch=false`: `22/22`.
- Playwright headless con API mockeada sobre Angular local `http://127.0.0.1:4224/`:
  - viewport movil `390px`;
  - viewport tablet `768px`;
  - viewport desktop `1366px`;
  - sin scroll horizontal global;
  - KPIs de donacion seleccionada y resumen visible usan mas columnas cuando caben;
  - filtro utilizable y con altura reducida.
- `git diff -- src/backend`: sin cambios.

## Fuera de alcance

- Modal para `Registrar donacion`.
- Cambios de backend, endpoints o contratos API.
- Cambios de permisos.
- Cambios de schema o migraciones.
- Cambios de logica funcional de Donatarias.
