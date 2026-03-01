import { useState } from 'react';
import './Assignments.css';

const initialAssignments = [
  { id: 1, title: 'Database Project', description: 'Design ER diagram and implement', due_date: '2025-03-15', priority: 'high', status: 'pending' },
  { id: 2, title: 'Web Dev Assignment', description: 'React components submission', due_date: '2025-03-20', priority: 'medium', status: 'in_progress' },
];

export default function Assignments() {
  const [assignments, setAssignments] = useState(initialAssignments);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState({ title: '', description: '', due_date: '', priority: 'medium' });

  const resetForm = () => {
    setForm({ title: '', description: '', due_date: '', priority: 'medium' });
    setEditingId(null);
    setShowForm(false);
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!form.title.trim() || !form.due_date) return;
    if (editingId) {
      setAssignments(prev => prev.map(a => a.id === editingId ? { ...a, ...form } : a));
    } else {
      setAssignments(prev => [...prev, { id: Date.now(), ...form, status: 'pending' }]);
    }
    resetForm();
  };

  const handleEdit = (a) => {
    setForm({ title: a.title, description: a.description || '', due_date: a.due_date, priority: a.priority });
    setEditingId(a.id);
    setShowForm(true);
  };

  const handleDelete = (id) => {
    setAssignments(prev => prev.filter(a => a.id !== id));
    if (editingId === id) resetForm();
  };

  const priorityClass = (p) => p === 'high' ? 'priority-high' : p === 'medium' ? 'priority-medium' : 'priority-low';

  return (
    <div className="assignments-page">
      <div className="page-header">
        <h1>Assignments</h1>
        <button className="btn btn-primary" onClick={() => { resetForm(); setShowForm(!showForm); }}>
          {showForm ? 'Cancel' : '+ Add Assignment'}
        </button>
      </div>

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
            <label>Priority</label>
            <select value={form.priority} onChange={e => setForm(f => ({ ...f, priority: e.target.value }))}>
              <option value="low">Low</option>
              <option value="medium">Medium</option>
              <option value="high">High</option>
            </select>
            <div className="form-actions">
              <button type="submit" className="btn btn-primary">{editingId ? 'Update' : 'Save'}</button>
              {editingId && <button type="button" className="btn btn-secondary" onClick={resetForm}>Cancel</button>}
            </div>
          </form>
        </div>
      )}

      <div className="card assignments-list">
        <h2>Your Assignments</h2>
        {assignments.length === 0 ? (
          <p className="muted">No assignments yet. Add one above.</p>
        ) : (
          <div className="assignment-cards">
            {assignments.map(a => (
              <div key={a.id} className={`assignment-card ${priorityClass(a.priority)}`}>
                <div className="card-header">
                  <h3>{a.title}</h3>
                  <span className={`badge status-${a.status}`}>{a.status.replace('_', ' ')}</span>
                </div>
                {a.description && <p className="card-desc">{a.description}</p>}
                <div className="card-meta">
                  <span>Due: {a.due_date}</span>
                  <span className={`badge priority-${a.priority}`}>{a.priority}</span>
                </div>
                <div className="card-actions">
                  <button type="button" className="btn btn-small" onClick={() => handleEdit(a)}>Edit</button>
                  <button type="button" className="btn btn-small btn-danger" onClick={() => handleDelete(a.id)}>Delete</button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
