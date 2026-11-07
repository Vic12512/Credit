# Documento técnico — SNIC

## Alcance del entregable

Este incremento implementa el mínimo funcional de RF-01 (autenticación con JWT) y RF-02 (control de permisos), además de la administración básica de instituciones y cuentas necesaria para demostrar ambos requisitos.

## Estructura

- `src/Snic.Api/Controllers`: endpoints HTTP.
- `src/Snic.Api/Contracts`: solicitudes y respuestas de la API.
- `src/Snic.Api/Domain`: modelos del dominio.
- `src/Snic.Api/Infrastructure`: DbContext, configuración de entidades, carga de entorno y migraciones.
- `src/Snic.Api/Repositories`: acceso a cuentas e instituciones.
- `src/Snic.Api/Services`: reglas de negocio, contraseñas y JWT.
- `src/snic-web`: cliente React/Vite.

## Configuración y base de datos

La conexión se define localmente mediante `ConnectionStrings__SnicDb` en `src/Snic.Api/.env`. `EnvFile` carga ese archivo antes de construir la aplicación y no sobrescribe variables ya definidas por el sistema. La migración `20261106001802_InitialCreate` crea las 13 tablas de negocio sincronizadas con `SNICDB_Creacion_Completa.sql`; Entity Framework agrega `__EFMigrationsHistory` para controlar migraciones.

`SnicDbContext` expone los modelos y `EntityConfigurations` define claves, relaciones, índices, restricciones y conversiones. Las dependencias se registran con inyección de dependencias en `Program.cs`.

## Seguridad

- `POST /api/v1/setup`: crea exclusivamente la primera cuenta con rol `ADMINISTRATOR`; después responde `409 Conflict` permanentemente.
- `POST /api/v1/auth/login`: valida usuario, contraseña PBKDF2, estado y bloqueo; entrega un JWT con vencimiento.
- `GET /api/v1/auth/me`: devuelve la cuenta autenticada.
- Cada solicitud autenticada vuelve a consultar la cuenta. Una desactivación o bloqueo invalida incluso un JWT previamente emitido.
- Las operaciones de instituciones y cuentas requieren el rol `ADMINISTRATOR`.
- Cinco intentos incorrectos bloquean temporalmente la cuenta durante 15 minutos.
- En `Development`, un inicializador idempotente puede crear la primera cuenta administradora usando únicamente `BootstrapAdmin__Username` y `BootstrapAdmin__Password`. No se ejecuta en otros ambientes ni modifica bases que ya tengan cuentas.

## Endpoints administrativos

- `GET|POST /api/v1/institutions`
- `GET /api/v1/institutions/{id}`
- `GET|POST /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `PATCH /api/v1/accounts/{id}/status`

Las rutas son neutrales: el rol no forma parte de la URL; la autorización se aplica mediante JWT y políticas del servidor.

## Ejecución y comprobación

```powershell
dotnet tool restore
dotnet restore
dotnet tool run dotnet-ef database update --project src/Snic.Api
dotnet run --project src/Snic.Api
```

En desarrollo, Swagger está disponible en `/swagger`. El botón **Authorize** acepta el token obtenido en el inicio de sesión. La solución se comprueba con `dotnet build Snic.slnx`; el cliente con `npm.cmd run build --prefix src/snic-web`.
