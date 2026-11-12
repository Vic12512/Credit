# Documento técnico — API SNIC

## 1. Información general de la API

La API del Sistema Nacional de Información Crediticia (SNIC) permite registrar personas y productos crediticios y consultar información consolidada por cédula. También administra instituciones y cuentas de usuario. Su finalidad es académica: recibe información crediticia preparada y permite corregirla; no ejecuta pagos ni movimientos de dinero.

    | Característica | Descripción |
    |---|---|
    | Nombre | API SNIC |
    | Versión del contrato | v1 |
    | Ruta base de negocio | `/api/v1` |
    | Tecnología | ASP.NET Core sobre .NET 10 |
    | Persistencia | SQL Server mediante Entity Framework Core |
    | Comunicación | HTTP/HTTPS; solicitudes y respuestas de negocio en JSON |
    | Autenticación | JWT Bearer con vencimiento |
    | Autorización | Roles y comprobación del estado de la cuenta en el servidor |
    | Documentación interactiva | Swagger UI en `/swagger`, disponible en `Development` |
    | Contrato de Swagger | `/swagger/v1/swagger.json`, disponible en `Development` |
    | URL local de ejemplo | `http://localhost:5041`, según el perfil de ejecución |

Las funciones disponibles incluyen autenticación, administración de accesos, registro y corrección de personas, registro y corrección de tarjetas, préstamos e hipotecas y consulta consolidada. Las correcciones de personas y productos guardan valores anteriores y nuevos en auditoría dentro de la misma transacción que la actualización.

La consulta consolidada devuelve la identificación de la persona, la institución acreedora, tipo y estado de cada producto, montos, saldos, fechas y días de atraso. Incluye cantidad de productos, monto aprobado total, saldo pendiente total y el indicador `hasCreditHistory`.

El sistema define tres roles de usuario:

- `ADMINISTRATOR`: administrador del sistema.
- `CONSUMER`: usuario consumidor de información crediticia.
- `DATA_ENTRY`: usuario institucional destinado al registro de información.

Las cuentas institucionales se asocian a una institución. Una cuenta de usuario representa un acceso al sistema, no una cuenta bancaria.

Los endpoints protegidos reciben el encabezado `Authorization: Bearer <token>`. El token se obtiene con `POST /api/v1/auth/login`. La API comprueba que la cuenta siga activa y no esté bloqueada en cada solicitud autenticada.

Las respuestas principales son `200 OK` (consulta o actualización), `201 Created` (registro creado), `400 Bad Request` (datos inválidos), `401 Unauthorized` (acceso no autenticado o inválido), `403 Forbidden` (permisos insuficientes), `404 Not Found` (recurso inexistente) y `409 Conflict` (duplicados o sistema ya inicializado).

## 2. Listado de endpoints y permisos

La tabla describe los permisos actualmente implementados en los controladores. Los roles corresponden a cuentas activas con un JWT válido, salvo las rutas públicas indicadas.

| Método | Endpoint | Operación | Roles o acceso permitido |
|---|---|---|---|
| POST | `/api/v1/setup` | Crear el primer administrador | Público; solo funciona si no existe ninguna cuenta |
| POST | `/api/v1/auth/login` | Validar credenciales y emitir JWT | Público; requiere credenciales válidas de una cuenta activa |
| GET | `/api/v1/auth/me` | Consultar la propia cuenta autenticada | ADMINISTRATOR, CONSUMER, DATA_ENTRY |
| POST | `/api/v1/institutions` | Registrar una institución | ADMINISTRATOR |
| GET | `/api/v1/institutions` | Listar instituciones | ADMINISTRATOR |
| GET | `/api/v1/institutions/{id}` | Consultar una institución por ID | ADMINISTRATOR |
| POST | `/api/v1/accounts` | Crear una cuenta y asignar su rol e institución | ADMINISTRATOR |
| GET | `/api/v1/accounts` | Listar cuentas de usuario | ADMINISTRATOR |
| GET | `/api/v1/accounts/{id}` | Consultar una cuenta de usuario por ID | ADMINISTRATOR |
| PATCH | `/api/v1/accounts/{id}/status` | Activar o desactivar una cuenta | ADMINISTRATOR; no permite desactivar la propia cuenta |
| POST | `/api/v1/persons` | Registrar una persona con cédula única | ADMINISTRATOR |
| GET | `/api/v1/persons` | Listar personas | ADMINISTRATOR |
| GET | `/api/v1/persons/{id}` | Consultar una persona por ID | ADMINISTRATOR |
| PUT | `/api/v1/persons/{id}` | Corregir datos personales y registrar auditoría | ADMINISTRATOR |
| POST | `/api/v1/credit-products` | Registrar un producto crediticio | ADMINISTRATOR |
| GET | `/api/v1/credit-products` | Listar productos crediticios | ADMINISTRATOR |
| GET | `/api/v1/credit-products/{id}` | Consultar un producto por ID | ADMINISTRATOR |
| PUT | `/api/v1/credit-products/{id}` | Corregir un producto y registrar auditoría | ADMINISTRATOR |
| GET | `/api/v1/credit-reports/{nationalId}` | Consultar el reporte consolidado por cédula | ADMINISTRATOR, CONSUMER |
| GET | `/health` | Comprobar la disponibilidad de la API y la base de datos | Público |

Se documentan 19 operaciones de negocio y una operación de salud. Las rutas de Swagger y del contrato OpenAPI son recursos de documentación, separados de estas operaciones.
