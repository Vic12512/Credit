import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { DataTable, Field, Modal, Notice, PageHeading, StatCard, StatusPill } from './ui'
import { AuditPage } from './AuditPage'
import { Indicators } from './Indicators'
import {
  apiRequest,
  ApiError,
  formatDate,
  formatDateTime,
  formatMoney,
  personName,
  roleNames,
  type Account,
  type CreditProduct,
  type CreditReport,
  type Institution,
  type Person,
  type CatalogItem,
  type ScoreRule,
} from './api'
import './App.css'

type Session = { token: string; account: Account; expiresAtUtc: string }
type Page = 'report' | 'persons' | 'products' | 'institutions' | 'accounts' | 'audit'
type PageProps = { token: string; account: Account; onUnauthorized: (error?: ApiError) => void }
type StoredSession = { token: string; expiresAtUtc: string }

const SESSION_KEY = 'snic.session'

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Ocurrió un error inesperado.'
}

function useResource<T>(path: string, token: string, onUnauthorized: (error?: ApiError) => void, enabled = true) {
  const [items, setItems] = useState<T[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [revision, setRevision] = useState(0)

  const reload = useCallback(() => setRevision((value) => value + 1), [])

  useEffect(() => {
    if (!enabled) { setLoading(false); return }
    let active = true
    setLoading(true)
    setError('')
    apiRequest<T[]>(path, token)
      .then((result) => {
        if (active) setItems(result)
      })
      .catch((reason: unknown) => {
        if (!active) return
        setError(getErrorMessage(reason))
        if (reason instanceof ApiError && reason.status === 401) onUnauthorized(reason)
      })
      .finally(() => {
        if (active) setLoading(false)
      })
    return () => {
      active = false
    }
  }, [path, token, revision, onUnauthorized, enabled])

  return { items, loading, error, reload }
}

function App() {
  const [session, setSession] = useState<Session | null>(null)
  const [checkingSession, setCheckingSession] = useState(true)
  const [authError, setAuthError] = useState('')
  const [page, setPage] = useState<Page>('report')

  const clearSession = useCallback(() => {
    localStorage.removeItem(SESSION_KEY)
    setSession(null)
    setPage('report')
    setAuthError('')
  }, [])
  const handleUnauthorized = useCallback((error?: ApiError) => {
    clearSession()
    setAuthError(error?.message ?? 'Tu sesión expiró o ya no está autorizada. Inicia sesión nuevamente.')
  }, [clearSession])

  useEffect(() => {
    let active = true
    const stored = localStorage.getItem(SESSION_KEY)
    if (!stored) {
      setCheckingSession(false)
      return
    }

    let parsed: StoredSession
    try {
      parsed = JSON.parse(stored) as StoredSession
    } catch {
      localStorage.removeItem(SESSION_KEY)
      setCheckingSession(false)
      return
    }

    if (!parsed || typeof parsed !== 'object' || typeof parsed.token !== 'string' || !parsed.token || !Number.isFinite(Date.parse(parsed.expiresAtUtc)) || new Date(parsed.expiresAtUtc) <= new Date()) {
      localStorage.removeItem(SESSION_KEY)
      setCheckingSession(false)
      return
    }

    apiRequest<Account>('/auth/me', parsed.token)
      .then((account) => {
        if (active) setSession({ token: parsed.token, account, expiresAtUtc: parsed.expiresAtUtc })
      })
      .catch((error: unknown) => {
        if (!active) return
        if (error instanceof ApiError && error.status === 401) {
          localStorage.removeItem(SESSION_KEY)
          setAuthError('Tu sesión expiró o ya no está autorizada. Inicia sesión nuevamente.')
        } else {
          setAuthError(getErrorMessage(error))
        }
      })
      .finally(() => {
        if (active) setCheckingSession(false)
      })

    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    if (!session) return
    const timer = window.setTimeout(handleUnauthorized, Math.max(0, Date.parse(session.expiresAtUtc) - Date.now()))
    return () => window.clearTimeout(timer)
  }, [session, handleUnauthorized])

  async function signIn(username: string, password: string) {
    setAuthError('')
    try {
      const result = await apiRequest<{ accessToken: string; expiresAtUtc: string; account: Account }>(
        '/auth/login',
        undefined,
        { method: 'POST', body: JSON.stringify({ username, password }) },
      )
      localStorage.setItem(SESSION_KEY, JSON.stringify({
        token: result.accessToken,
        expiresAtUtc: result.expiresAtUtc,
      }))
      setSession({ token: result.accessToken, account: result.account, expiresAtUtc: result.expiresAtUtc })
    } catch (error) {
      setAuthError(getErrorMessage(error))
      throw error
    }
  }

  if (checkingSession) {
    return <div className="boot-screen"><span className="spinner" />Cargando SNIC…</div>
  }

  if (!session) {
    return <LoginScreen onSignIn={signIn} error={authError} />
  }

  const { account, token } = session
  const navItems: { id: Page; label: string; icon: string }[] = [
    { id: 'report', label: 'Consulta crediticia', icon: '⌕' },
    { id: 'persons', label: 'Personas', icon: '◉' },
    { id: 'products', label: 'Productos', icon: '▤' },
    { id: 'institutions', label: 'Instituciones', icon: '⌂' },
    ...(account.roleCode === 'ADMINISTRATOR'
      ? [
          { id: 'accounts' as Page, label: 'Cuentas', icon: '♙' },
          { id: 'audit' as Page, label: 'Auditoría', icon: '▦' },
        ]
      : []),
  ]

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="#" onClick={(event) => { event.preventDefault(); setPage('report') }}>
          <span className="brand-mark">S</span>
          <span><strong>SNIC</strong><small>Sistema Nacional de Información Crediticia</small></span>
        </a>
        <div className="nav-caption">MENÚ PRINCIPAL</div>
        <nav className="main-nav" aria-label="Navegación principal">
          {navItems.map((item) => (
            <button
              className={`nav-item ${page === item.id ? 'selected' : ''}`}
              key={item.id}
              aria-current={page === item.id ? 'page' : undefined}
              onClick={() => setPage(item.id)}
            >
              <span className="nav-icon" aria-hidden="true">{item.icon}</span>
              {item.label}
              {page === item.id && <span className="nav-indicator" />}
            </button>
          ))}
        </nav>
        <div className="sidebar-note">
          <span className="shield-icon">✓</span>
          <div><strong>Acceso seguro</strong><p>Información protegida y disponible solo para usuarios autorizados.</p></div>
        </div>
        <div className="sidebar-footer">SNIC · Sistema de consulta crediticia</div>
      </aside>

      <main className="main-area">
        <header className="topbar">
          <div className="breadcrumb"><span>SNIC</span><span className="crumb-separator">/</span>{navItems.find((item) => item.id === page)?.label}</div>
          <div className="user-menu">
            <div className="user-avatar">{account.displayName.trim().charAt(0).toUpperCase()}</div>
            <div className="user-info"><strong>{account.displayName}</strong><span>{roleNames[account.roleCode]}</span></div>
            <button className="logout-button" onClick={clearSession} aria-label="Cerrar sesión" title="Cerrar sesión">↪</button>
          </div>
        </header>
        <div className="content-area">
          {page === 'report' && <ReportPage token={token} account={account} onUnauthorized={handleUnauthorized} />}
          {page === 'persons' && <PeoplePage token={token} account={account} onUnauthorized={handleUnauthorized} />}
          {page === 'products' && <ProductsPage token={token} account={account} onUnauthorized={handleUnauthorized} />}
          {page === 'institutions' && <InstitutionsPage token={token} account={account} onUnauthorized={handleUnauthorized} />}
          {page === 'accounts' && account.roleCode === 'ADMINISTRATOR' && <AccountsPage token={token} account={account} onUnauthorized={handleUnauthorized} />}
          {page === 'audit' && account.roleCode === 'ADMINISTRATOR' && <AuditPage token={token} onUnauthorized={handleUnauthorized} />}
        </div>
      </main>
    </div>
  )
}

