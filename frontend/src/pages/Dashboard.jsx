import { useState, useEffect } from 'react';
import './Dashboard.css';
import { assignmentsApi, aiApi } from '../api/client';

function formatDate(d) {
  if (!d) return '';
  const s = typeof d === 'string' ? d.slice(0, 10) : d;
  return s;
}

// Parse "09:00" or "14:00" (24h) to "9:00 AM" or "2:00 PM"
function formatTimeAmPm(timeStr) {
  if (!timeStr || typeof timeStr !== 'string') return '';
  const [h, m] = timeStr.trim().split(':').map(Number);
  if (Number.isNaN(h)) return timeStr;
  const hour = h % 12 || 12;
  const min = Number.isNaN(m) ? 0 : m;
  const ampm = h < 12 ? 'AM' : 'PM';
  return `${hour}:${min.toString().padStart(2, '0')} ${ampm}`;
}

function formatTimeRange(start, end) {
  if (!start && !end) return '';
  const s = formatTimeAmPm(start || '');
  const e = formatTimeAmPm(end || '');
  if (s && e) return `${s} to ${e}`;
  if (s) return s;
  return e;
}

// Build day labels from roadmap steps (sorted by date then time): Day 1, Day 2, ...
function getDayLabels(roadmapList) {
  if (!Array.isArray(roadmapList) || roadmapList.length === 0) return {};
  const sorted = [...roadmapList].sort((a, b) => {
    const dA = a.suggested_date || '';
    const dB = b.suggested_date || '';
    if (dA !== dB) return dA.localeCompare(dB);
    return (a.suggested_time || '').localeCompare(b.suggested_time || '');
  });
  const dateToDay = {};
  let day = 0;
  sorted.forEach(s => {
    const d = (s.suggested_date || '').slice(0, 10);
    if (d && !dateToDay[d]) { day += 1; dateToDay[d] = day; }
  });
  const byId = {};
  sorted.forEach(s => { byId[s.id] = dateToDay[(s.suggested_date || '').slice(0, 10)] || 1; });
  return byId;
}

