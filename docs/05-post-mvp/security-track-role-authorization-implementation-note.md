# Security Track Role Authorization Implementation Note

## Objetivo
- Continuar `Track 2` agregando una capa minima de autorizacion por roles sobre la autenticacion ya existente, sin abrir todavia RBAC fino ni gestion completa de usuarios.

## Que se implemento

### Backend
- `ApplicationUser` ahora persiste `RoleCode`.
- Se agrega la migracion `Track2SecurityRoleAuthorization`.
- La migracion rellena usuarios preexistentes con `READONLY` como valor seguro por defecto; los usuarios bootstrap locales sincronizan despues su rol esperado.
- El token JWT emitido por `POST /api/auth/login` ahora incluye el claim de rol.
- `GET /api/auth/session` devuelve el rol actual junto con el usuario autenticado.
- Se agregan tres politicas fijas:
  - `read`
  - `write`
  - `admin`
- Los endpoints se recablean por grupo:
  - lecturas operativas -> `read`
  - escrituras funcionales -> `write`
  - cierres formales y altas administrativas de catalogos -> `admin`

### Roles definidos
- `ADMIN`
  - Lectura, escrituras funcionales y endpoints administrativos actuales.
- `OPERATOR`
  - Lectura y escrituras funcionales normales.
  - Sin cierres formales ni altas administrativas de catalogos.
- `READONLY`
  - Solo lectura.

### Bootstrap local
- El usuario bootstrap principal `admin` queda como `ADMIN`.
- Se agregan usuarios bootstrap locales opcionales:
  - `operator`
  - `readonly`
- Solo se aprovisionan si su password local se configura fuera del repo.

### Frontend Angular
- La sesion local ahora conserva `roleCode`.
- Se agregan capacidades derivadas minimas:
  - `canWrite`
  - `canAdminister`
- Las rutas de catalogos compartidos quedan protegidas por un guard admin minimo.
- La navegacion principal oculta rutas administrativas para roles no admin.
- Las acciones visibles de escritura y cierre quedan deshabilitadas cuando el rol no puede ejecutarlas.

## Decisiones tecnicas tomadas
- Se uso `RoleCode` persistido en `ApplicationUser` en vez de una tabla adicional de roles para mantener el cambio pequeno y lineal con la foundation ya aprobada.
- Se usaron solo tres politicas backend (`read`, `write`, `admin`) en vez de permisos por endpoint ultra finos para evitar abrir RBAC antes de tiempo.
- Los catalogos compartidos mantienen lectura para roles autenticados porque algunos modulos necesitan esos datos para operar, pero sus altas quedan como capacidad administrativa.
- El frontend solo refleja restricciones minimas; la seguridad real sigue estando en backend.

## Que puede hacer cada rol

### READONLY
- Puede:
  - consultar dashboard y modulos
  - consultar bitacora, historico, comisiones y documentos
  - descargar documentos ya protegidos con autenticacion
- No puede:
  - crear registros
  - cargar evidencias
  - registrar comisiones
  - ejecutar cierres formales
  - entrar a pantallas administrativas de catalogos desde la navegacion protegida

### OPERATOR
- Puede:
  - consultar
  - crear y registrar operaciones funcionales del MVP
  - cargar evidencias y registrar movimientos operativos normales
- No puede:
  - ejecutar cierres formales
  - dar de alta catalogos compartidos sensibles

### ADMIN
- Puede:
  - todo el alcance actual del MVP local autenticado
  - cierres formales
  - altas administrativas de catalogos compartidos actuales

## Como usarlo localmente

### Variables minimas sugeridas
```bash
FMCPA_AUTH_BOOTSTRAP_USER_NAME=admin
FMCPA_AUTH_BOOTSTRAP_DISPLAY_NAME=Administrador local
FMCPA_AUTH_BOOTSTRAP_PASSWORD=LocalAdmin123!Aa

FMCPA_AUTH_OPERATOR_USER_NAME=operator
FMCPA_AUTH_OPERATOR_DISPLAY_NAME=Operador local
FMCPA_AUTH_OPERATOR_PASSWORD=LocalOperator123!Aa

FMCPA_AUTH_READONLY_USER_NAME=readonly
FMCPA_AUTH_READONLY_DISPLAY_NAME=Consulta local
FMCPA_AUTH_READONLY_PASSWORD=LocalReadonly123!Aa
```

### Arranque local minimo
```bash
./scripts/local/doctor.sh
./scripts/local/dev-up.sh
```

## Como probarlo localmente

### Login ADMIN
```bash
curl -s http://127.0.0.1:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<password-admin-local>"}'
```

### Login OPERATOR
```bash
curl -s http://127.0.0.1:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"operator","password":"<password-operator-local>"}'
```

### Login READONLY
```bash
curl -s http://127.0.0.1:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"readonly","password":"<password-readonly-local>"}'
```

### Ver sesion actual
```bash
TOKEN="<token-devuelto-por-login>"
curl -s http://127.0.0.1:5080/api/auth/session \
  -H "Authorization: Bearer ${TOKEN}"
```

### Probar lectura permitida
```bash
curl -s http://127.0.0.1:5080/api/dashboard/summary \
  -H "Authorization: Bearer ${TOKEN}"
```

### Probar escritura funcional permitida para OPERATOR
```bash
curl -i http://127.0.0.1:5080/api/contacts \
  -H "Authorization: Bearer ${TOKEN}" \
  -H 'Content-Type: application/json' \
  -d '{"name":"Probe","contactTypeId":1}'
```

### Probar endpoint administrativo denegado para OPERATOR
```bash
curl -i http://127.0.0.1:5080/api/commission-types \
  -H "Authorization: Bearer ${TOKEN}" \
  -H 'Content-Type: application/json' \
  -d '{"code":"PROBE","name":"Probe","sortOrder":900}'
```

### Probar escritura denegada para READONLY
```bash
curl -i http://127.0.0.1:5080/api/contacts \
  -H "Authorization: Bearer ${TOKEN}" \
  -H 'Content-Type: application/json' \
  -d '{"name":"Probe","contactTypeId":1}'
```

## Como se valido localmente
- `dotnet restore src/backend/FMCPA.Backend.sln`
- `dotnet build src/backend/FMCPA.Backend.sln`
- `npm run build` en `src/frontend`
- `dotnet tool restore`
- Generacion de migracion `Track2SecurityRoleAuthorization`
- Stack local aislado con `dev-up.sh`
- Login real de `admin`, `operator` y `readonly`
- `201 Created` para alta administrativa con `ADMIN`
- `201 Created` para escritura funcional con `OPERATOR`
- `403 Forbidden` para endpoint administrativo con `OPERATOR`
- `403 Forbidden` para escritura funcional con `READONLY`
- `smoke.sh` con probe opcional de `READONLY`
- `smoke-mvp.sh` autenticado con `ADMIN`
- Prueba puntual del guard Angular admin con `vitest`

## Que quedo explicitamente fuera
- RBAC completo por modulo o accion.
- Claims finos y politicas detalladas por recurso.
- Gestion completa de usuarios.
- Recuperacion o cambio de password.
- Refresh tokens o revocacion avanzada.
- Produccion, CI/CD o proveedores externos de identidad.
