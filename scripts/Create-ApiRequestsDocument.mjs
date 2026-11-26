import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Genera un DOCX Open XML sin instalar dependencias ni utilizar Microsoft Word.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const destination = path.join(root, 'output', 'SNIC_Requests_API.docx');
const all = 'ADMINISTRATOR, CONSUMER y DATA_ENTRY';
const row = (name, type, required, rules, location = 'Cuerpo JSON') => [name, location, type, required, rules];
const idRow = entity => row('id', 'Entero', 'Obligatorio', `Identificador numérico de ${entity} existente. No es su código ni su cédula.`, 'Ruta');
const credentials = [
  row('username', 'Texto', 'Obligatorio', 'De 3 a 100 caracteres.'),
  row('email', 'Texto', 'Obligatorio', 'Correo válido. Máximo 180 caracteres.'),
  row('displayName', 'Texto', 'Obligatorio', 'Máximo 180 caracteres.'),
  row('password', 'Texto', 'Obligatorio', 'De 12 a 128 caracteres. Los ejemplos son ficticios, no credenciales reales.')
];
const personFields = [
  row('nationalId', 'Texto', 'Obligatorio', 'Exactamente 11 dígitos, sin guiones. Cédula única.'),
  row('firstName', 'Texto', 'Obligatorio', 'Máximo 80 caracteres.'),
  row('middleName', 'Texto o null', 'Opcional', 'Máximo 80 caracteres. Puede omitirse o ser null.'),
  row('firstSurname', 'Texto', 'Obligatorio', 'Máximo 80 caracteres.'),
  row('secondSurname', 'Texto o null', 'Opcional', 'Máximo 80 caracteres. Puede omitirse o ser null.'),
  row('birthDate', 'Fecha o null', 'Opcional', 'YYYY-MM-DD. No puede estar en el futuro.')
];
const productFields = [
  row('personId', 'Entero', 'Obligatorio', 'Mayor que 0. Persona existente y activa.'),
  row('institutionId', 'Entero o null', 'Opcional', 'Si se omite o es null, se usa la institución de la cuenta. Si se envía, debe coincidir con ella y ser mayor que 0.'),
  row('productTypeCode', 'Texto', 'Obligatorio', 'Máximo 30 caracteres. Catálogo: CREDIT_CARD, LOAN o MORTGAGE.'),
  row('productStatusCode', 'Texto', 'Obligatorio', 'Máximo 30 caracteres. Catálogo: OPEN, CLOSED o CANCELLED.'),
  row('externalProductIdentifier', 'Texto', 'Obligatorio', 'Máximo 100 caracteres. Único dentro de la institución.'),
  row('approvedAmount', 'Decimal', 'Opcional técnico*', 'De 0 a 9999999999999999.99. Omitido: 0. Enviar explícitamente el monto correcto.'),
  row('currentBalance', 'Decimal', 'Opcional técnico*', 'De 0 a 9999999999999999.99. Omitido: 0. Enviar explícitamente el saldo correcto.'),
  row('currencyCode', 'Texto', 'Obligatorio', 'Exactamente tres letras; por ejemplo, DOP o USD.'),
  row('openingDate', 'Fecha', 'Enviar siempre*', 'YYYY-MM-DD. No es nullable; sin validación explícita de presencia en el DTO actual.'),
  row('closingDate', 'Fecha o null', 'Opcional', 'YYYY-MM-DD. No puede ser anterior a openingDate.'),
  row('latestReportDate', 'Fecha o null', 'Opcional', 'YYYY-MM-DD. No puede ser anterior a openingDate.'),
  row('latestDaysPastDue', 'Entero', 'Opcional técnico*', 'De 0 a 2147483647. Omitido: 0. No acepta null.')
];
const person = { nationalId: '00112345678', firstName: 'Ana', middleName: 'María', firstSurname: 'Pérez', secondSurname: 'Gómez', birthDate: '1995-05-20' };
const product = { personId: 1, institutionId: 1, productTypeCode: 'LOAN', productStatusCode: 'OPEN', externalProductIdentifier: 'PRESTAMO-DEMO-001', approvedAmount: 100000, currentBalance: 75000, currencyCode: 'DOP', openingDate: '2026-01-10', closingDate: null, latestReportDate: '2026-11-25', latestDaysPastDue: 15 };
const endpoints = [];
function add(title, method, route, access, fields = [], body = undefined, note = '', exampleRoute = route) {
  endpoints.push({ title, method, route, access, fields, body, note, exampleRoute });
}
add('Crear el primer administrador', 'POST', '/api/v1/setup', 'Público. Solo cuando no existe ninguna cuenta.', credentials,
  { username: 'admin', email: 'admin@example.com', displayName: 'Administrador SNIC', password: 'Ejemplo_Admin_2026!' },
  'El rol ADMINISTRATOR se asigna automáticamente. No recibe roleCode ni institutionId. Si ya existe una cuenta, devuelve 409.');