function LoginScreen({ onSignIn, error }: { onSignIn: (username: string, password: string) => Promise<void>; error: string }) {
  const [busy, setBusy] = useState(false)
  const [localError, setLocalError] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setLocalError('')
    const data = new FormData(event.currentTarget)
    try {
      await onSignIn(String(data.get('username') ?? '').trim(), String(data.get('password') ?? ''))
    } catch (reason) {
      setLocalError(getErrorMessage(reason))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="login-layout">
      <section className="login-visual">
        <div className="login-brand"><span className="brand-mark">S</span><strong>SNIC</strong></div>
        <div className="login-message">
          <span className="eyebrow">SISTEMA NACIONAL DE INFORMACIÓN CREDITICIA</span>
          <h1>Información confiable para decisiones responsables.</h1>
          <p>Consulta consolidada de historial crediticio para las instituciones autorizadas.</p>
        </div>
        <div className="login-visual-footer"><span>●</span> Plataforma segura para entidades autorizadas</div>
      </section>
      <section className="login-panel">
        <form className="login-form" onSubmit={submit}>
          <div className="login-icon">↗</div>
          <span className="eyebrow">BIENVENIDO</span>
          <h2>Inicia sesión</h2>
          <p className="muted">Ingresa tus credenciales institucionales para continuar.</p>
          {(localError || error) && <Notice message={localError || error} />}
          <label className="field-label" htmlFor="username">Usuario</label>
          <input id="username" name="username" autoComplete="username" placeholder="Tu usuario" required />
          <label className="field-label" htmlFor="password">Contraseña</label>
          <input id="password" name="password" type="password" autoComplete="current-password" placeholder="Tu contraseña" required />
          <button className="button button-primary login-submit" disabled={busy}>
            {busy ? <><span className="button-spinner" />Validando…</> : 'Iniciar sesión'}
            {!busy && <span aria-hidden="true">→</span>}
          </button>
          <p className="login-help">¿Necesitas acceso? Contacta al administrador de tu institución.</p>
        </form>
        <footer className="login-footer">SNIC · Acceso autorizado</footer>
      </section>
    </div>
  )
}

