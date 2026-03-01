import { Link, useNavigate } from 'react-router-dom';
import './Auth.css';

export default function Login() {
  const navigate = useNavigate();
  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Sign In</h1>
        <p className="auth-subtitle">Assignment Management System</p>
        <form className="auth-form" onSubmit={(e) => { e.preventDefault(); navigate('/dashboard'); }}>
          <label>Email</label>
          <input type="email" placeholder="your@email.com" required />
          <label>Password</label>
          <input type="password" placeholder="••••••••" required />
          <button type="submit" className="btn btn-primary">Sign In</button>
        </form>
        <p className="auth-footer">
          Don't have an account? <Link to="/register">Register</Link>
        </p>
      </div>
    </div>
  );
}
