import { useState, useEffect } from 'react';
import './Routine.css';
import { routineApi } from '../api/client';
import { useToast } from '../context/ToastContext';
import ConfirmDialog from '../components/ConfirmDialog';

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

function timeToMinutes(t) {
  if (!t || typeof t !== 'string') return 0;
  const [h, m] = t.trim().split(':').map(Number);
  return (Number.isNaN(h) ? 0 : h) * 60 + (Number.isNaN(m) ? 0 : m);
}

function slotsOverlap(slots) {
  const sorted = [...slots].sort((a, b) => timeToMinutes(a.start_time) - timeToMinutes(b.start_time));
  for (let i = 1; i < sorted.length; i++) {
    const prevEnd = timeToMinutes(sorted[i - 1].end_time);
    const currStart = timeToMinutes(sorted[i].start_time);
    if (currStart < prevEnd) return true;
  }
  return false;
}

export default function Routine() {
  const [selectedDay, setSelectedDay] = useState(1);
  const [slots, setSlots] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [editingSlot, setEditingSlot] = useState(null);
  const [form, setForm] = useState({ start_time: '09:00', end_time: '10:00', title: '', description: '' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [viewMode, setViewMode] = useState('day');
  const [copyFromDay, setCopyFromDay] = useState(null);
  const toast = useToast();

  const load = async () => {
    setError('');
    try {
      const list = await routineApi.getAll();
      setSlots(Array.isArray(list) ? list : []);
    } catch (err) {
      const msg = err.message || 'Failed to load routine.';
      setError(msg);
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const daySlots = slots.filter(s => s.day_of_week === selectedDay).sort((a, b) => (a.start_time || '').localeCompare(b.start_time || ''));
  const hasOverlap = slotsOverlap(daySlots);
  const slotsByDay = DAYS.map((_, i) => slots.filter(s => s.day_of_week === i).sort((a, b) => (a.start_time || '').localeCompare(b.start_time || '')));

  const resetForm = () => {
    setForm({ start_time: '09:00', end_time: '10:00', title: '', description: '' });
    setEditingSlot(null);
    setShowForm(false);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.title.trim()) return;
    const newSlot = { start_time: form.start_time, end_time: form.end_time };
    const wouldOverlap = !editingSlot && slotsOverlap([...daySlots, newSlot]);
    if (wouldOverlap) toast.error('This time overlaps with an existing slot on this day.');
    setError('');
    try {
      const payload = { dayOfWeek: selectedDay, startTime: form.start_time, endTime: form.end_time, title: form.title, description: form.description || '' };
      if (editingSlot) {
        await routineApi.update(editingSlot.id, payload);
        toast.success('Slot updated.');
      } else {
        await routineApi.create(payload);
        toast.success('Slot added.');
      }
      resetForm();
      await load();
    } catch (err) {
      const msg = err.message || 'Save failed.';
      setError(msg);
      toast.error(msg);
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
      toast.success('Slot deleted.');
    } catch (err) {
      const msg = err.message || 'Delete failed.';
      setError(msg);
      toast.error(msg);
    }
  };

  const handleCopyFromDay = async () => {
    if (copyFromDay === null || copyFromDay === selectedDay) return;
    const sourceSlots = slots.filter(s => s.day_of_week === copyFromDay).sort((a, b) => (a.start_time || '').localeCompare(b.start_time || ''));
    if (sourceSlots.length === 0) {
      toast.info('No slots to copy from that day.');
      return;
    }
    setCopyFromDay(null);
    try {
      for (const s of sourceSlots) {
        await routineApi.create({ dayOfWeek: selectedDay, startTime: s.start_time, endTime: s.end_time, title: s.title, description: s.description || '' });
      }
      await load();
      toast.success(`Copied ${sourceSlots.length} slot(s) to ${DAYS[selectedDay]}.`);
    } catch (err) {
      toast.error(err.message || 'Copy failed.');
    }
  };

  return (
    <div className="routine-page">
      <h1>Daily Routine</h1>
      <p className="muted">Manage your weekly routine. Add time slots per day.</p>
      {error && <p className="error-msg">{error}</p>}

      <div className="routine-view-toggle">
        <button type="button" className={`btn btn-small ${viewMode === 'day' ? 'btn-primary' : 'btn-secondary'}`} onClick={() => setViewMode('day')}>By day</button>
        <button type="button" className={`btn btn-small ${viewMode === 'week' ? 'btn-primary' : 'btn-secondary'}`} onClick={() => setViewMode('week')}>Week view</button>
      </div>

      {viewMode === 'day' && (
        <>
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
                <div className="routine-header-actions">
                  <select value={copyFromDay ?? ''} onChange={(e) => setCopyFromDay(e.target.value === '' ? null : Number(e.target.value))} className="copy-select">
                    <option value="">Copy from...</option>
                    {DAYS.map((d, i) => (i !== selectedDay ? <option key={i} value={i}>{d}</option> : null))}
                  </select>
                  {copyFromDay !== null && copyFromDay !== selectedDay && (
                    <button type="button" className="btn btn-small btn-primary" onClick={handleCopyFromDay}>Copy slots</button>
                  )}
                  <button type="button" className="btn btn-primary" onClick={() => { resetForm(); setShowForm(!showForm); }}>
                    {showForm ? 'Cancel' : '+ Add Slot'}
                  </button>
                </div>
              </div>

              {hasOverlap && (
                <div className="overlap-warning">Some slots on this day overlap. Consider adjusting times.</div>
              )}

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
        </>
      )}

      {viewMode === 'week' && (
        <div className="week-view">
          {loading ? (
            <p className="muted">Loading...</p>
          ) : (
            <div className="week-grid">
              {DAYS.map((day, i) => (
                <div key={day} className="week-day-card">
                  <h3 className="week-day-title">{day}</h3>
                  <ul className="slot-list">
                    {slotsByDay[i].length === 0 ? (
                      <li className="muted">No slots</li>
                    ) : (
                      slotsByDay[i].map(slot => (
                        <li key={slot.id} className="slot-item week-slot">
                          <span className="slot-time">{formatTimeAmPm(slot.start_time)} – {formatTimeAmPm(slot.end_time)}</span>
                          <span className="slot-title">{slot.title}</span>
                        </li>
                      ))
                    )}
                  </ul>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