export default function Dashboard() {
  const [assignments, setAssignments] = useState([]);
  const [schedule, setSchedule] = useState([]);
  const [roadmap, setRoadmap] = useState([]);
  const [selectedAssignmentId, setSelectedAssignmentId] = useState(null);
  const [loading, setLoading] = useState(true);
  const [scheduleLoading, setScheduleLoading] = useState(false);
  const [roadmapLoading, setRoadmapLoading] = useState(false);
  const [error, setError] = useState('');
  const [stepDetail, setStepDetail] = useState(null);
  const [roadmapContext, setRoadmapContext] = useState(null);
  const [liveAiResponse, setLiveAiResponse] = useState(''); // live streaming text from AI

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

  const handleLoadRoadmap = async (assignmentId) => {
    setSelectedAssignmentId(assignmentId);
    setRoadmapLoading(true);
    setRoadmap([]);
    setRoadmapContext(null);
    try {
      const [list, ctx] = await Promise.all([
        aiApi.getRoadmap(assignmentId),
        aiApi.getRoadmapContext(assignmentId).catch(() => null)
      ]);
      setRoadmap(Array.isArray(list) ? list : []);
      setRoadmapContext(ctx || null);
    } catch {
      setRoadmap([]);
    } finally {
      setRoadmapLoading(false);
    }
  };

  const handleGenerateRoadmap = async () => {
    if (!selectedAssignmentId) return;
    setRoadmapLoading(true);
    setError('');
    setLiveAiResponse('');
    try {
      await aiApi.generateRoadmapStream(selectedAssignmentId, {
        onChunk: (chunk) => setLiveAiResponse((prev) => prev + chunk),
        onDone: async (steps, streamError) => {
          setRoadmap(steps || []);
          setLiveAiResponse('');
          if (streamError) setError(streamError);
          const ctx = await aiApi.getRoadmapContext(selectedAssignmentId).catch(() => null);
          setRoadmapContext(ctx || null);
          if ((!steps || steps.length === 0) && streamError) {
            try {
              const list = await aiApi.generateRoadmap(selectedAssignmentId);
              setRoadmap(Array.isArray(list) ? list : []);
              const ctx2 = await aiApi.getRoadmapContext(selectedAssignmentId).catch(() => null);
              setRoadmapContext(ctx2 || null);
            } catch (_) {}
          }
        },
        onError: async (err) => {
          setLiveAiResponse('');
          try {
            const list = await aiApi.generateRoadmap(selectedAssignmentId);
            setRoadmap(Array.isArray(list) ? list : []);
            const ctx = await aiApi.getRoadmapContext(selectedAssignmentId).catch(() => null);
            setRoadmapContext(ctx || null);
          } catch (e) {
            setError(err?.message || e?.message || 'Failed to generate roadmap.');
          }
        },
      });
    } catch (e) {
      setError(e?.message || 'Failed to generate roadmap.');
    } finally {
      setRoadmapLoading(false);
    }
  };

  const upcoming = assignments
    .filter(a => a.status !== 'completed' && a.due_date)
    .sort((a, b) => (a.due_date || '').localeCompare(b.due_date || ''))
    .slice(0, 5);

  const pendingCount = assignments.filter(a => a.status === 'pending').length;
  const completedCount = assignments.filter(a => a.status === 'completed').length;

  return (
    <div className="dashboard">
      <h1>Dashboard</h1>
      <p className="dashboard-subtitle">Overview of your assignments and AI schedule</p>
      {error && <p className="dashboard-error">{error}</p>}

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
          <h2>Assignment Roadmap</h2>
          <p className="muted small">Steps to complete the assignment by the deadline, with date and time (AM/PM) for each. Upload a document for better steps.</p>
          <select
            value={selectedAssignmentId ?? ''}
            onChange={e => { const id = e.target.value ? parseInt(e.target.value, 10) : null; setSelectedAssignmentId(id); if (id) handleLoadRoadmap(id); }}
          >
            <option value="">Select assignment</option>
            {assignments.filter(a => a.status !== 'completed').map(a => (
              <option key={a.id} value={a.id}>{a.title}</option>
            ))}
          </select>
          {selectedAssignmentId && (
            <button type="button" className="btn btn-primary btn-small" onClick={handleGenerateRoadmap} disabled={roadmapLoading} style={{ marginTop: '0.5rem' }}>
              {roadmapLoading ? 'AI is generating...' : 'Generate / Refresh Roadmap (Live AI)'}
            </button>
          )}
          {liveAiResponse && (
            <div className="roadmap-live-response">
              <h4 className="roadmap-context-title">Live AI response</h4>
              <pre className="roadmap-live-text">{liveAiResponse}</pre>
            </div>
          )}
          {roadmapContext && selectedAssignmentId && (
            <div className="roadmap-context">
              <h4 className="roadmap-context-title">Used for this roadmap</h4>
              <div className="roadmap-context-grid">
                {roadmapContext.assignment_title && (
                  <div className="roadmap-context-item">
                    <span className="roadmap-context-label">Assignment</span>
                    <span className="roadmap-context-value">{roadmapContext.assignment_title}</span>
                  </div>
                )}
                <div className="roadmap-context-item">
                  <span className="roadmap-context-label">Deadline</span>
                  <span className="roadmap-context-value">{roadmapContext.due_date ? new Date(roadmapContext.due_date).toLocaleDateString('en-IN', { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' }) : '—'}</span>
                </div>
                <div className="roadmap-context-item">
                  <span className="roadmap-context-label">Routine</span>
                  <span className="roadmap-context-value">{roadmapContext.routine_summary || 'No routine'}</span>
                </div>
                {(roadmapContext.assignment_description || roadmapContext.document_text) && (
                  <div className="roadmap-context-item roadmap-context-full">
                    <span className="roadmap-context-label">Assignment & document</span>
                    <div className="roadmap-context-value roadmap-context-text">
                      {roadmapContext.assignment_description && <p><strong>Description:</strong> {roadmapContext.assignment_description}</p>}
                      {roadmapContext.document_text && <p><strong>Document used:</strong><br />{roadmapContext.document_text}</p>}
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}
          {roadmap.length === 0 && selectedAssignmentId && !roadmapLoading && <p className="muted">Generate roadmap to see steps.</p>}
          {roadmap.length > 0 && (() => {
            const dayLabels = getDayLabels(roadmap);
            return (
              <ul className="roadmap-list">
                {roadmap.map(s => {
                  const title = s.step_title || s.stepTitle || `Step ${s.step_order || s.stepOrder || 0}`;
                  const order = s.step_order ?? s.stepOrder ?? 0;
                  const dayNum = dayLabels[s.id];
                  const date = s.suggested_date ? new Date(s.suggested_date).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' }) : '';
                  const timeRange = formatTimeRange(s.suggested_time, s.suggested_end_time);
                  return (
                    <li key={s.id} className="roadmap-step-item" onClick={() => setStepDetail(s)}>
                      {dayNum != null && <span className="roadmap-day-badge">Day {dayNum}</span>}
                      <strong>Step {order}:</strong> {title}
                      {(date || timeRange) && <span className="roadmap-when"> — {date}{timeRange ? ` • ${timeRange}` : ''}</span>}
                      {s.completed ? ' ✓' : ''}
                    </li>
                  );
                })}
              </ul>
            );
          })()}
          {stepDetail && (() => {
            const dayLabels = getDayLabels(roadmap);
            const dayNum = stepDetail.id != null ? dayLabels[stepDetail.id] : null;
            const detailText = stepDetail.step_detail || stepDetail.stepDetail || 'Follow the step title and complete within the given time.';
            return (
              <div className="roadmap-detail-overlay" onClick={() => setStepDetail(null)}>
                <div className="roadmap-detail-modal" onClick={e => e.stopPropagation()}>
                  <div className="roadmap-detail-header">
                    <h3>Step {stepDetail.step_order ?? stepDetail.stepOrder}: {stepDetail.step_title || stepDetail.stepTitle}</h3>
                    <button type="button" className="roadmap-detail-close" onClick={() => setStepDetail(null)} aria-label="Close">×</button>
                  </div>
                  {dayNum != null && (
                    <div className="roadmap-detail-day">
                      <strong>Day {dayNum}</strong> of roadmap
                    </div>
                  )}
                  <div className="roadmap-detail-when">
                    <h4>When (date and time)</h4>
                    <p>
                      {stepDetail.suggested_date && new Date(stepDetail.suggested_date).toLocaleDateString('en-IN', { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' })}
                      {(stepDetail.suggested_time || stepDetail.suggested_end_time) && (
                        <span className="roadmap-detail-time">
                          {' — '}{formatTimeRange(stepDetail.suggested_time, stepDetail.suggested_end_time)}
                        </span>
                      )}
                      <br />
                      <span className="muted small">Complete this step within this time window.</span>
                    </p>
                  </div>
                  <div className="roadmap-detail-body">
                    <h4>Technical details / How to do this step</h4>
                    <div className="roadmap-detail-text">{detailText.split('\n').map((line, i) => (line.trim() ? <p key={i}>{line}</p> : <br key={i} />))}</div>
                  </div>
                </div>
              </div>
            );
          })()}
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
