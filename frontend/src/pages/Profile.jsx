import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { authApi } from '../api/client';
import { useToast } from '../context/ToastContext';
import './Profile.css';

export default function Profile() {
  const { user, updateUser } = useAuth();
  const toast = useToast();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    authApi.getProfile()
      .then(p => {
        setFullName(p.fullName ?? '');
        setEmail(p.email ?? '');
      })
      .catch(() => setError('Failed to load profile.'))
      .finally(() => setLoading(false));
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSaving(true);
    try {
      const body = { fullName: fullName.trim(), email: email.trim().toLowerCase() };
      if (newPassword.trim()) body.newPassword = newPassword;
      const updated = await authApi.updateProfile(body);
      updateUser({ fullName: updated.fullName, email: updated.email });
      setNewPassword('');
      toast.success('Profile updated.');
    } catch (err) {
      setError(err.message || 'Update failed.');
      toast.error(err.message || 'Update failed.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="profile-page"><p className="muted">Loading profile...</p></div>;
  }

  return (
    <div className="profile-page">
      <h1>Edit profile</h1>
      <p className="page-subtitle">Update your name, email, or password.</p>

      {error && <p className="error-msg">{error}</p>}

      <section className="card profile-card">
        <form className="profile-form" onSubmit={handleSubmit}>
          <label>Full name</label>
          <input
            type="text"
            placeholder="Your name"
            value={fullName}
            onChange={e => setFullName(e.target.value)}
            required
          />
          <label>Email</label>
          <input
            type="email"
            placeholder="your@email.com"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
          />
          <label>New password (leave blank to keep current)</label>
          <input
            type="password"
            placeholder="••••••••"
            value={newPassword}
            onChange={e => setNewPassword(e.target.value)}
            autoComplete="new-password"
          />
          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving...' : 'Save changes'}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
