import { test, expect, request, type APIRequestContext, type Page } from '@playwright/test'
import { randomInt, randomUUID } from 'node:crypto'

const credentials = {
  admin: { username: process.env.SNIC_ADMIN_USER ?? 'admin', password: process.env.SNIC_ADMIN_PASSWORD ?? 'Admin_Local_2026!' },
  consumer: { username: process.env.SNIC_CONSUMER_USER ?? 'consumer.demo', password: process.env.SNIC_CONSUMER_PASSWORD ?? 'Consumer_Local_2026!' },
  entry: { username: process.env.SNIC_ENTRY_USER ?? 'dataentry.demo', password: process.env.SNIC_ENTRY_PASSWORD ?? 'DataEntry_Local_2026!' },
}
let api: APIRequestContext
let entryToken: string
let nationalId: string
let emptyNationalId: string
let personId: number
let externalId: string
let foreignExternalId: string
let institutionId: number

async function signIn(page: Page, role: keyof typeof credentials) {
  await page.goto('/')
  await page.getByLabel('Usuario', { exact: true }).fill(credentials[role].username)
  await page.getByLabel('Contraseña', { exact: true }).fill(credentials[role].password)
  await page.getByRole('button', { name: 'Iniciar sesión' }).click()
  await expect(page.getByRole('navigation', { name: 'Navegación principal' })).toBeVisible()
}
async function navigate(page: Page, label: string) {
  await page.getByRole('navigation').getByRole('button', { name: label, exact: true }).click()
}
async function post(path: string, data: unknown, token: string) {
  const response = await api.post(path, { data, headers: { Authorization: `Bearer ${token}` } })
  expect(response.ok(), `${path}: HTTP ${response.status()}`).toBeTruthy()
  return response.json()
}

test.beforeAll(async () => {
  api = await request.newContext({ baseURL: process.env.SNIC_API_URL ?? 'http://localhost:5041/api/v1/', ignoreHTTPSErrors: true })
  const admin = await post('auth/login', credentials.admin, '')
  const entry = await post('auth/login', credentials.entry, '')
  entryToken = entry.accessToken
  institutionId = entry.account.institutionId
  nationalId = `7${randomInt(1_000_000_000, 9_999_999_999)}`
  emptyNationalId = `7${randomInt(1_000_000_000, 9_999_999_999)}`
  const body = { nationalId, firstName: 'Interfaz', firstSurname: 'Indicadores', middleName: null, secondSurname: null, birthDate: '1990-01-01' }
  const person = await post('persons', body, entryToken)
  personId = person.id
  await post('persons', { ...body, nationalId: emptyNationalId, firstName: 'Sin historial' }, entryToken)
  externalId = `UI-${randomUUID().slice(0, 12)}`
  const productBody = { personId, productTypeCode: 'LOAN', productStatusCode: 'OPEN', externalProductIdentifier: externalId, approvedAmount: 5000, currentBalance: 1000, currencyCode: 'DOP', openingDate: '2025-01-01', closingDate: null, latestReportDate: '2025-02-01', latestDaysPastDue: 35 }
  const product = await post('credit-products', productBody, entryToken)
  const update = await api.put(`credit-products/${product.id}`, { data: { ...productBody, currentBalance: 890 }, headers: { Authorization: `Bearer ${entryToken}` } })
  expect(update.ok()).toBeTruthy()
  const products = await api.get('credit-products', { headers: { Authorization: `Bearer ${admin.accessToken}` } })
  const foreign = (await products.json()).find((item: { institutionId: number }) => item.institutionId !== entry.account.institutionId)
  expect(foreign, 'Necesitas un producto de otra institución para comprobar el aislamiento visual').toBeTruthy()
  foreignExternalId = foreign.externalProductIdentifier
})
test.afterAll(async () => { await api?.dispose() })

