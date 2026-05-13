# Change log de guía rápida FMCPA

## Versión final 1.1

**Fecha de ajuste:** 13 de mayo de 2026
**Archivos base revisados:** `user-guide-source.md`, `feature-status-matrix.md`, `user-guide-outline.md`, `user-guide-final-source.md`, `user-guide.html`, `user-guide-final.html` y notas de Donatarias Transparencia Fases 1 a 5A.

## Qué se ajustó en esta versión

- Se actualizó Donatarias para presentarlo como módulo de transparencia del recurso donado, no solo como captura de donaciones, aplicaciones y evidencias.
- Se incorporaron los tabs reales de `/donatarias`: `Resumen`, `Donaciones`, `Aplicaciones / distribución`, `Evidencias` y `Reporte de transparencia`.
- Se agregaron KPIs visibles: total recibido, total aplicado, saldo pendiente, porcentaje aplicado, número de aplicaciones y evidencias registradas.
- Se documentaron distribución financiera por aplicación, evidencias por aplicación, semáforos financiero/documental/operativo y readiness `READY`/`PARTIAL`/`NOT_READY`.
- Se agregó el reporte de transparencia y su vista imprimible como capacidades disponibles.
- Se reforzó la advertencia: la evidencia mínima registrada no sustituye revisión legal, fiscal o contable.
- Se mantuvieron visibles los pendientes de fase posterior: CSV específico de Donatarias, PDF oficial, folio, firma, versionamiento de reporte, validación legal/fiscal/contable, checklist documental avanzado y catálogo formal de donantes.
- Se mejoró la versión HTML final para móviles, tablets y escritorio con navegación interna usable, tarjetas apilables, tablas con scroll controlado, protección contra overflow horizontal y reglas de impresión.
- No se tocó código funcional, backend, frontend de aplicación, permisos, schema ni migraciones.

## Versión final 1.0

**Fecha de ajuste:** 12 de mayo de 2026  
**Archivos base revisados:** `system-visual-reference.md`, `feature-status-matrix.md`, `user-guide-outline.md`, `user-guide-source.md`, `user-guide.html`.

## Qué se ajustó respecto a la versión inicial

- Se creó una fuente final orientada a usuario operativo en `docs/user-guide/drafts/user-guide-final-source.md`.
- Se creó una versión HTML final en `docs/user-guide/user-guide-final.html`.
- Se actualizó `docs/user-guide/user-guide.html` para conservar la identidad visual inicial con una presentación más institucional.
- Se agregó la sección inicial **Cómo usar esta guía**.
- Se incorporaron bloques destacados para **Inicio rápido**, **Módulos principales** y **Pendientes de fase posterior**.
- Se reorganizaron recomendaciones en cuatro grupos: diarias, documentales, seguridad y Financieras.
- Se agregó soporte de impresión con reglas para evitar cortes inadecuados de tarjetas, bloques y tablas.
- Se mantuvieron rutas reales del sistema, roles reales y alcance funcional derivado del repositorio.

## Lenguaje suavizado

La versión inicial usaba etiquetas útiles para análisis interno, pero duras para entrega a cliente. En la guía final se aplicó esta equivalencia:

| Lenguaje inicial | Lenguaje final |
| --- | --- |
| `Parcial` | `Disponible con alcance acotado` |
| `En desarrollo` | `En validación` |
| `Pendiente crítica` | `Pendiente de fase posterior` |
| `falta / no existe` | `No incluido en esta versión` o `Requiere definición adicional`, según el contexto |

También se redujo lenguaje técnico visible en portada y contenido principal:

- Se retiraron de portada referencias a tecnología interna.
- Se sustituyó lenguaje técnico de sesión por descripciones operativas.
- Se evitaron términos de arquitectura salvo en documentos de trazabilidad o evidencia.
- Se mantuvieron los nombres de rutas y roles porque forman parte de la operación real del sistema.

## Pendientes que siguen visibles

Los siguientes puntos se mantienen visibles como `Pendiente de fase posterior` o `Requiere definición adicional`:

- Aprobación formal de varios tracks post-MVP.
- Robustecimiento de bitácora, histórico y marca formal de cierre.
- Política documental avanzada: respaldo formal, retención formal, limpieza segura, legal hold formal, OCR, almacenamiento externo y cumplimiento especializado.
- Reportes avanzados, inteligencia de negocio, exportaciones masivas y reportes históricos formales.
- Seguridad avanzada: autenticación multifactor, proveedor externo de identidad, recuperación avanzada de contraseña, monitoreo especializado y sesiones avanzadas.
- Catálogo maestro y normalización fuerte para financieras, instituciones, dependencias y stands.
- Notificaciones externas por correo, WhatsApp u otros canales.
- Alcance acotado en suficiencia documental, reglas finas de Federación, bitácora, histórico y comisiones.

## Trazabilidad conservada

- `docs/user-guide/drafts/feature-status-matrix.md` conserva evidencia por módulo, pantalla, estado y justificación.
- `docs/user-guide/drafts/system-visual-reference.md` conserva la referencia visual tomada del frontend real.
- `docs/user-guide/drafts/user-guide-source.md` queda como fuente base inicial ajustada a la nueva nomenclatura.
- `docs/user-guide/drafts/user-guide-final-source.md` es la fuente recomendada para exportar o convertir a PDF/DOCX.
