import { Outlet, NavLink } from 'react-router-dom';
import './Layout.css';

export default function Layout() {
  return (
    <div className="layout">
      <aside className="sidebar">
        <h1 className="sidebar-title">Assignment Manager</h1>
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
        </nav>
      </aside>
      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}
