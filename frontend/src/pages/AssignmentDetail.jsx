import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import './Assignments.css';
import { assignmentsApi } from '../api/client';
import { useToast } from '../context/ToastContext';

function formatRoadmapDate(dateStr) {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return dateStr;
  const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  const day = days[d.getDay()];
  const date = d.getDate();
  const suffix = date === 1 || date === 21 || date === 31 ? 'st' : date === 2 || date === 22 ? 'nd' : date === 3 || date === 23 ? 'rd' : 'th';
  const month = d.toLocaleString('en', { month: 'long' });
  const time = d.toLocaleTimeString('en', { hour: '2-digit', minute: '2-digit', hour12: true });
  return `${day}, ${date}${suffix} ${month} - ${time}`;
}

function getDueBadge(dueDateStr) {
  if (!dueDateStr) return null;
  const due = new Date(dueDateStr.slice(0, 10));
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  due.setHours(0, 0, 0, 0);
  const diff = Math.ceil((due - today) / (1000 * 60 * 60 * 24));
  if (diff < 0) return { label: 'Overdue', class: 'due-overdue' };
  if (diff === 0) return { label: 'Due today', class: 'due-today' };
  if (diff === 1) return { label: 'Due tomorrow', class: 'due-soon' };
  if (diff <= 7) return { label: `Due in ${diff} days`, class: 'due-soon' };
  return null;
}

function parseJsonArray(str) {
  if (!str || typeof str !== 'string') return [];
  try {
    const a = JSON.parse(str);
    return Array.isArray(a) ? a : [];
  } catch {
    return [];
  }
}

