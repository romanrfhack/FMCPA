# Security Track Document Upload Hardening Implementation Note

## Objetivo
- Endurecer uploads y downloads documentales existentes en `Track 2`.
- Cubrir cédulas de Mercados, evidencias de Donatarias y evidencias de Federacion sin abrir antivirus, DLP, storage externo ni plataforma documental nueva.

## Que se implemento
- Helper reusable `DocumentUploadSecurity` en API.
- Validacion minima de `IFormFile` antes de guardar:
  - archivo obligatorio y no vacio
  - tamano maximo `10 MB`
  - extensiones permitidas `.pdf`, `.jpg`, `.jpeg`, `.png`
  - content-types permitidos `application/pdf`, `image/jpeg`, `image/png`
  - extension y content-type deben corresponder
  - firma basica PDF/JPEG/PNG antes de persistir
  - nombre original saneado y truncado como metadata
- Helper `DocumentDownloadResponseSupport` para respuestas de descarga:
  - `X-Content-Type-Options: nosniff`
  - `Cache-Control: no-store`
  - `Pragma: no-cache`
  - `Content-Disposition` de attachment usando nombre saneado
  - content-type de salida limitado a la whitelist u `application/octet-stream`
- `LocalDocumentBinaryStore` tambien sanea el nombre original en guardado y lectura.
- Los nombres fisicos siguen siendo GUID generados por el sistema; el nombre original queda solo como metadata.
- Las rutas relativas siguen resueltas por el storage local y rechazando path traversal.

## Decisiones tomadas
- Se usa una politica comun para las tres areas documentales porque no hay una regla documentada que justifique diferencias por modulo en esta etapa.
- El limite se fija en `10 MB` por archivo para mantener una barrera clara y suficiente para el MVP/post-MVP actual.
- La whitelist se limita a PDF/JPEG/PNG porque cubre documentos escaneados e imagenes simples sin permitir formatos ejecutables u ofimaticos mas riesgosos.
- Se agrega firma basica de archivo, no sniffing complejo ni analisis profundo.
- No se crea migracion nueva: `StoredDocument` y las tablas de negocio ya guardan metadata suficiente.
- No se agrega auditoria forense de descargas o rechazos; los uploads exitosos ya generan eventos operativos existentes en bitacora.

## Reglas aplicadas

| Area | Endpoint de upload | Endpoint de download | Permiso de download |
| --- | --- | --- | --- |
| Mercados - cédula locatario | `POST /api/markets/{marketId}/tenants` | `GET /api/markets/tenants/{tenantId}/cedula` | `MARKETS_READ` |
| Donatarias - evidencia | `POST /api/donations/applications/{applicationId}/evidences` | `GET /api/donations/applications/evidences/{evidenceId}/download` | `DONATIONS_READ` |
| Federacion - evidencia | `POST /api/federation/applications/{applicationId}/evidences` | `GET /api/federation/applications/evidences/{evidenceId}/download` | `FEDERATION_READ` |

## Como funciona la validacion del upload
1. El endpoint recibe el multipart autenticado.
2. Se validan campos de negocio existentes.
3. `DocumentUploadSecurity.ValidateAsync` valida archivo, metadata declarada y firma basica.
4. Si falla, la API responde `400 ValidationProblem` con mensajes claros.
5. Si pasa, se guarda con nombre fisico controlado por sistema y nombre original saneado.
6. Se persiste metadata en la entidad de negocio y en `StoredDocument`.

## Como funcionan las descargas
1. El endpoint exige el permiso de lectura del modulo por la policy ya aplicada al grupo.
2. Se busca metadata en `StoredDocument` y se conserva fallback legado hacia la entidad de negocio.
3. `IDocumentBinaryStore.InspectAsync` valida ruta segura, existencia y tamano esperado.
4. Si hay inconsistencia, se responde `409` sin exponer ruta fisica.
5. Si el archivo existe, se sirve con headers seguros y nombre original saneado.

