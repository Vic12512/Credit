# SNIC — Sistema Nacional de Información Crediticia

Monorepo para la API ASP.NET Core, el cliente React y SQL Server. El esquema se administra con migraciones de Entity Framework Core.

## Requisitos

- .NET SDK 10.0.201 o una versión compatible con `global.json`.
- Node.js 24 y npm.
- Docker Desktop con Docker Compose.

No es necesario instalar herramientas globales. `dotnet-ef` está fijado en `dotnet-tools.json` y las dependencias web viven en `src/snic-web/node_modules`.

## Configuración inicial

1. Copia `.env.example` como `.env` y reemplaza las dos claves de ejemplo. Usa la misma contraseña de SQL Server en `SA_PASSWORD` y dentro de `ConnectionStrings__SnicDb`.
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

4. Carga `.env` en la terminal actual y aplica la migración:

```powershell
Get-Content .env | Where-Object { $_ -and -not $_.StartsWith('#') } | ForEach-Object {
  $key, $value = $_ -split '=', 2
  [Environment]::SetEnvironmentVariable($key, $value, 'Process')
}
dotnet tool run dotnet-ef database update --project src/Snic.Api
```

## Ejecución

En dos terminales con las variables de `.env` cargadas:

```powershell
dotnet run --project src/Snic.Api
npm.cmd run dev --prefix src/snic-web
```

- Swagger UI: URL HTTPS de la API seguida de `/swagger`.
- OpenAPI JSON: `/openapi/v1.json`.
- Salud de API y base de datos: `/health`.
- React: `http://localhost:5173`.

## Migraciones posteriores

```powershell
dotnet tool run dotnet-ef migrations add NombreDescriptivo --project src/Snic.Api --output-dir Infrastructure/Migrations
dotnet tool run dotnet-ef database update --project src/Snic.Api
```

`SNIC_Entrega_2.pdf` describe un sistema distinto (autobuses y estudiantes). La migración inicial usa el DBML crediticio adjunto, coherente con `SNIC_Entrega_1.pdf`.