add('Iniciar sesión', 'POST', '/api/v1/auth/login', 'Público. Requiere una cuenta activa y credenciales válidas.', [
  row('username', 'Texto', 'Obligatorio', 'Nombre de usuario de una cuenta existente.'),
  row('password', 'Texto', 'Obligatorio', 'Contraseña de la cuenta.')
], { username: 'admin', password: 'Ejemplo_Admin_2026!' }, 'Usar el accessToken devuelto como Bearer en solicitudes protegidas. El ejemplo no crea ni modifica la contraseña.');
add('Consultar la propia cuenta', 'GET', '/api/v1/auth/me', all);
add('Registrar una institución', 'POST', '/api/v1/institutions', 'ADMINISTRATOR', [
  row('institutionTypeId', 'Entero', 'Obligatorio', 'De 1 a 32767. Tipo existente; consultar /api/v1/catalogs/institution-types.'),
  row('code', 'Texto', 'Obligatorio', 'Máximo 30 caracteres. No repetido.'),
  row('rnc', 'Texto', 'Obligatorio', 'De 9 a 11 dígitos, sin guiones. No repetido.'),
  row('legalName', 'Texto', 'Obligatorio', 'Máximo 180 caracteres.'),
  row('commercialName', 'Texto o null', 'Opcional', 'Máximo 180 caracteres.')
], { institutionTypeId: 1, code: 'BANCO_DEMO', rnc: '123456789', legalName: 'Banco Demostrativo SRL', commercialName: 'Banco Demo' });
add('Listar instituciones', 'GET', '/api/v1/institutions', all);
add('Consultar una institución por ID', 'GET', '/api/v1/institutions/{id}', all, [idRow('una institución')], undefined, '', '/api/v1/institutions/1');
add('Crear una cuenta', 'POST', '/api/v1/accounts', 'ADMINISTRATOR', [
  row('institutionId', 'Entero o null', 'Condicional', 'Obligatorio para CONSUMER y DATA_ENTRY: institución existente y activa. Para ADMINISTRATOR debe omitirse o ser null.'),
  row('roleCode', 'Texto', 'Obligatorio', 'ADMINISTRATOR, CONSUMER o DATA_ENTRY.'),
  ...credentials
], { institutionId: 1, roleCode: 'DATA_ENTRY', username: 'registrador.demo', email: 'registrador@example.com', displayName: 'Registrador de demostración', password: 'Ejemplo_Usuario_2026!' }, 'Username y email deben ser únicos. La nueva cuenta queda activa.');
add('Listar cuentas', 'GET', '/api/v1/accounts', 'ADMINISTRATOR');
add('Consultar una cuenta por ID', 'GET', '/api/v1/accounts/{id}', 'ADMINISTRATOR', [idRow('una cuenta')], undefined, '', '/api/v1/accounts/2');
add('Activar o desactivar una cuenta', 'PATCH', '/api/v1/accounts/{id}/status', 'ADMINISTRATOR', [
  idRow('una cuenta'),
  row('isActive', 'Booleano', 'Enviar siempre*', 'true: activar; false: desactivar. Omitido se interpreta como false en el DTO actual. No acepta null.'),
  row('reason', 'Texto o null', 'Opcional', 'Máximo 300 caracteres. Al desactivar sin motivo se usa un texto predeterminado. Al activar se limpia el motivo.')
], { isActive: false, reason: 'Cuenta deshabilitada para demostración' }, 'No permite desactivar la propia cuenta. La desactivación impide utilizar sus tokens, aunque aún no hayan vencido.', '/api/v1/accounts/2/status');
add('Registrar una persona', 'POST', '/api/v1/persons', 'DATA_ENTRY', personFields, person, 'La institución no se envía. Las personas son compartidas; se registra la cuenta creadora.');
add('Listar personas', 'GET', '/api/v1/persons', all);
add('Consultar una persona por ID', 'GET', '/api/v1/persons/{id}', all, [idRow('una persona')], undefined, '', '/api/v1/persons/1');
add('Corregir datos de una persona', 'PUT', '/api/v1/persons/{id}', 'ADMINISTRATOR', [idRow('una persona'), ...personFields], { ...person, secondSurname: 'Rodríguez' },
  'Enviar el objeto completo, incluidos los datos que deban conservarse. Los opcionales omitidos se limpian. Se registra auditoría de la corrección.', '/api/v1/persons/1');
