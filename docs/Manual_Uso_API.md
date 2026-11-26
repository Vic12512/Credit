# Manual de uso de la API SNIC

## 1. Iniciar el proyecto

Desde la raíz del repositorio:

Necesitas el SDK de .NET indicado en `global.json` y SQL Server. Docker Desktop permite levantar la instancia incluida en `compose.yaml`. Copia las plantillas solo si no tienes ya tus `.env`, para conservar tu configuración.

```powershell
Copy-Item .env.example .env
Copy-Item src/Snic.Api/.env.example src/Snic.Api/.env
docker compose up -d sqlserver
docker compose ps
dotnet tool restore
dotnet restore Snic.slnx
dotnet tool run dotnet-ef database update --project src/Snic.Api
dotnet run --project src/Snic.Api
```

Antes de ejecutar esos comandos, configura `SA_PASSWORD` en el `.env` raíz y la misma contraseña en `ConnectionStrings__SnicDb` del `.env` de la API. Si usas una instancia de SQL Server ya instalada, omite Docker y ajusta la conexión a esa instancia. No necesitas crear `SNICDB` manualmente: `database update` crea la base y sus tablas con una cuenta SQL que tenga esos permisos. Con Docker, espera a que `docker compose ps` indique `healthy` antes de aplicar la migración.

La migración carga roles, tipos de institución y producto, estados, eventos y reglas de score. No carga instituciones, personas ni productos ficticios: los datos de la computadora del autor no viajan con GitHub. En el primer arranque en `Development`, si no hay ninguna cuenta, la API crea el administrador con `BootstrapAdmin__Username` y `BootstrapAdmin__Password` del `.env`; si ya hay cuentas, no modifica sus credenciales. Con la plantilla de demostración el acceso es `admin` / `Admin_Local_2026!`. Configura también una clave `Jwt__Key` de al menos 32 caracteres. El `.env` real no se publica.

Para usar el perfil HTTPS local, ejecuta `dotnet run --project src/Snic.Api --launch-profile https` y abre `https://localhost:7212/swagger` con el certificado de desarrollo configurado. `/health` comprueba API y SQL Server. No es necesario repetir la migración si no hay cambios pendientes.

## 2. Acceder a Swagger

La terminal mostrará la URL de la API. Agrega `/swagger`, por ejemplo `http://localhost:5041/swagger`. Para las operaciones protegidas:

1. Ejecuta `POST /api/v1/auth/login`.
2. Copia `accessToken` sin comillas.
3. Pulsa **Authorize**, pega el token y confirma.

## 3. Operaciones

### Inicialización y autenticación

- `POST /api/v1/setup`: crea el primer administrador únicamente si no existen cuentas.
- `POST /api/v1/auth/login`: recibe `username` y `password`; devuelve el JWT, vencimiento y cuenta.
- `GET /api/v1/auth/me`: comprueba el token y devuelve la cuenta actual.

```json
{
  "username": "admin",
  "password": "Admin_Local_2026!"
}
```

### Instituciones y cuentas

- `GET /api/v1/institutions` y `GET /api/v1/institutions/{id}`: los tres roles pueden consultar instituciones.
- `POST /api/v1/institutions`: registra una institución.
- `GET /api/v1/accounts` y `GET /api/v1/accounts/{id}`: consultan cuentas.
- `POST /api/v1/accounts`: crea una cuenta asociada a una institución, excepto el administrador global.
- `PATCH /api/v1/accounts/{id}/status`: activa o desactiva una cuenta.

Crear instituciones y gestionar o consultar cuentas requiere `ADMINISTRATOR`.

Ejemplo de institución:

```json
{
  "institutionTypeId": 1,
  "code": "BANCO-DEMO",
  "rnc": "101234567",
  "legalName": "Banco de Demostracion, S.A.",
  "commercialName": "Banco Demo"
}
```

Ejemplo de cuenta institucional:

```json
{
  "institutionId": 1,
  "roleCode": "CONSUMER",
  "username": "consulta.demo",
  "email": "consulta@snic.local",
  "displayName": "Consulta Demo",
  "password": "Consumer_Local_2026!"
}
```

Ejemplo de cambio de estado:

```json
{
  "isActive": false,
  "reason": "Cuenta desactivada para prueba"
}
```

### Personas

`POST /api/v1/persons` registra una persona y requiere `DATA_ENTRY`. La cédula debe contener 11 dígitos y no puede repetirse.

```json
{
  "nationalId": "00112345678",
  "firstName": "Ana",
  "middleName": "Maria Elena",
  "firstSurname": "Perez",
  "secondSurname": "Gomez",
  "birthDate": "1992-04-18"
}
```

- `GET /api/v1/persons`: los tres roles pueden listar personas.
- `GET /api/v1/persons/{id}`: los tres roles pueden consultar una persona.
- `PUT /api/v1/persons/{id}`: solo `ADMINISTRATOR` corrige datos personales y registra la auditoría, independientemente de quién registró a la persona.

### Productos crediticios

