# Hardening Track Smoke MVP Implementation Note

## Objetivo
- Dejar una validacion funcional minima, repetible y automatizada del MVP para detectar regresiones rapidas en futuras sesiones sin convertirla en una suite pesada.

## Que se implemento
- Script `scripts/local/smoke-mvp.sh`.
- Reutilizacion de la convencion local ya aprobada en `scripts/local/common.sh`.
- Creacion de datos tecnicos minimos por API para:
  - Contacts
  - Markets
  - Donations
  - Financials
  - Federation
- Validacion automatica de:
  - `/health`
  - `/api/dashboard/summary`
  - `/api/dashboard/alerts`
  - `/api/commissions/consolidated`
  - `/api/bitacora`
  - `/api/history/closed-items`
  - `/api/documents/integrity`
- Cierre formal automatizado de los registros tecnicos cerrables para verificar historico y reducir ruido posterior.

## Decisiones tomadas
- `smoke.sh` permanece como smoke basico de plataforma y wiring rapido.
- `smoke-mvp.sh` queda separado como smoke funcional del MVP por API.
- El smoke MVP crea datos tecnicos etiquetados con un prefijo unico por corrida en lugar de depender de datos preexistentes.
- No se agregaron uploads documentales ni frontend interactivo al smoke MVP para mantenerlo corto y estable.

## Como usarlo

### Flujo recomendado
```bash
./scripts/local/doctor.sh
./scripts/local/up-sqlserver.sh
./scripts/local/apply-migrations.sh
./scripts/local/run-backend.sh
./scripts/local/smoke-mvp.sh
```

### Flujo determinista
```bash
./scripts/local/doctor.sh
./scripts/local/reset-db.sh --force
./scripts/local/run-backend.sh
./scripts/local/smoke-mvp.sh
```

### Overrides locales
- Si se requiere otra combinacion de puertos o contenedor, usar `.env.local` o variables de entorno del proceso.
- `SMOKE_MVP_TAG` puede definirse para forzar un prefijo tecnico especifico.

## Que valida
- Salud del backend y acceso a la base local configurada.
- Wiring sano del dashboard, alertas, comisiones consolidadas, bitacora, historico e integridad documental.
- Creacion minima por API de registros tecnicos en Markets, Donations, Financials y Federation.
- Accesibilidad de cierres formales e historico con `FORMAL_CLOSE_EVENT`.
- Respuesta funcional de la consolidacion de comisiones de Financieras y Federation.

## Que no valida
- Frontend interactivo real ni navegacion UI.
- Uploads y descargas documentales ricas.
- Analitica avanzada.
- Casos de negocio complejos o regresiones visuales.
- Seguridad, autenticacion o autorizacion.

## Que quedo fuera
- Suite de testing formal.
- Frameworks pesados de browser automation.
- Limpieza automatica completa de todos los datos tecnicos generados.
- Validacion de todos los endpoints del sistema.

## Convivencia con el tooling local existente
- `doctor.sh` se usa antes de empezar una sesion o cuando haya dudas sobre puertos y prerequisitos.
- `reset-db.sh` se usa cuando se necesita una corrida limpia y repetible.
- `smoke.sh` sigue siendo el chequeo corto de plataforma.
- `smoke-mvp.sh` se usa cuando se quiere una señal funcional minima del MVP.

## Riesgos operativos conocidos
- El script deja datos tecnicos etiquetados en la base si no se resetea antes o despues.
- Si faltan migraciones o seeds de catalogos, el smoke puede fallar en los POST de validacion.
- Si el backend no esta levantado en el puerto configurado, el smoke fallara de inmediato en `/health`.