const productNote = 'Solo una cuenta DATA_ENTRY con institución activa puede escribir productos de su propia institución. No permite trasladarlos a otra institución. *Los números sin Required pueden tomar 0 al omitirse; openingDate puede tomar el valor mínimo del tipo fecha. Enviar explícitamente estos campos para evitar datos involuntarios. Esto describe el código vigente, no una garantía de validación de presencia.';
add('Registrar un producto crediticio', 'POST', '/api/v1/credit-products', 'DATA_ENTRY, únicamente para su institución', productFields, product, productNote);
add('Listar productos crediticios', 'GET', '/api/v1/credit-products', all);
add('Consultar un producto por ID', 'GET', '/api/v1/credit-products/{id}', all, [idRow('un producto crediticio')], undefined, '', '/api/v1/credit-products/1');
add('Corregir un producto crediticio', 'PUT', '/api/v1/credit-products/{id}', 'DATA_ENTRY, únicamente para su institución', [idRow('un producto crediticio'), ...productFields],
  { ...product, currentBalance: 70000, latestDaysPastDue: 0 }, `${productNote} Enviar el objeto completo. No es una actualización parcial: los valores omitidos pueden borrarse o reemplazarse por valores predeterminados. Se registra auditoría.`, '/api/v1/credit-products/1');
add('Consultar reporte consolidado e indicadores', 'GET', '/api/v1/credit-reports/{nationalId}', all, [
  row('nationalId', 'Texto', 'Obligatorio', 'Cédula de exactamente 11 dígitos, sin guiones.', 'Ruta')
], undefined, 'Devuelve persona, productos, resumen e indicadores simulados. No recibe score ni filtros adicionales. Si la persona existe pero no tiene historial, devuelve 200 con NO_CREDIT_HISTORY e indicadores numéricos null.', '/api/v1/credit-reports/00112345678');
add('Consultar tipos de institución', 'GET', '/api/v1/catalogs/institution-types', all, [], undefined, 'Utilizar los id devueltos en institutionTypeId.');
add('Consultar tipos de producto', 'GET', '/api/v1/catalogs/product-types', all, [], undefined, 'Utilizar los code devueltos en productTypeCode: CREDIT_CARD, LOAN o MORTGAGE en el catálogo inicial.');
add('Consultar estados de producto', 'GET', '/api/v1/catalogs/product-statuses', all, [], undefined, 'Utilizar los code devueltos en productStatusCode: OPEN, CLOSED o CANCELLED en el catálogo inicial.');
add('Consultar reglas de indicadores', 'GET', '/api/v1/catalogs/score-rules', all, [], undefined, 'Devuelve las reglas del score, nivel de riesgo y porcentaje simulado de incumplimiento. No modifica reglas ni recibe parámetros de cálculo.');
add('Consultar roles', 'GET', '/api/v1/catalogs/roles', 'ADMINISTRATOR', [], undefined, 'Utilizar los code devueltos en roleCode.');
add('Consultar auditoría', 'GET', '/api/v1/audit-logs', 'ADMINISTRATOR', [
  row('from', 'Fecha/hora con zona', 'Opcional', 'Inicio inclusivo. Ejemplo: 2026-11-01T00:00:00Z.', 'Query string'),
  row('to', 'Fecha/hora con zona', 'Opcional', 'Fin inclusivo. No puede ser anterior a from.', 'Query string'),
  row('accountId', 'Entero', 'Opcional', 'Mayor que 0. Filtra por cuenta responsable.', 'Query string'),
  row('institutionId', 'Entero', 'Opcional', 'Mayor que 0. Filtra por institución del evento.', 'Query string'),
  row('personId', 'Entero', 'Opcional', 'Mayor que 0. Filtra por persona relacionada.', 'Query string'),
  row('operation', 'Texto', 'Opcional', 'Máximo 60 caracteres. Código completo de operación, sin búsqueda parcial. Ejemplo: CREDIT_PRODUCT_UPDATED.', 'Query string'),
  row('result', 'Texto', 'Opcional', 'Máximo 20 caracteres. Ejemplos actuales: SUCCESS, DENIED y FAILURE. No es un enum validado; otros valores pueden no devolver registros.', 'Query string'),
  row('page', 'Entero', 'Opcional', 'De 1 a 1000000. Predeterminado: 1.', 'Query string'),
  row('pageSize', 'Entero', 'Opcional', 'De 1 a 100. Predeterminado: 20.', 'Query string')
], undefined, 'Los filtros se combinan entre sí. Puede llamarse sin filtros. No lleva cuerpo JSON. Usar Z para UTC; si se envía un desplazamiento positivo, codificar + como %2B en la URL.',
  '/api/v1/audit-logs?from=2026-11-01T00:00:00Z&to=2026-11-30T23:59:59Z&accountId=2&institutionId=1&personId=1&operation=CREDIT_PRODUCT_UPDATED&result=SUCCESS&page=1&pageSize=20');