function ReportPage({ token, onUnauthorized }: PageProps) {
  const { items: rules, error: rulesError, reload: reloadRules } = useResource<ScoreRule>('/catalogs/score-rules', token, onUnauthorized)
  const [nationalId, setNationalId] = useState('')
  const [report, setReport] = useState<CreditReport | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const id = nationalId.replace(/\D/g, '')
    if (!/^\d{11}$/.test(id)) {
      setError('La cédula debe contener exactamente 11 dígitos.')
      setReport(null)
      return
    }
    setLoading(true)
    setError('')
    setReport(null)
    try {
      setReport(await apiRequest<CreditReport>(`/credit-reports/${encodeURIComponent(id)}`, token))
    } catch (reason) {
      setError(getErrorMessage(reason))
      if (reason instanceof ApiError && reason.status === 401) onUnauthorized(reason)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="page-wrap">
      <PageHeading eyebrow="CENTRO DE CONSULTA" title="Consulta crediticia" description="Consulta el historial consolidado de una persona por su número de cédula." />
      <section className="search-card">
        <div className="search-card-icon">⌕</div>
        <div className="search-copy"><h2>Buscar historial</h2><p>Consulta los registros reportados y sus indicadores académicos simulados.</p></div>
        <form className="search-form" onSubmit={search}>
          <label className="sr-only" htmlFor="nationalId">Número de cédula</label>
          <span className="input-prefix">ID</span>
          <input
            id="nationalId"
            value={nationalId}
            onChange={(event) => setNationalId(event.target.value.replace(/\D/g, '').slice(0, 11))}
            placeholder="00000000000"
            inputMode="numeric"
            maxLength={11}
            required
          />
          <button className="button button-primary" disabled={loading}>
            {loading ? <><span className="button-spinner" />Buscando</> : 'Consultar'}
          </button>
        </form>
        <div className="search-hint"><span className="info-dot">i</span> Ingresa los 11 dígitos de la cédula, sin guiones.</div>
      </section>
      {error && <Notice message={error} />}
      {report && <><ReportResult report={report} rules={rules} />{rulesError && <><Notice message={`No se pudieron cargar las reglas de simulación: ${rulesError}`} /><button className="button button-secondary" onClick={reloadRules}>Reintentar reglas</button></>}</>}
      {!report && !error && !loading && <div className="empty-prompt"><span className="empty-icon">⌕</span><strong>Tu consulta aparecerá aquí</strong><p>Ingresa una cédula para ver el resumen del historial crediticio.</p></div>}
      <div className="page-footnote"><span>◈</span> Los datos mostrados son confidenciales y deben tratarse conforme a las políticas de la institución.</div>
    </div>
  )
}

function ReportResult({ report, rules }: { report: CreditReport; rules: ScoreRule[] }) {
  return (
    <section className="report-results">
      <div className="person-summary">
        <div className="person-avatar">{report.person.firstName.charAt(0)}{report.person.firstSurname.charAt(0)}</div>
        <div className="person-details"><span className="eyebrow">PERSONA CONSULTADA</span><h2>{personName(report.person)}</h2><p>Cédula <strong>{report.person.nationalId}</strong>{report.person.birthDate && <> · Nacimiento {formatDate(report.person.birthDate)}</>}</p></div>
        <span className={`history-badge ${report.hasCreditHistory ? 'has-history' : 'no-history'}`}><span />{report.hasCreditHistory ? 'Con historial' : 'Sin historial'}</span>
      </div>
      <div className="stat-grid">
        <StatCard label="Productos reportados" value={report.summary.productCount.toLocaleString('es-DO')} icon="▤" />
        <StatCard label="Monto aprobado total" value={formatMoney(report.summary.totalApprovedAmount)} icon="↗" />
        <StatCard label="Balance pendiente total" value={formatMoney(report.summary.totalOutstandingBalance)} icon="◷" accent />
      </div>
      <Indicators report={report} rules={rules} />
      <div className="section-title-row"><div><h2>Productos crediticios</h2><p>Detalle de los productos reportados para esta persona.</p></div><span className="count-pill">{report.products.length} productos</span></div>
      {report.products.length === 0 ? <div className="table-empty">No hay productos reportados para esta persona.</div> : (
        <div className="table-card"><table><thead><tr><th>INSTITUCIÓN</th><th>PRODUCTO</th><th>ESTADO</th><th>MONTO APROBADO</th><th>BALANCE ACTUAL</th><th>ATRASO</th><th>APERTURA / CIERRE</th><th>ÚLTIMO REPORTE</th></tr></thead>
          <tbody>{report.products.map((product) => <tr key={product.id}><td><strong>{product.institutionName}</strong><small>{product.externalProductIdentifier}</small></td><td>{product.productTypeName}</td><td><StatusPill active={product.isActive}>{product.productStatusName}</StatusPill></td><td>{formatMoney(product.approvedAmount, product.currencyCode)}</td><td>{formatMoney(product.currentBalance, product.currencyCode)}</td><td>{product.latestDaysPastDue ? <span className="late-days">{product.latestDaysPastDue} días</span> : 'Al día'}</td><td>{formatDate(product.openingDate)}<small>Cierre: {formatDate(product.closingDate)}</small></td><td>{formatDate(product.latestReportDate)}</td></tr>)}</tbody>
        </table></div>
      )}
    </section>
  )
}

