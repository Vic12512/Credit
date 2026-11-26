# SNIC Web

Frontend React + TypeScript para consultar y administrar información crediticia a través de `Snic.Api`.

## Desarrollo local

1. Inicia la API desde `src/Snic.Api` y confirma que su dirección coincida con `VITE_API_BASE_URL`.
2. Copia `.env.example` a `.env.local` si necesitas configurar otra dirección de API.
3. Usa Node.js 20.19 o posterior, ejecuta `npm ci` y luego `npm run dev`.

La URL base predeterminada es `http://localhost:5041/api/v1`. El backend debe permitir el origen local del frontend en su configuración CORS.

## Funcionalidad

- Inicio de sesión JWT y validación de la sesión con `/auth/me`.
- Consulta de reporte consolidado por cédula.
- Listado de personas y productos; los registradores pueden crear personas/productos y actualizar productos.
- Administración de cuentas e instituciones para administradores; actualización de datos de personas para administradores.
- Los permisos de navegación se muestran según el rol devuelto por la API; el backend sigue siendo responsable de autorizar cada operación.

El reporte presenta los campos que devuelve la API: persona, resumen y productos. No se estiman puntuaciones o niveles de riesgo en el cliente.
