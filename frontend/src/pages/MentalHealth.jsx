import { useState, useEffect } from 'react';
import './MentalHealth.css';
import { dashboardApi } from '../api/client';
import StressMeter from '../components/StressMeter';

export default function MentalHealth() {
  const [stressSummary, setStressSummary] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const loadStressSummary = () => {
    setError('');
    return dashboardApi.stressSummary()
      .then(r => setStressSummary({
        previous_week: r?.previous_week ?? null,
        current_week: r?.current_week ?? null,
        future_predicted: r?.future_predicted ?? null,
        message: r?.message ?? null,
      }))
      .catch(() => setStressSummary(null));
  };

  useEffect(() => {
    loadStressSummary().finally(() => setLoading(false));
  }, []);

  const handleRefresh = () => {
    setRefreshing(true);
    loadStressSummary().finally(() => setRefreshing(false));
  };

  return (
    <div className="mental-health-page">
      <header className="mh-hero">
        <div className="mh-hero-badge">Wellness</div>
        <h1 className="mh-title">
          <span className="mh-title-icon" aria-hidden>🧠</span>
          Mental health & stress
        </h1>
        <p className="mh-lead">
          Stress is calculated from your <strong>routine</strong> (busy vs free hours per day) and how many <strong>pending assignments</strong> are due in each week. Each assignment is assumed to need <strong>2 hours per day</strong> from your free time.
        </p>
        <p className="mh-sub">
          Bands: 1 assignment → Low · 2 → Mild · 3 → High · 4+ → Extreme. If daily need (2h × assignments) exceeds your free time from routine, the level can increase. Scale: <span className="mh-scale-pill">1</span> low → <span className="mh-scale-pill mh-scale-high">5</span> high.
        </p>
      </header>

      {error && <div className="mh-alert mh-alert-error" role="alert">{error}</div>}

      {loading ? (
        <div className="mh-loading" aria-busy="true">
          <div className="mh-spinner" />
          <p>Analyzing your workload…</p>
        </div>
      ) : (
        <>
          <div className="mh-toolbar">
            <h2 className="mh-section-title">Stress overview</h2>
            <button
              type="button"
              className="mh-btn-refresh"
              onClick={handleRefresh}
              disabled={refreshing}
              title="Get latest estimate"
            >
              <span className="mh-btn-refresh-icon" aria-hidden>{refreshing ? '⏳' : '🔄'}</span>
              {refreshing ? 'Updating…' : 'Refresh'}
            </button>
          </div>

          <div className="mh-legend">
            <span className="mh-legend-label">Scale</span>
            <div className="mh-legend-bar" aria-hidden>
              <span className="mh-legend-seg mh-l1">1</span>
              <span className="mh-legend-seg mh-l2">2</span>
              <span className="mh-legend-seg mh-l3">3</span>
              <span className="mh-legend-seg mh-l4">4</span>
              <span className="mh-legend-seg mh-l5">5</span>
            </div>
            <span className="mh-legend-hint">Low to extreme</span>
          </div>

          <div className="mh-meters-grid">
            <article className="mh-meter-card">
              <div className="mh-meter-card-head">
                <span className="mh-meter-icon" aria-hidden>📅</span>
                <div>
                  <h3 className="mh-meter-card-title">Previous week</h3>
                  <p className="mh-meter-card-desc">Estimated stress from your recent load</p>
                </div>
              </div>
              <div className="mh-meter-card-body">
                <StressMeter
                  value={stressSummary?.previous_week ?? null}
                  label=""
                  size="medium"
                  showValue={true}
                />
              </div>
            </article>

            <article className="mh-meter-card mh-meter-card--featured">
              <div className="mh-meter-card-ribbon">Now</div>
              <div className="mh-meter-card-head">
                <span className="mh-meter-icon" aria-hidden>✨</span>
                <div>
                  <h3 className="mh-meter-card-title">Current week</h3>
                  <p className="mh-meter-card-desc">Where you stand this week</p>
                </div>
              </div>
              <div className="mh-meter-card-body">
                <StressMeter
                  value={stressSummary?.current_week ?? null}
                  label=""
                  size="medium"
                  showValue={true}
                />
              </div>
            </article>

            <article className="mh-meter-card">
              <div className="mh-meter-card-head">
                <span className="mh-meter-icon" aria-hidden>🔮</span>
                <div>
                  <h3 className="mh-meter-card-title">Future (predicted)</h3>
                  <p className="mh-meter-card-desc">Outlook from upcoming deadlines</p>
                </div>
              </div>
              <div className="mh-meter-card-body">
                <StressMeter
                  value={stressSummary?.future_predicted ?? null}
                  label=""
                  size="medium"
                  showValue={true}
                />
              </div>
            </article>
          </div>

          {stressSummary?.message && (
            <aside className="mh-tip">
              <span className="mh-tip-icon" aria-hidden>💡</span>
              <div>
                <strong className="mh-tip-title">Tip</strong>
                <p className="mh-tip-text">{stressSummary.message}</p>
              </div>
            </aside>
          )}

          <footer className="mh-footnote">
            <p>
              Estimates are for awareness only. If you feel overwhelmed, reach out to someone you trust or a professional.
            </p>
          </footer>
        </>
      )}
    </div>
  );
}