test('consumidor consulta score, riesgo, porcentaje y reglas simuladas', async ({ page }) => {
  await signIn(page, 'consumer')
  await page.getByLabel('Número de cédula').fill(nationalId)
  await page.getByRole('button', { name: 'Consultar', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Indicadores académicos' })).toBeVisible()
  await expect(page.getByRole('meter')).toHaveAttribute('value', '60')
  await expect(page.locator('.risk-label')).toHaveText('Medio')
  await expect(page.locator('.indicator-card').filter({ hasText: 'INCUMPLIMIENTO SIMULADO' })).toContainText('40')
  await expect(page.getByText('Estos valores son simulados con fines académicos.', { exact: false })).toBeVisible()
  await page.getByText('Consultar las reglas de simulación', { exact: true }).click()
  await expect(page.locator('.rules-panel table tbody tr')).toHaveCount(5)
  await expect(page.getByRole('navigation').getByRole('button', { name: 'Auditoría', exact: true })).toHaveCount(0)
  await page.evaluate(() => window.scrollTo(0, 0))
  await page.screenshot({ path: 'test-results/report-desktop.png', fullPage: true })
})

test('distingue sin historial, persona inexistente y cédula inválida', async ({ page }) => {
  await signIn(page, 'consumer')
  await page.getByLabel('Número de cédula').fill(emptyNationalId)
  await page.getByRole('button', { name: 'Consultar', exact: true }).click()
  await expect(page.getByText('Sin historial para calcular indicadores')).toBeVisible()
  await expect(page.getByRole('meter')).toHaveCount(0)
  await page.getByLabel('Número de cédula').fill('00000000000')
  await page.getByRole('button', { name: 'Consultar', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('No se encontró una persona')
  await page.getByLabel('Número de cédula').fill('123')
  await page.getByRole('button', { name: 'Consultar', exact: true }).click()
  await expect(page.getByRole('alert')).toContainText('exactamente 11 dígitos')
})

test('instituciones disponibles como consulta para consumidor', async ({ page }) => {
  await signIn(page, 'consumer')
  await navigate(page, 'Instituciones')
  await expect(page.getByRole('heading', { name: 'Instituciones', exact: true })).toBeVisible()
  await expect(page.locator('tbody tr').first()).toBeVisible()
  await expect(page.getByRole('button', { name: 'Nueva institución', exact: false })).toHaveCount(0)
  await navigate(page, 'Personas')
  await expect(page.getByRole('button', { name: 'Registrar persona', exact: false })).toHaveCount(0)
})

test('DATA_ENTRY solo edita productos propios y usa catálogos de la API', async ({ page }) => {
  await signIn(page, 'entry')
  await navigate(page, 'Productos')
  const own = page.getByRole('row').filter({ hasText: externalId })
  const foreign = page.getByRole('row').filter({ hasText: foreignExternalId })
  await expect(own.getByRole('button', { name: 'Editar', exact: true })).toBeEnabled()
  await expect(foreign.getByRole('button', { name: 'Editar', exact: true })).toHaveCount(0)
  await expect(foreign).toContainText('Solo consulta')
  await own.getByRole('button', { name: 'Editar', exact: true }).click()
  const dialog = page.getByRole('dialog')
  await expect(dialog.getByLabel('Tipo de producto')).toHaveValue('LOAN')
  await expect(dialog.getByLabel('Tipo de producto').locator('option')).toHaveCount(4)
  await dialog.getByLabel('Balance actual').fill('870')
  await dialog.getByRole('button', { name: 'Guardar producto' }).click()
  await expect(dialog).toHaveCount(0)
  await expect(page.getByRole('status')).toContainText('Producto actualizado')
})

test('administrador filtra auditoría, pagina y abre valores anteriores y nuevos', async ({ page }) => {
  await signIn(page, 'admin')
  await navigate(page, 'Auditoría')
  await expect(page.getByRole('heading', { name: 'Auditoría', exact: true })).toBeVisible()
  await expect(page.locator('tbody tr').first()).toBeVisible()
  await page.screenshot({ path: 'test-results/audit-desktop.png', fullPage: true })
  await page.getByLabel('Operaciones por página').selectOption('10')
  await expect(page.getByRole('button', { name: 'Siguiente', exact: true })).toBeEnabled()
  await page.getByRole('button', { name: 'Siguiente', exact: true }).click()
  await expect(page.locator('.pagination')).toContainText('Página 2')
  await page.getByLabel('ID de persona', { exact: true }).fill(String(personId))
  await page.getByLabel('Operación', { exact: true }).fill('CREDIT_PRODUCT_UPDATED')
  await page.getByLabel('Resultado', { exact: true }).selectOption('SUCCESS')
  await page.getByRole('button', { name: 'Aplicar filtros' }).click()
  await expect(page.locator('.pagination')).toContainText('Página 1')
  await page.getByRole('button', { name: /Ver detalle de operación/ }).first().click()
  const dialog = page.getByRole('dialog')
  await expect(dialog.getByRole('heading', { name: 'Valores anteriores' })).toBeVisible()
  await expect(dialog.locator('pre')).toHaveCount(2)
  await expect(dialog.locator('pre').last()).toContainText('currentBalance')
  await page.keyboard.press('Escape')
  await expect(dialog).toHaveCount(0)
  await page.getByLabel('ID de persona', { exact: true }).fill('999999999999')
  await page.getByRole('button', { name: 'Aplicar filtros' }).click()
  await expect(page.getByText('No hay operaciones que coincidan con los filtros seleccionados.')).toBeVisible()
  await page.getByLabel('Desde', { exact: true }).fill('2026-12-31')
  await page.getByLabel('Hasta', { exact: true }).fill('2026-01-01')
  await page.getByRole('button', { name: 'Aplicar filtros' }).click()
  await expect(page.getByRole('alert')).toContainText('fecha inicial no puede ser posterior')
})

test('pantallas móviles mantienen indicadores y navegación utilizables', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await signIn(page, 'admin')
  await page.getByLabel('Número de cédula').fill(nationalId)
  await page.getByRole('button', { name: 'Consultar', exact: true }).click()
  await expect(page.getByRole('meter')).toBeVisible()
  await page.evaluate(() => window.scrollTo(0, 0))
  await page.screenshot({ path: 'test-results/report-mobile.png' })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy()
  await navigate(page, 'Auditoría')
  await expect(page.getByLabel('Desde', { exact: true })).toBeVisible()
  await page.evaluate(() => window.scrollTo(0, 0))
  await page.screenshot({ path: 'test-results/audit-mobile.png' })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy()
})

test('sesión vencida se cierra al recibir TOKEN_EXPIRED', async ({ page }) => {
  await signIn(page, 'consumer')
  await page.route('**/api/v1/persons', route => route.fulfill({ status: 401, contentType: 'application/json', body: JSON.stringify({ code: 'TOKEN_EXPIRED', message: 'El token ha vencido.', traceId: 'ui-test' }) }))
  await navigate(page, 'Personas')
  await expect(page.getByRole('heading', { name: 'Inicia sesión' })).toBeVisible()
  await expect(page.getByRole('alert')).toContainText('sesión venció')
  expect(await page.evaluate(() => localStorage.getItem('snic.session'))).toBeNull()
})

test('registrador crea una persona y recibe un error claro al duplicarla', async ({ page }) => {
  await signIn(page, 'entry')
  await navigate(page, 'Personas')
  const id = `6${randomInt(1_000_000_000, 9_999_999_999)}`
  async function fillPerson() {
    await page.getByRole('button', { name: 'Registrar persona', exact: false }).click()
    const dialog = page.getByRole('dialog')
    await dialog.locator('[name=nationalId]').fill(id)
    await dialog.locator('[name=firstName]').fill('Prueba de interfaz')
    await dialog.locator('[name=firstSurname]').fill('Registro')
    await dialog.getByRole('button', { name: 'Guardar persona' }).click()
  }
  await fillPerson()
  await expect(page.getByRole('dialog')).toHaveCount(0)
  await expect(page.getByRole('status')).toContainText('Persona registrada')
  await fillPerson()
  await expect(page.getByRole('dialog').getByRole('alert')).toContainText('Ya existe una persona')
  await page.keyboard.press('Escape')
  await expect(page.getByRole('dialog')).toHaveCount(0)
})

test('administrador crea una cuenta y confirma su desactivación y activación', async ({ page }) => {
  await signIn(page, 'admin')
  await navigate(page, 'Cuentas')
  const username = `ui.${randomUUID().slice(0,8)}`
  await page.getByRole('button', { name: 'Nueva cuenta', exact: false }).click()
  const dialog = page.getByRole('dialog')
  await dialog.locator('[name=displayName]').fill('Cuenta prueba de interfaz')
  await dialog.locator('[name=username]').fill(username)
  await dialog.locator('[name=email]').fill(`${username}@snic.local`)
  await dialog.locator('[name=roleCode]').selectOption('CONSUMER')
  await dialog.locator('[name=institutionId]').selectOption(String(institutionId))
  await dialog.locator('[name=password]').fill('Prueba_Interfaz_2026!')
  await dialog.getByRole('button', { name: 'Crear cuenta', exact: true }).click()
  await expect(dialog).toHaveCount(0)
  const row = page.getByRole('row').filter({ hasText: `${username}@snic.local` })
  await row.getByRole('button', { name: 'Desactivar', exact: true }).click()
  await expect(page.getByRole('dialog')).toContainText('incluso si tiene una sesión abierta')
  await page.getByRole('dialog').getByLabel('Motivo').fill('Prueba de desactivación desde interfaz')
  await page.getByRole('dialog').getByRole('button', { name: 'Confirmar cambio' }).click()
  await expect(row.getByRole('button', { name: 'Activar', exact: true })).toBeVisible()
  const rejected = await api.post('auth/login', { data: { username, password: 'Prueba_Interfaz_2026!' } })
  expect(rejected.status()).toBe(401)
  await row.getByRole('button', { name: 'Activar', exact: true }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Confirmar cambio' }).click()
  await expect(row.getByRole('button', { name: 'Desactivar', exact: true })).toBeVisible()
})
