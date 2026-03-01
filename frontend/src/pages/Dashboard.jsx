import './Dashboard.css';

export default function Dashboard() {
  return (
    <div className="dashboard">
      <h1>Dashboard</h1>
      <p className="dashboard-subtitle">Overview of your assignments and AI schedule</p>

      <div className="dashboard-grid">
        <section className="card">
          <h2>Upcoming Deadlines</h2>
          <p className="muted">Connect backend to see assignments.</p>
        </section>
        <section className="card">
          <h2>AI Schedule</h2>
          <p className="muted">AI-generated study schedule will appear here.</p>
        </section>
        <section className="card">
          <h2>Assignment Roadmap</h2>
          <p className="muted">AI roadmap for current assignment.</p>
        </section>
        <section className="card">
          <h2>Productivity</h2>
          <p className="muted">Track completed vs pending tasks.</p>
        </section>
      </div>
    </div>
  );
}
