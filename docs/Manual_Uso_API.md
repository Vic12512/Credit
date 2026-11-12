# Manual de uso de la API SNIC

## 1. Iniciar el proyecto

Desde la raíz del repositorio:

```powershell
Copy-Item .env.example .env
Copy-Item src/Snic.Api/.env.example src/Snic.Api/.env
docker compose up -d sqlserver
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Snic.Api
dotnet run --project src/Snic.Api
```

El archivo `.env` real no se publico. Deben configurarse la conexión, la clave JWT y, si la base está vacía, `BootstrapAdmin__Username` y `BootstrapAdmin__Password`.

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

### Instituciones y cuentas — administrador

- `GET /api/v1/institutions` y `GET /api/v1/institutions/{id}`: consultan instituciones.
- `POST /api/v1/institutions`: registra una institución.
- `GET /api/v1/accounts` y `GET /api/v1/accounts/{id}`: consultan cuentas.
- `POST /api/v1/accounts`: crea una cuenta asociada a una institución, excepto el administrador global.
- `PATCH /api/v1/accounts/{id}/status`: activa o desactiva una cuenta.

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

### Personas — administrador

`POST /api/v1/persons` registra una persona. La cédula debe contener 11 dígitos y no puede repetirse.

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

- `GET /api/v1/persons`: lista personas.
- `GET /api/v1/persons/{id}`: consulta una persona.
- `PUT /api/v1/persons/{id}`: corrige sus datos y registra la auditoría.

### Productos crediticios — administrador

`POST /api/v1/credit-products` registra una tarjeta, préstamo o hipoteca. Códigos permitidos para el tipo: `CREDIT_CARD`, `LOAN`, `MORTGAGE`. Estados disponibles: `OPEN`, `CLOSED`, `CANCELLED`.

```json
{
  "personId": 1,
  "institutionId": 1,
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

- `GET /api/v1/credit-products`: lista productos.
- `GET /api/v1/credit-products/{id}`: consulta un producto.
- `PUT /api/v1/credit-products/{id}`: corrige sus datos y registra valores anteriores y nuevos en auditoría.

### Consulta consolidada — administrador o consumidor

`GET /api/v1/credit-reports/{nationalId}` busca por cédula. Devuelve la persona, todos sus productos activos y un resumen con cantidad, monto aprobado total y saldo pendiente.

- `hasCreditHistory: true`: existen productos.
- `hasCreditHistory: false`: la persona existe, pero no posee historial.
- `404`: la persona no está registrada.

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

Los errores de negocio contienen `message`. Los errores automáticos de validación contienen `errors`, organizado por campo.

## 5. Pruebas recomendadas

1. Iniciar sesión como administrador y autorizar Swagger.
2. Crear una institución y una cuenta `CONSUMER`.
3. Registrar una persona y repetir la misma cédula; la segunda solicitud debe responder `409`.
4. Registrar uno o más productos para esa persona.
5. Corregir la persona y un producto mediante `PUT`.
6. Consultar el reporte por cédula y comprobar los totales.
7. Iniciar sesión como consumidor: el reporte debe responder `200` y las operaciones administrativas `403`.
8. Desactivar la cuenta consumidora y reutilizar su token: debe responder `401`.