## Validacion local sugerida
```bash
dotnet restore src/backend/FMCPA.Backend.sln
dotnet build src/backend/FMCPA.Backend.sln
npm run build --prefix src/frontend

FMCPA_SQL_CONTAINER_NAME=fmcpa-sql-docupload-14338 \
FMCPA_SQL_PORT=14338 \
FMCPA_DB_NAME=FMCPA_DocumentUploadHardening_20260505 \
FMCPA_API_PORT=5095 \
FMCPA_WEB_PORT=4204 \
FMCPA_STORAGE_ROOT=/tmp/fmcpa-doc-upload-hardening-storage \
FMCPA_AUTH_BOOTSTRAP_PASSWORD='LocalAdmin123!Aa' \
FMCPA_AUTH_OPERATOR_PASSWORD='LocalOperator123!Aa' \
FMCPA_AUTH_READONLY_PASSWORD='LocalReadonly123!Aa' \
FMCPA_AUTH_JWT_SIGNING_KEY='DocumentUploadHardeningLocalSigningKey20260505OnlyForValidation1234567890' \
./scripts/local/dev-up.sh --no-frontend
```

Crear archivos temporales:
```bash
mkdir -p /tmp/fmcpa-doc-upload-validation
printf '%b' '%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF\n' > /tmp/fmcpa-doc-upload-validation/valid.pdf
: > /tmp/fmcpa-doc-upload-validation/empty.pdf
printf '%s' 'not an allowed executable' > /tmp/fmcpa-doc-upload-validation/invalid.exe
printf '%b' '%PDF-1.4\ntext type mismatch\n%%EOF\n' > /tmp/fmcpa-doc-upload-validation/wrong-content-type.pdf
dd if=/dev/zero of=/tmp/fmcpa-doc-upload-validation/oversized.pdf bs=1M count=11
```

Verificar por API:
- login `ADMIN` y `READONLY`
- crear datos minimos de Mercado, Donacion, Aplicacion de Donatarias, Donacion de Federacion y Aplicacion de Federacion
- subir `valid.pdf` en las tres areas y esperar `201`
- subir `empty.pdf` y esperar `400`
- subir `invalid.exe` y esperar `400`
- subir `wrong-content-type.pdf` como `text/plain` y esperar `400`
- subir `oversized.pdf` y esperar `400`
- descargar los tres documentos como `ADMIN` y esperar `200` con `nosniff`, `no-store` y `Content-Disposition: attachment`
- intentar upload documental con `READONLY` y esperar `403`
- intentar download sin token y esperar `401`

## Validacion ejecutada
- `dotnet restore src/backend/FMCPA.Backend.sln` -> exitoso.
- `dotnet build src/backend/FMCPA.Backend.sln` -> exitoso, `0 Warning(s)`, `0 Error(s)`.
- `npm run build` en `src/frontend` -> exitoso.
- `dotnet tool restore` -> `dotnet-ef` `10.0.6` restaurado durante `dev-up`.
- Migraciones aplicadas sobre `FMCPA_DocumentUploadHardening_20260505`; no se genero migracion nueva.
- Backend ejecutado en `http://127.0.0.1:5095`.
- Resultado runtime:
  - login `ADMIN`: `200`
  - login `READONLY`: `200`
  - upload valido Mercados: `201`
  - upload valido Donatarias: `201`
  - upload valido Federacion: `201`
  - upload vacio: `400`
  - upload extension invalida: `400`
  - upload content-type invalido: `400`
  - upload tamano excedido: `400`
  - upload con `READONLY`: `403`
  - download Mercados: `200`, headers seguros `OK`
  - download Donatarias: `200`, headers seguros `OK`
  - download Federacion: `200`, headers seguros `OK`
  - download sin token: `401`
  - nombre original `../cedula validacion.PDF` quedo como `cedula validacion.pdf`
- `npx vitest run src/app/core/interceptors/auth.interceptor.spec.ts src/app/core/services/auth.service.spec.ts` -> `2 passed`, `4 passed`.

## Observacion sobre descarga denegada por permiso
- La matriz vigente otorga lectura documental a `READONLY`, `OPERATOR` y `ADMIN`.
- Por eso no existe un rol oficial que produzca `403` de descarga por falta de `*_READ` sin cambiar la matriz aprobada.
- La proteccion de descarga si esta aplicada por policy de modulo; la validacion runtime cubrio `401` sin token y `403` para escritura documental con `READONLY`.

## Que quedo fuera
- Antivirus, DLP, sandboxing de archivos o analisis profundo de contenido.
- Storage externo, CDN, object storage o presigned URLs.
- Politica documental completa de respaldo, retencion, versionado o limpieza.
- Permisos por documento individual o RBAC ultra fino.
- Auditoria forense de cada descarga o de cada rechazo de upload.
- Cambios de produccion o CI/CD.
