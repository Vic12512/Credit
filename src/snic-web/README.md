# SNIC Web

Aplicación React + TypeScript para consultar y administrar información crediticia a través de `Snic.Api`. El frontend no se conecta directamente a SQL Server.

## Requisitos

- Windows, con PowerShell para los comandos de esta guía.
- Node.js 24 y npm. Vite 8 requiere Node.js 20.19+ o 22.12+; se recomienda Node.js 24, igual que el resto del repositorio.
- Para ejecutar la API localmente: .NET SDK 10.0.201 (o una versión compatible con `global.json`), Docker Desktop con Docker Compose y SQL Server.

Comprueba las versiones:

```powershell
node --version
npm.cmd --version
dotnet --version
docker --version
```

## Ejecutar solo el frontend

Si la API ya está ejecutándose y tienes una cuenta SNIC:

1. Abre PowerShell en la raíz del repositorio.
2. Instala las dependencias del frontend:

   ```powershell
   npm.cmd ci --prefix src\snic-web
   ```

3. Configura la dirección de la API. La plantilla usa `https://localhost:7212/api/v1`:

   ```powershell
   Copy-Item src\snic-web\.env.example src\snic-web\.env
   ```

   Si la API está en otra dirección, edita `src\snic-web\.env` y establece `VITE_API_BASE_URL`, por ejemplo:

   ```dotenv
   VITE_API_BASE_URL=http://localhost:5041/api/v1
   ```

4. Inicia Vite:

   ```powershell
   npm.cmd run dev --prefix src\snic-web
   ```

5. Abre la dirección que muestra Vite, normalmente <http://localhost:5173>, e inicia sesión con una cuenta creada en la API.

Después de cambiar `VITE_API_BASE_URL`, reinicia el servidor de Vite para que cargue el nuevo valor.

## Preparar y ejecutar todo localmente

Ejecuta estos pasos desde la raíz del repositorio. Si ya tienes SQL Server, la base de datos y la API configurados, puedes saltar a **Iniciar los servidores**.

### 1. Crear la configuración local

```powershell
Copy-Item .env.example .env
Copy-Item src\Snic.Api\.env.example src\Snic.Api\.env
Copy-Item src\snic-web\.env.example src\snic-web\.env
```

Edita los archivos `.env` locales:

- En el `.env` de la raíz, define `SA_PASSWORD`.
- En `src\Snic.Api\.env`, usa la misma contraseña en `ConnectionStrings__SnicDb` y define `Jwt__Key` con al menos 32 caracteres.
- Revisa `BootstrapAdmin__Username` y `BootstrapAdmin__Password`; la contraseña debe tener entre 12 y 128 caracteres.
- `FrontendUrl` debe coincidir con el origen del frontend. El valor local predeterminado es `http://localhost:5173`.
- En `src\snic-web\.env`, `VITE_API_BASE_URL` debe apuntar a la API, normalmente `https://localhost:7212/api/v1` o `http://localhost:5041/api/v1`.

Los `.env` reales son archivos locales y no deben subirse al repositorio. La plantilla del administrador contiene credenciales de demostración: cámbialas para cualquier entorno compartido. En Development, la API crea ese administrador solo si la base de datos todavía no contiene cuentas; si ya está inicializada, usa las credenciales de una cuenta existente.

### 2. Iniciar SQL Server y preparar la base de datos

```powershell
docker compose up -d sqlserver
docker compose ps
dotnet tool restore
dotnet restore
dotnet tool run dotnet-ef database update --project src\Snic.Api
```

Espera a que el contenedor `snic-sqlserver` esté saludable antes de aplicar la migración.

### 3. Iniciar los servidores

Abre dos terminales en la raíz del repositorio.

**Terminal 1 — API** (el perfil HTTPS corresponde a la plantilla del frontend):

```powershell
dotnet run --project src\Snic.Api --launch-profile https
```

La API también escucha en `http://localhost:5041`. Si el certificado HTTPS de desarrollo no es de confianza, ejecuta `dotnet dev-certs https --trust` o cambia `VITE_API_BASE_URL` a `http://localhost:5041/api/v1`.

**Terminal 2 — frontend:**

```powershell
npm.cmd ci --prefix src\snic-web
npm.cmd run dev --prefix src\snic-web
```

Abre <http://localhost:5173>. Para el primer inicio de sesión, usa las credenciales definidas en `BootstrapAdmin__Username` y `BootstrapAdmin__Password` de `src\Snic.Api\.env`; si la base ya tenía cuentas, usa una cuenta existente.

