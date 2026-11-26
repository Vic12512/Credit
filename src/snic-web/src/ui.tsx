import { useEffect, useId, useRef, type ReactNode } from 'react'

export function PageHeading({ eyebrow, title, description, action }: { eyebrow: string; title: string; description: string; action?: ReactNode }) {
  return <div className="page-heading"><div><span className="eyebrow">{eyebrow}</span><h1>{title}</h1><p>{description}</p></div>{action && <div className="heading-action">{action}</div>}</div>
}

export function StatCard({ label, value, icon, accent = false }: { label: string; value: string; icon: string; accent?: boolean }) {
  return <div className={`stat-card ${accent ? 'stat-accent' : ''}`}><span className="stat-icon">{icon}</span><span className="stat-label">{label}</span><strong>{value}</strong></div>
}

export function StatusPill({ active, children }: { active: boolean; children: ReactNode }) {
  return <span className={`status-pill ${active ? 'status-active' : 'status-inactive'}`}><span />{children}</span>
}

export function Notice({ message, kind = 'error' }: { message: string; kind?: 'error' | 'success' }) {
  return <div className={`notice ${kind === 'success' ? 'notice-success' : ''}`} role={kind === 'error' ? 'alert' : 'status'}><span>{kind === 'error' ? '!' : '✓'}</span><p>{message}</p></div>
}

export function DataTable({ loading, error, empty, count, children, onRetry }: { loading: boolean; error: string; empty: string; count: number; children: ReactNode; onRetry?: () => void }) {
  if (loading) return <div className="table-card table-loading"><span className="spinner" />Cargando información…</div>
  if (error) return <div className="table-card table-empty"><p>No fue posible cargar la información.</p>{onRetry && <button className="button button-secondary" onClick={onRetry}>Reintentar</button>}</div>
  if (!count) return <div className="table-card table-empty"><span className="empty-icon">▤</span>{empty}</div>
  return <div className="table-card">{children}</div>
}

export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  const panel = useRef<HTMLElement>(null)
  const close = useRef(onClose)
  const titleId = useId()
  useEffect(() => { close.current = onClose }, [onClose])
  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null
    const overflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const focusable = () => Array.from(panel.current?.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea, [tabindex="0"]') ?? [])
    ;(focusable()[0] ?? panel.current)?.focus()
    function keydown(event: KeyboardEvent) {
      if (event.key === 'Escape') close.current()
      if (event.key !== 'Tab') return
      const elements = focusable()
      if (!elements.length) { event.preventDefault(); return }
      const first = elements[0], last = elements[elements.length - 1]
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
    }
    document.addEventListener('keydown', keydown)
    return () => { document.removeEventListener('keydown', keydown); document.body.style.overflow = overflow; previous?.focus() }
  }, [])
  return <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}><section ref={panel} className="modal-panel" role="dialog" aria-modal="true" aria-labelledby={titleId} tabIndex={-1}><header className="modal-header"><div><span className="eyebrow">SNIC</span><h2 id={titleId}>{title}</h2></div><button className="modal-close" onClick={onClose} aria-label="Cerrar">×</button></header><div className="modal-content">{children}</div></section></div>
}

export function Field({ label, name, help, ...props }: { label: string; name: string; help?: string; type?: string; defaultValue?: string | number; required?: boolean; minLength?: number; maxLength?: number; min?: string; max?: string; step?: string; pattern?: string }) {
  return <label className="form-field"><span>{label}{props.required && <span className="required-mark" aria-hidden="true"> *</span>}</span><input name={name} {...props} />{help && <small>{help}</small>}</label>
}
