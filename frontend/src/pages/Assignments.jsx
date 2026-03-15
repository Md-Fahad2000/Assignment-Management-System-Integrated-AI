import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import './Assignments.css';
import { assignmentsApi } from '../api/client';
import { useToast } from '../context/ToastContext';
import ConfirmDialog from '../components/ConfirmDialog';

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

export default function Assignments() {
  const [assignments, setAssignments] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState({ title: '', description: '', due_date: '', priority: 'medium', status: 'pending' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [uploadingId, setUploadingId] = useState(null);
  const fileInputRef = useRef(null);
  const [uploadTargetId, setUploadTargetId] = useState(null);
  const [createDocumentFile, setCreateDocumentFile] = useState(null);
  const createFileInputRef = useRef(null);
  const [detailAssignment, setDetailAssignment] = useState(null);
  const [roadmapLoading, setRoadmapLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [dragOver, setDragOver] = useState(false);
  const [summaryLoading, setSummaryLoading] = useState(false);
  const [summary, setSummary] = useState(null);
  const [suggestSlotsLoading, setSuggestSlotsLoading] = useState(false);
  const [suggestedSlots, setSuggestedSlots] = useState(null);
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
  const navigate = useNavigate();
  const toast = useToast();

  const load = async () => {
    setError('');
    try {
      const list = await assignmentsApi.getAll();
      setAssignments(Array.isArray(list) ? list : []);
    } catch (err) {
      const msg = err.message || 'Failed to load assignments.';
      setError(msg);
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  useEffect(() => {
    if (!detailAssignment?.id) return;
    const json = detailAssignment?.roadmap_json;
    if (!json || typeof json !== 'string') return;
    let arr = [];
    try { arr = JSON.parse(json); } catch { return; }
    if (!Array.isArray(arr) || arr.length === 0) return;
    assignmentsApi.roadmapConflicts(detailAssignment.id).then(res => setConflicts(Array.isArray(res?.conflicts) ? res.conflicts : [])).catch(() => setConflicts([]));
  }, [detailAssignment?.id, detailAssignment?.roadmap_json]);

  useEffect(() => {
    const onKeyDown = (e) => {
      if (e.key === 'Escape') {
        if (detailAssignment) setDetailAssignment(null);
        if (deleteConfirm) setDeleteConfirm(null);
      }
      if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
        e.preventDefault();
        if (!showForm) { resetForm(); setShowForm(true); }
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [detailAssignment, deleteConfirm, showForm]);

  const resetForm = () => {
    setForm({ title: '', description: '', due_date: '', priority: 'medium', status: 'pending' });
    setEditingId(null);
    setShowForm(false);
    setCreateDocumentFile(null);
    if (createFileInputRef.current) createFileInputRef.current.value = '';
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.title.trim() || !form.due_date) return;
    setError('');
    try {
      if (editingId) {
        await assignmentsApi.update(editingId, { title: form.title, description: form.description || '', dueDate: form.due_date, priority: form.priority, status: form.status });
        toast.success('Assignment updated.');
      } else {
        const created = await assignmentsApi.create({ title: form.title, description: form.description || '', dueDate: form.due_date, priority: form.priority });
        if (created?.id && createDocumentFile) {
          await assignmentsApi.uploadDocument(created.id, createDocumentFile);
        }
        toast.success('Assignment created.');
      }
      resetForm();
      await load();
    } catch (err) {
      const msg = err.message || 'Save failed.';
      setError(msg);
      toast.error(msg);
    }
  };

  const handleEdit = (a) => {
    setForm({
      title: a.title,
      description: a.description || '',
      due_date: a.due_date?.slice(0, 10) || '',
      priority: a.priority || 'medium',
      status: a.status || 'pending',
    });
    setEditingId(a.id);
    setShowForm(true);
  };

  const handleUploadClick = (assignmentId) => {
    setUploadTargetId(assignmentId);
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e) => {
    const file = e.target.files?.[0];
    if (!file || !uploadTargetId) return;
    e.target.value = '';
    setError('');
    setUploadingId(uploadTargetId);
    try {
      await assignmentsApi.uploadDocument(uploadTargetId, file);
      await load();
      toast.success('Document uploaded. AI roadmap can use it.');
    } catch (err) {
      const msg = err.message || 'Upload failed.';
      setError(msg);
      toast.error(msg);
    } finally {
      setUploadingId(null);
      setUploadTargetId(null);
    }
  };

  const handleDelete = async (id) => {
    setDeleteConfirm(null);
    setError('');
    try {
      await assignmentsApi.delete(id);
      if (editingId === id) resetForm();
      if (detailAssignment?.id === id) setDetailAssignment(null);
      await load();
      toast.success('Assignment deleted.');
    } catch (err) {
      const msg = err.message || 'Delete failed.';
      setError(msg);
      toast.error(msg);
    }
  };

  const openDetail = async (a) => {
    setError('');
    setSummary(null);
    setSuggestedSlots(null);
    setKeyPoints(null);
    setConflicts(null);
    setSuggestedTitle(null);
    setKeyDates(null);
    setQuiz(null);
    setOneLineSummary(null);
    setExplainPopover(null);
    setDifficultyRoadmap(null);
    setBestDay(null);
    setConflictSuggestion(null);
    try {
      const full = await assignmentsApi.get(a.id);
      setDetailAssignment(full);
    } catch (err) {
      setError(err.message || 'Failed to load assignment.');
    }
  };

  const handleSummarize = async () => {
    if (!detailAssignment?.id) return;
    setSummaryLoading(true);
    setSummary(null);
    try {
      const res = await assignmentsApi.summarize(detailAssignment.id);
      setSummary(res?.summary || '');
      toast.success('Summary generated.');
    } catch (err) {
      toast.error(err.message || 'Summary failed.');
    } finally {
      setSummaryLoading(false);
    }
  };

  const handleSuggestSlots = async () => {
    if (!detailAssignment?.id) return;
    setSuggestSlotsLoading(true);
    setSuggestedSlots(null);
    try {
      const res = await assignmentsApi.suggestSlots(detailAssignment.id, { detailed: true });
      const slots = Array.isArray(res?.slots) ? res.slots : [];
      setSuggestedSlots(slots);
      toast.success(slots.length ? 'Suggested times loaded.' : 'No slots.');
    } catch (err) {
      toast.error(err.message || 'Suggest slots failed.');
    } finally {
      setSuggestSlotsLoading(false);
    }
  };

  const handleKeyPoints = async () => {
    if (!detailAssignment?.id) return;
    setKeyPointsLoading(true);
    setKeyPoints(null);
    try {
      const res = await assignmentsApi.keyPoints(detailAssignment.id);
      setKeyPoints(Array.isArray(res?.points) ? res.points : []);
      toast.success('Key points loaded.');
    } catch (err) {
      toast.error(err.message || 'Key key points failed.');
    } finally {
      setKeyPointsLoading(false);
    }
  };

  const handleSuggestTitle = async () => {
    if (!detailAssignment?.id) return;
    setSuggestTitleLoading(true);
    setSuggestedTitle(null);
    try {
      const res = await assignmentsApi.suggestTitle(detailAssignment.id);
      setSuggestedTitle(res?.title || null);
      toast.success('Suggested title ready.');
    } catch (err) {
      toast.error(err.message || 'Suggest title failed.');
    } finally {
      setSuggestTitleLoading(false);
    }
  };

  const handleSimplifyRoadmap = async () => {
    if (!detailAssignment?.id) return;
    setSimplifyLoading(true);
    try {
      const res = await assignmentsApi.simplifyRoadmap(detailAssignment.id);
      if (res?.roadmap_json) {
        await assignmentsApi.setRoadmap(detailAssignment.id, res.roadmap_json);
        const updated = await assignmentsApi.get(detailAssignment.id);
        setDetailAssignment(updated);
        toast.success('Roadmap simplified and saved.');
      }
    } catch (err) {
      toast.error(err.message || 'Simplify failed.');
    } finally {
      setSimplifyLoading(false);
    }
  };

  const loadConflicts = async () => {
    if (!detailAssignment?.id || !roadmapSteps.length) return;
    try {
      const res = await assignmentsApi.roadmapConflicts(detailAssignment.id);
      setConflicts(Array.isArray(res?.conflicts) ? res.conflicts : []);
    } catch {
      setConflicts([]);
    }
  };

  const handleKeyDates = async () => {
    if (!detailAssignment?.id) return;
    setKeyDatesLoading(true);
    setKeyDates(null);
    try {
      const res = await assignmentsApi.keyDates(detailAssignment.id);
      setKeyDates(Array.isArray(res?.dates) ? res.dates : []);
      toast.success('Key dates loaded.');
    } catch (err) {
      toast.error(err.message || 'Failed.');
    } finally {
      setKeyDatesLoading(false);
    }
  };

  const handleQuiz = async () => {
    if (!detailAssignment?.id) return;
    setQuizLoading(true);
    setQuiz(null);
    try {
      const res = await assignmentsApi.quiz(detailAssignment.id);
      setQuiz(res?.quiz || '');
      toast.success('Quiz generated.');
    } catch (err) {
      toast.error(err.message || 'Failed.');
    } finally {
      setQuizLoading(false);
    }
  };

  const handleOneLineSummary = async () => {
    if (!detailAssignment?.id) return;
    setOneLineLoading(true);
    setOneLineSummary(null);
    try {
      const res = await assignmentsApi.oneLineSummary(detailAssignment.id);
      setOneLineSummary(res?.summary || '');
      toast.success('Summary loaded.');
    } catch (err) {
      toast.error(err.message || 'Failed.');
    } finally {
      setOneLineLoading(false);
    }
  };

  const handleExplainStep = async (stepTitle, stepDescription) => {
    if (!detailAssignment?.id) return;
    setExplainPopover({ loading: true });
    try {
      const res = await assignmentsApi.explainStep(detailAssignment.id, stepTitle, stepDescription);
      setExplainPopover({ text: res?.explanation || '', loading: false });
    } catch {
      setExplainPopover({ text: 'Could not load explanation.', loading: false });
    }
  };

  const handleDifficultyTime = async () => {
    if (!detailAssignment?.id) return;
    setDifficultyLoading(true);
    setDifficultyRoadmap(null);
    try {
      const res = await assignmentsApi.difficultyTime(detailAssignment.id);
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
    if (!detailAssignment?.id) return;
    setBestDayLoading(true);
    setBestDay(null);
    try {
      const res = await assignmentsApi.bestDay(detailAssignment.id);
      setBestDay(res?.suggestion || '');
      toast.success('Suggestion loaded.');
    } catch (err) {
      toast.error(err.message || 'Failed.');
    } finally {
      setBestDayLoading(false);
    }
  };

  const handleConflictAlternative = async (stepIndex, conflictWith) => {
    if (!detailAssignment?.id) return;
    setConflictSuggestion(null);
    try {
      const res = await assignmentsApi.conflictAlternative(detailAssignment.id, stepIndex, conflictWith);
      setConflictSuggestion(res?.suggestion || '');
    } catch {
      setConflictSuggestion('Could not get suggestion.');
    }
  };

  const handleRescheduleRoadmap = async (fromStepIndex) => {
    if (!detailAssignment?.id) return;
    setRescheduleLoading(true);
    try {
      const res = await assignmentsApi.rescheduleRoadmap(detailAssignment.id, fromStepIndex);
      if (res?.roadmap_json) {
        await assignmentsApi.setRoadmap(detailAssignment.id, res.roadmap_json);
        const updated = await assignmentsApi.get(detailAssignment.id);
        setDetailAssignment(updated);
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
    navigator.clipboard.writeText(detailAssignment?.title + ' – Roadmap\n\n' + text).then(() => toast.success('Roadmap copied to clipboard.'));
  };

  const handleGenerateRoadmap = async () => {
    if (!detailAssignment?.id) return;
    setRoadmapLoading(true);
    setError('');
    try {
      await assignmentsApi.generateRoadmap(detailAssignment.id);
      const updated = await assignmentsApi.get(detailAssignment.id);
      setDetailAssignment(updated);
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

  const roadmapSteps = (() => {
    const json = detailAssignment?.roadmap_json;
    if (!json || typeof json !== 'string') return [];
    try {
      const arr = JSON.parse(json);
      return Array.isArray(arr) ? arr : [];
    } catch {
      return [];
    }
  })();

  const priorityClass = (p) => p === 'high' ? 'priority-high' : p === 'medium' ? 'priority-medium' : 'priority-low';

  const filteredAssignments = assignments.filter((a) => {
    const matchSearch = !search.trim() || a.title?.toLowerCase().includes(search.toLowerCase());
    const matchStatus = statusFilter === 'all' || (a.status || '').replace('_', '') === statusFilter.replace('_', '');
    return matchSearch && matchStatus;
  });

  const handleDrop = (e, targetId) => {
    e.preventDefault();
    setDragOver(false);
    const file = e.dataTransfer?.files?.[0];
    if (!file || (!targetId && !showForm)) return;
    const ext = (file.name || '').toLowerCase().slice(-4);
    if (ext !== '.pdf' && !file.name?.toLowerCase().endsWith('.txt')) return;
    if (targetId) {
      setUploadTargetId(targetId);
      setUploadingId(targetId);
      assignmentsApi.uploadDocument(targetId, file).then(() => { load(); toast.success('Document uploaded.'); }).catch((err) => { toast.error(err.message || 'Upload failed.'); }).finally(() => { setUploadingId(null); setUploadTargetId(null); });
    } else if (showForm && !editingId) {
      setCreateDocumentFile(file);
    }
  };

  const handleDragOver = (e, isOver) => {
    e.preventDefault();
    setDragOver(isOver);
  };

  return (
    <div className="assignments-page">
      <div className="page-header">
        <h1>Assignments</h1>
        <button className="btn btn-primary" onClick={() => { resetForm(); setShowForm(!showForm); }} title="Shortcut: Ctrl+N">
          {showForm ? 'Cancel' : '+ Add Assignment'}
        </button>
      </div>

      {error && <p className="error-msg">{error}</p>}

      {showForm && (
        <div className="card form-card">
          <h2>{editingId ? 'Edit Assignment' : 'Add Assignment'}</h2>
          <form onSubmit={handleSubmit}>
            <label>Title</label>
            <input type="text" placeholder="Assignment title" value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} required />
            <label>Description</label>
            <textarea placeholder="Details (optional)" rows={3} value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            <label>Due Date</label>
            <input type="date" value={form.due_date} onChange={e => setForm(f => ({ ...f, due_date: e.target.value }))} required />
            {!editingId && (
              <div
                className={`file-drop-zone ${dragOver && !uploadTargetId ? 'file-drop-zone-active' : ''}`}
                onDragOver={(e) => handleDragOver(e, true)}
                onDragLeave={(e) => handleDragOver(e, false)}
                onDrop={(e) => handleDrop(e, null)}
              >
                <label>Assignment document (optional, for AI roadmap)</label>
                <input
                  type="file"
                  ref={createFileInputRef}
                  accept=".pdf,.txt"
                  onChange={e => setCreateDocumentFile(e.target.files?.[0] ?? null)}
                />
                <p className="file-drop-hint">or drag and drop a PDF or TXT file here</p>
                {createDocumentFile && <span className="file-name">{createDocumentFile.name}</span>}
              </div>
            )}
            <label>Priority</label>
            <select value={form.priority} onChange={e => setForm(f => ({ ...f, priority: e.target.value }))}>
              <option value="low">Low</option>
              <option value="medium">Medium</option>
              <option value="high">High</option>
            </select>
            {editingId ? (
              <div>
                <label>Status</label>
                <select value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value }))}>
                  <option value="pending">Pending</option>
                  <option value="in_progress">In Progress</option>
                  <option value="completed">Completed</option>
                </select>
              </div>
            ) : null}
            <div className="form-actions">
              <button type="submit" className="btn btn-primary">{editingId ? 'Update' : 'Save'}</button>
              {editingId && <button type="button" className="btn btn-secondary" onClick={resetForm}>Cancel</button>}
            </div>
          </form>
        </div>
      )}

      <div className="card assignments-list">
        <h2>Your Assignments</h2>
        {assignments.length > 0 && (
          <div className="list-toolbar">
            <input
              type="search"
              placeholder="Search by title..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="search-input"
              aria-label="Search assignments"
            />
            <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className="filter-select" aria-label="Filter by status">
              <option value="all">All statuses</option>
              <option value="pending">Pending</option>
              <option value="in_progress">In progress</option>
              <option value="completed">Completed</option>
            </select>
          </div>
        )}
        {loading ? (
          <p className="muted">Loading...</p>
        ) : assignments.length === 0 ? (
          <div className="empty-state">
            <p className="empty-state-text">No assignments yet. Add your first assignment to get started.</p>
            <button type="button" className="btn btn-primary" onClick={() => { resetForm(); setShowForm(true); }}>Add your first assignment</button>
          </div>
        ) : filteredAssignments.length === 0 ? (
          <p className="muted">No assignments match your search or filter.</p>
        ) : (
          <div className="assignment-cards">
            {filteredAssignments.map(a => {
              const dueBadge = getDueBadge(a.due_date);
              return (
                <div
                  key={a.id}
                  className={`assignment-card ${priorityClass(a.priority)} ${uploadingId === a.id ? 'uploading' : ''}`}
                  onDragOver={(e) => { e.preventDefault(); e.stopPropagation(); }}
                  onDrop={(e) => handleDrop(e, a.id)}
                >
                  <div className="card-header">
                    <h3>{a.title}</h3>
                    <span className={`badge status-${a.status}`}>{a.status?.replace('_', ' ')}</span>
                  </div>
                  {a.description && <p className="card-desc">{a.description}</p>}
                  <div className="card-meta">
                    <span>Due: {a.due_date?.slice(0, 10)}</span>
                    {dueBadge && <span className={`badge ${dueBadge.class}`}>{dueBadge.label}</span>}
                    <span className={`badge priority-${a.priority}`}>{a.priority}</span>
                    {a.has_document && <span className="badge badge-doc">Document</span>}
                  </div>
                  <input
                    type="file"
                    ref={fileInputRef}
                    accept=".pdf,.txt"
                    style={{ display: 'none' }}
                    onChange={handleFileChange}
                  />
                  <div className="card-actions">
                    <button type="button" className="btn btn-small btn-primary" onClick={() => navigate(`/assignments/${a.id}`)}>View details & roadmap</button>
                    <button type="button" className="btn btn-small" onClick={() => handleUploadClick(a.id)} disabled={uploadingId === a.id}>
                      {uploadingId === a.id ? 'Uploading...' : a.has_document ? 'Replace doc' : 'Upload doc'}
                    </button>
                    <button type="button" className="btn btn-small" onClick={() => handleEdit(a)}>Edit</button>
                    <button type="button" className="btn btn-small btn-danger" onClick={() => setDeleteConfirm({ id: a.id, title: a.title })}>Delete</button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <ConfirmDialog
        open={!!deleteConfirm}
        title="Delete assignment?"
        message={deleteConfirm ? `"${deleteConfirm.title}" will be permanently deleted. This cannot be undone.` : ''}
        confirmLabel="Delete"
        cancelLabel="Cancel"
        danger
        onConfirm={() => deleteConfirm && handleDelete(deleteConfirm.id)}
        onCancel={() => setDeleteConfirm(null)}
      />

      {detailAssignment && (
        <div className="assignment-detail-overlay" onClick={() => setDetailAssignment(null)}>
          <div className="assignment-detail-modal" onClick={e => e.stopPropagation()}>
            <div className="assignment-detail-header">
              <h2>{detailAssignment.title}</h2>
              <button type="button" className="detail-close" onClick={() => setDetailAssignment(null)} aria-label="Close">×</button>
            </div>
            {detailAssignment.description && <p className="detail-desc">{detailAssignment.description}</p>}
            <div className="detail-meta">
              <span>Due: {detailAssignment.due_date?.slice(0, 10)}</span>
              {getDueBadge(detailAssignment.due_date) && (
                <span className={`badge ${getDueBadge(detailAssignment.due_date).class}`}>
                  {getDueBadge(detailAssignment.due_date).label}
                </span>
              )}
              <span className={`badge priority-${detailAssignment.priority}`}>{detailAssignment.priority}</span>
              {detailAssignment.has_document && <span className="badge badge-doc">Document</span>}
            </div>

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
                {detailAssignment?.has_document && (
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
                      await assignmentsApi.setRoadmap(detailAssignment.id, difficultyRoadmap);
                      const updated = await assignmentsApi.get(detailAssignment.id);
                      setDetailAssignment(updated);
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
                if (!detailAssignment?.id) return;
                try {
                  await assignmentsApi.update(detailAssignment.id, { title: suggestedTitle, description: detailAssignment.description || '', dueDate: detailAssignment.due_date, priority: detailAssignment.priority, status: detailAssignment.status });
                  setDetailAssignment(d => d ? { ...d, title: suggestedTitle } : null);
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
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => detailAssignment?.id && navigate(`/assignments/${detailAssignment.id}/assistant`)}
                >
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
                        <button type="button" className="btn btn-small" onClick={() => navigate(`/assignments/${detailAssignment?.id}/assistant?explainStep=${i + 1}&stepTitle=${encodeURIComponent(item.step || `Step ${i + 1}`)}`)}>
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
      )}
    </div>
  );
}
