import { useState } from 'react';
import './Routine.css';

const DAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

const initialSlots = {
  1: [
    { id: 1, start_time: '09:00', end_time: '11:00', title: 'Lectures', description: 'Morning classes' },
    { id: 2, start_time: '14:00', end_time: '16:00', title: 'Study Block', description: '' },
  ],
  3: [{ id: 3, start_time: '10:00', end_time: '12:00', title: 'Lab', description: 'Practical session' }],
};

export default function Routine() {
  const [selectedDay, setSelectedDay] = useState(1);
  const [slots, setSlots] = useState(initialSlots);
  const [showForm, setShowForm] = useState(false);
  const [editingSlot, setEditingSlot] = useState(null);
  const [form, setForm] = useState({ start_time: '09:00', end_time: '10:00', title: '', description: '' });

  const daySlots = slots[selectedDay] || [];

  const resetForm = () => {
    setForm({ start_time: '09:00', end_time: '10:00', title: '', description: '' });
    setEditingSlot(null);
    setShowForm(false);
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!form.title.trim()) return;
    const slot = { ...form, id: editingSlot?.id ?? Date.now() };
    if (editingSlot) {
      setSlots(prev => ({
        ...prev,
        [selectedDay]: (prev[selectedDay] || []).map(s => s.id === editingSlot.id ? slot : s),
      }));
    } else {
      setSlots(prev => ({
        ...prev,
        [selectedDay]: [...(prev[selectedDay] || []), slot],
      }));
    }
    resetForm();
  };

  const handleEdit = (slot) => {
    setForm({
      start_time: slot.start_time,
      end_time: slot.end_time,
      title: slot.title,
      description: slot.description || '',
    });
    setEditingSlot(slot);
    setShowForm(true);
  };

  const handleDelete = (id) => {
    setSlots(prev => ({
      ...prev,
      [selectedDay]: (prev[selectedDay] || []).filter(s => s.id !== id),
    }));
    if (editingSlot?.id === id) resetForm();
  };

  return (
    <div className="routine-page">
      <h1>Daily Routine</h1>
      <p className="muted">Manage your weekly routine. Add time slots per day.</p>

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

          {daySlots.length === 0 && !showForm ? (
            <p className="muted">No slots for this day. Click &quot;Add Slot&quot; to add one.</p>
          ) : (
            <ul className="slot-list">
              {daySlots
                .sort((a, b) => a.start_time.localeCompare(b.start_time))
                .map(slot => (
                  <li key={slot.id} className="slot-item">
                    <div className="slot-time">{slot.start_time} – {slot.end_time}</div>
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
