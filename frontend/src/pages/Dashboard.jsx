import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import './Dashboard.css';
import { assignmentsApi, aiApi, dashboardApi } from '../api/client';

function formatDate(d) {
  if (!d) return '';
  const s = typeof d === 'string' ? d.slice(0, 10) : d;
  return s;
}

function formatTimeAmPm(timeStr) {
  if (!timeStr || typeof timeStr !== 'string') return '';
  const [h, m] = timeStr.trim().split(':').map(Number);
  if (Number.isNaN(h)) return timeStr;
  const hour = h % 12 || 12;
  const min = Number.isNaN(m) ? 0 : m;
  const ampm = h < 12 ? 'AM' : 'PM';
  return `${hour}:${min.toString().padStart(2, '0')} ${ampm}`;
}

export default function Dashboard() {
  const [assignments, setAssignments] = useState([]);
  const [schedule, setSchedule] = useState([]);
  const [loading, setLoading] = useState(true);
  const [scheduleLoading, setScheduleLoading] = useState(false);
  const [error, setError] = useState('');
  const [focusTip, setFocusTip] = useState(null);
  const [weeklyPlan, setWeeklyPlan] = useState(null);
  const [overloadWarning, setOverloadWarning] = useState(null);
  const navigate = useNavigate();

  const from = new Date().toISOString().slice(0, 10);
  const to = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setError('');
      try {
        const list = await assignmentsApi.getAll();
        if (!cancelled) setAssignments(Array.isArray(list) ? list : []);
      } catch (err) {
        if (!cancelled) setError(err.message || 'Failed to load.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, []);

  const [overloadSuggestion, setOverloadSuggestion] = useState(null);

  useEffect(() => {
    if (loading || assignments.length === 0) return;
    dashboardApi.focusTip().then(r => setFocusTip(r?.tip ?? null)).catch(() => setFocusTip(null));
    dashboardApi.overloadWarning().then(r => {
      setOverloadWarning(r?.warning ?? null);
      setOverloadSuggestion(r?.suggestion ?? null);
    }).catch(() => { setOverloadWarning(null); setOverloadSuggestion(null); });
  }, [loading, assignments.length]);

  const loadWeeklyPlan = () => {
    dashboardApi.weeklyPlan().then(r => setWeeklyPlan(r?.plan ?? null)).catch(() => setWeeklyPlan(null));
  };

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const list = await aiApi.getSchedule(from, to);
        if (!cancelled) setSchedule(Array.isArray(list) ? list : []);
      } catch {
        if (!cancelled) setSchedule([]);
      }
    })();
    return () => { cancelled = true; };
  }, [from, to]);

  const handleGenerateSchedule = async () => {
    setScheduleLoading(true);
    setError('');
    try {
      const list = await aiApi.generateSchedule(from, to);
      setSchedule(Array.isArray(list) ? list : []);
    } catch (err) {
      setError(err.message || 'Failed to generate schedule.');
    } finally {
      setScheduleLoading(false);
    }
  };

  const upcoming = assignments
    .filter(a => a.status !== 'completed' && a.due_date)
    .sort((a, b) => (a.due_date || '').localeCompare(b.due_date || ''))
    .slice(0, 5);

  const pendingCount = assignments.filter(a => a.status === 'pending').length;
  const completedCount = assignments.filter(a => a.status === 'completed').length;

  const todayStr = new Date().toISOString().slice(0, 10);
  const stepsToday = assignments.reduce((acc, a) => {
    try {
      const json = a.roadmap_json;
      if (!json || typeof json !== 'string') return acc;
      const arr = JSON.parse(json);
      if (!Array.isArray(arr)) return acc;
      const count = arr.filter(s => (s.startDate || s.start_date || '').toString().slice(0, 10) === todayStr).length;
      return acc + count;
    } catch { return acc; }
  }, 0);

  const inSevenDays = assignments.filter(a => {
    if (a.status === 'completed' || !a.due_date) return false;
    const d = (a.due_date || '').slice(0, 10);
    if (!d) return false;
    const diff = (new Date(d) - new Date(todayStr)) / (1000 * 60 * 60 * 24);
    return diff >= 0 && diff <= 7;
  }).length;

  const suggestedOrder = assignments
    .filter(a => a.status !== 'completed' && a.due_date)
    .sort((a, b) => {
      const d = (a.due_date || '').localeCompare(b.due_date || '');
      if (d !== 0) return d;
      const p = { high: 0, medium: 1, low: 2 };
      return (p[a.priority] ?? 1) - (p[b.priority] ?? 1);
    })
    .slice(0, 5);

  const riskAlert = assignments
    .filter(a => a.status !== 'completed' && a.due_date && a.roadmap_json)
    .map(a => {
      try {
        const arr = JSON.parse(a.roadmap_json || '[]');
        const steps = Array.isArray(arr) ? arr.length : 0;
        const due = (a.due_date || '').slice(0, 10);
        const daysLeft = Math.ceil((new Date(due) - new Date(todayStr)) / (1000 * 60 * 60 * 24));
        return { ...a, steps, daysLeft };
      } catch { return { ...a, steps: 0, daysLeft: 999 }; }
    })
    .filter(a => a.steps >= 5 && a.daysLeft >= 0 && a.daysLeft <= 3)
    .sort((a, b) => a.daysLeft - b.daysLeft)[0];

  return (
    <div className="dashboard">
      <h1>Dashboard</h1>
      <p className="dashboard-subtitle">Overview of your assignments and AI schedule</p>
      {error && <p className="dashboard-error">{error}</p>}

      {!loading && assignments.length > 0 && (
        <section className="card digest-card">
          <h2>Daily digest</h2>
          <p className="digest-date">{new Date().toLocaleDateString('en', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}</p>
          <ul className="digest-list">
            <li>{stepsToday} roadmap step(s) scheduled for today</li>
            <li>{inSevenDays} assignment(s) due in the next 7 days</li>
          </ul>
          {suggestedOrder.length > 0 && (
            <div className="digest-order">
              <h4>Suggested order to work on</h4>
              <ol>
                {suggestedOrder.map((a, i) => (
                  <li key={a.id}><strong>{a.title}</strong> — due {formatDate(a.due_date)} ({a.priority})</li>
                ))}
              </ol>
            </div>
          )}
          {riskAlert && (
            <p className="digest-risk">Tip: <strong>{riskAlert.title}</strong> has {riskAlert.steps} steps and is due in {riskAlert.daysLeft} day(s). Consider breaking it down or asking the AI assistant for help.</p>
          )}
          {focusTip && <p className="digest-focus-tip"><strong>AI focus:</strong> {focusTip}</p>}
          {overloadWarning && (
            <p className="digest-risk">
              {overloadWarning}
              {overloadSuggestion && <><br /><strong>Suggestion:</strong> {overloadSuggestion}</>}
            </p>
          )}
          <div className="digest-weekly">
            <button type="button" className="btn btn-small btn-secondary" onClick={loadWeeklyPlan}>Summarise my week</button>
            {weeklyPlan && <p className="weekly-plan-text">{weeklyPlan}</p>}
            {suggestedOrder.length > 0 && (
              <button type="button" className="btn btn-small btn-primary" style={{ marginTop: '0.5rem' }} onClick={() => navigate(`/assignments/${suggestedOrder[0].id}/assistant?focusNow=1`)}>
                Focus now – open AI assistant
              </button>
            )}
          </div>
        </section>
      )}

      <div className="dashboard-grid">
        <section className="card">
          <h2>Upcoming Deadlines</h2>
          {loading ? (
            <p className="muted">Loading...</p>
          ) : upcoming.length === 0 ? (
            <p className="muted">No upcoming assignments.</p>
          ) : (
            <ul className="deadline-list">
              {upcoming.map(a => (
                <li key={a.id}>
                  <strong>{a.title}</strong> — {formatDate(a.due_date)} ({a.priority})
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="card">
          <h2>AI Schedule</h2>
          <p className="muted small">Earliest-Deadline-First with your routine.</p>
          <button type="button" className="btn btn-primary btn-small" onClick={handleGenerateSchedule} disabled={scheduleLoading}>
            {scheduleLoading ? 'Generating...' : 'Generate AI Schedule'}
          </button>
          {schedule.length === 0 ? (
            <p className="muted">No schedule yet. Click to generate.</p>
          ) : (
            <ul className="schedule-list">
              {schedule.slice(0, 7).map((s, i) => (
                <li key={s.id || i}>
                  {s.slot_date} {formatTimeAmPm(s.start_time)} – {formatTimeAmPm(s.end_time)}: {s.assignmentTitle || s.notes || 'Study'}
                </li>
              ))}
              {schedule.length > 7 && <li className="muted">+{schedule.length - 7} more</li>}
            </ul>
          )}
        </section>

        <section className="card">
          <h2>Productivity</h2>
          {loading ? (
            <p className="muted">Loading...</p>
          ) : (
            <>
              <p><strong>Pending:</strong> {pendingCount}</p>
              <p><strong>Completed:</strong> {completedCount}</p>
              <p className="muted small">Total: {assignments.length} assignments</p>
            </>
          )}
        </section>
      </div>
    </div>
  );
}
