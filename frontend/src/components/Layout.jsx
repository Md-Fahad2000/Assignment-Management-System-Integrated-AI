import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './Layout.css';

export default function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const handleLogout = () => { logout(); navigate('/login'); };

  return (
    <div className="layout">
      <aside className="sidebar">
        <h1 className="sidebar-title">Assignment Manager</h1>
        {user && <p className="sidebar-user">{user.fullName}</p>}
        <nav className="nav">
          <NavLink to="/dashboard" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Dashboard
          </NavLink>
          <NavLink to="/assignments" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Assignments
          </NavLink>
          <NavLink to="/routine" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Routine
          </NavLink>
          <NavLink to="/mental-health" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Mental health
          </NavLink>
          <NavLink to="/profile" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Profile
          </NavLink>
          <NavLink to="/help" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Help
          </NavLink>
        </nav>
        <button type="button" className="nav-link logout-btn" onClick={handleLogout}>Logout</button>
      </aside>
      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}
