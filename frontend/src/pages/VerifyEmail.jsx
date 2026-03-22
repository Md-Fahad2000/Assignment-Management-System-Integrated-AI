import { useState, useEffect } from 'react';
import { Link, useNavigate, useLocation, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { authApi } from '../api/client';
import './Auth.css';

export default function VerifyEmail() {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const { user, login } = useAuth();
  const [email, setEmail] = useState('');
  const [code, setCode] = useState('');
  const [devHint, setDevHint] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [resendLoading, setResendLoading] = useState(false);

  useEffect(() => {
    if (user) navigate('/dashboard');
  }, [user, navigate]);

  useEffect(() => {
    const st = location.state;
    const q = searchParams.get('email');
    if (st?.email) setEmail(st.email);
    else if (q) setEmail(q);
    if (st?.devVerificationCode) setDevHint(st.devVerificationCode);
  }, [location.state, searchParams]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const data = await authApi.verifyEmail({ email: email.trim(), code: code.trim() });
      login(data);
      navigate('/dashboard');
    } catch (err) {
      setError(err.message || 'Verification failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleResend = async () => {
    if (!email.trim()) return;
    setResendLoading(true);
    setError('');
    try {
      await authApi.resendVerification(email.trim());
    } catch (err) {
      setError(err.message || 'Could not resend.');
    } finally {
      setResendLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Verify your email</h1>
        <p className="auth-subtitle">Enter the 6-digit code we sent to your inbox.</p>
        {devHint && (
          <p className="auth-dev-hint">
            <strong>Development:</strong> SMTP not configured — use code: <code>{devHint}</code>
          </p>
        )}
        {error && <p className="auth-error">{error}</p>}
        <form className="auth-form" onSubmit={handleSubmit}>
          <label>Email</label>
          <input type="email" value={email} onChange={e => setEmail(e.target.value)} required />
          <label>Verification code</label>
          <input type="text" inputMode="numeric" pattern="[0-9]*" maxLength={6} placeholder="000000" value={code} onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))} required />
          <button type="submit" className="btn btn-primary" disabled={loading}>{loading ? 'Verifying...' : 'Verify & continue'}</button>
        </form>
        <p className="auth-footer">
          <button type="button" className="link-button" onClick={handleResend} disabled={resendLoading || !email.trim()}>
            {resendLoading ? 'Sending...' : 'Resend code'}
          </button>
          {' · '}
          <Link to="/login">Back to sign in</Link>
        </p>
      </div>
    </div>
  );
}
