# Referencia visual del sistema FMCPA

## Alcance de la revisión

Esta referencia visual se extrajo del frontend real en `src/frontend` y de las pantallas existentes. No se generaron capturas ni assets nuevos. El HTML de la guía debe replicar la identidad visual con CSS propio, sin dependencias externas.

## Colores principales detectados

| Uso | Color / token observado | Recomendación para la guía | Evidencia |
| --- | --- | --- | --- |
| Fondo base | `#f4f2eb` | Usar como fondo principal de página. | `src/frontend/src/styles.css:8-9`, `src/frontend/src/app/app.css:8-10` |
| Fondo elevado claro | `#f7f6f2`, `rgba(255,255,255,0.82-0.90)` | Usar en tarjetas, tablas y paneles. | `src/frontend/src/app/app.css:10`, `src/frontend/src/app/app.css:55-62`, `src/frontend/src/app/features/documents/documents-page.component.ts:770-776` |
| Verde petróleo oscuro | `#123f3b`, `rgba(18,63,59,0.92)` | Usar en navegación activa, portada y botones primarios. | `src/frontend/src/app/app.css:71-77`, `src/frontend/src/app/app.css:160-162` |
| Teal operativo | `#0f766e` | Usar en kicker, enlaces, botones secundarios, bordes activos y badges informativos. | `src/frontend/src/app/app.css:26-32`, `src/frontend/src/app/app.css:153-156` |
| Texto principal | `#1d2d2a`, `#29403b` | Usar para títulos y texto principal. | `src/frontend/src/app/app.css:11`, `src/frontend/src/app/app.css:140-145` |
| Texto secundario | `#445854`, `#4d615c`, `#60716d` | Usar para descripciones, notas y metadatos. | `src/frontend/src/app/app.css:41-46`, `src/frontend/src/app/features/admin/security-admin-page.component.ts:151-156`, `src/frontend/src/app/features/documents/documents-page.component.ts:874-878` |

## Colores secundarios y estados

| Estado visual | Color detectado | Uso sugerido en guía | Evidencia |
| --- | --- | --- | --- |
| Éxito / disponible | `#166534`, `rgba(22,101,52,0.08-0.10)` | Badge `[Disponible]`. | `src/frontend/src/app/features/documents/documents-page.component.ts:1049-1056`, `src/frontend/src/app/features/documents/related-documents-panel.component.ts:276-277` |
| Advertencia / alcance acotado | `#92400e`, `rgba(146,64,14,0.10)` | Badge `[Disponible con alcance acotado]` y notas de atención. | `src/frontend/src/app/features/documents/documents-work-queue-page.component.ts:209-214`, `src/frontend/src/app/features/documents/related-documents-panel.component.ts:270-271` |
| Riesgo / fase posterior | `#9f1239`, `#be123c`, `#b42318` | Badge `[Pendiente de fase posterior]`, alertas y bloque de definiciones pendientes. | `src/frontend/src/app/features/documents/documents-page.component.ts:1040-1051`, `src/frontend/src/app/features/documents/documents-work-queue-page.component.ts:203-207`, `src/frontend/src/app/features/dashboard/dashboard-page.component.ts:386-387` |
| Informativo / validación | `#1d4ed8`, `#175cd3`, `rgba(59,130,246,0.14)` | Badge `[En validación]` o referencias operativas. | `src/frontend/src/app/features/dashboard/dashboard-page.component.ts:398-399`, `src/frontend/src/app/features/documents/documents-page.component.ts:1015-1017` |
| Admin / restringido | `#4338ca`, `#6d28d9` | Badge `[Solo ADMIN]`. | `src/frontend/src/app/features/donatarias/donatarias-page.component.ts:785-786`, `src/frontend/src/app/features/documents/documents-page.component.ts:1010-1012` |

## Tipografía y base visual

- Tipografía base: `"IBM Plex Sans", "Segoe UI", sans-serif`.
- Tamaño base: `16px`.
- Color scheme: claro.
- Titulares grandes en shell: `clamp(2rem, 4vw, 3.6rem)` con interlínea compacta.
- Para la guía se recomienda mantener una escala más editorial y estable: títulos grandes solo en portada, secciones compactas para contenido operativo.

