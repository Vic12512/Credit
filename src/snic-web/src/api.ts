const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5041/api/v1').replace(/\/$/, '')

export type Account = {
  id: number
  institutionId: number | null
  institutionName: string | null
  roleCode: 'ADMINISTRATOR' | 'CONSUMER' | 'DATA_ENTRY'
  username: string
  email: string
  displayName: string
  isActive: boolean
  lastLoginAt: string | null
  createdAt: string
}

export type Institution = {
  id: number
  institutionTypeId: number
  institutionTypeName: string
  code: string
  rnc: string
  legalName: string
  commercialName: string | null
  isActive: boolean
  createdAt: string
}

export type Person = {
  id: number
  nationalId: string
  firstName: string
  middleName: string | null
  firstSurname: string
  secondSurname: string | null
  birthDate: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export type CreditProduct = {
  id: number
  personId: number
  personNationalId: string
  institutionId: number
  institutionName: string
  productTypeCode: string
  productTypeName: string
  productStatusCode: string
  productStatusName: string
  externalProductIdentifier: string
  approvedAmount: number
  currentBalance: number
  currencyCode: string
  openingDate: string
  closingDate: string | null
  latestReportDate: string | null
  latestDaysPastDue: number
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export type CreditReport = {
  person: Person
  hasCreditHistory: boolean
  summary: {
    productCount: number
    totalApprovedAmount: number
    totalOutstandingBalance: number
  }
  products: CreditProduct[]
  code: 'CREDIT_REPORT_FOUND' | 'NO_CREDIT_HISTORY'
  message: string
  indicators: AcademicIndicators
}

export type CatalogItem = { id: number; code: string; name: string }
export type ScoreRule = { id: number; minDaysPastDue: number; maxDaysPastDue: number | null; score: number; riskLevel: string; estimatedDefaultPercentage: number; isSimulated: boolean }
export type AcademicIndicators = {
  isSimulated: boolean; status: 'SIMULATED' | 'NO_CREDIT_HISTORY'; message: string
  maximumDaysPastDue: number | null; score: number | null; riskLevel: 'LOW' | 'MEDIUM' | 'HIGH' | null
  estimatedDefaultPercentage: number | null
}
export type AuditEntry = {
  id: number; occurredAt: string; accountId: number | null; username: string | null
  institutionId: number | null; personId: number | null; operation: string; entityType: string | null
  entityId: number | null; result: string; attemptedUsername: string | null
  oldValues: string | null; newValues: string | null; details: string | null; ipAddress: string | null
}
export type PagedResponse<T> = { items: T[]; totalCount: number; page: number; pageSize: number }
export const riskNames = { LOW: 'Bajo', MEDIUM: 'Medio', HIGH: 'Alto' }

export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly traceId?: string
  readonly fieldErrors: Record<string, string[]>

  constructor(message: string, status: number, code = 'UNKNOWN_ERROR', fieldErrors: Record<string, string[]> = {}, traceId?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.fieldErrors = fieldErrors
    this.traceId = traceId
  }
}

export async function apiRequest<T>(
  path: string,
  token?: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers)
  if (options.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ApiError(`No se pudo conectar con la API (${API_BASE_URL}). Verifica que el backend esté ejecutándose y que CORS esté configurado.`, 0)
  }

  if (!response.ok) {
    let message = `La solicitud falló (${response.status}).`
    let code = 'UNKNOWN_ERROR'
    let traceId: string | undefined
    const fieldErrors: Record<string, string[]> = {}
    try {
      const body: unknown = await response.json()
      if (body && typeof body === 'object' && 'message' in body && typeof body.message === 'string') {
        message = body.message
      } else if (body && typeof body === 'object' && 'title' in body && typeof body.title === 'string') {
        message = body.title
      }
      if (body && typeof body === 'object') {
        if ('code' in body && typeof body.code === 'string') code = body.code
        if ('traceId' in body && typeof body.traceId === 'string') traceId = body.traceId
        if ('errors' in body && body.errors && typeof body.errors === 'object') {
          for (const [key, value] of Object.entries(body.errors)) {
            if (Array.isArray(value)) fieldErrors[key] = value.filter((item): item is string => typeof item === 'string')
          }
        }
      }
    } catch {
      // Some responses, such as 204, have no JSON body.
    }
    const messages: Record<string, string> = {
      PERSON_NOT_FOUND: 'No se encontró una persona registrada con esa cédula.',
      TOKEN_EXPIRED: 'Tu sesión venció. Inicia sesión nuevamente.',
      INVALID_TOKEN: 'La sesión no es válida. Inicia sesión nuevamente.',
      ACCOUNT_UNAVAILABLE: 'Tu cuenta ya no tiene acceso. Contacta al administrador.',
      INSUFFICIENT_PERMISSIONS: 'No tienes permiso para esta operación o el producto pertenece a otra institución.',
    }
    const details = [...new Set(Object.values(fieldErrors).flat())]
    throw new ApiError([messages[code] ?? message, ...details].join('\n'), response.status, code, fieldErrors, traceId)
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export const roleNames: Record<Account['roleCode'], string> = {
  ADMINISTRATOR: 'Administrador',
  CONSUMER: 'Entidad consumidora',
  DATA_ENTRY: 'Registrador',
}

export function personName(person: Person): string {
  return [person.firstName, person.middleName, person.firstSurname, person.secondSurname]
    .filter(Boolean)
    .join(' ')
}

export function formatMoney(value: number, currency = 'DOP'): string {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency,
    maximumFractionDigits: 2,
  }).format(value)
}

export function formatDate(value: string | null): string {
  if (!value) return '—'
  return new Intl.DateTimeFormat('es-DO', { dateStyle: 'medium' }).format(new Date(`${value.slice(0, 10)}T12:00:00`))
}

export function formatDateTime(value: string | null): string {
  if (!value) return '—'
  const date = new Date(/(?:Z|[+-]\d\d:\d\d)$/i.test(value) ? value : `${value}Z`)
  if (Number.isNaN(date.getTime())) return '—'
  return new Intl.DateTimeFormat('es-DO', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}
