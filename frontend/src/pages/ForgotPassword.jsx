import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { authApi } from '../api/client';
import './Auth.css';

export default function ForgotPassword() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (user) navigate('/dashboard');
  }, [user, navigate]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await authApi.forgotPassword(email.trim());
      setDone(true);
    } catch (err) {
      setError(err.message || 'Request failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Forgot password</h1>
        <p className="auth-subtitle">We&apos;ll email you a 6-digit code if an account exists.</p>
        {error && <p className="auth-error">{error}</p>}
        {done ? (
          <>
            <p className="auth-success">Check your email for the reset code, then continue to reset your password.</p>
            <p className="auth-footer">
              <Link to="/reset-password" state={{ email }}>Enter reset code</Link>
              {' · '}
              <Link to="/login">Sign in</Link>
            </p>
          </>
        ) : (
          <form className="auth-form" onSubmit={handleSubmit}>
            <label>Email</label>
            <input type="email" value={email} onChange={e => setEmail(e.target.value)} required />
            <button type="submit" className="btn btn-primary" disabled={loading}>{loading ? 'Sending...' : 'Send code'}</button>
          </form>
        )}
        {!done && (
          <p className="auth-footer">
            <Link to="/login">Back to sign in</Link>
          </p>
        )}
      </div>
    </div>
  );
}