add('Comprobar disponibilidad', 'GET', '/health', 'Público', [], undefined, 'Comprueba la disponibilidad de la API y la conexión a la base de datos. Está fuera del prefijo /api/v1.');

const escape = value => String(value).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
export function paragraph(text, style = 'Normal') {
  return `<w:p><w:pPr><w:pStyle w:val="${style}"/></w:pPr><w:r><w:t xml:space="preserve">${escape(text)}</w:t></w:r></w:p>`;
}
export function table(headers, rows, widths) {
  const tr = (cells, header = false) => `<w:tr><w:trPr>${header ? '<w:tblHeader/>' : ''}<w:cantSplit/></w:trPr>${cells.map((cell, i) => `<w:tc><w:tcPr><w:tcW w:w="${widths[i]}" w:type="dxa"/><w:shd w:fill="${header ? '17365D' : 'F4F7FA'}"/></w:tcPr><w:p><w:pPr><w:pStyle w:val="TableText"/></w:pPr><w:r>${header ? '<w:rPr><w:b/><w:color w:val="FFFFFF"/></w:rPr>' : ''}<w:t xml:space="preserve">${escape(cell)}</w:t></w:r></w:p></w:tc>`).join('')}</w:tr>`;
  return `<w:tbl><w:tblPr><w:tblW w:w="9638" w:type="dxa"/><w:tblLayout w:type="fixed"/><w:tblBorders>${['top', 'left', 'bottom', 'right', 'insideH', 'insideV'].map(edge => `<w:${edge} w:val="single" w:sz="4" w:color="D9E2EC"/>`).join('')}</w:tblBorders><w:tblCellMar><w:top w:w="85" w:type="dxa"/><w:left w:w="90" w:type="dxa"/><w:bottom w:w="85" w:type="dxa"/><w:right w:w="90" w:type="dxa"/></w:tblCellMar></w:tblPr><w:tblGrid>${widths.map(width => `<w:gridCol w:w="${width}"/>`).join('')}</w:tblGrid>${tr(headers, true)}${rows.map(cells => tr(cells)).join('')}</w:tbl>`;
}
let content = paragraph('SNIC · Requests de la API', 'Title');
content += paragraph('Referencia de endpoints, ejemplos y parámetros obligatorios u opcionales', 'Subtitle');
content += paragraph('Versión del documento: 25 de noviembre de 2026. Basado en los controladores, contratos y servicios actuales del proyecto.');
content += paragraph('Configuración común', 'Heading1');
content += table(['Elemento', 'Valor / uso'], [
  ['URL base local HTTP', 'http://localhost:5041'],
  ['URL base local HTTPS', 'https://localhost:7212 (si se inicia el perfil https y se confía en su certificado). Usar HTTPS fuera del entorno local.'],
  ['Authorization', 'Bearer <accessToken>. Obligatorio para todos los endpoints excepto setup, login y health. Usar un token con el rol indicado en cada operación.'],
  ['Content-Type', 'application/json en POST, PUT y PATCH con cuerpo JSON.'],
  ['Accept', 'application/json para los controladores de /api/v1. /health puede responder texto.'],
  ['Datos de ejemplo', 'Ficticios. Sustituir IDs por registros existentes. Las contraseñas aquí mostradas no son las credenciales reales del entorno.'],
  ['Campos opcionales', 'Los campos nullable del cuerpo pueden omitirse o enviarse como null. En query string, omitir los filtros que no se utilicen; no enviar el texto null.'],
  ['PUT', 'Enviar el objeto completo. Omitir campos opcionales puede borrar sus valores actuales.'],
  ['Obligatoriedad técnica (*)', 'Se distingue validación de presencia de uso recomendado: algunos tipos no nullable admiten su omisión y toman 0, false o fecha mínima. Enviarlos explícitamente. No se modificó la API para crear este documento.'],
  ['Lecturas sin filtros', 'Los listados de instituciones, cuentas, personas y productos no exponen filtros ni paginación actualmente. Solo auditoría admite los filtros documentados.']
], [2300, 7338]);
content += paragraph('Cómo copiar y probar', 'Heading1');
content += paragraph('En Postman: elegir el método, concatenar la URL base con la ruta, agregar el Bearer cuando corresponda y pegar el JSON en Body > raw > JSON. En Swagger: Authorize para el token, Try it out, completar parámetros y Execute. Los bloques siguientes muestran mensajes HTTP; no son comandos de PowerShell.');
content += paragraph('Índice de las 26 operaciones', 'Heading1');
content += table(['N.º', 'Método', 'Endpoint', 'Acceso'], endpoints.map((e, i) => [i + 1, e.method, e.route, e.access]), [500, 900, 4438, 3800]);
for (const [index, endpoint] of endpoints.entries()) {
  content += paragraph(`${index + 1}. ${endpoint.title}`, 'Heading1');
  content += table(['Propiedad', 'Detalle'], [
    ['Método y endpoint', `${endpoint.method} ${endpoint.route}`],
    ['Acceso permitido', endpoint.access],
    ['Cuerpo JSON', endpoint.body === undefined ? 'No lleva cuerpo.' : 'Sí. Ver ejemplo y tabla de campos.']
  ], [2300, 7338]);
  content += paragraph('Ejemplo de request', 'Heading2');
  const request = [`${endpoint.method} ${endpoint.exampleRoute} HTTP/1.1`, 'Host: localhost:5041'];
  if (!['/api/v1/setup', '/api/v1/auth/login', '/health'].includes(endpoint.route)) request.push('Authorization: Bearer <accessToken>');
  if (endpoint.body !== undefined) request.push('Content-Type: application/json');
  if (endpoint.route !== '/health') request.push('Accept: application/json');
  if (endpoint.body !== undefined) request.push('', ...JSON.stringify(endpoint.body, null, 2).split('\n'));
  content += request.map(line => paragraph(line || ' ', 'Code')).join('');
  content += paragraph('Parámetros del request', 'Heading2');
  content += endpoint.fields.length
    ? table(['Parámetro', 'Ubicación', 'Tipo', 'Requisito', 'Reglas / valores posibles'], endpoint.fields, [2050, 1100, 1250, 1500, 3738])
    : table(['Ruta', 'Query string', 'Cuerpo'], [['Sin parámetros.', 'Sin parámetros.', 'Sin cuerpo.']], [3212, 3213, 3213]);
  if (endpoint.note) content += paragraph(`Observaciones: ${endpoint.note}`);
  if (endpoint.route === '/api/v1/accounts/{id}/status') content += paragraph('*Enviar siempre isActive. El DTO vigente no rechaza por sí mismo la ausencia de esta propiedad: omitirla podría desactivar la cuenta al tomar false.');
}
content += paragraph('Documentación interactiva (rutas adicionales)', 'Heading1');
content += paragraph('No son operaciones de negocio ni se incluyen en las 26 anteriores. Disponibles cuando la API se ejecuta en Development.');
content += table(['Método', 'Ruta', 'Uso', 'Parámetros'], [
  ['GET', '/swagger', 'Interfaz Swagger UI.', 'Sin parámetros ni cuerpo.'],
  ['GET', '/swagger/v1/swagger.json', 'Contrato usado por Swagger.', 'Sin parámetros ni cuerpo.'],
  ['GET', '/openapi/v1.json', 'Contrato OpenAPI.', 'Sin parámetros ni cuerpo.']
], [850, 3200, 3000, 2588]);