`POST /api/v1/credit-products` requiere `DATA_ENTRY` y registra una tarjeta, préstamo o hipoteca de su institución. Códigos permitidos para el tipo: `CREDIT_CARD`, `LOAN`, `MORTGAGE`. Estados disponibles: `OPEN`, `CLOSED`, `CANCELLED`. La API obtiene la institución de la cuenta autenticada; `institutionId` es opcional y, si se envía, debe coincidir con ella. No se permite trasladar productos a otra institución.

```json
{
  "personId": 1,
  "productTypeCode": "LOAN",
  "productStatusCode": "OPEN",
  "externalProductIdentifier": "PREST-DEMO-001",
  "approvedAmount": 250000,
  "currentBalance": 180000,
  "currencyCode": "DOP",
  "openingDate": "2024-01-15",
  "closingDate": null,
  "latestReportDate": "2026-11-01",
  "latestDaysPastDue": 8
}
```

- `GET /api/v1/credit-products`: los tres roles pueden listar productos.
- `GET /api/v1/credit-products/{id}`: los tres roles pueden consultar un producto de cualquier institución.
- `PUT /api/v1/credit-products/{id}`: solo `DATA_ENTRY` corrige productos de su institución; guarda valores anteriores y nuevos en auditoría. Intentar modificar un producto ajeno devuelve `403`.

### Consulta consolidada — los tres roles

`GET /api/v1/credit-reports/{nationalId}` busca por cédula. Devuelve la persona, todos sus productos activos y un resumen con cantidad, monto aprobado total y saldo pendiente.

- `hasCreditHistory: true`: existen productos.
- `hasCreditHistory: false`: la persona existe, pero no posee historial.
- `404`: la persona no está registrada.

El reporte agrega `code`, `message` e `indicators`. Una persona con historial devuelve `CREDIT_REPORT_FOUND`; una persona existente sin productos devuelve `NO_CREDIT_HISTORY`, lista vacía y score nulo.

```json
{
  "isSimulated": true,
  "status": "SIMULATED",
  "message": "Indicadores académicos simulados; no constituyen una evaluación financiera real.",
  "maximumDaysPastDue": 35,
  "score": 60,
  "riskLevel": "MEDIUM",
  "estimatedDefaultPercentage": 40
}
```

Ese es el contenido de `indicators`, no el reporte completo. Las reglas son: 0 días → 100; 1–30 → 80; 31–60 → 60; 61–90 → 40; más de 90 → 20. El porcentaje es `100 - score`. Se toma el máximo atraso de los productos activos y sus eventos históricos activos; un producto cerrado o cancelado sigue contando si no ha sido desactivado. No se aplica una ventana temporal. Los valores son simulados y no se almacenan como un score permanente en la persona. Sin historial, `status` es `NO_CREDIT_HISTORY` y los valores de score, riesgo, atraso y porcentaje son nulos.

### Catálogos para formularios

Los tres roles pueden consultar:

- `GET /api/v1/catalogs/institution-types`: identificadores, códigos y nombres de tipos de institución.
- `GET /api/v1/catalogs/product-types`: códigos y nombres de tipos de producto.
- `GET /api/v1/catalogs/product-statuses`: códigos y nombres de estados de producto.
- `GET /api/v1/catalogs/score-rules`: rangos de atraso, score, riesgo y porcentaje, marcados como simulados.

Solo `ADMINISTRATOR` puede consultar `GET /api/v1/catalogs/roles`, para crear cuentas. React puede cargar los selectores desde estos endpoints y obtener el rol e institución actuales desde `/api/v1/auth/me`.

### Auditoría — administrador

`GET /api/v1/audit-logs` devuelve `items`, `totalCount`, `page` y `pageSize`. Filtros opcionales:

| Parámetro | Uso |
|---|---|
| `from`, `to` | Fecha y hora ISO 8601 con zona, límites inclusivos; se convierten a UTC |
| `accountId` | Cuenta que realizó la operación |
| `institutionId` | Institución asociada a la operación |
| `personId` | Persona afectada o consultada |
| `operation` | Código exacto, por ejemplo `CREDITREPORTS_GETBYNATIONALID` o `PERSON_UPDATED` |
| `result` | `SUCCESS`, `DENIED` o `FAILURE` para los registros nuevos |
| `page` | Página, desde 1; predeterminado 1 |
| `pageSize` | Entre 1 y 100; predeterminado 20 |

Ejemplo: `/api/v1/audit-logs?from=2026-11-01T00:00:00Z&to=2026-11-30T23:59:59Z&institutionId=1&page=1&pageSize=20`.

Se registran consultas, registros, inicios de sesión, cambios de estado, correcciones y accesos rechazados a operaciones existentes de `/api/v1`. Los registros incluyen fecha UTC, cuenta, institución cuando aplica, persona o entidad, operación y resultado. En intentos no autenticados la cuenta puede ser nula. Las correcciones conservan además JSON de valores anteriores y nuevos. La lectura de auditoría también se registra y aparece en una consulta posterior. No se guardan contraseñas, hashes, tokens ni cuerpos completos de solicitudes. Los registros anteriores a esta funcionalidad se conservan; no se reconstruyen consultas pasadas.

