# Assignment Management System with Integrated AI

**Student:** Muhammad Fahad 
**Supervisor:** Marie Keary  

A web-based system to help students manage daily routines, assignments, deadlines, and priorities, with **AI-powered scheduling** and assignment roadmaps.

## Tech Stack

- **Frontend:** React.js
- **Backend:** C# ASP.NET Core Web API
- **Database:** MySQL
- **Version control:** GitHub

## Features

- Student authentication (register/login)
- Daily routine management
- Assignment and deadline management
- AI smart scheduling
- AI assignment roadmap
- Deadline reminders
- Productivity tracking

## Project Structure

```
├── frontend/          # React app
├── backend/           # ASP.NET Web API
├── database/          # MySQL schema and scripts

└── README.md
```

## Week 1 & 2 Deliverables ✓

- **Week 1:** React UI (Login, Register, Dashboard, Assignments, Routine), MySQL schema.
- **Week 2:** Backend auth (BCrypt + JWT), Assignment & Routine CRUD with MySQL, **AI** scheduling (Earliest-Deadline-First), priority logic, assignment roadmap. Frontend connected to API; protected routes.
- **AI roadmap:** Each step has a **date and time** (e.g. “Step 1: Read chapter — 2025-03-02 at 09:00”). Optional: set `OpenAI:ApiKey` in `appsettings.json` to use GPT for “how to do this assignment” steps; otherwise the app splits your uploaded document and assigns date+time from your routine. Run `database/03_add_roadmap_suggested_time.sql` for time support.

## Getting Started

### Prerequisites

- Node.js (for React)
- .NET SDK 10 (for backend)
- MySQL (for database)

### Run frontend

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:3000 — use Dashboard, Assignments, and Routine .

### Run backend

```bash
cd backend/AssignmentManagement.Api
dotnet run
```

API runs at http://localhost:5000; Swagger at http://localhost:5000/swagger.



## License

Academic project — FYP.
