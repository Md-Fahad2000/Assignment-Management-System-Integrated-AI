import { useState, useEffect, useRef } from 'react';
import './Assignments.css';
import { assignmentsApi } from '../api/client';

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

  const load = async () => {
    setError('');
    try {
      const list = await assignmentsApi.getAll();
      setAssignments(Array.isArray(list) ? list : []);
    } catch (err) {
      setError(err.message || 'Failed to load assignments.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

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
      } else {
        const created = await assignmentsApi.create({ title: form.title, description: form.description || '', dueDate: form.due_date, priority: form.priority });
        if (created?.id && createDocumentFile) {
          await assignmentsApi.uploadDocument(created.id, createDocumentFile);
        }
      }
      resetForm();
      await load();
    } catch (err) {
      setError(err.message || 'Save failed.');
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
    } catch (err) {
      setError(err.message || 'Upload failed.');
    } finally {
      setUploadingId(null);
      setUploadTargetId(null);
    }
  };

  const handleDelete = async (id) => {
    setError('');
    try {
      await assignmentsApi.delete(id);
      if (editingId === id) resetForm();
      if (detailAssignment?.id === id) setDetailAssignment(null);
      await load();
    } catch (err) {
      setError(err.message || 'Delete failed.');
    }
  };

  const openDetail = async (a) => {
    setError('');
    try {
      const full = await assignmentsApi.get(a.id);
      setDetailAssignment(full);
    } catch (err) {
      setError(err.message || 'Failed to load assignment.');
    }
  };

  const handleGenerateRoadmap = async () => {
    if (!detailAssignment?.id) return;
    setRoadmapLoading(true);
    setError('');
    try {
      await assignmentsApi.generateRoadmap(detailAssignment.id);
      const updated = await assignmentsApi.get(detailAssignment.id);
      setDetailAssignment(updated);
    } catch (err) {
      setError(err.message || 'Failed to generate roadmap.');
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

  return (
    <div className="assignments-page">
      <div className="page-header">
        <h1>Assignments</h1>
        <button className="btn btn-primary" onClick={() => { resetForm(); setShowForm(!showForm); }}>
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
              <div>
                <label>Assignment document (optional, for AI roadmap)</label>
                <input
                  type="file"
                  ref={createFileInputRef}
                  accept=".pdf,.txt"
                  onChange={e => setCreateDocumentFile(e.target.files?.[0] ?? null)}
                />
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
        {loading ? (
          <p className="muted">Loading...</p>
        ) : assignments.length === 0 ? (
          <p className="muted">No assignments yet. Add one above.</p>
        ) : (
          <div className="assignment-cards">
            {assignments.map(a => (
              <div key={a.id} className={`assignment-card ${priorityClass(a.priority)}`}>
                <div className="card-header">
                  <h3>{a.title}</h3>
                  <span className={`badge status-${a.status}`}>{a.status?.replace('_', ' ')}</span>
                </div>
                {a.description && <p className="card-desc">{a.description}</p>}
                <div className="card-meta">
                  <span>Due: {a.due_date?.slice(0, 10)}</span>
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
                  <button type="button" className="btn btn-small btn-primary" onClick={() => openDetail(a)}>View details & roadmap</button>
                  <button type="button" className="btn btn-small" onClick={() => handleUploadClick(a.id)} disabled={uploadingId === a.id}>
                    {uploadingId === a.id ? 'Uploading...' : a.has_document ? 'Replace doc' : 'Upload doc'}
                  </button>
                  <button type="button" className="btn btn-small" onClick={() => handleEdit(a)}>Edit</button>
                  <button type="button" className="btn btn-small btn-danger" onClick={() => handleDelete(a.id)}>Delete</button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

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
              <span className={`badge priority-${detailAssignment.priority}`}>{detailAssignment.priority}</span>
              {detailAssignment.has_document && <span className="badge badge-doc">Document</span>}
            </div>

            <div className="roadmap-section">
              <h3>AI Roadmap</h3>
              <button type="button" className="btn btn-primary" onClick={handleGenerateRoadmap} disabled={roadmapLoading}>
                {roadmapLoading ? 'AI is analyzing your PDF and Routine...' : 'Generate AI Roadmap'}
              </button>
              {roadmapSteps.length === 0 && !roadmapLoading && (
                <p className="muted">Upload a PDF and click Generate to get a step-by-step roadmap (excluding 10 AM–5 PM).</p>
              )}
              {roadmapSteps.length > 0 && (
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
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
