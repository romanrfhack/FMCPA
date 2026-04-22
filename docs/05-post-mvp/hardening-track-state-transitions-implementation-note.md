# Hardening Track State Transitions Implementation Note

## Objetivo del subpaso
- Endurecer transiciones de cierre y mutaciones obvias sobre registros terminales.
- Mejorar consistencia entre alertas, historico y bitacora sin introducir una state machine compleja.

## Que se implemento
- Se agrega `StateTransitionSupport` como capa minima compartida para reglas y mensajes de cierre o mutacion invalida.
- Se bloquea el cierre formal cuando la entidad ya:
  - tiene un evento de cierre registrado
  - o ya se encuentra en estado terminal
- Se bloquean mutaciones nuevas sobre padres cerrados o archivados en:
  - Mercados
  - Donatarias
  - Financieras
  - Federacion
- El frontend ahora deshabilita el cierre formal en registros ya terminales y muestra mensajes mas claros si el metodo se dispara de todas formas.
- No se crea migracion porque no hubo cambio de esquema.

## Reglas minimas aplicadas por modulo
### Markets
- `Market`
  - no permite registrar cierre formal si ya existe evento de cierre
  - no permite registrar cierre formal si ya esta `CLOSED` o `ARCHIVED`
  - no permite registrar nuevos locatarios si el mercado esta terminal
  - no permite registrar nuevas incidencias si el mercado esta terminal

### Donations
- `Donation`
  - no permite registrar cierre formal si ya existe evento de cierre
  - no permite registrar cierre formal si ya esta `CLOSED`
  - si la donacion sigue en estado no terminal, se permite cierre formal por decision operativa, incluso si no quedo totalmente aplicada
  - no permite registrar nuevas aplicaciones sobre una donacion terminal
  - no permite adjuntar nuevas evidencias sobre aplicaciones de una donacion terminal

### Financials
- `FinancialPermit`
  - no permite registrar cierre formal si ya existe evento de cierre
  - no permite registrar cierre formal si ya esta terminal
  - se permite cierre formal desde estados no terminales como `ACCEPTED`, `IN_PROCESS` o `RENEW`
  - no permite registrar nuevos creditos sobre un oficio terminal
  - no permite registrar nuevas comisiones sobre creditos cuyo oficio padre ya esta terminal

### Federation
- `FederationAction`
  - no permite registrar cierre formal si ya existe evento de cierre
  - no permite registrar cierre formal si ya esta `CLOSED`
  - no permite vincular nuevos participantes si la gestion ya es terminal
- `FederationDonation`
  - no permite registrar cierre formal si ya existe evento de cierre
  - no permite registrar cierre formal si ya esta `CLOSED`
  - si la donacion sigue en estado no terminal, se permite cierre formal por decision operativa aunque no este totalmente aplicada
  - no permite registrar nuevas aplicaciones sobre una donacion terminal
  - no permite adjuntar nuevas evidencias sobre aplicaciones de una donacion terminal
  - no permite registrar nuevas comisiones sobre aplicaciones de una donacion terminal

## Decisiones tomadas
- No se redisenaron los modelos de estado; el endurecimiento vive en API como validacion operativa minima.
- Se uso `409 Conflict` con mensajes claros para conflictos de transicion y cierre.
- La bitacora solo registra eventos cuando la operacion realmente ocurre.
- El criterio operativo de “carpetazo” se conserva para donaciones, oficios y gestiones en estados no terminales.

## Como validar localmente
### Compilacion
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
npm run build
```

### Entorno local
```bash
FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5091 ./scripts/local/reset-db.sh --force
FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5091 ./scripts/local/dev-up.sh --no-frontend
```

### Casos a probar
- Crear un `Market`, `Donation`, `FinancialPermit`, `FederationAction` y `FederationDonation` en estado no terminal.
- Registrar un cierre formal valido para cada uno.
- Repetir el cierre y confirmar `409 Conflict` con mensaje claro.
- Intentar una mutacion nueva sobre el padre ya terminal y confirmar `409 Conflict`.
- Consultar:
  - `/api/bitacora?q=STATE-T1&take=100`
  - `/api/history/closed-items?q=STATE-T1`

## Validacion local ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln`
- `dotnet build src/backend/FMCPA.Backend.sln`
- `npm run build`
- `FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5090 ./scripts/local/reset-db.sh --force`
- `FMCPA_SQL_PORT=14334 FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-local FMCPA_API_PORT=5091 ./scripts/local/dev-up.sh --no-frontend`
- Creacion tecnica por API de:
  - `Market`
  - `Donation`
  - `FinancialPermit`
  - `FederationAction`
  - `FederationDonation`
  - un `Contact` interno para validar participantes
- Registro de cinco cierres formales validos.
- Intentos repetidos de cierre sobre las cinco entidades, todos bloqueados con `409`.
- Intentos de mutacion sobre padres terminales:
  - incidencia en mercado cerrado
  - aplicacion sobre donacion cerrada
  - credito sobre oficio cerrado
  - participante sobre gestion cerrada
  - aplicacion sobre donacion de federacion cerrada
- Creacion de cinco registros ya terminales desde origen y validacion de que tampoco aceptan cierre formal.

## Resultado de la validacion
- Los cinco cierres validos respondieron `200`.
- Los cierres repetidos respondieron `409` con mensajes claros.
- Las mutaciones sobre padres terminales respondieron `409` con mensajes claros.
- Los registros creados ya terminales respondieron `409` al intentar cierre formal.
- La bitacora de la corrida de validacion mostro exactamente `5` eventos de cierre formal reales y ninguno adicional por intentos fallidos.
- El historico de la corrida de validacion mostro `5` registros con `FORMAL_CLOSE_EVENT`.

## Que quedo fuera
- Una state machine formal por modulo.
- Reglas finas para todas las entidades hijas y todas las combinaciones posibles de estado.
- Flujos de reapertura o transiciones controladas de salida desde estados terminales.
- Cambios de seguridad, documentos transversales o analitica.