Evidencia: `src/frontend/src/styles.css:1-9`, `src/frontend/src/app/app.css:35-46`.

## Layout y navegación

- Login institucional con marca FMCPA visible, composición centrada, copy operativo y sin referencias técnicas de implementación.
- Shell autenticado con header superior: logo FMCPA, nombre de plataforma, descriptor operativo y menú de usuario compacto.
- El menú de usuario concentra cambio de contraseña y cierre de sesión; la sesión ya no ocupa un bloque permanente como elemento principal de producto.
- Navegación principal agrupada por secciones: `Inicio`, `Operación`, `Control` y `Administración`.
- La navegación se filtra por permisos efectivos; los grupos sin elementos visibles se ocultan.
- Contenido en pantallas compactas, con tabs locales, KPIs, listados densos, modales contextuales y botones secundarios para exportaciones o acciones de apoyo.
- En pantallas documentales recientes se usa un estilo más compacto con radios de `8px`, tablas/listas densas y botones rectangulares.

Evidencia:
- Shell/header: `src/frontend/src/app/app.html`, `src/frontend/src/app/app.css`.
- Navegación agrupada real: `src/frontend/src/app/app.ts`.
- Rutas reales: `src/frontend/src/app/app.routes.ts`.
- Documentos compactos: `src/frontend/src/app/features/documents/documents-page.component.ts:770-840`.

## Componentes visuales a replicar

| Componente | Patrón detectado | Aplicación en la guía |
| --- | --- | --- |
| Portada / hero | Tarjeta clara con kicker en teal, título fuerte y texto secundario. | Portada simple con "FMCPA Platform" y fecha de corte. |
| Badges de estado | Pills redondeados con fondo suave y texto fuerte. | Leyenda y etiquetas `[Disponible]`, `[Disponible con alcance acotado]`, `[En validación]`, `[Pendiente de fase posterior]`, `[Solo ADMIN]`. |
| Tarjetas operativas | Paneles blancos/translúcidos, sombra `0 16px 30px rgba(32,44,41,0.06)`. | Módulos, recomendaciones y pendientes de fase posterior. |
| Tablas compactas | Bordes suaves, alto contraste moderado, filas separadas. | Matriz funcional y permisos. |
| Navegación interna | Lista lateral/adhesiva en desktop, grid en móvil. | Índice de la guía HTML. |
| Alertas | Fondo rojizo claro para fase posterior; ámbar para alcance acotado. | Bloque visible de pendientes de fase posterior. |
| Tabs locales | Barra compacta con estado activo verde petróleo. | Donatarias, Mercados, Financieras, Federación y Documentos. |
| Modales contextuales | Diálogos para altas/capturas secundarias sin saturar la pantalla principal. | Donatarias, Mercados, Financieras, Federación y Admin Users. |

## Tokens recomendados para `user-guide.html`

```css
:root {
  --fmcpa-bg: #f4f2eb;
  --fmcpa-bg-soft: #f7f6f2;
  --fmcpa-bg-warm: #ede8db;
  --fmcpa-surface: rgba(255, 255, 255, 0.88);
  --fmcpa-ink: #1d2d2a;
  --fmcpa-muted: #445854;
  --fmcpa-muted-2: #60716d;
  --fmcpa-teal: #0f766e;
  --fmcpa-teal-dark: #123f3b;
  --fmcpa-cream: #f6f6f2;
  --fmcpa-border: rgba(29, 45, 42, 0.10);
  --fmcpa-shadow: 0 16px 30px rgba(32, 44, 41, 0.06);
  --fmcpa-danger: #9f1239;
  --fmcpa-warning: #92400e;
  --fmcpa-success: #166534;
  --fmcpa-info: #1d4ed8;
  --fmcpa-admin: #4338ca;
}
```

## Observaciones

- El sistema no usa una librería visual externa pesada; los estilos están embebidos por componente.
- La guía debe mantener el tono operativo y sobrio del producto: tarjetas, KPIs, badges, tablas y navegación clara.
- La aplicación usa logo FMCPA en login y header. La guía HTML no debe depender de assets fuera de `docs`; si no existe una copia documental del logo, debe representarse la marca con un identificador visual textual para no romper el HTML.
- No se incluyeron screenshots porque el entregable solicitado no requiere levantar el sistema y no debe inventar capturas.
