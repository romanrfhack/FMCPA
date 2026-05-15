# Admin Users UX Etapa A - Compactacion visual

Fecha: 2026-05-15

## Objetivo

Mejorar `/admin/users` como quick win visual: eliminar overflow global, compactar acciones por usuario y alinear la pantalla con el estandar visual reciente del sistema, sin modificar backend, contratos API, permisos, rutas ni logica funcional.

## Implementado

- Encabezado compacto con accion primaria `Crear usuario` y accion secundaria `Actualizar`.
- Formulario de alta movido de bloque inline permanente a dialog local responsive con `role="dialog"` y `aria-modal="true"`.
- Listado de usuarios conservado como tarjetas compactas con badges tipo pill y grilla de metadata responsive.
- Acciones por usuario compactadas:
  - `Desactivar` / `Reactivar` queda como accion visible por fila.
  - `Cambiar rol`, `Restablecer contraseña` y `Limpiar bloqueo` quedan dentro de un panel `Gestionar` desplegable por usuario.
- Labels principales traducidos a lenguaje operativo:
  - `UserName` -> `Usuario`.
  - `RoleCode` / `Rol base` -> `Rol`.
  - `Reset password` -> `Restablecer contraseña`.
  - `Lockout` -> `Bloqueo`.
  - `Ultimo login` -> `Ultimo acceso`.
  - `Creado` / `Actualizado` -> `Alta` / `Actualizacion`.
- Roles visibles traducidos:
  - `ADMIN` -> `Administrador`.
  - `OPERATOR` -> `Operador`.
  - `READONLY` -> `Consulta`.

## Como se resolvio overflow

- Se elimino la grilla horizontal de seis controles permanentes por fila.
- El panel de acciones usa columnas responsivas y pasa a una sola columna en anchos menores.
- Se agregaron `min-width: 0`, `overflow-wrap: anywhere`, botones con wrapping seguro en movil y `repeat(auto-fit, minmax(...))` para metadata.
- El alta de usuario ya no ocupa una columna lateral fija ni empuja el listado.

## Validacion responsive

- `npm run build`: exitoso; conserva warnings preexistentes de budget CSS en Documents y Donatarias.
- `npm test -- --watch=false`: exitoso, `25/25`.
- `dotnet build src/backend/FMCPA.Backend.sln --no-restore`: exitoso.
- Playwright/Chromium headless con API mockeada sobre Angular local:
  - login ADMIN;
  - abrir `/admin/users`;
  - crear usuario;
  - cambiar rol;
  - desactivar/reactivar;
  - restablecer contraseña;
  - limpiar bloqueo;
  - validar viewports `390px`, `768px` y `1366px`;
  - confirmar sin overflow horizontal global con paneles de acciones abiertos.
- `git diff -- src/backend`: sin cambios.
- `git diff --check`: exitoso.

## Fuera de alcance

- Cambios backend, endpoints, contratos API o permisos.
- Cambios de rutas, guards, migraciones, CI/CD o produccion.
- Borrado de usuarios, self-service, invitaciones o recuperacion avanzada.
- Menu contextual avanzado con componente compartido; se uso un panel desplegable local para mantener bajo riesgo.
