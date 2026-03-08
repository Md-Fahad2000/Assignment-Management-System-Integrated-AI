import { useState, useEffect } from 'react';
import './Routine.css';
import { routineApi } from '../api/client';

const DAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

function formatTimeAmPm(timeStr) {
  if (!timeStr || typeof timeStr !== 'string') return '';
  const [h, m] = timeStr.trim().split(':').map(Number);
  if (Number.isNaN(h)) return timeStr;
  const hour = h % 12 || 12;
  const min = Number.isNaN(m) ? 0 : m;
  const ampm = h < 12 ? 'AM' : 'PM';
  return `${hour}:${min.toString().padStart(2, '0')} ${ampm}`;
}

export default function Routine() {
  const [selectedDay, setSelectedDay] = useState(1);
  const [slots, setSlots] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [editingSlot, setEditingSlot] = useState(null);
  const [form, setForm] = useState({ start_time: '09:00', end_time: '10:00', title: '', description: '' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = async () => {
    setError('');
    try {
      const list = await routineApi.getAll();
      setSlots(Array.isArray(list) ? list : []);
    } catch (err) {
      setError(err.message || 'Failed to load routine.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const daySlots = slots.filter(s => s.day_of_week === selectedDay).sort((a, b) => (a.start_time || '').localeCompare(b.start_time || ''));

  const resetForm = () => {
    setForm({ start_time: '09:00', end_time: '10:00', title: '', description: '' });
    setEditingSlot(null);
    setShowForm(false);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.title.trim()) return;
    setError('');
    try {
      const payload = { dayOfWeek: selectedDay, startTime: form.start_time, endTime: form.end_time, title: form.title, description: form.description || '' };
      if (editingSlot) {
        await routineApi.update(editingSlot.id, payload);
      } else {
        await routineApi.create(payload);
      }
      resetForm();
      await load();
    } catch (err) {
      setError(err.message || 'Save failed.');
    }
  };

  const handleEdit = (slot) => {
    setForm({
      start_time: slot.start_time || '09:00',
      end_time: slot.end_time || '10:00',
      title: slot.title,
      description: slot.description || '',
    });
    setEditingSlot(slot);
    setShowForm(true);
  };

  const handleDelete = async (id) => {
    setError('');
    try {
      await routineApi.delete(id);
      if (editingSlot?.id === id) resetForm();
      await load();
    } catch (err) {
      setError(err.message || 'Delete failed.');
    }
  };

  return (
    <div className="routine-page">
      <h1>Daily Routine</h1>
      <p className="muted">Manage your weekly routine. Add time slots per day.</p>
      {error && <p className="error-msg">{error}</p>}

      <div className="day-tabs">
        {DAYS.map((day, i) => (
          <button
            key={day}
            className={`day-tab ${selectedDay === i ? 'active' : ''}`}
            onClick={() => { setSelectedDay(i); resetForm(); }}
          >
            {day.slice(0, 3)}
          </button>
        ))}
      </div>

      <div className="routine-content">
        <div className="card routine-slots">
          <div className="routine-slots-header">
            <h2>{DAYS[selectedDay]}</h2>
            <button type="button" className="btn btn-primary" onClick={() => { resetForm(); setShowForm(!showForm); }}>
              {showForm ? 'Cancel' : '+ Add Slot'}
            </button>
          </div>

          {showForm && (
            <form className="slot-form" onSubmit={handleSubmit}>
              <label>Title</label>
              <input type="text" placeholder="e.g. Lecture, Study" value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} required />
              <label>Start – End time</label>
              <div className="time-row">
                <input type="time" value={form.start_time} onChange={e => setForm(f => ({ ...f, start_time: e.target.value }))} />
                <span>to</span>
                <input type="time" value={form.end_time} onChange={e => setForm(f => ({ ...f, end_time: e.target.value }))} />
              </div>
              <label>Description (optional)</label>
              <input type="text" placeholder="Short note" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
              <div className="form-actions">
                <button type="submit" className="btn btn-primary">{editingSlot ? 'Update' : 'Add Slot'}</button>
                {editingSlot && <button type="button" className="btn btn-secondary" onClick={resetForm}>Cancel</button>}
              </div>
            </form>
          )}

          {loading ? (
            <p className="muted">Loading...</p>
          ) : daySlots.length === 0 && !showForm ? (
            <p className="muted">No slots for this day. Click &quot;Add Slot&quot; to add one.</p>
          ) : (
            <ul className="slot-list">
              {daySlots.map(slot => (
                <li key={slot.id} className="slot-item">
                  <div className="slot-time">{formatTimeAmPm(slot.start_time)} – {formatTimeAmPm(slot.end_time)}</div>
                  <div className="slot-title">{slot.title}</div>
                  {slot.description && <div className="slot-desc">{slot.description}</div>}
                  <div className="slot-actions">
                    <button type="button" className="btn btn-small" onClick={() => handleEdit(slot)}>Edit</button>
                    <button type="button" className="btn btn-small btn-danger" onClick={() => handleDelete(slot.id)}>Delete</button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
