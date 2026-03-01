import { Link, useNavigate } from 'react-router-dom';
import './Auth.css';

export default function Register() {
  const navigate = useNavigate();
  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Create Account</h1>
        <p className="auth-subtitle">Assignment Management System</p>
        <form className="auth-form" onSubmit={(e) => { e.preventDefault(); navigate('/dashboard'); }}>
          <label>Full Name</label>
          <input type="text" placeholder="Your name" required />
          <label>Email</label>
          <input type="email" placeholder="your@email.com" required />
          <label>Password</label>
          <input type="password" placeholder="••••••••" required />
          <button type="submit" className="btn btn-primary">Register</button>
        </form>
        <p className="auth-footer">
          Already have an account? <Link to="/login">Sign In</Link>
        </p>
      </div>
    </div>
  );
}
