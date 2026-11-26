import { useEffect, useState, type FormEvent } from 'react'
import { apiRequest, ApiError, formatDateTime, type Account, type AuditEntry, type Institution, type PagedResponse } from './api'
import { DataTable, Modal, Notice, PageHeading } from './ui'

type Filters = { from: string; to: string; accountId: string; institutionId: string; personId: string; operation: string; result: string }
const emptyFilters: Filters = { from: '', to: '', accountId: '', institutionId: '', personId: '', operation: '', result: '' }
const resultNames: Record<string, string> = { SUCCESS: 'Correcta', DENIED: 'Acceso rechazado', FAILURE: 'Fallida' }
const operationNames: Record<string, string> = {
  PERSON_UPDATED: 'Corrección de persona', CREDIT_PRODUCT_UPDATED: 'Corrección de producto',
  PERSONS_CREATE: 'Registro de persona', CREDITPRODUCTS_CREATE: 'Registro de producto',
  CREDITREPORTS_GETBYNATIONALID: 'Consulta crediticia', AUTH_LOGIN: 'Inicio de sesión', AUTH_ME: 'Consulta de sesión',
  ACCOUNTS_CREATE: 'Creación de cuenta', ACCOUNTS_CHANGESTATUS: 'Cambio de acceso',
  INSTITUTIONS_CREATE: 'Registro de institución', AUDITLOGS_LIST: 'Consulta de auditoría', SETUP_ADMIN_CREATED: 'Administrador inicial',
  PERSONS_LIST: 'Consulta de personas', PERSONS_FIND: 'Consulta de persona',
  CREDITPRODUCTS_LIST: 'Consulta de productos', CREDITPRODUCTS_FIND: 'Consulta de producto',
  INSTITUTIONS_LIST: 'Consulta de instituciones', INSTITUTIONS_FIND: 'Consulta de institución',
  ACCOUNTS_LIST: 'Consulta de cuentas', ACCOUNTS_FIND: 'Consulta de cuenta',
  CATALOGS_ROLES: 'Consulta de roles', CATALOGS_INSTITUTIONTYPES: 'Consulta de tipos de institución',
  CATALOGS_PRODUCTTYPES: 'Consulta de tipos de producto', CATALOGS_PRODUCTSTATUSES: 'Consulta de estados', CATALOGS_SCORERULES: 'Consulta de reglas de simulación',
}

function toQuery(filters: Filters, page: number, pageSize: number) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  for (const [key, value] of Object.entries(filters)) {
    if (!value.trim()) continue
    if (key === 'from' || key === 'to') {
      const date = new Date(`${value}T00:00:00`)
      if (key === 'to') date.setHours(23, 59, 59, 999)
      params.set(key, date.toISOString())
    } else params.set(key, value.trim())
  }
  return `/audit-logs?${params.toString()}`
}

