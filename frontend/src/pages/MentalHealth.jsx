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
      <h1>Mental health & stress</h1>
      <p className="page-subtitle">
        Stress is estimated by AI from your assignments and routine — previous week, current week, and predicted for next week (1 = low, 5 = high).
      </p>

      {error && <p className="error-msg">{error}</p>}

      {loading ? (
        <p className="muted">Loading...</p>
      ) : (
        <>
          <section className="card stress-card">
            <div className="stress-card-header">
              <h2>Your stress levels</h2>
              <button type="button" className="btn btn-small btn-secondary" onClick={handleRefresh} disabled={refreshing}>
                {refreshing ? 'Refreshing...' : 'Refresh'}
              </button>
            </div>
            <div className="stress-meters-row">
              <StressMeter
                value={stressSummary?.previous_week ?? null}
                label="Previous week"
                size="small"
                showValue={true}
              />
              <StressMeter
                value={stressSummary?.current_week ?? null}
                label="Current week"
                size="medium"
                showValue={true}
              />
              <StressMeter
                value={stressSummary?.future_predicted ?? null}
                label="Future (predicted)"
                size="small"
                showValue={true}
              />
            </div>
            {stressSummary?.message && <p className="stress-message">{stressSummary.message}</p>}
          </section>
        </>
      )}
    </div>
  );
}