function PeoplePage({ token, account, onUnauthorized }: PageProps) {
  const { items: people, loading, error, reload } = useResource<Person>('/persons', token, onUnauthorized)
  const [editing, setEditing] = useState<Person | null | undefined>(undefined)
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [success, setSuccess] = useState('')
  const canCreate = account.roleCode === 'DATA_ENTRY'
  const canEdit = account.roleCode === 'ADMINISTRATOR'

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const data = new FormData(form)
    const body = {
      nationalId: String(data.get('nationalId') ?? '').trim(),
      firstName: String(data.get('firstName') ?? '').trim(),
      middleName: optionalValue(data.get('middleName')),
      firstSurname: String(data.get('firstSurname') ?? '').trim(),
      secondSurname: optionalValue(data.get('secondSurname')),
      birthDate: optionalValue(data.get('birthDate')),
    }
    setSaving(true)
    setFormError('')
    setSuccess('')
    try {
      const path = editing ? `/persons/${editing.id}` : '/persons'
      await apiRequest(path, token, { method: editing ? 'PUT' : 'POST', body: JSON.stringify(body) })
      setSuccess(editing ? 'Datos personales actualizados.' : 'Persona registrada correctamente.')
      setEditing(undefined)
      reload()
    } catch (reason) {
      setFormError(getErrorMessage(reason))
      if (reason instanceof ApiError && reason.status === 401) onUnauthorized(reason)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="page-wrap">
      <PageHeading eyebrow="GESTIÓN DE INFORMACIÓN" title="Personas" description="Consulta el registro de personas y administra la información según tus permisos." action={canCreate ? <button className="button button-primary" onClick={() => { setFormError(''); setEditing(null) }}>＋ Registrar persona</button> : undefined} />
      {success && <Notice message={success} kind="success" />}
      {error && <Notice message={error} />}
      <DataTable loading={loading} error={error} empty="Todavía no hay personas registradas." count={people.length} onRetry={reload}>
        <table><thead><tr><th>PERSONA</th><th>CÉDULA</th><th>FECHA DE NACIMIENTO</th><th>ESTADO</th>{canEdit && <th></th>}</tr></thead><tbody>
          {people.map((person) => <tr key={person.id}><td><strong>{personName(person)}</strong><small>Registro #{person.id}</small></td><td>{person.nationalId}</td><td>{formatDate(person.birthDate)}</td><td><StatusPill active={person.isActive}>{person.isActive ? 'Activa' : 'Inactiva'}</StatusPill></td>{canEdit && <td><button className="text-button" onClick={() => { setFormError(''); setEditing(person) }}>Editar</button></td>}</tr>)}
        </tbody></table>
      </DataTable>
      {editing !== undefined && <Modal title={editing ? 'Editar persona' : 'Registrar persona'} onClose={() => setEditing(undefined)}>
        {formError && <Notice message={formError} />}
        <form className="form-grid" onSubmit={submit}>
          <Field label="Cédula" name="nationalId" defaultValue={editing?.nationalId} required pattern="[0-9]{11}" maxLength={11} help="11 dígitos, sin guiones." />
          <Field label="Nombre" name="firstName" defaultValue={editing?.firstName} required maxLength={80} />
          <Field label="Segundo nombre" name="middleName" defaultValue={editing?.middleName ?? ''} maxLength={80} />
          <Field label="Primer apellido" name="firstSurname" defaultValue={editing?.firstSurname} required maxLength={80} />
          <Field label="Segundo apellido" name="secondSurname" defaultValue={editing?.secondSurname ?? ''} maxLength={80} />
          <Field label="Fecha de nacimiento" name="birthDate" type="date" defaultValue={editing?.birthDate ?? ''} />
          <div className="form-actions"><button type="button" className="button button-secondary" onClick={() => setEditing(undefined)}>Cancelar</button><button className="button button-primary" disabled={saving}>{saving ? 'Guardando…' : 'Guardar persona'}</button></div>
        </form>
      </Modal>}
    </div>
  )
}

function ProductsPage({ token, account, onUnauthorized }: PageProps) {
  const { items: products, loading, error, reload } = useResource<CreditProduct>('/credit-products', token, onUnauthorized)
  const { items: people } = useResource<Person>('/persons', token, onUnauthorized)
  const { items: institutions } = useResource<Institution>('/institutions', token, onUnauthorized)
  const { items: types, loading: typesLoading, error: typesError, reload: reloadTypes } = useResource<CatalogItem>('/catalogs/product-types', token, onUnauthorized)
  const { items: statuses, loading: statusesLoading, error: statusesError, reload: reloadStatuses } = useResource<CatalogItem>('/catalogs/product-statuses', token, onUnauthorized)
  const [editing, setEditing] = useState<CreditProduct | null | undefined>(undefined)
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [success, setSuccess] = useState('')
  const canManage = account.roleCode === 'DATA_ENTRY'
  const catalogError = typesError || statusesError

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const optionalNumber = (name: string) => {
      const value = String(data.get(name) ?? '')
      return value ? Number(value) : null
    }
    const body = {
      personId: Number(data.get('personId')),
      institutionId: optionalNumber('institutionId'),
      productTypeCode: String(data.get('productTypeCode')),
      productStatusCode: String(data.get('productStatusCode')),
      externalProductIdentifier: String(data.get('externalProductIdentifier')).trim(),
      approvedAmount: Number(data.get('approvedAmount')),
      currentBalance: Number(data.get('currentBalance')),
      currencyCode: String(data.get('currencyCode')).toUpperCase(),
      openingDate: String(data.get('openingDate')),
      closingDate: optionalValue(data.get('closingDate')),
      latestReportDate: optionalValue(data.get('latestReportDate')),
      latestDaysPastDue: Number(data.get('latestDaysPastDue')),
    }
    if (editing && editing.institutionId !== account.institutionId) { setFormError('Solo puedes corregir productos de tu institución.'); return }
    setSaving(true)
    setFormError('')
    try {
      await apiRequest(editing ? `/credit-products/${editing.id}` : '/credit-products', token, {
        method: editing ? 'PUT' : 'POST',
        body: JSON.stringify(body),
      })
      setSuccess(editing ? 'Producto actualizado correctamente.' : 'Producto registrado en tu institución.')
      setEditing(undefined)
      reload()
    } catch (reason) {
      setFormError(getErrorMessage(reason))
      if (reason instanceof ApiError && reason.status === 401) onUnauthorized(reason)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="page-wrap">
      <PageHeading eyebrow="GESTIÓN DE INFORMACIÓN" title="Productos crediticios" description="Consulta los productos reportados y registra información de tu institución." action={canManage ? <button className="button button-primary" disabled={typesLoading || statusesLoading || Boolean(catalogError) || !account.institutionId} onClick={() => { setFormError(''); setEditing(null) }}>＋ Registrar producto</button> : undefined} />
      {error && <Notice message={error} />}
      {catalogError && <><Notice message={`No se pudieron cargar los catálogos: ${catalogError}`} /><button className="button button-secondary" onClick={() => { reloadTypes(); reloadStatuses() }}>Reintentar catálogos</button></>}
      {success && <Notice message={success} kind="success" />}
      <DataTable loading={loading} error={error} empty="Todavía no hay productos crediticios registrados." count={products.length} onRetry={reload}>
        <table><thead><tr><th>PRODUCTO</th><th>PERSONA</th><th>INSTITUCIÓN</th><th>MONTO / BALANCE</th><th>ESTADO</th>{canManage && <th></th>}</tr></thead><tbody>
          {products.map((product) => <tr key={product.id}><td><strong>{product.productTypeName}</strong><small>{product.externalProductIdentifier}</small></td><td>{product.personNationalId}</td><td>{product.institutionName}</td><td>{formatMoney(product.approvedAmount, product.currencyCode)}<small>Balance {formatMoney(product.currentBalance, product.currencyCode)}</small></td><td><StatusPill active={product.isActive}>{product.productStatusName}</StatusPill></td>{canManage && <td>{product.institutionId === account.institutionId ? <button className="text-button" disabled={Boolean(catalogError) || typesLoading || statusesLoading} onClick={() => { setFormError(''); setEditing(product) }}>Editar</button> : <span className="read-only-label">Solo consulta</span>}</td>}</tr>)}
        </tbody></table>
      </DataTable>
      {editing !== undefined && <Modal title={editing ? 'Editar producto crediticio' : 'Registrar producto crediticio'} onClose={() => setEditing(undefined)}>
        {formError && <Notice message={formError} />}
        <form className="form-grid" onSubmit={submit}>
          <label className="form-field"><span>Persona</span><select name="personId" defaultValue={editing?.personId ?? ''} required><option value="" disabled>Selecciona una persona</option>{people.filter((person) => person.isActive).map((person) => <option key={person.id} value={person.id}>{person.nationalId} · {personName(person)}</option>)}</select></label>
          <label className="form-field"><span>Institución</span><select name="institutionId" defaultValue={editing?.institutionId ?? account.institutionId ?? ''} disabled={account.roleCode === 'DATA_ENTRY'}><option value="">Institución asociada a mi cuenta</option>{institutions.map((item) => <option key={item.id} value={item.id}>{item.legalName}</option>)}</select>{account.roleCode === 'DATA_ENTRY' && <small>Los productos se registran en la institución de tu cuenta.</small>}</label>
          <label className="form-field"><span>Tipo de producto *</span><select name="productTypeCode" defaultValue={editing?.productTypeCode ?? ''} required><option value="" disabled>Selecciona un tipo</option>{types.map(item => <option key={item.id} value={item.code}>{item.name}</option>)}</select></label>
          <label className="form-field"><span>Estado *</span><select name="productStatusCode" defaultValue={editing?.productStatusCode ?? ''} required><option value="" disabled>Selecciona un estado</option>{statuses.map(item => <option key={item.id} value={item.code}>{item.name}</option>)}</select></label>
          <Field label="Identificador externo" name="externalProductIdentifier" defaultValue={editing?.externalProductIdentifier} required maxLength={100} />
          <Field label="Moneda (ISO 4217)" name="currencyCode" defaultValue={editing?.currencyCode ?? 'DOP'} required pattern="[A-Za-z]{3}" maxLength={3} />
          <Field label="Monto aprobado" name="approvedAmount" type="number" defaultValue={editing?.approvedAmount ?? 0} min="0" step="0.01" required />
          <Field label="Balance actual" name="currentBalance" type="number" defaultValue={editing?.currentBalance ?? 0} min="0" step="0.01" required />
          <Field label="Fecha de apertura" name="openingDate" type="date" defaultValue={editing?.openingDate ?? ''} required />
          <Field label="Fecha de cierre" name="closingDate" type="date" defaultValue={editing?.closingDate ?? ''} />
          <Field label="Último reporte" name="latestReportDate" type="date" defaultValue={editing?.latestReportDate ?? ''} />
          <Field label="Días de atraso" name="latestDaysPastDue" type="number" defaultValue={editing?.latestDaysPastDue ?? 0} min="0" step="1" required />
          <div className="form-actions"><button type="button" className="button button-secondary" onClick={() => setEditing(undefined)}>Cancelar</button><button className="button button-primary" disabled={saving || people.length === 0}>{saving ? 'Guardando…' : 'Guardar producto'}</button></div>
        </form>
      </Modal>}
    </div>
  )
}

function InstitutionsPage({ token, account, onUnauthorized }: PageProps) {
  const { items, loading, error, reload } = useResource<Institution>('/institutions', token, onUnauthorized)
  const [creating, setCreating] = useState(false)
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [success, setSuccess] = useState('')
  const canCreate = account.roleCode === 'ADMINISTRATOR'
  const { items: types, loading: typesLoading, error: typesError, reload: reloadTypes } = useResource<CatalogItem>('/catalogs/institution-types', token, onUnauthorized, canCreate)

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const body = {
      institutionTypeId: Number(data.get('institutionTypeId')),
      code: String(data.get('code')).trim(),
      rnc: String(data.get('rnc')).trim(),
      legalName: String(data.get('legalName')).trim(),
      commercialName: optionalValue(data.get('commercialName')),
    }
    setSaving(true)
    setFormError('')
    try {
      await apiRequest('/institutions', token, { method: 'POST', body: JSON.stringify(body) })
      setSuccess('Institución registrada correctamente.')
      setCreating(false)
      reload()
    } catch (reason) {
      setFormError(getErrorMessage(reason))
      if (reason instanceof ApiError && reason.status === 401) onUnauthorized(reason)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="page-wrap">
      <PageHeading eyebrow={canCreate ? 'ADMINISTRACIÓN' : 'DIRECTORIO INSTITUCIONAL'} title="Instituciones" description="Instituciones autorizadas para participar en el sistema." action={canCreate ? <button className="button button-primary" disabled={typesLoading || Boolean(typesError)} onClick={() => { setFormError(''); setCreating(true) }}>＋ Nueva institución</button> : undefined} />
      {error && <Notice message={error} />}
      {typesError && <><Notice message={typesError} /><button className="button button-secondary" onClick={reloadTypes}>Reintentar catálogos</button></>}
      {success && <Notice message={success} kind="success" />}
      <DataTable loading={loading} error={error} empty="No hay instituciones registradas." count={items.length} onRetry={reload}>
        <table><thead><tr><th>INSTITUCIÓN</th><th>CÓDIGO</th><th>RNC</th><th>TIPO</th><th>ESTADO</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td><strong>{item.commercialName || item.legalName}</strong><small>{item.legalName}</small></td><td>{item.code}</td><td>{item.rnc}</td><td>{item.institutionTypeName}</td><td><StatusPill active={item.isActive}>{item.isActive ? 'Activa' : 'Inactiva'}</StatusPill></td></tr>)}</tbody></table>
      </DataTable>
      {creating && <Modal title="Registrar institución" onClose={() => setCreating(false)}>
        {formError && <Notice message={formError} />}
        <form className="form-grid" onSubmit={submit}>
          <label className="form-field"><span>Tipo de institución *</span><select name="institutionTypeId" defaultValue="" required><option value="" disabled>Selecciona un tipo</option>{types.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
          <Field label="Código institucional" name="code" required maxLength={30} />
          <Field label="RNC" name="rnc" required pattern="[0-9]{9,11}" maxLength={11} help="9 a 11 dígitos." />
          <Field label="Razón social" name="legalName" required maxLength={180} />
          <Field label="Nombre comercial" name="commercialName" maxLength={180} />
          <div className="form-actions"><button type="button" className="button button-secondary" onClick={() => setCreating(false)}>Cancelar</button><button className="button button-primary" disabled={saving}>{saving ? 'Guardando…' : 'Guardar institución'}</button></div>
        </form>
      </Modal>}
    </div>
  )
}

function AccountsPage({ token, account, onUnauthorized }: PageProps) {
  const { items, loading, error, reload } = useResource<Account>('/accounts', token, onUnauthorized)
  const { items: institutions } = useResource<Institution>('/institutions', token, onUnauthorized)
  const { items: roles, loading: rolesLoading, error: rolesError, reload: reloadRoles } = useResource<CatalogItem>('/catalogs/roles', token, onUnauthorized)
  const [creating, setCreating] = useState(false)
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [newRole, setNewRole] = useState<Account['roleCode']>('CONSUMER')
  const [newInstitutionId, setNewInstitutionId] = useState('')
  const [statusTarget, setStatusTarget] = useState<Account | null>(null)
  const [changingStatus, setChangingStatus] = useState(false)
  const [success, setSuccess] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const body = {
      institutionId: newRole === 'ADMINISTRATOR' || !newInstitutionId ? null : Number(newInstitutionId),
      roleCode: newRole,
      username: String(data.get('username')).trim(),
      email: String(data.get('email')).trim(),
      displayName: String(data.get('displayName')).trim(),
      password: String(data.get('password')),
    }
    setSaving(true)
    setFormError('')
    try {
      await apiRequest('/accounts', token, { method: 'POST', body: JSON.stringify(body) })
      setSuccess('Cuenta de usuario creada correctamente.')
      setCreating(false)
      reload()
    } catch (reason) {
      setFormError(getErrorMessage(reason))
      if (reason instanceof ApiError && reason.status === 401) onUnauthorized(reason)
    } finally {
      setSaving(false)
    }
  }

  async function toggleStatus(item: Account, reason: string) {
    if (item.id === account.id) {
      setFormError('No puedes cambiar el estado de tu propia cuenta.')
      return
    }
    setFormError('')
    setChangingStatus(true)
    try {
      await apiRequest(`/accounts/${item.id}/status`, token, {
        method: 'PATCH',
        body: JSON.stringify({ isActive: !item.isActive, reason }),
      })
      setSuccess(item.isActive ? 'Cuenta desactivada. Sus accesos anteriores ya no son válidos.' : 'Cuenta activada correctamente.')
      setStatusTarget(null)
      reload()
    } catch (reason) {
      setFormError(getErrorMessage(reason))
      if (reason instanceof ApiError && reason.status === 401) onUnauthorized(reason)
    } finally {
      setChangingStatus(false)
    }
  }

  return (
    <div className="page-wrap">
      <PageHeading eyebrow="ADMINISTRACIÓN" title="Cuentas de usuario" description="Crea cuentas institucionales y administra su acceso al sistema." action={<button className="button button-primary" disabled={rolesLoading || Boolean(rolesError)} onClick={() => { setFormError(''); setNewRole('CONSUMER'); setNewInstitutionId(''); setCreating(true) }}>＋ Nueva cuenta</button>} />
      {rolesError && <><Notice message={rolesError} /><button className="button button-secondary" onClick={reloadRoles}>Reintentar roles</button></>}
      {success && <Notice message={success} kind="success" />}
      {formError && <Notice message={formError} />}
      {error && <Notice message={error} />}
      <DataTable loading={loading} error={error} empty="No hay cuentas de usuario registradas." count={items.length} onRetry={reload}>
        <table><thead><tr><th>USUARIO</th><th>INSTITUCIÓN</th><th>ROL</th><th>ÚLTIMO ACCESO</th><th>ESTADO</th><th></th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td><strong>{item.displayName}</strong><small>{item.username} · {item.email}</small></td><td>{item.institutionName ?? 'SNIC'}</td><td><span className="role-pill">{roleNames[item.roleCode]}</span></td><td>{formatDateTime(item.lastLoginAt)}</td><td><StatusPill active={item.isActive}>{item.isActive ? 'Activa' : 'Inactiva'}</StatusPill></td><td><button className="text-button" disabled={item.id === account.id} onClick={() => { setFormError(''); setStatusTarget(item) }}>{item.isActive ? 'Desactivar' : 'Activar'}</button></td></tr>)}</tbody></table>
      </DataTable>
      {creating && <Modal title="Crear cuenta de usuario" onClose={() => setCreating(false)}>
        {formError && <Notice message={formError} />}
        <form className="form-grid" onSubmit={submit}>
          <Field label="Nombre completo" name="displayName" required maxLength={180} />
          <Field label="Nombre de usuario" name="username" required minLength={3} maxLength={100} />
          <Field label="Correo electrónico" name="email" type="email" required maxLength={180} />
          <label className="form-field"><span>Rol *</span><select name="roleCode" value={newRole} required onChange={(event) => { const role = event.target.value; if (!isRoleCode(role)) return; setNewRole(role); if (role === 'ADMINISTRATOR') setNewInstitutionId('') }}>{roles.filter(item => isRoleCode(item.code)).map(item => <option key={item.id} value={item.code}>{item.name}</option>)}</select></label>
          <label className="form-field"><span>Institución</span><select name="institutionId" value={newRole === 'ADMINISTRATOR' ? '' : newInstitutionId} onChange={(event) => setNewInstitutionId(event.target.value)} required={newRole !== 'ADMINISTRATOR'} disabled={newRole === 'ADMINISTRATOR'}><option value="">Sin institución</option>{institutions.filter(item => item.isActive).map((item) => <option key={item.id} value={item.id}>{item.legalName}</option>)}</select><small>{newRole === 'ADMINISTRATOR' ? 'Las cuentas administradoras no pertenecen a una institución.' : 'Obligatoria para cuentas consumidoras y registradoras.'}</small></label>
          <Field label="Contraseña temporal" name="password" type="password" required minLength={12} maxLength={128} help="Debe tener al menos 12 caracteres." />
          <div className="form-actions"><button type="button" className="button button-secondary" onClick={() => setCreating(false)}>Cancelar</button><button className="button button-primary" disabled={saving}>{saving ? 'Creando…' : 'Crear cuenta'}</button></div>
        </form>
      </Modal>}
      {statusTarget && <Modal title={statusTarget.isActive ? 'Desactivar cuenta' : 'Activar cuenta'} onClose={() => { if (!changingStatus) setStatusTarget(null) }}>
        <p className="confirmation-copy">{statusTarget.isActive ? 'Se impedirá el acceso de esta cuenta, incluso si tiene una sesión abierta.' : 'Esta cuenta podrá iniciar sesión nuevamente.'} <strong>{statusTarget.displayName} ({statusTarget.username})</strong></p>
        {formError && <Notice message={formError} />}
        <form className="form-grid" onSubmit={event => { event.preventDefault(); const data = new FormData(event.currentTarget); void toggleStatus(statusTarget, String(data.get('reason') ?? '').trim()) }}>
          <Field label="Motivo" name="reason" required maxLength={300} defaultValue="Cambio de acceso realizado desde SNIC." />
          <div className="form-actions"><button type="button" className="button button-secondary" disabled={changingStatus} onClick={() => setStatusTarget(null)}>Cancelar</button><button className="button button-primary" disabled={changingStatus}>{changingStatus ? 'Guardando…' : 'Confirmar cambio'}</button></div>
        </form>
      </Modal>}
    </div>
  )
}


function optionalValue(value: FormDataEntryValue | null): string | null {
  const result = String(value ?? '').trim()
  return result || null
}

function isRoleCode(value: string): value is Account['roleCode'] {
  return value === 'ADMINISTRATOR' || value === 'CONSUMER' || value === 'DATA_ENTRY'
}

export default App