export function AuditPage({ token, onUnauthorized }: { token: string; onUnauthorized: (error?: ApiError) => void }) {
  const [draft, setDraft] = useState<Filters>(emptyFilters)
  const [applied, setApplied] = useState<Filters>(emptyFilters)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [revision, setRevision] = useState(0)
  const [data, setData] = useState<PagedResponse<AuditEntry>>({ items: [], totalCount: 0, page: 1, pageSize: 20 })
  const [accounts, setAccounts] = useState<Account[]>([])
  const [institutions, setInstitutions] = useState<Institution[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [filterError, setFilterError] = useState('')
  const [catalogError, setCatalogError] = useState('')
  const [selected, setSelected] = useState<AuditEntry | null>(null)
  const path = toQuery(applied, page, pageSize)

  useEffect(() => {
    const controller = new AbortController()
    Promise.all([apiRequest<Account[]>('/accounts', token, { signal: controller.signal }), apiRequest<Institution[]>('/institutions', token, { signal: controller.signal })])
      .then(([users, entities]) => { setAccounts(users); setInstitutions(entities); setCatalogError('') })
      .catch((reason: unknown) => {
        if (controller.signal.aborted) return
        setCatalogError('No se pudieron cargar las opciones de cuenta e institución. Puedes volver a actualizar.')
        if (reason instanceof ApiError && reason.status === 401) onUnauthorized(reason)
      })
    return () => controller.abort()
  }, [token, onUnauthorized, revision])

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true); setError('')
    apiRequest<PagedResponse<AuditEntry>>(path, token, { signal: controller.signal })
      .then(result => { if (!controller.signal.aborted) setData(result) })
      .catch((reason: unknown) => {
        if (controller.signal.aborted) return
        setError(reason instanceof Error ? reason.message : 'No fue posible consultar la auditoría.')
        if (reason instanceof ApiError && reason.status === 401) onUnauthorized(reason)
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [path, token, onUnauthorized, revision])

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (draft.from && draft.to && draft.from > draft.to) { setFilterError('La fecha inicial no puede ser posterior a la fecha final.'); return }
    setFilterError(''); setPage(1); setApplied({ ...draft }); setRevision(value => value + 1)
  }
  function reset() { setDraft(emptyFilters); setApplied(emptyFilters); setPage(1); setFilterError('') }
  function change(key: keyof Filters, value: string) { setDraft(current => ({ ...current, [key]: value })) }
  const pages = Math.max(1, Math.ceil(data.totalCount / pageSize))

  return <div className="page-wrap">
    <PageHeading eyebrow="CONTROL Y TRAZABILIDAD" title="Auditoría" description="Consulta quién accedió a la información y qué operaciones se realizaron." action={<button className="button button-secondary" disabled={loading} onClick={() => setRevision(value => value + 1)}>Actualizar</button>} />
    <form className="audit-filters" onSubmit={submit}>
      <div className="filter-intro"><h2>Filtrar operaciones</h2><p>Las fechas se interpretan en tu hora local. Todos los filtros son opcionales.</p></div>
      <label className="form-field"><span>Desde</span><input type="date" value={draft.from} onChange={event => change('from', event.target.value)} /></label>
      <label className="form-field"><span>Hasta</span><input type="date" value={draft.to} onChange={event => change('to', event.target.value)} /></label>
      <label className="form-field"><span>Cuenta responsable</span><select aria-label="Cuenta responsable" value={draft.accountId} onChange={event => change('accountId', event.target.value)}><option value="">Todas las cuentas</option>{accounts.map(item => <option key={item.id} value={item.id}>{item.displayName} · {item.username}</option>)}</select></label>
      <label className="form-field"><span>Institución</span><select aria-label="Institución" value={draft.institutionId} onChange={event => change('institutionId', event.target.value)}><option value="">Todas las instituciones</option>{institutions.map(item => <option key={item.id} value={item.id}>{item.legalName}</option>)}</select></label>
      <label className="form-field"><span>Resultado</span><select aria-label="Resultado" value={draft.result} onChange={event => change('result', event.target.value)}><option value="">Todos los resultados</option>{Object.entries(resultNames).map(([code, name]) => <option key={code} value={code}>{name}</option>)}</select></label>
      <label className="form-field" htmlFor="audit-operation"><span>Operación</span><input id="audit-operation" value={draft.operation} maxLength={60} placeholder="Ej. PERSON_UPDATED" onChange={event => change('operation', event.target.value.toUpperCase())} list="audit-operations" /></label><datalist id="audit-operations">{Object.entries(operationNames).map(([code, name]) => <option key={code} value={code}>{name}</option>)}</datalist>
      <label className="form-field"><span>ID de persona</span><input type="number" min="1" step="1" value={draft.personId} onChange={event => change('personId', event.target.value)} placeholder="Cualquier persona" /></label>
      <div className="audit-filter-actions"><button type="button" className="button button-secondary" onClick={reset}>Limpiar filtros</button><button className="button button-primary" disabled={loading}>Aplicar filtros</button></div>
    </form>
    {filterError && <Notice message={filterError} />}{catalogError && <Notice message={catalogError} />}
    {error && <Notice message={error} />}
    <div className="section-title-row"><div><h2>Registro de operaciones</h2><p aria-live="polite">{loading ? 'Consultando operaciones…' : `${data.totalCount.toLocaleString('es-DO')} operaciones encontradas`}</p></div><label className="page-size-label">Por página<select aria-label="Operaciones por página" value={pageSize} onChange={event => { setPageSize(Number(event.target.value)); setPage(1) }}>{[10,20,50,100].map(size => <option key={size}>{size}</option>)}</select></label></div>
    <DataTable loading={loading} error={error} count={data.items.length} empty="No hay operaciones que coincidan con los filtros seleccionados." onRetry={() => setRevision(value => value + 1)}>
      <table><thead><tr><th>Fecha y hora</th><th>Responsable</th><th>Operación</th><th>Institución</th><th>Resultado</th><th>Detalle</th></tr></thead><tbody>{data.items.map(item => <tr key={item.id}><td>{formatDateTime(item.occurredAt)}</td><td><strong>{item.username ?? item.attemptedUsername ?? 'Sin autenticar'}</strong><small>{item.accountId ? `Cuenta #${item.accountId}` : 'Sin cuenta autenticada'}</small></td><td><strong>{operationNames[item.operation] ?? 'Operación del sistema'}</strong><small>{item.operation}</small></td><td>{institutions.find(entity => entity.id === item.institutionId)?.commercialName ?? institutions.find(entity => entity.id === item.institutionId)?.legalName ?? (item.institutionId ? `Institución #${item.institutionId}` : '—')}</td><td><span className={`audit-result result-${item.result.toLowerCase()}`}>{resultNames[item.result] ?? item.result}</span></td><td><button className="text-button" onClick={() => setSelected(item)} aria-label={`Ver detalle de operación ${item.id}`}>Ver detalle</button></td></tr>)}</tbody></table>
    </DataTable>
    <div className="pagination"><span>Página {page} de {pages}</span><div><button className="button button-secondary" disabled={loading || page <= 1} onClick={() => setPage(value => value - 1)}>Anterior</button><button className="button button-secondary" disabled={loading || page >= pages || Boolean(error)} onClick={() => setPage(value => value + 1)}>Siguiente</button></div></div>
    <p className="page-footnote">La consulta de auditoría también se registra y aparecerá en una actualización posterior.</p>
    {selected && <Modal title={`Detalle de operación #${selected.id}`} onClose={() => setSelected(null)}>
      <dl className="audit-details"><div><dt>Fecha y hora</dt><dd>{formatDateTime(selected.occurredAt)}</dd></div><div><dt>Responsable</dt><dd>{selected.username ?? selected.attemptedUsername ?? 'Sin autenticar'}</dd></div><div><dt>Operación</dt><dd>{selected.operation}</dd></div><div><dt>Resultado</dt><dd>{resultNames[selected.result] ?? selected.result}</dd></div><div><dt>Persona</dt><dd>{selected.personId ?? '—'}</dd></div><div><dt>Entidad / registro</dt><dd>{selected.entityType ?? '—'} / {selected.entityId ?? '—'}</dd></div><div><dt>Institución</dt><dd>{selected.institutionId ?? '—'}</dd></div><div><dt>Dirección IP</dt><dd>{selected.ipAddress ?? '—'}</dd></div></dl>
      {selected.details && <p className="audit-technical-detail">{selected.details}</p>}
      <div className="audit-snapshots"><Snapshot title="Valores anteriores" value={selected.oldValues} /><Snapshot title="Valores nuevos" value={selected.newValues} /></div>
    </Modal>}
  </div>
}

function Snapshot({ title, value }: { title: string; value: string | null }) {
  let formatted = value
  if (value) { try { formatted = JSON.stringify(JSON.parse(value), null, 2) } catch { /* Mostrar como texto seguro. */ } }
  return <section><h3>{title}</h3>{formatted ? <pre>{formatted}</pre> : <p className="muted">Esta operación no contiene una comparación de valores.</p>}</section>
}