## 4. Interpretar respuestas

| Código | Significado |
|---|---|
| 200 | Consulta o actualización correcta |
| 201 | Registro creado |
| 400 | JSON o datos inválidos |
| 401 | Token ausente, inválido, vencido o cuenta desactivada |
| 403 | El rol no tiene permiso |
| 404 | Recurso o persona inexistente |
| 409 | Cédula, usuario, correo o identificador duplicado |

Los errores contienen `code`, `message`, `traceId` y, cuando corresponde, `errors` por campo. El frontend debe decidir por `code`; el texto es para mostrar al usuario.

| Código de aplicación | HTTP | Caso |
|---|---|---|
| `INVALID_CREDENTIALS` | 401 | Usuario o contraseña incorrectos, cuenta inactiva o bloqueada al iniciar sesión |
| `AUTHENTICATION_REQUIRED` | 401 | No se envió token |
| `INVALID_TOKEN` | 401 | Token inválido o alterado |
| `TOKEN_EXPIRED` | 401 | Token vencido |
| `ACCOUNT_UNAVAILABLE` | 401 | Cuenta desactivada, bloqueada o eliminada después de emitir el token |
| `INSUFFICIENT_PERMISSIONS` | 403 | Rol no autorizado o producto de otra institución |
| `INVALID_DATA` | 400 | Datos o filtros inválidos |
| `PERSON_NOT_FOUND` | 404 | Persona inexistente |
| `RESOURCE_NOT_FOUND` | 404 | Otro recurso inexistente |
| `CONFLICT` | 409 | Registro duplicado o inicialización ya realizada |
| `INTERNAL_ERROR` | 500 | Error interno sin detalles sensibles |

`NO_CREDIT_HISTORY` es una respuesta exitosa `200`, no un error. Un `traceId` permite relacionar un error con los registros del servidor.

## 5. Pruebas recomendadas

1. Iniciar sesión como administrador y autorizar Swagger.
2. Crear una institución y cuentas `CONSUMER` y `DATA_ENTRY`.
3. Cambiar al token de `DATA_ENTRY`, registrar una persona y repetir la misma cédula; la segunda solicitud debe responder `409`.
4. Registrar uno o más productos para esa persona como `DATA_ENTRY`, sin enviar `institutionId`.
5. Corregir productos propios como `DATA_ENTRY` y datos personales como administrador. Un producto ajeno debe devolver `403`.
6. Consultar el reporte por cédula y comprobar los totales.
7. Iniciar sesión como consumidor: el reporte debe responder `200` y las operaciones administrativas `403`.
8. Desactivar la cuenta consumidora y reutilizar su token: debe responder `401`.

## 6. Pruebas automatizadas y evidencia funcional

Con la API iniciada y los usuarios de demostración disponibles, ejecuta:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ApiPermissions.ps1 -BaseUrl http://localhost:5041
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-BackendRequirements.ps1 -BaseUrl http://localhost:5041
```

Los scripts aceptan parámetros de usuario y contraseña para cada rol. El segundo lee la clave JWT del `.env` de la API únicamente para firmar un token ya vencido y verificar ese caso; nunca imprime la clave ni el token. Ambos conservan los datos ficticios de prueba. La prueba de permisos necesita dos instituciones y un producto de una institución ajena; los usuarios `consumer.demo` y `dataentry.demo` no se crean automáticamente en un clon nuevo: se crean como administrador antes de las pruebas.

Verificación realizada el 25 de noviembre de 2026 contra SQL Server local: 58 comprobaciones de permisos y 43 de RF-08 a RF-10 correctas.

| Entrada o acción | Resultado esperado | Resultado obtenido |
|---|---|---|
| Atrasos 0, 1, 30, 31, 60, 61, 90 y 91 | Scores 100, 80, 80, 60, 60, 40, 40 y 20 | Coinciden todos los límites |
| Persona registrada sin productos | 200, `NO_CREDIT_HISTORY`, score nulo | Conforme |
| Cédula inexistente | 404, `PERSON_NOT_FOUND` | Conforme |
| Cédula de 3 dígitos o campos obligatorios ausentes | 400, `INVALID_DATA` | Conforme |
| Credenciales incorrectas | 401, `INVALID_CREDENTIALS` | Conforme |
| Token alterado / vencido | 401, `INVALID_TOKEN` / `TOKEN_EXPIRED` | Conforme |
| Reutilizar JWT después de desactivar cuenta | 401, `ACCOUNT_UNAVAILABLE` | Conforme |
| Consumidor consulta auditoría | 403, `INSUFFICIENT_PERMISSIONS` | Conforme |
| Filtrar auditoría por fecha, cuenta, institución y persona | Solo resultados coincidentes | Conforme |
| `pageSize=101` o rango de fechas invertido | 400 | Conforme |
| Consultas y creaciones de productos | Auditorías de resultado y entidad | Conforme |
