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
}

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
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
  } catch {
    throw new ApiError(`No se pudo conectar con la API (${API_BASE_URL}). Verifica que el backend esté ejecutándose y que CORS esté configurado.`, 0)
  }

  if (!response.ok) {
    let message = `La solicitud falló (${response.status}).`
    try {
      const body: unknown = await response.json()
      if (body && typeof body === 'object' && 'message' in body && typeof body.message === 'string') {
        message = body.message
      } else if (body && typeof body === 'object' && 'title' in body && typeof body.title === 'string') {
        message = body.title
      }
    } catch {
      // Some responses, such as 204, have no JSON body.
    }
    throw new ApiError(message, response.status)
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