export default function AssignmentDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const toast = useToast();
  const [assignment, setAssignment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [roadmapLoading, setRoadmapLoading] = useState(false);
  const [summary, setSummary] = useState(null);
  const [summaryLoading, setSummaryLoading] = useState(false);
  const [suggestedSlots, setSuggestedSlots] = useState(null);
  const [suggestSlotsLoading, setSuggestSlotsLoading] = useState(false);
  const [keyPoints, setKeyPoints] = useState(null);
  const [keyPointsLoading, setKeyPointsLoading] = useState(false);
  const [conflicts, setConflicts] = useState(null);
  const [suggestedTitle, setSuggestedTitle] = useState(null);
  const [suggestTitleLoading, setSuggestTitleLoading] = useState(false);
  const [simplifyLoading, setSimplifyLoading] = useState(false);
  const [keyDates, setKeyDates] = useState(null);
  const [keyDatesLoading, setKeyDatesLoading] = useState(false);
  const [quiz, setQuiz] = useState(null);
  const [quizLoading, setQuizLoading] = useState(false);
  const [oneLineSummary, setOneLineSummary] = useState(null);
  const [oneLineLoading, setOneLineLoading] = useState(false);
  const [explainPopover, setExplainPopover] = useState(null);
  const [difficultyRoadmap, setDifficultyRoadmap] = useState(null);
  const [difficultyLoading, setDifficultyLoading] = useState(false);
  const [bestDay, setBestDay] = useState(null);
  const [bestDayLoading, setBestDayLoading] = useState(false);
  const [conflictSuggestion, setConflictSuggestion] = useState(null);
  const [rescheduleLoading, setRescheduleLoading] = useState(false);

  const loadAssignment = async () => {
    if (!id) return;
    setError('');
    try {
      const full = await assignmentsApi.get(Number(id));
      setAssignment(full);
    } catch (err) {
      setError(err.message || 'Failed to load assignment.');
    } finally {
      setLoading(false);
    }
  };

  const loadSavedResponses = async () => {
    if (!id) return;
    try {
      const saved = await assignmentsApi.getAiResponses(Number(id));
      if (saved && typeof saved === 'object') {
        if (saved.summary) setSummary(saved.summary);
        if (saved.key_points) setKeyPoints(parseJsonArray(saved.key_points));
        if (saved.key_dates) setKeyDates(parseJsonArray(saved.key_dates));
        if (saved.quiz) setQuiz(saved.quiz);
        if (saved.one_line_summary) setOneLineSummary(saved.one_line_summary);
        if (saved.best_day) setBestDay(saved.best_day);
        if (saved.suggested_title) setSuggestedTitle(saved.suggested_title);
        if (saved.suggested_slots) {
          const arr = parseJsonArray(saved.suggested_slots);
          setSuggestedSlots(arr.length ? arr : null);
        }
      }
    } catch {
      // ignore
    }
  };

  useEffect(() => {
    loadAssignment();
  }, [id]);

  useEffect(() => {
    if (!assignment?.id) return;
    loadSavedResponses();
  }, [assignment?.id]);

  useEffect(() => {
    const json = assignment?.roadmap_json;
    if (!json || typeof json !== 'string') return;
    let arr = [];
    try { arr = JSON.parse(json); } catch { return; }
    if (!Array.isArray(arr) || arr.length === 0) return;
    assignmentsApi.roadmapConflicts(assignment.id).then(res => setConflicts(Array.isArray(res?.conflicts) ? res.conflicts : [])).catch(() => setConflicts([]));
  }, [assignment?.id, assignment?.roadmap_json]);

  const roadmapSteps = (() => {
    const json = assignment?.roadmap_json;
    if (!json || typeof json !== 'string') return [];
    try {
      const arr = JSON.parse(json);
      return Array.isArray(arr) ? arr : [];
    } catch {
      return [];
    }
  })();

  const priorityClass = (p) => p === 'high' ? 'priority-high' : p === 'medium' ? 'priority-medium' : 'priority-low';

  const handleSummarize = async () => {
    if (!assignment?.id) return;
    setSummaryLoading(true);
    setSummary(null);
    try {
      const res = await assignmentsApi.summarize(assignment.id);
      setSummary(res?.summary || '');
      toast.success('Summary generated and saved.');
    } catch (err) {
      toast.error(err.message || 'Summary failed.');
    } finally {
      setSummaryLoading(false);
    }
  };

  const handleSuggestSlots = async () => {
    if (!assignment?.id) return;
    setSuggestSlotsLoading(true);
    setSuggestedSlots(null);
    try {
      const res = await assignmentsApi.suggestSlots(assignment.id, { detailed: true });
      const slots = Array.isArray(res?.slots) ? res.slots : [];
      setSuggestedSlots(slots);
      toast.success(slots.length ? 'Suggested times saved.' : 'No slots.');
    } catch (err) {
      toast.error(err.message || 'Suggest slots failed.');
    } finally {
      setSuggestSlotsLoading(false);
    }
  };

  const handleKeyPoints = async () => {
    if (!assignment?.id) return;
    setKeyPointsLoading(true);
    setKeyPoints(null);
    try {
      const res = await assignmentsApi.keyPoints(assignment.id);
      setKeyPoints(Array.isArray(res?.points) ? res.points : []);
      toast.success('Key points saved.');
    } catch (err) {
      toast.error(err.message || 'Key points failed.');
    } finally {
      setKeyPointsLoading(false);
    }
  };

  const handleSuggestTitle = async () => {
    if (!assignment?.id) return;
    setSuggestTitleLoading(true);
    setSuggestedTitle(null);
    try {
      const res = await assignmentsApi.suggestTitle(assignment.id);
      setSuggestedTitle(res?.title || null);
      toast.success('Suggested title saved.');
    } catch (err) {
      toast.error(err.message || 'Suggest title failed.');
    } finally {
      setSuggestTitleLoading(false);
    }
  };

  const handleSimplifyRoadmap = async () => {
    if (!assignment?.id) return;
    setSimplifyLoading(true);
    try {
      const res = await assignmentsApi.simplifyRoadmap(assignment.id);
      if (res?.roadmap_json) {
        await assignmentsApi.setRoadmap(assignment.id, res.roadmap_json);
        const updated = await assignmentsApi.get(assignment.id);
        setAssignment(updated);
        toast.success('Roadmap simplified and saved.');
      }
    } catch (err) {
      toast.error(err.message || 'Simplify failed.');
    } finally {
      setSimplifyLoading(false);
    }
  };

  const handleKeyDates = async () => {
    if (!assignment?.id) return;
    setKeyDatesLoading(true);
    setKeyDates(null);
    try {
      const res = await assignmentsApi.keyDates(assignment.id);
      setKeyDates(Array.isArray(res?.dates) ? res.dates : []);
      toast.success('Key dates saved.');
    } catch (err) {
      toast.error(err.message || 'Failed.');
    } finally {
      setKeyDatesLoading(false);
    }
  };

  const handleQuiz = async () => {
    if (!assignment?.id) return;
    setQuizLoading(true);
    setQuiz(null);
    try {
      const res = await assignmentsApi.quiz(assignment.id);
      setQuiz(res?.quiz || '');
      toast.success('Quiz generated and saved.');
    } catch (err) {
      toast.error(err.message || 'Failed.');
    } finally {
      setQuizLoading(false);
    }
  };

  const handleOneLineSummary = async () => {
    if (!assignment?.id) return;
    setOneLineLoading(true);
    setOneLineSummary(null);
    try {
      const res = await assignmentsApi.oneLineSummary(assignment.id);
      setOneLineSummary(res?.summary || '');
      toast.success('Summary saved.');
    } catch (err) {
      toast.error(err.message || 'Failed.');
    } finally {
      setOneLineLoading(false);
    }
  };

  const handleExplainStep = async (stepTitle, stepDescription) => {
    if (!assignment?.id) return;
    setExplainPopover({ loading: true });
    try {
      const res = await assignmentsApi.explainStep(assignment.id, stepTitle, stepDescription);
      setExplainPopover({ text: res?.explanation || '', loading: false });
    } catch {
      setExplainPopover({ text: 'Could not load explanation.', loading: false });
    }
  };

  const handleDifficultyTime = async () => {
    if (!assignment?.id) return;
    setDifficultyLoading(true);
    setDifficultyRoadmap(null);
    try {
      const res = await assignmentsApi.difficultyTime(assignment.id);
      if (res?.roadmap_json) {
        setDifficultyRoadmap(res.roadmap_json);
        toast.success('Difficulty and time added.');
      }
    } catch (err) {
      toast.error(err.message || 'Failed.');
    } finally {
      setDifficultyLoading(false);
    }
  };

  const handleBestDay = async () => {
    if (!assignment?.id) return;
    setBestDayLoading(true);
    setBestDay(null);
    try {
      const res = await assignmentsApi.bestDay(assignment.id);
      setBestDay(res?.suggestion || '');
      toast.success('Suggestion saved.');
    } catch (err) {
      toast.error(err.message || 'Failed.');
    } finally {
      setBestDayLoading(false);
    }
  };

  const handleConflictAlternative = async (stepIndex, conflictWith) => {
    if (!assignment?.id) return;
    setConflictSuggestion(null);
    try {
      const res = await assignmentsApi.conflictAlternative(assignment.id, stepIndex, conflictWith);
      setConflictSuggestion(res?.suggestion || '');
    } catch {
      setConflictSuggestion('Could not get suggestion.');
    }
  };

  const handleRescheduleRoadmap = async (fromStepIndex) => {
    if (!assignment?.id) return;
    setRescheduleLoading(true);
    try {
      const res = await assignmentsApi.rescheduleRoadmap(assignment.id, fromStepIndex);
      if (res?.roadmap_json) {
        await assignmentsApi.setRoadmap(assignment.id, res.roadmap_json);
        const updated = await assignmentsApi.get(assignment.id);
        setAssignment(updated);
        toast.success('Roadmap rescheduled from step ' + (fromStepIndex + 1) + '.');
      }
    } catch (err) {
      toast.error(err.message || 'Failed.');
    } finally {
      setRescheduleLoading(false);
    }
  };

  const handleCopyRoadmap = () => {
    if (roadmapSteps.length === 0) return;
    const text = roadmapSteps.map((s, i) => `${i + 1}. ${s.step || 'Step ' + (i + 1)}${s.description ? '\n   ' + s.description : ''}${s.startDate ? '\n   ' + formatRoadmapDate(s.startDate) + (s.endDate && s.endDate !== s.startDate ? ' → ' + formatRoadmapDate(s.endDate) : '') : ''}`).join('\n\n');
    navigator.clipboard.writeText((assignment?.title || '') + ' – Roadmap\n\n' + text).then(() => toast.success('Roadmap copied to clipboard.'));
  };

  const handleGenerateRoadmap = async () => {
    if (!assignment?.id) return;
    setRoadmapLoading(true);
    setError('');
    try {
      await assignmentsApi.generateRoadmap(assignment.id);
      const updated = await assignmentsApi.get(assignment.id);
      setAssignment(updated);
      const steps = (() => { try { const j = updated?.roadmap_json; if (!j) return []; const a = JSON.parse(j); return Array.isArray(a) ? a : []; } catch { return []; } })();
      toast.success(steps.length ? `Roadmap generated: ${steps.length} steps.` : 'AI roadmap generated.');
    } catch (err) {
      const msg = err.message || 'Failed to generate roadmap.';
      setError(msg);
      toast.error(msg);
    } finally {
      setRoadmapLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="assignment-detail-page">
        <p className="muted">Loading assignment...</p>
      </div>
    );
  }

  if (error || !assignment) {
    return (
      <div className="assignment-detail-page">
        <p className="error-msg">{error || 'Assignment not found.'}</p>
        <button type="button" className="btn btn-secondary" onClick={() => navigate('/assignments')}>Back to Assignments</button>
      </div>
    );
  }

  const dueBadge = getDueBadge(assignment.due_date);

  return (
    <div className="assignment-detail-page">
      <div className="assignment-detail-header assignment-detail-header-full">
        <button type="button" className="btn btn-secondary back-btn" onClick={() => navigate('/assignments')}>← Back to Assignments</button>
        <h1>{assignment.title}</h1>
      </div>
      {assignment.description && <p className="detail-desc">{assignment.description}</p>}
      <div className="detail-meta">
        <span>Due: {assignment.due_date?.slice(0, 10)}</span>
        {dueBadge && <span className={`badge ${dueBadge.class}`}>{dueBadge.label}</span>}
        <span className={`badge priority-${assignment.priority}`}>{assignment.priority}</span>
        {assignment.has_document && <span className="badge badge-doc">Document</span>}
      </div>

      {error && <p className="error-msg">{error}</p>}

      <div className="detail-ai-tools">
        <h3>AI tools</h3>
        <div className="detail-ai-buttons">
          <button type="button" className="btn btn-small btn-primary" onClick={handleSummarize} disabled={summaryLoading}>
            {summaryLoading ? 'Generating...' : 'Summarize assignment'}
          </button>
          <button type="button" className="btn btn-small btn-secondary" onClick={handleSuggestSlots} disabled={suggestSlotsLoading}>
            {suggestSlotsLoading ? 'Loading...' : 'When should I work on this?'}
          </button>
          <button type="button" className="btn btn-small btn-secondary" onClick={handleKeyPoints} disabled={keyPointsLoading}>
            {keyPointsLoading ? 'Loading...' : 'Key points'}
          </button>
          <button type="button" className="btn btn-small btn-secondary" onClick={handleKeyDates} disabled={keyDatesLoading}>
            {keyDatesLoading ? '...' : 'Key dates from doc'}
          </button>
          <button type="button" className="btn btn-small btn-secondary" onClick={handleQuiz} disabled={quizLoading}>
            {quizLoading ? '...' : 'Practice quiz'}
          </button>
          <button type="button" className="btn btn-small" onClick={handleOneLineSummary} disabled={oneLineLoading}>
            {oneLineLoading ? '...' : "What's this about?"}
          </button>
          <button type="button" className="btn btn-small" onClick={handleBestDay} disabled={bestDayLoading}>
            {bestDayLoading ? '...' : 'Best day to work'}
          </button>
          <button type="button" className="btn btn-small" onClick={handleDifficultyTime} disabled={difficultyLoading}>
            {difficultyLoading ? '...' : 'Difficulty & time per step'}
          </button>
          {assignment?.has_document && (
            <button type="button" className="btn btn-small" onClick={handleSuggestTitle} disabled={suggestTitleLoading}>
              {suggestTitleLoading ? '...' : 'Suggest title from doc'}
            </button>
          )}
        </div>
        {oneLineSummary && <div className="detail-summary-box"><h4>One-line summary</h4><p>{oneLineSummary}</p></div>}
        {keyDates && keyDates.length > 0 && (
          <div className="detail-summary-box">
            <h4>Key dates from document</h4>
            <ul>{keyDates.map((d, i) => <li key={i}>{d}</li>)}</ul>
          </div>
        )}
        {quiz && (
          <div className="detail-summary-box">
            <h4>Practice quiz</h4>
            <pre className="quiz-text">{quiz}</pre>
          </div>
        )}
        {bestDay && <div className="detail-summary-box"><h4>Best day</h4><p>{bestDay}</p></div>}
        {difficultyRoadmap && (
          <div className="detail-summary-box">
            <h4>Roadmap with difficulty & time</h4>
            <p className="muted small">Replace roadmap with this version to see difficulty and estimated minutes per step.</p>
            <button type="button" className="btn btn-small btn-primary" onClick={async () => {
              try {
                await assignmentsApi.setRoadmap(assignment.id, difficultyRoadmap);
                const updated = await assignmentsApi.get(assignment.id);
                setAssignment(updated);
                setDifficultyRoadmap(null);
                toast.success('Roadmap updated.');
              } catch (e) { toast.error(e.message); }
            }}>Apply to roadmap</button>
          </div>
        )}
        {suggestedTitle && (
          <div className="detail-suggested-title">
            <span>Suggested title: {suggestedTitle}</span>
            <button type="button" className="btn btn-small btn-primary" onClick={async () => {
              try {
                await assignmentsApi.update(assignment.id, { title: suggestedTitle, description: assignment.description || '', dueDate: assignment.due_date, priority: assignment.priority, status: assignment.status });
                setAssignment(a => a ? { ...a, title: suggestedTitle } : null);
                setSuggestedTitle(null);
                toast.success('Title updated.');
              } catch (e) { toast.error(e.message || 'Update failed.'); }
            }}>Apply</button>
          </div>
        )}
        {summary && <div className="detail-summary-box"><h4>Summary</h4><p>{summary}</p></div>}
        {keyPoints && keyPoints.length > 0 && (
          <div className="detail-summary-box">
            <h4>Key points</h4>
            <ul>{keyPoints.map((p, i) => <li key={i}>{p}</li>)}</ul>
          </div>
        )}
        {suggestedSlots && suggestedSlots.length > 0 && (
          <div className="detail-slots-box">
            <h4>Suggested time slots</h4>
            <ul>{suggestedSlots.map((slot, i) => (
              <li key={i}>
                {typeof slot === 'string' ? slot : (slot?.text || '')}
                {slot?.reason && <span className="slot-reason"> — {slot.reason}</span>}
              </li>
            ))}</ul>
          </div>
        )}
      </div>

      <div className="roadmap-section">
        <h3>AI Roadmap</h3>
        <div className="roadmap-actions">
          <button type="button" className="btn btn-primary" onClick={handleGenerateRoadmap} disabled={roadmapLoading}>
            {roadmapLoading ? 'AI is analyzing your PDF and Routine...' : roadmapSteps.length > 0 ? 'Regenerate AI Roadmap' : 'Generate AI Roadmap'}
          </button>
          <button type="button" className="btn btn-secondary" onClick={() => navigate(`/assignments/${assignment.id}/assistant`)}>
            Open AI Assistant
          </button>
        </div>
        {roadmapSteps.length === 0 && !roadmapLoading && (
          <p className="muted">Upload a PDF and click Generate to get a step-by-step roadmap (excluding 10 AM–5 PM).</p>
        )}
        {conflicts && conflicts.length > 0 && (
          <div className="roadmap-conflicts-warning">
            <strong>Overlap with routine:</strong> {conflicts.map(c => `Step ${c.stepIndex} (${c.stepTitle}) ↔ ${c.conflictWith}`).join('; ')}
            <div className="conflict-actions">
              {conflicts.map((c, i) => (
                <button key={i} type="button" className="btn btn-small" onClick={() => handleConflictAlternative(c.stepIndex, c.conflictWith)}>
                  Suggest alternative for step {c.stepIndex}
                </button>
              ))}
            </div>
            {conflictSuggestion && <p className="conflict-suggestion">{conflictSuggestion}</p>}
          </div>
        )}
        {roadmapSteps.length > 0 && (
          <>
            <div className="roadmap-actions-row">
              <button type="button" className="btn btn-small btn-secondary" onClick={handleCopyRoadmap}>Copy as text</button>
              <button type="button" className="btn btn-small" onClick={() => window.print()}>Print / Save as PDF</button>
              <button type="button" className="btn btn-small" onClick={handleSimplifyRoadmap} disabled={simplifyLoading}>
                {simplifyLoading ? 'Simplifying...' : 'Simplify roadmap (fewer steps)'}
              </button>
            </div>
            <div className="roadmap-timeline">
              {roadmapSteps.map((item, i) => (
                <div key={i} className="timeline-item">
                  <div className="timeline-marker" />
                  <div className="timeline-content">
                    <strong className="timeline-step">{item.step || `Step ${i + 1}`}</strong>
                    {item.description && <p className="timeline-desc">{item.description}</p>}
                    <div className="timeline-dates">
                      {item.startDate && <span>{formatRoadmapDate(item.startDate)}</span>}
                      {item.endDate && item.startDate !== item.endDate && <span> → {formatRoadmapDate(item.endDate)}</span>}
                    </div>
                    <div className="step-actions">
                      <button type="button" className="btn btn-small explain-step-btn" onClick={() => handleExplainStep(item.step || `Step ${i + 1}`, item.description)}>
                        Explain in place
                      </button>
                      <button type="button" className="btn btn-small" onClick={() => navigate(`/assignments/${assignment?.id}/assistant?explainStep=${i + 1}&stepTitle=${encodeURIComponent(item.step || `Step ${i + 1}`)}`)}>
                        Open in assistant
                      </button>
                      <button type="button" className="btn btn-small" onClick={() => handleRescheduleRoadmap(i)} disabled={rescheduleLoading}>
                        {rescheduleLoading ? 'Rescheduling...' : "Couldn't do – reschedule from here"}
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </div>

      {explainPopover && (
        <div className="explain-popover-overlay" onClick={() => setExplainPopover(null)}>
          <div className="explain-popover" onClick={e => e.stopPropagation()}>
            <button type="button" className="detail-close" onClick={() => setExplainPopover(null)} aria-label="Close">×</button>
            <h4>Step explanation</h4>
            {explainPopover.loading ? <p className="muted">Loading...</p> : <p>{explainPopover.text}</p>}
          </div>
        </div>
      )}
    </div>
  );
}
