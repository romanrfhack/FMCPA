# Security Track Authorization Surface Guardrails Implementation Note

## Que se implemento
- Se agrego un manifiesto operativo machine-readable en `docs/05-post-mvp/security-authorization-surface-guardrails.json`.
- Se agregaron pruebas `AuthorizationSurfaceGuardrailTests` dentro de `FMCPA.Api.AuthorizationRegressionTests`.
- Las pruebas introspectan la superficie real del backend via `EndpointDataSource` y extraen:
  - ruta;
  - metodo HTTP;
  - si el endpoint es publico o declara autorizacion;
  - policies declaradas en metadata `Authorize`.
- El guardrail falla ante:
  - endpoints publicos no listados en el allowlist;
  - endpoints `/api` sin regla publica o protegida en el manifiesto;
  - reglas protegidas del manifiesto sin endpoint real;
  - endpoints protegidos con policy distinta a la esperada.

## Enfoque elegido
El manifiesto JSON es la referencia operativa ejecutable. `security-authorization-surface-inventory.md` queda como inventario humano y debe mantenerse alineado con el JSON.

Esta decision evita parsear Markdown desde tests y evita deducciones fragiles por nombres de ruta. El test usa metadata real del host de ASP.NET Core levantado por `WebApplicationFactory`.

## Artefactos
- Operativo para tests: `docs/05-post-mvp/security-authorization-surface-guardrails.json`
- Humano/documental: `docs/05-post-mvp/security-authorization-surface-inventory.md`
- Suite: `src/backend/tests/FMCPA.Api.AuthorizationRegressionTests`

## Pruebas nuevas
- `Public_endpoints_match_the_explicit_allowlist`
- `Api_endpoints_are_either_protected_or_explicitly_public`
- `Protected_manifest_rules_have_matching_real_endpoints`
- `Protected_api_endpoints_declare_the_expected_authorization_metadata`

## Como mantenerlo
Cuando se agregue o cambie un endpoint:
- si es publico, debe agregarse explicitamente al `publicEndpoints` del JSON con su razon;
- si es protegido, debe existir una regla en `protectedEndpointRules` con metodo, patron de ruta y policy esperada;
- si cambia una superficie funcional o la regla por rol, se actualiza tambien el inventario Markdown;
- si el cambio requiere comportamiento nuevo por rol, se agrega o ajusta una prueba representativa en `AuthorizationRegressionTests`.

## Validacion local
Comandos:

```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln --no-restore
dotnet test src/backend/tests/FMCPA.Api.AuthorizationRegressionTests/FMCPA.Api.AuthorizationRegressionTests.csproj --logger "console;verbosity=minimal"
```

Resultado esperado:
- restore exitoso;
- build exitoso;
- suite `FMCPA.Api.AuthorizationRegressionTests` exitosa, incluyendo los guardrails de superficie.

## Hallazgo durante implementacion
El guardrail detecto una deriva documental previa: el inventario/manifiesto inicial incluia `POST /api/contact-types`, pero el backend real no expone ese endpoint. Se corrigio el inventario para dejar solo `POST /api/contacts` bajo `CONTACTS_WRITE`.

## Que quedo fuera
- No se agregaron nuevas features de autenticacion o autorizacion.
- No se abrio RBAC fino por endpoint/accion.
- No se agrego edicion manual de permisos por usuario.
- No se tocaron produccion ni CI/CD.
- No se agrego E2E pesado ni framework nuevo.
- No se validan payloads o reglas de negocio profundas; este guardrail cubre superficie HTTP y metadata de autorizacion.