## Comandos útiles

Desde la raíz del repositorio:

```powershell
npm.cmd run build --prefix src\snic-web
npm.cmd run lint --prefix src\snic-web
```

`build` ejecuta el chequeo de TypeScript y genera los archivos de producción en `src\snic-web\dist`. Para previsualizar ese build:

```powershell
npm.cmd run preview --prefix src\snic-web
```

## Solución de problemas

- **No conecta con la API:** confirma que la API está activa y que `VITE_API_BASE_URL` incluye `/api/v1` y el puerto correcto.
- **Error de CORS:** confirma que `FrontendUrl` en `src\Snic.Api\.env` sea `http://localhost:5173`. Si Vite inició en otro puerto, actualiza este valor y reinicia la API.
- **Error de certificado HTTPS:** confía el certificado de desarrollo de .NET con `dotnet dev-certs https --trust` o usa la URL HTTP de la API en `.env`.
- **Error de inicio de sesión:** verifica el usuario y contraseña configurados. La cuenta de bootstrap solo se crea en Development y cuando no existen cuentas en la base.
- **`npm` no ejecuta en PowerShell:** usa `npm.cmd`, como en los ejemplos, para evitar restricciones de ejecución de scripts de PowerShell.
- **Node demasiado antiguo:** instala Node.js 24 y abre una terminal nueva; Vite 8 no funciona con la versión Node 20.11.

## Funcionalidad y permisos

- Inicio de sesión JWT y validación de sesión con `/auth/me`.
- Consulta de reporte consolidado por cédula.
- Visualización de score, riesgo y porcentaje de incumplimiento devueltos por el backend, identificados como simulados; las reglas se consultan desde `/catalogs/score-rules`. Sin historial se muestran indicadores no disponibles, nunca un score inventado.
- Listado de personas y productos; los registradores pueden crear personas y crear o corregir productos de su institución.
- Administración de cuentas e instituciones para administradores; los administradores también pueden corregir datos personales.
- Las opciones visibles dependen del rol, pero la API es quien autoriza cada operación.
- Directorio de instituciones en modo consulta para los tres roles; la creación sigue reservada al administrador.
- Auditoría para administradores: filtros por fechas locales, cuenta, institución, persona, operación y resultado; paginación; y detalle de valores anteriores y nuevos.
- Tipos de institución, tipos y estados de producto y roles cargados desde los catálogos de la API.
- Manejo de errores por código, mensajes de validación del servidor, reintentos de carga y cierre de sesión ante respuestas 401 o vencimiento local del JWT.
- Confirmación y motivo para activar o desactivar cuentas, mensajes de éxito y diálogos navegables por teclado.

El reporte presenta persona, resumen, productos, fechas e indicadores. El cliente no calcula puntuaciones ni niveles de riesgo: muestra los valores recibidos de la API. Para la configuración completa de la API y SQL Server, consulta el [README del repositorio](../../README.md) y el [manual de uso de la API](../../docs/Manual_Uso_API.md).

## Pruebas de navegador

Con SQL Server y la API activos, las cuentas ficticias `admin`, `consumer.demo` y `dataentry.demo`, y productos de al menos dos instituciones:

```powershell
npm.cmd ci --prefix src\snic-web
npm.cmd run test:e2e --prefix src\snic-web
```

Playwright es una dependencia de desarrollo local. En Windows, las pruebas utilizan Microsoft Edge ya instalado; puedes elegir Chrome instalado con `$env:PLAYWRIGHT_CHANNEL='chrome'`. En otros sistemas debe estar disponible un navegador compatible con Playwright. La API de pruebas predeterminada es `http://localhost:5041/api/v1/`; puedes cambiarla con `SNIC_API_URL`, y el `.env` de Vite debe apuntar a la misma API. Las credenciales de prueba pueden configurarse con `SNIC_ADMIN_USER`, `SNIC_ADMIN_PASSWORD`, `SNIC_CONSUMER_USER`, `SNIC_CONSUMER_PASSWORD`, `SNIC_ENTRY_USER` y `SNIC_ENTRY_PASSWORD`.

Las nueve pruebas verifican reportes e indicadores, casos sin historial, errores, permisos de consulta, edición de productos propios, filtros y detalles de auditoría, navegación móvil, sesiones vencidas, registro de personas y cambios de estado de cuentas. Generan registros ficticios que permanecen en la base. Los reportes y capturas se guardan en `playwright-report/` y `test-results/`, ignorados por Git. Vite se reutiliza si ya está iniciado, o se levanta temporalmente para la prueba.
