import { riskNames, type CreditReport, type ScoreRule } from './api'

export function Indicators({ report, rules = [] }: { report: CreditReport; rules?: ScoreRule[] }) {
  const indicators = report.indicators
  const available = indicators?.score != null && report.hasCreditHistory
  const risk = indicators?.riskLevel
  return <section className="indicators-section" aria-labelledby="indicators-title">
    <div className="section-title-row"><div><h2 id="indicators-title">Indicadores académicos</h2><p>Una lectura simulada del atraso registrado.</p></div><span className="simulation-pill">SIMULACIÓN</span></div>
    {!available ? <div className="info-panel"><strong>Sin historial para calcular indicadores</strong><p>{report.message || 'Esta persona no tiene productos reportados. El score y el porcentaje de incumplimiento no están disponibles.'}</p></div> : <div className="indicator-grid">
      <article className="indicator-card score-card"><span className="eyebrow">SCORE SIMULADO</span><div className="score-number">{indicators.score}<span>/ 100</span></div><meter min={0} max={100} value={indicators.score ?? 0} aria-label="Score académico simulado" /><p>Basado en el mayor atraso registrado: <strong>{indicators.maximumDaysPastDue} días</strong>.</p></article>
      <article className="indicator-card"><span className="eyebrow">NIVEL DE RIESGO SIMULADO</span><div className={`risk-label risk-${risk?.toLowerCase() ?? 'none'}`}>{risk ? riskNames[risk] : 'No disponible'}</div><p>Clasificación académica según las reglas de simulación.</p></article>
      <article className="indicator-card"><span className="eyebrow">INCUMPLIMIENTO SIMULADO</span><div className="score-number">{indicators.estimatedDefaultPercentage}<span>%</span></div><p>Porcentaje simulado, calculado como 100 menos el score.</p></article>
    </div>}
    <p className="simulation-disclaimer">Estos valores son simulados con fines académicos. No constituyen una evaluación financiera real.</p>
    {rules.length > 0 && <details className="rules-panel"><summary>Consultar las reglas de simulación</summary><div className="table-card"><table><thead><tr><th>Atraso registrado</th><th>Score</th><th>Riesgo simulado</th><th>Incumplimiento simulado</th></tr></thead><tbody>{rules.map(rule => <tr key={rule.id}><td>{rule.maxDaysPastDue == null ? `Desde ${rule.minDaysPastDue} días` : rule.minDaysPastDue === rule.maxDaysPastDue ? `${rule.minDaysPastDue} días` : `${rule.minDaysPastDue}–${rule.maxDaysPastDue} días`}</td><td>{rule.score} / 100</td><td>{riskNames[rule.riskLevel as keyof typeof riskNames] ?? rule.riskLevel}</td><td>{rule.estimatedDefaultPercentage}%</td></tr>)}</tbody></table></div></details>}
  </section>
}
