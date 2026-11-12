# SNIC — Sistema Nacional de Información Crediticia

Monorepo para la API ASP.NET Core, el cliente React y SQL Server. El esquema se administra con migraciones de Entity Framework Core.

La fuente canónica del esquema es `SNICDB_Creacion_Completa.sql`. La migración `InitialCreate` está sincronizada con sus 13 tablas, relaciones, restricciones, índices y datos iniciales. EF Core agrega únicamente su tabla técnica `__EFMigrationsHistory` al aplicar la migración.

## Requisitos

- .NET SDK 10.0.201 o una versión compatible con `global.json`.
- Node.js 24 y npm.
- Docker Desktop con Docker Compose.

No es necesario instalar herramientas globales. `dotnet-ef` está fijado en `dotnet-tools.json` y las dependencias web viven en `src/snic-web/node_modules`.

## Configuración inicial

1. Crea los archivos locales a partir de las plantillas si todavía no existen:

```powershell
Copy-Item .env.example .env
Copy-Item src/Snic.Api/.env.example src/Snic.Api/.env
Copy-Item src/snic-web/.env.example src/snic-web/.env
```

Usa la misma contraseña en `SA_PASSWORD`, dentro del `.env` raíz, y en `Password=...`, dentro de `src/Snic.Api/.env`. La API carga automáticamente su archivo; Vite carga automáticamente el suyo.

En ambiente `Development`, `BootstrapAdmin__Username` y `BootstrapAdmin__Password` crean el primer administrador cuando la tabla de cuentas está vacía. Si ya existe cualquier cuenta, el inicializador no modifica la base de datos. Las credenciales incluidas en la plantilla son únicamente demostrativas y deben cambiarse fuera del entorno local.

2. Restaura las dependencias:

```powershell
dotnet tool restore
dotnet restore
npm.cmd install --prefix src/snic-web
```

3. Inicia SQL Server:

```powershell
docker compose up -d sqlserver
docker compose ps
```

4. Aplica la migración; la herramienta carga `src/Snic.Api/.env`:

```powershell
dotnet tool run dotnet-ef database update --project src/Snic.Api
```

## Ejecución

En dos terminales:

```powershell
dotnet run --project src/Snic.Api
npm.cmd run dev --prefix src/snic-web
```

- Swagger UI: URL HTTPS de la API seguida de `/swagger`.
- OpenAPI JSON: `/openapi/v1.json`.
- Salud de API y base de datos: `/health`.
- React: `http://localhost:5173`.

## Autenticación y administración

La primera cuenta se crea una sola vez con `POST /api/v1/setup`. Cuando ya existe una cuenta, la ruta responde `409 Conflict`. El inicio de sesión está en `POST /api/v1/auth/login`; las consultas y operaciones protegidas usan `Authorization: Bearer <token>`.

Las rutas administrativas son neutrales (`/api/v1/institutions` y `/api/v1/accounts`): el rol `ADMINISTRATOR` se comprueba en el servidor y no forma parte de la URL. Consulta ejemplos en `src/Snic.Api/Snic.Api.http` y el detalle técnico en `docs/Documento_Tecnico.md`.

La API incluye además registro y corrección de personas y productos crediticios, auditoría de correcciones y consulta consolidada por cédula. El manual completo está en `docs/Manual_Uso_API.md`.

Los tres roles pueden consultar instituciones, personas, productos y reportes. `ADMINISTRATOR` administra instituciones y cuentas y corrige datos personales. `DATA_ENTRY` registra personas y crea o corrige exclusivamente productos de su institución; esta se obtiene desde la cuenta autenticada. `CONSUMER` tiene acceso de consulta. Swagger describe los roles permitidos en cada operación.

## Migraciones posteriores

```powershell
dotnet tool run dotnet-ef migrations add NombreDescriptivo --project src/Snic.Api --output-dir Infrastructure/Migrations
dotnet tool run dotnet-ef database update --project src/Snic.Api
```

`SNIC_Entrega_2.pdf` describe un sistema distinto (autobuses y estudiantes). Para la base de datos crediticia prevalece `SNICDB_Creacion_Completa.sql`.