export function writeWordDocument(content, destination) {
const declaration = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>';
const styles = `${declaration}<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii="Calibri" w:hAnsi="Calibri"/><w:sz w:val="21"/><w:lang w:val="es-DO"/></w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:spacing w:after="110"/></w:pPr></w:pPrDefault></w:docDefaults>${[
  ['Normal', 'Normal', 21, '243746', false], ['Title', 'Título', 40, '17365D', true], ['Subtitle', 'Subtítulo', 25, '486581', false],
  ['Heading1', 'Título 1', 28, '17365D', true], ['Heading2', 'Título 2', 23, '147D92', true], ['TableText', 'Texto de tabla', 18, '243746', false], ['Code', 'Código', 18, '17365D', false]
].map(([id, name, size, color, bold]) => `<w:style w:type="paragraph" w:styleId="${id}"${id === 'Normal' ? ' w:default="1"' : ''}><w:name w:val="${name}"/><w:basedOn w:val="Normal"/><w:pPr><w:spacing w:before="${id.startsWith('Heading') ? '220' : '0'}" w:after="${id === 'Code' ? '0' : '100'}"/>${id.startsWith('Heading') ? `<w:keepNext/><w:outlineLvl w:val="${id === 'Heading1' ? 0 : 1}"/>` : ''}${id === 'Code' ? '<w:shd w:fill="EFF4F8"/>' : ''}</w:pPr><w:rPr><w:sz w:val="${size}"/><w:color w:val="${color}"/>${bold ? '<w:b/>' : ''}${id === 'Code' ? '<w:rFonts w:ascii="Consolas" w:hAnsi="Consolas"/>' : ''}</w:rPr></w:style>`).join('')}</w:styles>`;
const files = {
  '[Content_Types].xml': `${declaration}<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/><Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/></Types>`,
  '_rels/.rels': `${declaration}<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>`,
  'word/_rels/document.xml.rels': `${declaration}<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>`,
  'word/styles.xml': styles.replace('<w:basedOn w:val="Normal"/>', ''),
  'word/document.xml': `${declaration}<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>${content}<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1134" w:right="1134" w:bottom="1134" w:left="1134"/></w:sectPr></w:body></w:document>`
};

// ZIP sin compresión: formato DOCX estándar; CRC32 por entrada.
function crc32(bytes) {
  let crc = 0xffffffff;
  for (const byte of bytes) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0);
  }
  return (crc ^ 0xffffffff) >>> 0;
}
const locals = [], central = [];
let offset = 0;
for (const [name, value] of Object.entries(files)) {
  const nameBytes = Buffer.from(name), bytes = Buffer.from(value), crc = crc32(bytes);
  const local = Buffer.alloc(30);
  local.writeUInt32LE(0x04034b50, 0); local.writeUInt16LE(20, 4); local.writeUInt16LE(0x800, 6);
  local.writeUInt16LE(0x21, 12); local.writeUInt32LE(crc, 14); local.writeUInt32LE(bytes.length, 18);
  local.writeUInt32LE(bytes.length, 22); local.writeUInt16LE(nameBytes.length, 26);
  locals.push(local, nameBytes, bytes);
  const entry = Buffer.alloc(46);
  entry.writeUInt32LE(0x02014b50, 0); entry.writeUInt16LE(20, 4); entry.writeUInt16LE(20, 6); entry.writeUInt16LE(0x800, 8);
  entry.writeUInt16LE(0x21, 14); entry.writeUInt32LE(crc, 16); entry.writeUInt32LE(bytes.length, 20);
  entry.writeUInt32LE(bytes.length, 24); entry.writeUInt16LE(nameBytes.length, 28); entry.writeUInt32LE(offset, 42);
  central.push(entry, nameBytes);
  offset += local.length + nameBytes.length + bytes.length;
}
const directory = Buffer.concat(central), end = Buffer.alloc(22);
end.writeUInt32LE(0x06054b50, 0); end.writeUInt16LE(Object.keys(files).length, 8); end.writeUInt16LE(Object.keys(files).length, 10);
end.writeUInt32LE(directory.length, 12); end.writeUInt32LE(offset, 16);
fs.mkdirSync(path.dirname(destination), { recursive: true });
fs.writeFileSync(destination, Buffer.concat([...locals, directory, end]));
console.log(`Creado: ${destination}`);
}
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
writeWordDocument(content, destination);
console.log(`Operaciones documentadas: ${endpoints.length}. Tablas de parámetros por operación: ${endpoints.length}.`);
}
